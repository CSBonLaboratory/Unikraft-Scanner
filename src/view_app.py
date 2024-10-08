import logging
import pymongo
from SourceTrie import SourceTrie
from helpers.CSourceDocument import CSourceDocument
from coverage import LOGGER_NAME
logger = logging.getLogger(LOGGER_NAME)


def view_app_subcommand(db : pymongo.MongoClient, compilation_tags : list[str], out_file_name : str):

    from coverage import DATABASE, SOURCES_COLLECTION, COMPILATION_COLLECTION

    # very important to have ascending in order to have a tree representation similar to ones presented in an IDE
    app_src_documents_dicts : dict = db[DATABASE][SOURCES_COLLECTION].find(
        filter={"triggered_compilations" : {"$in" : compilation_tags}},
        sort={"source_path" : pymongo.ASCENDING}
    )

    app_src_documents = [CSourceDocument(src_doc_dict) for src_doc_dict in app_src_documents_dicts]

    appTrie = SourceTrie(".")

    # remove other compiled stats of compilations that are not wished to be visualized 
    for src_doc in app_src_documents:
        
        for tag in src_doc.compiled_stats:
            if tag not in compilation_tags:
                del src_doc.compiled_stats[tag]
                
        appTrie.add_node(src_doc.source_path.split("/"), src_doc)

    
    out_file = open(out_file_name, "a")

    appTrie.print_trie_view(out_file, compilation_tags)

    out_file.close()

    





