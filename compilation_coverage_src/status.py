import os
import logging
from helpers.CSourceDocument import CSourceDocument
from helpers.CSourceNoDocument import CSourceNoDocument
from symbol_engine import find_compilation_blocks_and_lines
from SourceTrie import SourceTrie
from functools import reduce

logger = logging.getLogger(__name__)

def status_subcommand(saved_outfile : str, target_dir : str):

    from coverage import DATABASE, SOURCES_COLLECTION, db
    
    unikraft_root = os.environ['UK_WORKDIR']

    status_trie = SourceTrie(unikraft_root)

    out_file = open(saved_outfile, "a")

    for(current_dir, _, files) in os.walk(unikraft_root, topdown=True):
        
        if target_dir in current_dir:
            
            for src_file in files:

                if src_file[ -2 : ] == ".c":

                    src_path = os.path.relpath(current_dir, unikraft_root) + f"/{src_file}"

                    src_document_dict : dict = db[DATABASE][SOURCES_COLLECTION].find_one({"source_path" : src_path})

                    # this is a document that has not appeared in any compilation
                    if src_document_dict == None:
                        logger.info(f"Could not find {src_path} in database for viewing status. No compilation/app ever used it.")
                        
                        compile_blocks, universal_lines = find_compilation_blocks_and_lines(current_dir + "/" + src_file, False)

                        total_lines = universal_lines

                        src_document_dict = {
                            "source_path" : src_path,
                            "compile_blocks" : [block.to_mongo_dict() for block in compile_blocks],
                            "universal_lines" : universal_lines,
                            "total_lines" : reduce(lambda acc, block : acc + block.lines, compile_blocks, total_lines)
                        }

                    
                        status_trie.add_node(src_path.split("/"), CSourceNoDocument(src_document_dict))

                    else:
                        status_trie.add_node(src_path.split("/"), CSourceDocument(src_document_dict))
                

    status_trie.print_trie_status(out_file)

    out_file.close()            

