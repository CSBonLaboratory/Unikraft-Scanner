#!/usr/bin/python3

import argparse
import pymongo
import logging
import os
import time
from colorama import Fore, Back
from enum import Enum

DATABASE = "unikraft"
SOURCES_COLLECTION = "sources"
COMPILATION_COLLECTION = "compilations"

import add_app
import list_app
import view_app
import status

default_log_file = "./coverage_logs.log"
default_out_file = "./coverage_out.ansi"
saved_verbose = None
saved_logfile = None
saved_outfile = None

db = pymongo.MongoClient("mongodb://localhost:27017/")

def main():

    parser = argparse.ArgumentParser(
        description="Compilation coverage tool for Unikraft. To be used with Coverity to detect new unscanned code regions"
        )

    
    subparser = parser.add_subparsers(dest='operations', help='Available operations')
    
    init_parser = subparser.add_parser(
        description="Initialize log stream and output stream of the tool",
        name="init",
        help="Initialize log stream and output stream of the tool"
    )

    init_parser.add_argument(
        "-l",
        "--logfile",
        required=False,
        action="store",
        help="Log file path. If not specified, logs will be printed at coverage_logs.txt in the current path",
        default=default_log_file,
        type=str
    )

    init_parser.add_argument(
        '-v',
        '--verbose', 
        required=False, 
        action='store', 
        help='Verbose mode, 5 = CRITICAL, 1 =  DEBUG. Default is CRITICAL', 
        choices=range(1,6), 
        default=5, 
        type=int
    )

    init_parser.add_argument(
        '-o',
        '--outfile', 
        required=False, 
        action='store', 
        help='Where to put output. Default is coverage_out.txt in the current path', 
        default=default_out_file, 
        type=str
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
        "-p",
        "--path",
        required=False,
        action="store",
        help="Absolute path from which source files will be searched recursively for showing compilation statistics. Default value is UK_WORKDIR",
        default=os.environ["UK_WORKDIR"]
    )
    
    app_sub_parser = app_parser.add_subparsers(dest="app_operations", help="Available app-related operations")

    add_app_parser = app_sub_parser.add_parser(
        description="Register a new app compilation process for static analysis",
        name="add",
        help="Register a new app compilation process for static analysis"
    )

    add_app_parser.add_argument(
        '-b',
        '--build',
        required=False,
        action='store',
        help='Build directory of the Unikraft application. Default value is the build directory in the app workspace',
        default=None
    )

    add_app_parser.add_argument(
        "-f",
        "--format",
        required=True,
        action='store',
        choices=["native", "bincompat"]
    )

    add_app_parser.add_argument(
        '-a',
        '--app',
        required=False,
        action='store',
        help='App path which will run with Unikraft',
        default=None
    )

    add_app_parser.add_argument(
        '-t', 
        '--tag', 
        required=True, 
        action='store', 
        help="A new unique tag/description that identifies an app's compilation process"
    )

    list_app_parser = app_sub_parser.add_parser(
        description="List all apps and their compilation process",
        name="list",
        help="List all apps and their compilation proces"
    )

    view_app_parser = app_sub_parser.add_parser(
        description="View a specific app and its analysis statistics",
        name="view",
        help="View a specific app and its analysis statistics"
    )

    view_app_parser.add_argument(
        '-t', 
        '--tags', 
        required=True, 
        nargs="+", 
        help="A list of existing compilation tags (delimited by whitespace) that you want to visualize together"
    )

    delete_app_parser = app_sub_parser.add_parser(
        description="Delete a specific app and its analysis statistics",
        name="delete",
        help="Delete a specific app and its analysis statistics"
    )

    args = parser.parse_args()
    
    if args.operations == "init":
        with open("./cache.json", "w") as conf:
            import json
            info = json.dumps(
                {
                    "logfile" : args.logfile,
                    "outfile" : args.outfile,
                    "verbose" : args.verbose
                },
                indent=4
            )
            conf.write(info)
        return
    else:
        with open("./cache.json", "r") as conf:
            import json
            info = json.load(conf)
            saved_verbose = info["verbose"]
            saved_logfile = info["logfile"]
            saved_outfile = info["outfile"]
        


    # config logging
    FORMAT = "[%(filename)s:%(lineno)s - %(funcName)20s() ] %(message)s"
    logging.basicConfig(level=saved_verbose * 10, format=FORMAT, filename=saved_logfile)

    # print banner
    with open(saved_outfile,"a") as out:
        out.writelines("----------------------------------------------------------------------------------------------------\n")
        out.writelines(Back.CYAN + Fore.MAGENTA + "-----------------COVERAGE TOOL SESSION-----------------\n" + Fore.RESET + Back.RESET)
        out.writelines(f"-----------------------------------------------{time.ctime()}------------------------------------------")
        out.writelines("----------------------------------------------------------------------------------------------------\n")
    

    if args.operations == "app":

        if args.app_operations == "add":
            build_dir = args.build if args.build != None else args.app + "/build"
            add_app.add_app_subcommand(args.app, build_dir, args.tag)

        if args.app_operations == "list":
            list_app.list_app_subcommand(saved_outfile)

        if args.app_operations == "view":
            view_app.view_app_subcommand(args.tags, saved_outfile)

    elif args.operations == "status":
        status.status_subcommand(saved_outfile, args.path)
    else:
        logging.critical("Unknown " + str(args.operations) + " operation")

    

if __name__ == "__main__":

    
    
    main()