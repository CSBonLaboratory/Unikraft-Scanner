import logging
import pymongo
import os
import csv
import shutil
from typing import Union
from symbol_engine import find_compilation_blocks_and_lines, find_children
from helpers.helpers import (
    get_source_git_commit_hashes,
    trigger_compilation_blocks,
    try_compilers_for_src_path,
    get_source_compilation_command,
    instrument_source,
    prepare_coverity_defects_for_db
)

from helpers.InterceptorTimeout import InterceptorTimeout
from helpers.CompilationBlock import CompilationBlock
from helpers.CSourceDocument import CSourceDocument
from helpers.helpers import SourceVersionInfo
from CoverityAPI import CoverityAPI

from bson.objectid import ObjectId
from pymongo.results import DeleteResult
from pymongo.cursor import Cursor
from defects.AbstractLineDefect import AbstractLineDefect
from defects.CoverityDefect import CoverityDefect
from selenium.common.exceptions import TimeoutException

import coverage
logger = logging.getLogger(coverage.LOGGER_NAME)
DATABASE = coverage.DATABASE
SOURCES_COLLECTION = coverage.SOURCES_COLLECTION
COMPILATION_COLLECTION = coverage.COMPILATION_COLLECTION
UNKNOWN_DEFECTS_COLLECTION = coverage.UNKNOWN_DEFECTS_COLLECTION

lines_of_code = 0
blocks_of_code = 0

def panic_delete_compilation(db : pymongo.MongoClient, compilation_tag : str):
    
    res : DeleteResult = None

    res = db[DATABASE][COMPILATION_COLLECTION].delete_one(
        {"tag" : compilation_tag} 
    )

    logger.warning(f"Delete compilation from COMPILATION collection: {res.raw_result}")

    db[DATABASE][UNKNOWN_DEFECTS_COLLECTION].delete_many(
        {"compilation_tag" : compilation_tag}
    )

    logger.warning(f"Delete all unknown results from UNKNOWN DELETES collection: {res.raw_result}")

    
    res = db[DATABASE][SOURCES_COLLECTION].delete_many(
        {"compilation_tag" : compilation_tag}
    )

    logger.warning(f"Delete all analyzed sources in this compilation from SOURCES collection: {res.raw_result}")

    return


def is_new_source(db : pymongo.MongoClient, src_path : str, app_path : str, compilation_tag : str) -> SourceVersionInfo:

    '''
    Checks to see the status of the source file.

    It can detect the hashing strategy used for versioning (git commit id or sha-1) and queries the db.

    Args:
        src_path(str): Absolute path of the source file, fetched from its *.o.cmd file.
        app_path(str): Absolute path of the app directory.
    
    Returns:
        A SourceStatus instance which means that the source file is new, existing but modified or existing and unmodified.
    '''

    latest_commits : list[str] | None = get_source_git_commit_hashes(src_path)
    
    if latest_commits == None:
        logger.critical(f"Failed to use git in finding source file commit hash and repo commit hash. Problem is not that the file is not part of any git repo, but git command crashed")
        panic_delete_compilation(db, compilation_tag)
        exit(12)

    source_version = SourceVersionInfo()
    source_version.git_file_commit_hash = latest_commits[0]
    source_version.git_repo_commit_hash = latest_commits[1]
    source_snapshot : Cursor = db[DATABASE][SOURCES_COLLECTION].find_one(
        {
            "$and": [
                {"source_path" : os.path.relpath(src_path, app_path)},
                {"git_file_commit_hash" : latest_commits[0]}
            ]
        }
    )

    if source_snapshot == None:
        logger.debug(f"New source {os.path.relpath(src_path, app_path)} with commit hash {latest_commits[0]} in repo with hash {latest_commits[1]}")
        source_version.status = SourceVersionInfo.SourceStatus.NEW
        return source_version
    
    logger.debug(f"Source {os.path.relpath(src_path, app_path)} with commit hash {latest_commits[0]} in repo with hash {latest_commits[1]} already existing.")
    source_version.status = SourceVersionInfo.SourceStatus.EXISTING
    return source_version
    
    
        

def update_db_activated_compile_blocks(db : pymongo.MongoClient, activated_block_counters : list[int], rel_src_path : str, compilation_tag: str, source_version : SourceVersionInfo) -> Union[CSourceDocument, dict]:
    '''
    Updates the document of the source file from the db with the activated compilation blocks (triggered by the current compilation).

    Args:
        activated_block_counter(list[int]): List of indexes of triggered/activated compilation block (info given by a prior call to trigger_compialtion_blocks() )

        rel_src_path: Relative path of the source file in the context of the app directory

    Returns:
        The updated document as a dict.
    '''
    
    source_document : Union[CSourceDocument, dict] = db[DATABASE][SOURCES_COLLECTION].find_one(
        {
            "$and" : [
                {"source_path": rel_src_path},
                {"git_file_commit_hash" : source_version.git_file_commit_hash}
            ]
        }
        
    )

    logger.debug(f"BEFORE update of activated compile blocks\n{source_document}")

    logger.debug(f"ACTIVATED {activated_block_counters}")

    compile_blocks : Union[list[CompilationBlock], list[dict]] = source_document['compile_blocks']
        
    # maybe we are lucky and made some progress by activating new blocks :)
    # also calculate the number of compiled lines for this particular compilation for this source file
    compiled_lines = source_document["universal_lines"]
    for existing_block in compile_blocks:

        if existing_block['_local_id'] in activated_block_counters:
            existing_block['triggered_compilations'].append(compilation_tag)
            compiled_lines += existing_block["lines"]
        
    source_document["compiled_stats"][compilation_tag] = compiled_lines

    updated_source_document = db[DATABASE][SOURCES_COLLECTION].find_one_and_update(
        filter= {"source_path" : rel_src_path},
        update= {"$set" : source_document},
        return_document=pymongo.ReturnDocument.AFTER
    )

    logger.debug(f"AFTER update of compile regions\n{updated_source_document}")

    return updated_source_document

def init_source_in_db(db : pymongo.MongoClient, source_version : SourceVersionInfo, src_path : str, rel_src_path : str, total_blocks : list[CompilationBlock], universal_lines : int, compilation_tag : str, lib_name : str):
    '''
    Creates a new document in db for a new source file, or updates an existing source file that has been modified.

    Adds various other metadata such as total lines of code and inits other fields that will be populated laters.

    Args:
        source_status(SourceStatus): Status of the source file.
        src_path(str): Absolute path of the source file.
        rel_src_path(str): Relative path of the source file in the context of the app directory
        total_blocks(list[CompilationBlocks]): List of CompilationBlocks instances found in the source file
        universal_lines(int): Number of lines that are compiled no matter what symbols are used

    Throws:
        `Exception` object if status of the source is neither SourceStatus.NEW nor SourceStatus.DEPRECATED

    '''

    # build hierarchy of compilation blocks (usefull for vizualization if we have nested blocks)
    find_children(total_blocks)

    # calculate total lines of code
    total_lines = universal_lines
    for cb in total_blocks:
        total_lines += cb.lines
    
    new_src_document : Union[CSourceDocument, dict] = {
                "source_path" :  rel_src_path,
                "compile_blocks" : [compile_block.to_mongo_dict() for compile_block in total_blocks],
                "universal_lines" : universal_lines,
                "triggered_compilations" : [compilation_tag],
                "compiled_stats" : {},
                "total_lines" : total_lines,
                "lib" : lib_name,
                "defects" : [],
                "git_file_commit_hash" : source_version.git_file_commit_hash,
                "git_repo_commit_hash" : source_version.git_repo_commit_hash
    }

    # the source is new so we create a new entry in the database
    db[DATABASE][SOURCES_COLLECTION].insert_one(new_src_document)
        
    logger.debug(f"Initialized source in db\n{new_src_document}")

    return

    

def fetch_existing_compilation_blocks(db : pymongo.MongoClient, rel_src_path : str, source_version : SourceVersionInfo) -> list[CompilationBlock]:
    '''
    Fetch compilation blocks from the db.

    To be used on an existing source in the db.

    This is an optimization, so that it will not reparse the source file for the compilation blocks.

    Args:
        rel_src_path(str): RELATIVE path of the source file in the context of the app directory

    Returns:
        A list of CompilationBlock instances foudn in the source file
    '''

    existing_blocks : Union[list[CompilationBlock], dict] = db[DATABASE][SOURCES_COLLECTION].find_one(
            filter= {"$and" : [{"source_path" : rel_src_path}, {"git_file_commit_hash" : source_version.git_file_commit_hash}]},
            projection= {"compile_blocks" : 1, "_id" : 0}
        )

    total_blocks = [CompilationBlock(raw_block) for raw_block in existing_blocks['compile_blocks']]
    
    logger.debug(f"Fetched compilation blocks from database:\n{total_blocks}")

    return total_blocks


def get_source_compile_coverage(db : pymongo.MongoClient, compilation_tag : str, lib_name : str, app_dir : str, app_build_dir : str, src_path : str, configs : dict) -> Union[CSourceDocument, dict]:
    global lines_of_code, blocks_of_code

    # we need relative path since this tool might be run in various environments, or on various apps on the same environment
    rel_src_path = os.path.relpath(src_path, app_dir)

    # very important !! the source file is copied before code instrumentation, later it will be back to its initial form
    copy_source_path = f"{app_dir}/.scanner/copies/{rel_src_path}"
    os.makedirs(os.path.dirname(copy_source_path), exist_ok=True)
    shutil.copy(src_path, copy_source_path)

    source_version : SourceVersionInfo = is_new_source(db, src_path, app_dir, compilation_tag)

    # the source version is already registered, so we can fetch all compilation blocks from the database
    # also bind the compilation id to this file since the universal lines of code (ouside any conditional compile block like #ifdef) are compiled anyways
    if source_version.status == SourceVersionInfo.SourceStatus.EXISTING:
        total_blocks : list[CompilationBlock] = fetch_existing_compilation_blocks(db, os.path.relpath(src_path, app_dir), source_version)

        db[DATABASE][SOURCES_COLLECTION].find_one_and_update(
            filter={"$and" : [{"source_path" : rel_src_path}, {"git_file_commit_hash" : source_version.git_file_commit_hash}]},
            update={"$push": {"triggered_compilations" : compilation_tag}}
        )

    # the source is not registered so we must parse the source file and find compilation blocks
    # or the source needs to be cleared due to deprecation so we must parse the updated source file and find compilation blocks
    elif source_version.status == SourceVersionInfo.SourceStatus.NEW:

        total_blocks, universal_lines, lines = find_compilation_blocks_and_lines(src_path, True)
        lines_of_code += lines
        blocks_of_code += len(total_blocks)
        init_source_in_db(db, source_version, src_path, rel_src_path, total_blocks, universal_lines, compilation_tag, lib_name)

    compile_command = get_source_compilation_command(app_build_dir, lib_name, src_path)

    if compile_command == None:
        logger.critical(f"No .o.cmd file which has compilation command for {src_path} in {lib_name}")
        db[DATABASE][SOURCES_COLLECTION].find_one_and_delete({"$and" : [{"source_path" : rel_src_path}, {"git_file_commit_hash" : source_version.git_file_commit_hash}]})
        return None

    instrument_source(total_blocks, src_path)

    preproc_file_path = os.path.join(f"{app_dir}/.scanner/copies/", configs['preprocFile'])

    try:
        activated_block_counters = trigger_compilation_blocks(compile_command, preproc_file_path, src_path)

        updated_src_document = update_db_activated_compile_blocks(
                db=db,
                activated_block_counters= activated_block_counters,
                rel_src_path= rel_src_path,
                compilation_tag= compilation_tag,
                source_version=source_version
        )
    except Exception as e:
        raise e
    finally:
        # go back to original source code without instrumentation even if an exception occured
        shutil.copyfile(copy_source_path, src_path)
    

    return updated_src_document

def insert_defects_in_db(db : pymongo.MongoClient, cov_defects : list[AbstractLineDefect], compilation_tag : str) -> None:
        
        # sort defects in ascending order so that when viewing them we will also see them in ascending order
        # pushing them in db is like pushing them in a queue
        cov_defects.sort(key= lambda x : x.line_number)

        for defect in cov_defects:
            logger.debug(f"Looking for source {defect.source_path} compiled during {compilation_tag} that has defect line number {defect.line_number}")
            
            print("\n")
            print(defect.to_mongo_dict())
            print("||||||||||||||||||||||||||||||||||||")
            source_document_dict : dict = db[DATABASE][SOURCES_COLLECTION].find_one(
                filter={
                    "$and" : [
                        {"source_path" : defect.source_path}, 
                        {"triggered_compilations" : {"$in" : [compilation_tag]}}
                    ]
                }
            )
            
            # defect does not appear in a C source file but in a header file which is not considered suring the `app add` operation
            # insert defect in a collection for such defects
            # it's a band-aid fix for https://github.com/CSBonLaboratory/Unikraft-Scanner/issues/5
            if source_document_dict == None:
                logger.critical(f"No such source {defect.source_path} featured in defect {defect.to_mongo_dict()}. Do not try to add it in source database but see if it does not already exist in unknown defects collection")

                unknown_defect_res = db[DATABASE][UNKNOWN_DEFECTS_COLLECTION].find_one(
                    filter={"cid" : defect.id}
                )

                if unknown_defect_res == None:
                    logger.debug(f"Defect with id {defect.id} not found in unknown defects database. Add it now.")
                    db[DATABASE][UNKNOWN_DEFECTS_COLLECTION].insert_one(defect.to_mongo_dict())
                    return
                else:
                    logger.debug(f"Defect with id {defect.id} and compialtion tag {defect.compilation_tag} is the same as the one with compilation tag: {unknown_defect_res['compilation_tag']}. Do not add it even in the unknown defects collection")
                    return
            
            logger.debug(f"-------------------- Start inserting defect in {source_document_dict['source_path']} ---------------------")

            logger.debug(f"DEFECT {defect.to_mongo_dict()}")
            
            source_document = CSourceDocument(source_document_dict)

            # artificial CompilationBlock root (which includes the entire source file) that contains all real CompilationBlock roots
            root_block = CompilationBlock(
                {
                    "start_line" : 0,
                    "symbol_condition" : "",
                    "end_line" : source_document.total_lines - 1,
                    "_local_id" : -1,
                    "_parent_id" : -1,
                    "lines" : source_document.total_lines,
                    "children" : [compblock.block_counter for compblock in source_document.compile_blocks if compblock.parent_counter == -1]
                }
            )
            
            # traverse the compilation blocks tree until we get the most specific (small) block that contains the defect
            current_block = root_block
            tree_end = False

            if defect.line_number == -1:
                tree_end = True

            while tree_end != True:
                
                # if partial solution does not have any children, then it is a final solution
                if current_block.children == []:
                    tree_end = True
                
                # try every children to find a smaller compilation block that includes the defect
                for i, next_node_idx in enumerate(current_block.children):

                    next_block = source_document.compile_blocks[next_node_idx]

                    # compile blocks of same tree depth are disjunct, only 1 solution is possible
                    # defects with 'Various' instead of a line number will always fail (happens for Coverity defects)
                    if next_block.start_line <= defect.line_number and defect.line_number <= next_block.end_line:
                        current_block = next_block
                        break

                    # even the last child cannot contain the defect, we found the solution
                    elif i == len(current_block.children) - 1:
                        tree_end = True
            
            # the source does not have any compile blocks
            # defect appears in the universal lines (either there are no compile blocks or there is no compile block to include the defect or defect appears in 'Various lines')
            if current_block == root_block:
                logger.debug(f"Defect {defect.to_mongo_dict()} inserted in universal part of the document")
                db[DATABASE][SOURCES_COLLECTION].update_one(
                    filter={"$and" : [
                        {"source_path" : defect.source_path}, 
                        {"triggered_compilations" : {"$in" : [compilation_tag]}}
                    ]},
                    update={
                        "$push" : {"defects" : defect.to_mongo_dict()}
                    }
                )
            else:
                
                logger.debug(f"Defect {defect} inserted in compile block {current_block.to_mongo_dict()}")
                # insert the defect in the suitable compile block
                cb : dict = db[DATABASE][SOURCES_COLLECTION].find_one(
                    filter={"$and" : [
                        {"source_path" : defect.source_path}, 
                        {"triggered_compilations" : {"$in" : [compilation_tag]}}
                    ]},
                    projection={
                            "_id" : False,
                            "compile_blocks" : True
                        }
                )

                cb["compile_blocks"][current_block.block_counter]["defects"].append(defect.to_mongo_dict())

                db[DATABASE][SOURCES_COLLECTION].find_one_and_update(
                    filter={
                        "source_path" : defect.source_path
                    },
                    update={
                        "$set" : {"compile_blocks" : cb}
                    }
                )
        return
def coverity_intercept_compilation_step(db : pymongo.MongoClient, configs : dict, compile_cmd : str, coverity_bin_path : str, app_path : str, compilation_tag : str, coverity : CoverityAPI, do_panic_delete : bool):

    # start build with the chosen kraft command to be intercepted by cov-build
    if not coverity.intercept_build(compile_cmd, coverity_bin_path, app_path):
        logger.critical("Failed intercepting build")
        if do_panic_delete: panic_delete_compilation(db, compilation_tag)
        db.close()
        exit(9)
    return

def find_compilation_coverage_step(db : pymongo.MongoClient, app_build_dir : str, compilation_tag : str, app_path : str, configs : dict, do_panic_delete : bool):
     # after the intercepted build finished, now we also have C source files that can be analyzed
    import time
    start_time = time.time()
    try:

        for (current_lib, _, uk_files) in os.walk(app_build_dir, topdown=True):

            for file in uk_files:
                
                # first filter, get *.o.cmd, kconfig directory has same format of files but they are weird
                if file[-6 : ] == ".o.cmd" and current_lib.split("/")[-1] != "kconfig":
                    
                    o_cmd_file_abs_path = f"{current_lib}/{file}"

                    with open(o_cmd_file_abs_path, "r") as f:
                        # ignore the start which is ""
                        command = f.read()[2 : ]
                        f.close()

                    src_path = try_compilers_for_src_path(command, o_cmd_file_abs_path)

                    if src_path == None:
                        logger.debug(f"{o_cmd_file_abs_path} does not have a compilation command")
                        continue
                    
                    logger.debug(f"{o_cmd_file_abs_path} has a correct compilation command file")
                    logger.debug(f"|||||||||||||||||||||||{src_path}||||||||||||||||||||||||||||||")

                    try:
                        src_doc : dict= get_source_compile_coverage(
                            db=db,
                            compilation_tag=compilation_tag,
                            lib_name=current_lib.split("/")[-1],
                            app_dir=app_path,
                            app_build_dir=app_build_dir,
                            src_path=src_path,
                            configs=configs
                        )

                    except Exception as e:
                        logger.critical(e)
                        if do_panic_delete: panic_delete_compilation(db, compilation_tag)
                        exit(7)
    except Exception as e:
        logger.critical(e)
        if do_panic_delete: panic_delete_compilation(db, compilation_tag)
        db.close()
        exit(8)

    logger.info(f"Compilation coverage research was done in : {time.time() - start_time} by analyzing {lines_of_code} and {blocks_of_code} blocks")

    return

def coverity_submit_step(db : pymongo.MongoClient, app_path : str, compile_cmd : str, compilation_tag : str, coverity : CoverityAPI, do_panic_delete : bool):

    if not coverity.submit_build(app_path, compile_cmd, compilation_tag):
        if do_panic_delete: panic_delete_compilation(db, compilation_tag)
        logger.critical("Failed uploading build to Coverity for static analysis. Exiting")
        db.close()
        exit(1)
    return

def coverity_wait_recent_snapshot_step(db : pymongo.MongoClient, configs : dict, compilation_tag : str, coverity : CoverityAPI, do_panic_delete : bool):
    # busy wait until the most recent snapshot is the one that has been uploaded now
    current_snapshot_retries = 0
    try:
        while current_snapshot_retries < configs['coverityAPI']['recentSnapshotRetries'] and coverity.poll_recent_snapshot(compilation_tag) == False:
            logger.warning(f"Retry {current_snapshot_retries}/{configs['coverityAPI']['recentSnapshotRetries']} finding uploaded build in the snapshots:  {current_snapshot_retries}/{configs['coverityAPI']['recentSnapshotRetries']}")
            current_snapshot_retries += 1

        if current_snapshot_retries == configs['coverityAPI']['recentSnapshotRetries']:
            if do_panic_delete: panic_delete_compilation(db, compilation_tag)
            logger.critical(f"All {current_snapshot_retries} retries for finding recent snapshot used.")
            db.close()
            exit(6)

    # authentication failed before fetching the most recent snapshot
    except TimeoutException:
        if do_panic_delete: panic_delete_compilation(db, compilation_tag)
        logger.critical(f"Deleted compilation/app \"{compilation_tag}\" from db since authentication failed")
        db.close()
        exit(2)

    # timeout during waiting for the interceptor to catch a request containg any data about the snapshots no matter if the data is correct or not
    except InterceptorTimeout:
        if do_panic_delete: panic_delete_compilation(db, compilation_tag)
        db.close()
        exit(3)
    return
def coverity_get_defects_step(db : pymongo, compilation_tag : str, coverity : CoverityAPI, do_panic_delete : bool) -> list[CoverityDefect]:
    # we waited for the most recent snapshot to be the one resulted from the current file submition
    # now get the defects found 
    try:
        coverity.fetch_and_cache_recent_defects_1(compilation_tag)

    # authentication or web interaction (clicking the most recent snapshot cell)
    except TimeoutException:
        if do_panic_delete: panic_delete_compilation(db, compilation_tag)
        logger.critical(f"Deleted compilation/app \"{compilation_tag}\" from db since authentication or double click interaction with the most recent snapshot cell")
        db.close()
        exit(4)

    except InterceptorTimeout:
        if do_panic_delete: panic_delete_compilation(db, compilation_tag)
        logger.critical(f"Deleted compilation/app \"{compilation_tag}\" from db since request for defects got timeout")
        db.close()
        exit(5)

    # get the Coverity defects, cache them until we finish analyzing source files
    cov_defects : list[AbstractLineDefect] = list(map(lambda d : CoverityDefect(prepare_coverity_defects_for_db(d, compilation_tag)), coverity.cached_last_defect_results))
    
    # we do not need these cached results anymore, maybe we can save some memory
    del coverity.cached_last_defect_results
    del coverity.cached_recent_snapshot

    return cov_defects

def analyze_application_sources(db : pymongo.MongoClient, compilation_tag : str, app_build_dir : str, app_path : str, compile_cmd : str, configs : dict):

    # main logic to register a new compilation
    # 1. compile to Unikraft app using the coverity tools and kraft
    # 2. recompile every source individually in order to include source file data in the database (such as compile blocks through instrumentation)
    # 3. submit the build to the Coverity platform
    # 4. wait for the Coverity build to be analyzed
    # 5. fetch Coverity defects and insert them in the database along with data for every source file

    # Up to commit `7560d9468e4dfc22cb7a7b79ba22fc8913bbd4ef` the logic was:
    # 1. compile to Unikraft app using the coverity tools and kraft
    # 2. recompile every source individually in order to include source file data in the database (such as compile blocks)
    # 3. submit the build to the Coverity platform
    # 4. wait for the Coverity build to be analyzed
    # 5. fetch Coverity defects and insert them in the database along with data for every source file

    # Old logic is more efficient since submiting a build takes time that can be used for doing recompilation and instrumentation
    # but since you cannot delete a submited snapshot in Coverity Scan, we whould introduce potential wrong builds that would fail later
    # at recompilations and instrumentation

    # New logic will do recompilation and instrumentation first, and if any Exception occurs, delete current app from DB and exit
    # so that we whould not submit a failing build to Coverity

    # init interface between Coverity and Unikraft scanner tool
    coverity : CoverityAPI = CoverityAPI(configs, app_path)
    coverity_intercept_compilation_step(db, configs, compile_cmd, configs["coverityAPI"]["covSuitePath"], app_path, compilation_tag, coverity, True)
        
    find_compilation_coverage_step(db, app_build_dir, compilation_tag, app_path, configs, True)

    coverity_submit_step(db, app_path, compile_cmd, compilation_tag, coverity, True)
    
    # used to scrape crucial metadata for further defects fetching
    coverity.init_snapshot_and_project_views()

    # wait for uploaded build to finish being analyzed and appear as snapshot
    coverity_wait_recent_snapshot_step(db, configs, compilation_tag, coverity, True)

    # get defects
    cov_defects = coverity_get_defects_step(db, compilation_tag, coverity, True)

    insert_defects_in_db(db, cov_defects, compilation_tag)

def add_app_subcommand(db : pymongo.MongoClient, app_workspace : str, app_build_dir : str, compilation_tag : str, app_format : str, compile_command : str, configs : dict, is_partial : bool):


    # check if an identic compilation occured

    existing_compilation = db[DATABASE][COMPILATION_COLLECTION].find_one(
                    {"tag" : compilation_tag}
        )
    if existing_compilation != None:
        logger.critical(f"An existing compilation has been previously registered with this tag and app\n{existing_compilation}")
        db.close()
        exit(15)
    
    logger.info(f"No compilation has been found. Proceeding with analyzing source {app_workspace}")

    compilation_id : ObjectId = db[DATABASE][COMPILATION_COLLECTION].insert_one(
        {
            "tag" : compilation_tag,
            "app": app_workspace,
            "format" : app_format,
            "partial_status" : is_partial
        }
    ).inserted_id
    logger.debug(f"New compilation has now id {compilation_id}")

    if not is_partial:
        analyze_application_sources(db, compilation_tag, app_build_dir, app_workspace, compile_command, configs)
        with open(configs['outfile'],"a") as out:
            out.write(f"Registering the {compilation_tag} has been completed.")
    else:
        coverity : CoverityAPI = CoverityAPI(configs, app_workspace)
        coverity_intercept_compilation_step(db, configs, compile_command, configs["coverityAPI"]['covSuitePath'], app_workspace, compilation_tag, coverity, True)
        find_compilation_coverage_step(db, app_build_dir, compilation_tag, app_workspace, configs, False)
        logger.info(f"Finished processing compilation coverage for {app_workspace} with tag {compilation_tag}.")

        with open(configs['outfile'],"a") as out:
            out.write(f"Compilation {compilation_tag} has been partially registered to Unikraft Scanner.\n Only the compilation coverage has been researched and scanned defects are missing. You will need to submit the build archive created at {app_workspace}/cov-int, download defects from Coverity platform and enrich the partial compilation with them.\n")
    
    return

def download_coverity_most_recent(db : pymongo.MongoClient, compilation_tag : str, configs : dict, download_path : str):

    existing_compilation = db[DATABASE][COMPILATION_COLLECTION].find_one(
                    {"tag" : compilation_tag}
        )
    
    if existing_compilation != None:
        logger.debug(f"Compilation tag exists which means that the compilation has been previously registered. Continue...")
    else:
        logger.critical(f"Compilation {compilation_tag} does not exist. Cannot download results from non existing compilation")
        exit(16)

    # init interface between Coverity and Unikraft scanner tool
    coverity : CoverityAPI = CoverityAPI(configs, download_path)

    # used to scrape crucial metadata for further defects fetching
    coverity.init_snapshot_and_project_views()

    coverity_wait_recent_snapshot_step(db, configs, compilation_tag, coverity, False)

    coverity_get_defects_step(db, compilation_tag, coverity, False)

    return

def enrich_coverity_compilation(db : pymongo.MongoClient, compilation_tag : str, results_document_path : str):

    existing_compilation = db[DATABASE][COMPILATION_COLLECTION].find_one(
                            {"tag" : compilation_tag}
                        )
    
    if existing_compilation != None:
        logger.debug(f"Compilation tag exists which means that the compilation has been previously registered. Continue...")
    else:
        logger.critical(f"Compilation {compilation_tag} does not exist. Cannot enrich with results a non existing compilation")
        exit(17)

    cov_defects : list[AbstractLineDefect] = []
    with open(results_document_path, 'r', encoding="utf-8-sig") as f:
            csv_lines = csv.DictReader(f)
            for line in csv_lines:
                print("--------------------")
                print(line)
                defect = CoverityDefect(prepare_coverity_defects_for_db(line, compilation_tag))
                cov_defects.append(defect)
    
    
    insert_defects_in_db(db, cov_defects, compilation_tag)

    db[DATABASE][SOURCES_COLLECTION].update_one(
        filter={"tag" : compilation_tag},
        update={"$set" : {"partial_status" : False}}
    )
    return