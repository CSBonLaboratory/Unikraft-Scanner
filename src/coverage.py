#!/usr/bin/python3

import argparse
import pymongo
import logging
import time
import yaml
import os.path
from colorama import Fore, Back

DATABASE = "unikraft"
SOURCES_COLLECTION = "sources"
COMPILATION_COLLECTION = "compilations"
LOGGER_NAME = "unikraft_scanner_logger"
UNKNOWN_DEFECTS_COLLECTION = "unknown_defects"

import add_app
import list_app
import view_app
import status
import setup

from helpers.helpers import find_app_format, find_latest_schema_remote_version, find_latest_schema_local_version
from helpers.helpers import AppFormat


def main():

    parser = argparse.ArgumentParser(
        description="Compilation coverage tool for Unikraft. To be used with Coverity to detect new unscanned code regions"
    )
    
    subparser = parser.add_subparsers(dest='operations', help='Available operations')

    setup_parser = subparser.add_parser(
        description="Initial setup action. Use only when you made a Coverity Scan account and received credentials, from the tool's admin, for interacting with the compilation coverage DB",
        name='setup',
        help="Initial setup action. Use only when you made a Coverity Scan account and received credentials, from the tool's admin, for interacting with the compilation coverage DB"
    )

    setup_parser.add_argument(
        '-s',
        "--settings",
        required=True,
        action='store',
        help='Path to the future configuration file that will contain settings chosen during setup phase.'
    )

    app_parser = subparser.add_parser(
        description='App-related operations such as registering an app compilation etc.', 
        name='app', 
        help='App-related operations such as registering an app compilation etc.'
    )

    status_parser = subparser.add_parser(
        description="Print compilation statistics for whole Unikraft or specific subsections of it",
        name="status",
        help="Print compilation statistics for whole Unikraft or specific subsections of it"
    )

    status_parser.add_argument(
        "-k",
        "--kernel",
        required=True,
        action="store",
        help="Absolute path from which source files will be searched recursively for showing global compilation statistics."
    )

    status_parser.add_argument(
        '-s',
        "--settings",
        required=True,
        action='store',
        help='Path to the configuration file.'  
    )
    
    app_sub_parser = app_parser.add_subparsers(dest="app_operations", help="Available app-related operations")

    compile_app_parser = app_sub_parser.add_parser(
        description="Partial step of `app add`. Does first compilation with local Coverity interception, code instrumentation, second compilation and dicovering compilation conditioned code blocks. Add info about source code files in database. Does NOT submit the compilation archive to Coverity platform\n",
        name="compile",
        help="Partial step of `app add`. Does first compilation with local Coverity interception, code instrumentation, second compilation and dicovering compilation conditioned code blocks. Add info about source code files in database. Does NOT submit the compilation archive to Coverity platform\n"
    )

    compile_app_parser.add_argument(
        '-s',
        "--settings",
        required=True,
        action='store',
        help='Path to the configuration file.'  
    )

    compile_app_parser.add_argument(
        '-b',
        '--build',
        required=False,
        action='store',
        help='Build directory of the Unikraft application. Default value is the .unikraft directory in the app workspace (-a option)',
        type=str,
        default=None
    )

    compile_app_parser.add_argument(
        '-a',
        '--app',
        required=False,
        action='store',
        help='App path which will run with Unikraft. This is considered to be the app workspace.',
        default=None
    )

    compile_app_parser.add_argument(
        '-t', 
        '--tag', 
        required=True, 
        action='store', 
        help="A new unique tag/description that identifies an app's compilation process."
    )

    compile_app_parser.add_argument(
        '-c',
        '--compile',
        required=True,
        action='store',
        help='Kraft compilation command used for the targeted app.'
    )

    submit_app_parser = app_sub_parser.add_parser(
        description="Partial step for `app add`. Based on a intercepted compilation by the Coverity cov-build tool, send the build archive to the coverity platform.",
        name="submit",
        help="Partial step for `app add`. Based on a intercepted compilation by the Coverity cov-build tool, send the build archive to the coverity platform."
    )

    submit_app_parser.add_argument(
        '-s',
        "--settings",
        required=True,
        action='store',
        help='Path to the configuration file.'  
    )

    submit_app_parser.add_argument(
        '-c',
        '--compile',
        required=True,
        action='store',
        help='Kraft compilation command used for the targeted app.'
    )

    submit_app_parser.add_argument(
        '-t',
        '--tag',
        required=True,
        action='store',
        help=" A new unique tag/description that identifies an app's compilation process."
    )

    submit_app_parser.add_argument(
        '-a',
        '--archive',
        required=True,
        action='store',
        help="Path to the cov-int intercepted archive generated by the Coverity build&interception tools."
    )

    download_app_parser = app_sub_parser.add_parser(
        description="[UNSTABLE] Partial step for `app add`. Download defects from Coverity as a CSV file for the most recent compilation registered using `app add` and insert them in database based on compilation tag\n",
        name="download",
        help="[UNSTABLE] Partial step of `app add`. Download defects from Coverity as a CSV file for the most recent compilation registered using `app add` and insert them in database based on compilation tag\n"
    )

    download_app_parser.add_argument(
        '-t',
        '--tag',
        required=True,
        action='store',
        help=" A new unique tag/description that identifies an app's compilation process."
    )

    download_app_parser.add_argument(
        '-s',
        "--settings",
        required=True,
        action='store',
        help='Path to the configuration file.'  
    )

    download_app_parser.add_argument(
        '-d',
        "--download",
        required=True,
        action='store',
        help='Path to where the defects will be downloaded as a csv document.'  
    )

    enrich_app_parser = app_sub_parser.add_parser(
        description="Partial step for `app add`. Given a local CSV document with Coverity defect results, enrich an incomplete compiled app with the final defects to make it complete and ready for manual triage",
        name="enrich",
        help="Partial step for `app add`. Given a local CSV document with Coverity defect results, enrich an incomplete compiled app with the final defects to make it complete and ready for manual triage"
    )

    enrich_app_parser.add_argument(
        '-s',
        "--settings",
        required=True,
        action='store',
        help='Path to the configuration file.'  
    )

    enrich_app_parser.add_argument(
        '-t',
        '--tag',
        required=True,
        action='store',
        help=" A new unique tag/description that identifies an app's compilation process."
    )
    
    enrich_app_parser.add_argument(
        '-r',
        '--results',
        required=True,
        action='store',
        help="Path to the CSV document containing Coverity results."
    )

    add_app_parser = app_sub_parser.add_parser(
        description="Register a new app compilation process for static analysis. Contains all the compile, submit, download, enrich steps",
        name="add",
        help="[UNSTABLE] Register a new app compilation process for static analysis. Contains all the compile, submit, download, enrich steps"
    )

    add_app_parser.add_argument(
        '-b',
        '--build',
        required=False,
        action='store',
        help='Build directory of the Unikraft application. Default value is the .unikraft directory in the app workspace (-a option)',
        type=str,
        default=None
    )

    add_app_parser.add_argument(
        '-s',
        "--settings",
        required=True,
        action='store',
        help='Path to the configuration file.'  
    )

    add_app_parser.add_argument(
        "-f",
        "--format",
        required=False,
        action='store',
        help="Type of Unikraft app. It must be native or through binary compatibility of elfloader. If it is not present, this tool will automatically find its type",
        choices=[f.name for f in AppFormat]
    )

    add_app_parser.add_argument(
        '-a',
        '--app',
        required=False,
        action='store',
        help='App path which will run with Unikraft. This is considered to be the app workspace.',
        default=None
    )

    add_app_parser.add_argument(
        '-t', 
        '--tag', 
        required=True, 
        action='store', 
        help="A new unique tag/description that identifies an app's compilation process."
    )

    add_app_parser.add_argument(
        '-c',
        '--compile',
        required=True,
        action='store',
        help='Kraft compilation command used for the targeted app.'
    )

    list_app_parser = app_sub_parser.add_parser(
        description="List all apps and their compilation process.\n",
        name="list",
        help="List all apps and their compilation process.\n"
    )

    list_app_parser.add_argument(
        '-s',
        "--settings",
        required=True,
        action='store',
        help='Path to the configuration file.'  
    )

    view_app_parser = app_sub_parser.add_parser(
        description="View a specific app and its analysis statistics\n",
        name="view",
        help="View a specific app and its analysis statistics\n"
    )

    view_app_parser.add_argument(
        '-t', 
        '--tags', 
        required=True, 
        nargs="+", 
        help="A list of existing compilation tags (delimited by whitespace) that you want to visualize together"
    )

    view_app_parser.add_argument(
        '-s',
        "--settings",
        required=True,
        action='store',
        help='Path to the configuration file.'  
    )

    delete_app_parser = app_sub_parser.add_parser(
        description="Delete a specific app and its analysis statistics\n",
        name="delete",
        help="Delete a specific app and its analysis statistics\n"
    )

    delete_app_parser.add_argument(
        '-s',
        "--settings",
        required=True,
        action='store',
        help='Path to the configuration file.'  
    )

    delete_app_parser.add_argument(
        '-t',
        "--tag",
        required=True,
        action='store', 
        help="A new unique tag/description that identifies an app's compilation process."
    )

    
    args = parser.parse_args()

    if args.operations == "setup":
        remote_max_version = find_latest_schema_remote_version()

        local_max_version = find_latest_schema_local_version()

        if local_max_version < remote_max_version:
            print(f"{Fore.RED}Your local configuration schema version {local_max_version} is behind the lastest version {remote_max_version}. Git pull the new features.{Fore.RESET}")
            exit(11)
        else:
            print(f"{Fore.GREEN}Client is up to date.{Fore.RESET}")
        
        setup.setup_tool(local_max_version, args.settings)

        exit(0)

    with open(args.settings) as config_fd:
        configs = yaml.safe_load(config_fd)

    global db

    db = pymongo.MongoClient(configs['unikraftCoverageDB']['ip'],
                     username=configs['unikraftCoverageDB']['user'],
                     password=configs['unikraftCoverageDB']['pass'],
                     authSource='admin',
                     authMechanism='SCRAM-SHA-1')
    
    logger = logging.getLogger(LOGGER_NAME)
    logger.setLevel(configs['verbose'] * 10)
    log_fh = logging.FileHandler(configs['logfile'])
    log_format = logging.Formatter("[%(filename)s:%(lineno)s - %(funcName)20s() ] %(message)s")
    log_fh.setFormatter(log_format)
    logger.addHandler(log_fh)

    # print banner
    with open(configs['logfile'],"a") as out:
        out.writelines("----------------------------------------------------------------------------------------------------\n")
        out.writelines(Back.CYAN + Fore.MAGENTA + "-----------------COVERAGE TOOL SESSION-----------------\n" + Fore.RESET + Back.RESET)
        out.writelines(f"-----------------------------------------------{time.ctime()}------------------------------------------")
        out.writelines("----------------------------------------------------------------------------------------------------\n")

    with open(configs['outfile'],"a") as out:
        out.writelines("----------------------------------------------------------------------------------------------------\n")
        out.writelines(Back.CYAN + Fore.MAGENTA + "-----------------COVERAGE TOOL SESSION-----------------\n" + Fore.RESET + Back.RESET)
        out.writelines(f"-----------------------------------------------{time.ctime()}------------------------------------------")
        out.writelines("----------------------------------------------------------------------------------------------------\n")
    
    logger.info(db.server_info())

    if args.operations == "app":

        if args.app_operations in ["add", "compile"]:
            partial = False if args.app_operations == "add" else True
            build_dir = args.app + "/.unikraft/build" if args.build == None else args.build
            app_format = find_app_format(args.app)
            add_app.add_app_subcommand(db, args.app, build_dir, args.tag, app_format, args.compile, configs, partial)

        elif args.app_operations == "submit":
            from CoverityAPI import CoverityAPI
            coverityAPI = CoverityAPI(configs, "/home/karakitay/Desktop/Unikraft-Scanner/src/extern")
            add_app.coverity_submit_step(db, args.archive, args.compile, args.tag, coverityAPI, True)

        elif args.app_operations == "list":
            list_app.list_app_subcommand(db, configs['outfile'])

        elif args.app_operations == "view":
            view_app.view_app_subcommand(db, args.tags, configs['outfile'])

        elif args.app_operations == "delete":
            add_app.panic_delete_compilation(db, args.tag)

        elif args.app_operations == "download":
            add_app.download_coverity_most_recent(db, args.tag, configs, args.download)
        elif args.app_operations == "enrich":
            add_app.enrich_coverity_compilation(db, args.tag, args.results)

    elif args.operations == "status":
        status.status_subcommand(db, configs['outfile'], os.path.abspath(args.kernel))
    else:
        logger.critical("Unknown " + str(args.operations) + " operation")

    db.close()

    print("Operation completed. Check the output and log files configured during the `setup` phase.")

if __name__ == "__main__":
    main()