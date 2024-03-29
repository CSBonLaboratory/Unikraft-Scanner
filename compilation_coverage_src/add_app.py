import subprocess
import re
import logging
import pymongo
import os
import shutil
from typing import Union
from symbol_engine import find_compilation_blocks_and_lines, find_children
from helpers.helpers import get_source_version_info, trigger_compilation_blocks, find_real_source_file, get_source_compilation_command, instrument_source
from helpers.CompilationBlock import CompilationBlock
from helpers.CSourceDocument import CSourceDocument
from helpers.helpers import SourceStatus, SourceVersionStrategy, GitCommitStrategy, SHA1Strategy
from CoverityAPI import CoverityAPI
import coverage
from bson.objectid import ObjectId
from pymongo import ReturnDocument
from defects.AbstractLineDefect import AbstractLineDefect
from defects.CoverityDefect import CoverityDefect

DATABASE = coverage.DATABASE
SOURCES_COLLECTION = coverage.SOURCES_COLLECTION
COMPILATION_COLLECTION = coverage.COMPILATION_COLLECTION

db = coverage.db

logger = logging.getLogger(__name__)

def is_new_source(src_path : str) -> SourceStatus:
    
    global db

    latest_version : Union[SourceVersionStrategy, dict] = get_source_version_info(src_path)

    existing_source : Union[CSourceDocument, dict] = db[DATABASE][SOURCES_COLLECTION].find_one(
        {"source_path" : os.path.relpath(src_path, os.environ["UK_WORKDIR"])}
    )

    logger.debug(f"{src_path} has git commit log {latest_version}")
        
    if existing_source == None:
        logger.debug(f"{src_path} has not been found in the database. STATUS: NEW")
        return SourceStatus.NEW
    
    if GitCommitStrategy.version_key in latest_version and GitCommitStrategy.version_key in existing_source:
        if existing_source[GitCommitStrategy.version_key] == latest_version[GitCommitStrategy.version_key]:
            logger.debug(f"{src_path} source is already registered. STATUS: EXISTING checked using git commit id {latest_version[GitCommitStrategy.version_key]}")
            return SourceStatus.EXISTING
        else:
            # since it is a previous/deprecated version of that source we need to delete its compile blocks from the db, update the commit_id and restart the analysis process
            logger.debug(f"{src_path} has commit {latest_version[GitCommitStrategy.version_key]} but database has outdated commit {existing_source[GitCommitStrategy.version_key]}.STATUS: DEPRECATED")
            return SourceStatus.DEPRECATED
    
    if SHA1Strategy.version_key in latest_version and SHA1Strategy.version_key in existing_source:
        if existing_source[SHA1Strategy.version_key] == latest_version[SHA1Strategy.version_key]:
            logger.debug(f"{src_path} source is already registered. STATUS: EXISTING checked using sha1 id {latest_version[SHA1Strategy.version_key]}")
            return SourceStatus.EXISTING
        else:
            # since it is a previous/deprecated version of that source we need to delete its compile blocks from the db, update the commit_id and restart the analysis process
            logger.debug(f"{src_path} has hash {latest_version[SHA1Strategy.version_key]} but database has outdated hash {existing_source[SHA1Strategy.version_key]}.STATUS: DEPRECATED")
            return SourceStatus.DEPRECATED
        

    logger.critical(f"Cannot check source status. DB entry and current source have different version id types")
    logger.critical(f"{existing_source} VS {latest_version}")

    return SourceStatus.UNKNOWN
    
        

def update_db_activated_compile_blocks(activated_block_counters : list[int], src_path : str, compilation_tag: str) -> Union[CSourceDocument, dict]:

    global db
    
    source_document : Union[CSourceDocument, dict] = db[DATABASE][SOURCES_COLLECTION].find_one(
        {"source_path": os.path.relpath(src_path, os.environ["UK_WORKDIR"])}
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
        filter= {"source_path" : os.path.relpath(src_path, os.environ["UK_WORKDIR"])},
        update= {"$set" : source_document},
        return_document=pymongo.ReturnDocument.AFTER
    )

    logger.debug(f"AFTER update of compile regions\n{updated_source_document}")

    return updated_source_document

def init_source_in_db(source_status : SourceStatus, src_path : str, real_src_path : str, total_blocks : Union[list[CompilationBlock]], universal_lines : int, compilation_tag : str, lib_name : str) -> bool:

    global db

    find_children(total_blocks)

    # calculate total lines of code
    total_lines = universal_lines
    for cb in total_blocks:
        total_lines += cb.lines
    
    new_src_document : Union[CSourceDocument, dict] = {
                "source_path" :  os.path.relpath(src_path, os.environ["UK_WORKDIR"]),
                "compile_blocks" : [compile_block.to_mongo_dict() for compile_block in total_blocks],
                "universal_lines" : universal_lines,
                "triggered_compilations" : [compilation_tag],
                "compiled_stats" : {},
                "total_lines" : total_lines,
                "lib" : lib_name,
                "defects" : []
    }

    # TODO right now will only have git_commit_id since hashes are employed for generated or out of repo C source files
    version_info : Union[SourceVersionStrategy, dict] = get_source_version_info(real_src_path)

    new_src_document.update(version_info)

    #TODO what to add for real_src_path ? 

    # the source is new so we create a new entry in the database
    if source_status == SourceStatus.NEW:

        db[DATABASE][SOURCES_COLLECTION].insert_one(new_src_document)
        
        logger.debug(f"Initialized source in db\n{new_src_document}")

    # the source is deprecated we need to clear the compilation blocks of the existing entry and update latest git commit id
    elif source_status == SourceStatus.DEPRECATED:
        
        logger.debug(
            f"BEFORE source update due to deprecated commit\n" + 
            f"{db[DATABASE][SOURCES_COLLECTION].find_one({'source_path' : os.path.relpath(src_path, os.environ['UK_WORKDIR'])})}"
        )

        updated_source_document : Union[CSourceDocument, dict] = db[DATABASE][SOURCES_COLLECTION].find_one_and_update(
            filter= {"source_path" : os.path.relpath(src_path, os.environ['UK_WORKDIR'])},
            update= {"$set" : new_src_document},
            return_document= ReturnDocument.AFTER
        )

        logger.debug(f"AFTER source update due to deprecated commit {updated_source_document}")
    else:
        logger.critical("Initialization failed since source_status is not NEW or DEPRECATED !")
        return False
    
    return True

def fetch_existing_compilation_blocks(src_path) -> list[CompilationBlock]:

    global db

    existing_blocks : Union[list[CompilationBlock], dict] = db[DATABASE][SOURCES_COLLECTION].find_one(
            filter= {"source_path" : os.path.relpath(src_path, os.environ['UK_WORKDIR'])},
            projection= {"compile_blocks" : 1, "_id" : 0}
        )

    total_blocks = [CompilationBlock(raw_block) for raw_block in existing_blocks['compile_blocks']]
    
    logger.debug(f"Fetched compilation blocks from database:\n{total_blocks}")

    return total_blocks


def get_source_compile_coverage(compilation_tag : str, lib_name : str, app_build_dir : str, src_path : str) -> Union[CSourceDocument, dict]:

    global db

    real_src_path = find_real_source_file(src_path, app_build_dir, lib_name)

    # TODO, analysis of c source files that do not exist but are generated by other files is disabled for now 
    if src_path != real_src_path:
        logger.warning(f"Found generator file {src_path} which will generate {real_src_path}. Skiping analysis for now ...")
        return None
    
    real_src_file_name = real_src_path.split("/")[-1]

    copy_source_path = f"{app_build_dir}/srcs/{real_src_file_name}"

    # very important !! the source file is copied before code instrumentation, later it will be back to its initial form
    shutil.copyfile(src_path, copy_source_path)

    source_status : SourceStatus = is_new_source(src_path)

    # the source is already registered, so we can fetch all compilation blocks from the database
    # also bind the compilation id to this file since the universal lines of code are compiled
    if source_status == SourceStatus.EXISTING:
        total_blocks : list[CompilationBlock] = fetch_existing_compilation_blocks(src_path)

        db[DATABASE][SOURCES_COLLECTION].find_one_and_update(
            filter={"source_path" :  os.path.relpath(src_path, os.environ["UK_WORKDIR"])},
            update={"$push": {"triggered_compilations" : compilation_tag}}
        )

    # the source is not registered so we must parse the source file and find compilation blocks
    # or the source needs to be cleared due to deprecation so we must parse the updated source file and find compilation blocks
    elif source_status == SourceStatus.NEW or source_status == SourceStatus.DEPRECATED:

        total_blocks, universal_lines = find_compilation_blocks_and_lines(real_src_path, True)
        init_source_in_db(source_status, src_path, real_src_path, total_blocks, universal_lines, compilation_tag, lib_name)


    compile_command = get_source_compilation_command(app_build_dir, lib_name, real_src_path)

    if compile_command == None:
        logger.critical(f"No .o.cmd file which has compilation command for {src_path} in {lib_name}")
        db[DATABASE][SOURCES_COLLECTION].find_one_and_delete({"source_path" : os.path.relpath(src_path, os.environ['UK_WORKDIR'])})
        return None

    instrument_source(total_blocks, real_src_path)

    activated_block_counters = trigger_compilation_blocks(compile_command)

    updated_src_document = update_db_activated_compile_blocks(
            activated_block_counters= activated_block_counters,
            src_path= src_path,
            compilation_tag= compilation_tag
    )
    
    # go back to source original code without instrumentation
    shutil.copyfile(copy_source_path, real_src_path)

    return updated_src_document

def insert_defects_in_db(cov_defects : list[AbstractLineDefect], compilation_tag : str) -> None:

        for defect in cov_defects:

            source_document_dict  : dict = db[DATABASE][SOURCES_COLLECTION].find_one(
                filter={
                    "source_path" : defect.source_path
                }
            )
            
            
            if source_document_dict == None:
                logger.critical(f"No such source {defect.source_path} featured in defect {defect.to_mongo_dict()}")

            source_document = CSourceDocument(source_document_dict)

            logger.debug(f"-------------------- Starting inserting defect in {source_document.source_path} ---------------------")

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

            while tree_end:
                
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
                    filter={
                       "source_path" : defect.source_path
                    },
                    update={
                        "$push" : {"defects" : defect.to_mongo_dict()}
                    }
                )
            else:
                
                logger.debug(f"Defect {defect} inserted in compile block {current_block.__dict__}")
                # insert the defect in the suitable compile block
                cb : dict = db[DATABASE][SOURCES_COLLECTION].find_one(
                    filter={
                        "source_path" : defect.source_path
                    },
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

def analyze_application_sources(compilation_tag : str, app_build_dir : str, app_path : str):

    global rootTrie, db

    # get all source file using the make print-srcs
    logger.debug(app_path)
    proc = subprocess.Popen(f"cd {app_path} && make print-srcs", shell=True, stdout=subprocess.PIPE)

    stdout_raw, stderr_raw = proc.communicate()

    # TODO: additional replace() calls for the Makefile temporary fix when having "/bin/bash Argument list too long error"
    make_stdout = stdout_raw.decode().replace("'", "").replace("\\n", "\n").replace("\\t", "\t").replace("\\", "")
    #make_stderr = stderr_raw.decode()

    # try to find at least a lib and all sources on the next line, if not delete the compilation from the db
    lib_str_match = re.search("\s*([a-zA-Z0-9_-]+:)\s*\n\s*(.*)\n", make_stdout)

    if lib_str_match == None:
        logger.critical("No lib or app source file dependencies found. Maybe `make print-srcs` was not called correctly")
        db[DATABASE][COMPILATION_COLLECTION].find_one_and_delete({"tag" : compilation_tag})
        exit(1)


    for lib_and_srcs_match in re.finditer("\s*([a-zA-Z0-9_-]+):\s*\n\s*(.*)\n", make_stdout):

        # match the first group -> [a-zA-Z_]
        lib_name = lib_and_srcs_match.group(1)

        # match the second group which is the next line after libblabla with source file paths -> .*
        srcs_line = lib_and_srcs_match.group(2)

        for src_path_raw in srcs_line.split(" "):
            src_path = src_path_raw.split("|")[0]

            if src_path[-2:] == ".c":
                logger.debug(f"---------------------{src_path}------------------------------------")

                get_source_compile_coverage(
                    compilation_tag= compilation_tag, 
                    lib_name= lib_name, 
                    app_build_dir= app_build_dir, 
                    src_path= src_path
                )

    coverity = CoverityAPI()

    # get the Coverity defects and insert them in a table
    cov_defects : list[AbstractLineDefect] = list(map(lambda d : CoverityDefect(coverity.prepare_defects_for_db(d, compilation_tag, app_path)), coverity.fetch_raw_defects()))

    insert_defects_in_db(cov_defects, compilation_tag)

def add_app_subcommand(app_workspace : str, app_build_dir : str, compilation_tag : str):

    global db

    if not os.path.exists(f"{app_build_dir}/srcs"):
        os.mkdir(f"{app_build_dir}/srcs")

    # check if an identic compilation occured

    existing_compilation = db[DATABASE][COMPILATION_COLLECTION].find_one(
                    {"tag" : compilation_tag}
        )
    if existing_compilation != None:
        logger.critical(f"An existing compilation has been previously registered with this tag and app\n{existing_compilation}")
        return
    
    logger.info(f"No compilation has been found. Proceeding with analyzing source {app_workspace}")

    compilation_id : ObjectId = db[DATABASE][COMPILATION_COLLECTION].insert_one({"tag" : compilation_tag, "app": app_workspace}).inserted_id
    logger.debug(f"New compilation has now id {compilation_id}")

    analyze_application_sources(compilation_tag, app_build_dir, app_workspace)