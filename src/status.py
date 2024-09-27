import os
import logging
from helpers.CSourceDocument import CSourceDocument
from helpers.CSourceNoDocument import CSourceNoDocument
from helpers.helpers import get_source_git_commit_hashes, hash_no_git_commit
from symbol_engine import find_compilation_blocks_and_lines
from SourceTrie import SourceTrie
from coverage import LOGGER_NAME
import pymongo
from pathlib import Path

logger = logging.getLogger(LOGGER_NAME)


def status_subcommand(db : pymongo.MongoClient, saved_outfile : str, target_dir : str):

    from coverage import DATABASE, SOURCES_COLLECTION

    if not os.path.isabs(target_dir):
        logger.critical(f"Path to the kernel is not absolute")
        exit(18)

    status_trie = SourceTrie(target_dir)

    out_file = open(saved_outfile, "a")

    all_lines = 0

    compiled_lines = 0

    for(current_dir, _, files) in os.walk(target_dir, topdown=True):
         
        for src_file in files:

            if src_file[ -2 : ] == ".c":

                abs_source_path = os.path.join(current_dir, src_file) 
                    
                src_path = os.path.join(os.path.relpath(current_dir, Path(target_dir).parent.absolute()), src_file)

                src_document_dict : dict = db[DATABASE][SOURCES_COLLECTION].find_one({"source_path" : src_path})

                # this is a document that has not appeared in any compilation
                if src_document_dict == None:
                    logger.info(f"Could not find {src_path} in database for viewing status. No compilation/app ever used it.")
                        
                    compile_blocks, universal_lines, lines_of_code = find_compilation_blocks_and_lines(current_dir + "/" + src_file, False)

                    latest_commits = get_source_git_commit_hashes(abs_source_path)

                    src_document_dict = {
                        "source_path" : src_path,
                        "compile_blocks" : [block.to_mongo_dict() for block in compile_blocks],
                        "universal_lines" : universal_lines,
                        "total_lines" : lines_of_code,
                        "git_file_commit_hash" : latest_commits[0],
                        "git_repo_commit_hash" : latest_commits[1]
                    }

                    # band-aid fix for https://github.com/CSBonLaboratory/Unikraft-Scanner/issues/11
                    # if the source file is not part of a git repo than just do a hash 
                    if latest_commits[0] == '':
                        src_document_dict['hash_no_git_commit'] = hash_no_git_commit(abs_source_path)
                    else:
                        src_document_dict['hash_no_git_commit'] = None

                    document_new = CSourceNoDocument(src_document_dict)
                    all_lines += document_new.total_lines
                    status_trie.add_node(src_path.split("/"), document_new)

                else:
                    document = CSourceDocument(src_document_dict)
                    compiled_lines += document.total_lines
                    all_lines += document.total_lines
                    status_trie.add_node(src_path.split("/"), document)
                

    status_trie.print_trie_status(out_file)

    out_file.write(f"FINAL RESULTS: {compiled_lines} compiled lines out of {all_lines} total lines")

    out_file.close()            

