import logging
from colorama import Fore

logger = logging.getLogger(__name__)

def get_app_coverage(compilation_id) -> tuple[int, ...]:

    from coverage import DATABASE, db, SOURCES_COLLECTION, COMPILATION_COLLECTION, COVERITY_DEFECTS_COLLECTION

    total_lines = 0
    compiled_lines = 0

    for source_document in db[DATABASE][SOURCES_COLLECTION].find(filter={"triggered_compilations" : {"$elemMatch" : {"$eq" : compilation_id}}}):

        total_lines += source_document["universal_lines"]
        compiled_lines += source_document["universal_lines"]

        for compile_block in source_document["compile_blocks"]:

            total_lines += compile_block["lines"]

            if compilation_id in compile_block["triggered_compilations"]:
                compiled_lines += compile_block["lines"]

    return compiled_lines, total_lines


def print_app_coverage(compilation_id, compilation_tag, saved_outfile):

    logger.debug(f"Found app with tag {compilation_tag} and id {compilation_id}")

    compiled_lines, total_lines = get_app_coverage(compilation_id)

    logger.info(f"Total lines {total_lines} with compiled lines {compiled_lines}")

    with open(saved_outfile, "a") as out:
        out.write(f"App: {compilation_tag}\n")

        out.write(f"\tTotal lines: {total_lines}\n")

        out.write(f"\tCompiled lines: {compiled_lines}\n")

        ratio = (compiled_lines / total_lines) * 100

        if ratio == 100:
            out.write(Fore.GREEN + f"\tRatio:{ratio}\n" + Fore.RESET)
        else:
            out.write(Fore.RED + f"\tRatio:{ratio}\n" + Fore.RESET)

    return

def list_app_subcommand(saved_outfile : str):

    from coverage import DATABASE, db, SOURCES_COLLECTION, COMPILATION_COLLECTION, COVERITY_DEFECTS_COLLECTION

    for compilation_document in db[DATABASE][COMPILATION_COLLECTION].find({}):
        print_app_coverage(compilation_document["_id"], compilation_document["tag"], saved_outfile)
    
    return
    

    
