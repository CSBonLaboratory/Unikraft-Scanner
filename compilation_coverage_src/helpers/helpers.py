
import subprocess
import logging
import os
import re
import logging
import hashlib
from bson.objectid import ObjectId
from dataclasses import dataclass
from typing import Union
from enum import Enum
from helpers.CompilationBlock import CompilationBlock
from helpers.MongoEntityInterface import MongoEntityInterface
from compilers.CompilerInterface import CompilerInterface
from compilers.GCC import GCC

def git_commit_strategy(real_src_path : str) -> str:
    src_tokens = real_src_path.split("/")

    src_sub_path = "/".join(src_tokens[:-1])

    src_name = src_tokens[-1]
    
    proc = subprocess.Popen(f"cd {src_sub_path} && git log -n 1 --pretty=format:%H {src_name}", shell=True, stdout=subprocess.PIPE)

    latest_commit_id_raw, _ = proc.communicate()

    latest_commit_id = latest_commit_id_raw.decode()

    return latest_commit_id

class AppFormat(Enum):
    NATIVE = "native"
    BINCOMPAT = "bincompat"

def find_app_format(app_path : str) -> str:

    if os.path.exists(app_path + "/.unikraft/apps/elfloader"):
        return AppFormat.BINCOMPAT.value
    return AppFormat.NATIVE.value


def remove_comments(cpy_src : str):

    # C comment remover, courtesy to https://gist.github.com/ChunMinChang/88bfa5842396c1fbbc5b

    def replacer(match):
        s = match.group(0)
        if s.startswith('/'):
            return "\n" * s.count( "\n" )
        else:
            return s
    pattern = re.compile(
        r'//.*?$|/\*.*?\*/|\'(?:\\.|[^\\\'])*\'|"(?:\\.|[^\\"])*"',
        re.DOTALL | re.MULTILINE
    )

    with open(cpy_src, "rt") as fdr:
        text = fdr.read()

    with open(cpy_src, "wt") as fdw:
        fdw.write(re.sub(pattern, replacer, text))

    return 

def hash_strategy(real_src_path) -> str:
    sha1 = hashlib.sha1()

    with open(real_src_path, 'rb') as f:
        while True:
            data = f.read(4096)
            if not data:
                break
            sha1.update(data)

    return sha1.hexdigest()




class SourceStatus(Enum):
    NEW = 0
    DEPRECATED = 1
    EXISTING = 2
    UNKNOWN = 3

class CoverageStatus(Enum):
    NOTHING = 0
    PARTIAL = 1
    TOTAL = 2

    
@dataclass
class SourceVersionStrategy(MongoEntityInterface):

    version_value = None

    version_key = None

    def apply_strategy(self, real_src_path : str):
        pass

    def to_mongo_dict(self) -> dict:
        return {self.version_key : self.version_value}

@dataclass(init=False)
class GitCommitStrategy(SourceVersionStrategy):

    version_key : str = "git_commit_id"
    version_value : str = None
    
    def __init__(self) -> None:
        self.version_key = "git_commit_id"

    def apply_strategy(self, real_src_path: str):
        self.version_value = git_commit_strategy(real_src_path)
        
    
    def to_mongo_dict(self) -> dict:
        return super().to_mongo_dict()

    
@dataclass(init=False)
class SHA1Strategy(SourceVersionStrategy):

    version_key : str = "sha1_id"
    version_value : str = None

    def __init__(self) -> None:
        self.version_key = "sha1_id"

    def apply_strategy(self, real_src_path: str):
        self.version_value = hash_strategy(real_src_path)
    
    def to_mongo_dict(self) -> dict:
        return super().to_mongo_dict()


def get_source_version_info(real_src_path : str) -> SourceVersionStrategy:

    '''
    Uses the most apropriate hashing strategy for versioning the file.

    Firstly, it tries to get the latest git commit id of the file
    Otherwise, hashes the file contents using sha-1.

    Args:
        real_src_path(str): Absolute path of the source file

    Returns:
        A SourceVersionStrategy instance for this file
    '''

    logger = logging.getLogger(__name__)

    latest_commit_id = git_commit_strategy(real_src_path)
    
    if latest_commit_id != "":
        version = GitCommitStrategy()
        version.version_value = latest_commit_id

        return version

    logger.warning(f"{real_src_path} is not part of a valid git repository or submodule. Defaulting to SHA1 strategy ...")
    
    version = SHA1Strategy()
    version.apply_strategy(real_src_path)
    return version


def trigger_compilation_blocks(activation_cmd : str) -> list[int]:

    '''
    Opens a shell process that executes the compilation command of a source file.

    Args:
        activation_cmd(str): Compilation command
    
    Returns:
        A list of indexes of compilation blocks which were activated after rerunning the command.
    '''

    logger = logging.getLogger(__name__)

    proc = subprocess.Popen(activation_cmd, shell=True, stderr = subprocess.PIPE)

    _, warnings_raw = proc.communicate()
    
    warnings = warnings_raw.decode()

    # TODO: Debug only. Remove in near future
    f = open("dorel","a")
    f.write(warnings)
    f.close()

    # return the indexes of compilation blocks found in warning directives
    activated_blocks = [int(block_match) for block_match in re.findall("warning: #warning COMPILATION_COVERAGE_([0-9]+)", warnings)]

    logger.debug(f"Compilation blocks triggered are {activated_blocks}")

    return activated_blocks


def find_real_source_file(src_path : str, app_build_dir : str, lib_name : str) -> str:

    logger = logging.getLogger(__name__)

    # copy the source so that after the code instrumentation of the original files, we swap back to the starting version
    # something like file swaping

    src_path_tokens = src_path.split('/')

    src_file_name = src_path_tokens[-1]

    src_parent_path = "/".join(src_path_tokens[ : -1])

    # in case there are some files like namemap.awk>.c
    # then src_file_name whould also include intermediate extension that might dissapear during compilation 
    src_file_name_no_extension = src_file_name.split(".")[0]

    # pure src file name with only .c extension for cases like namemap.awk>.c
    src_file_name_c_extension = src_file_name_no_extension + ".c"

    possible_src_locations = []

    possible_src_locations.append(
        (
            src_path,
            "Found the original src file in the path specified by make print-srcs"
        )
    )
    possible_src_locations.append(
        (
            src_parent_path + "/" + src_file_name_c_extension,
            "Found src file without intermediate extensions in the path specified by make print-srcs"
        )
    )
    possible_src_locations.append(
        (
            app_build_dir + "/" + lib_name + src_file_name,
            "Found original src file in the build directory"
        )
    )
    possible_src_locations.append(
        (
            app_build_dir + "/" + lib_name + src_file_name_c_extension,
            "Found src file without intermediate extensions in the build directory"
        )
    )

    real_src_path = None

    for (location, message) in possible_src_locations:
        if os.path.isfile(location):
            real_src_path = location
            logger.debug(message)
            logger.debug(f"Real path {real_src_path} VS Input path {src_path}")
            break

    if real_src_path == None:
        logger.critical(f"No source file found for {src_path}. Skiping this source file")
        return None
    
    return real_src_path


def instrument_source(parsed_compilation_blocks : list[CompilationBlock], src_path : str):

    '''
    Add a #warning directive at the start of every compilation block so that we can see what blocks are triggered during a compilation.

    This operation modifies the original source files. Do not forget to restore them using a prior clone ! 

    Args:
        parsed_compilation_blocks(list[CompilationBlock]): List of compilation blocks found in the source file
        src_path(str): Absolute path of the source file

    Returns:
        None
    '''

    copy_src_fd = open(src_path, 'r')

    instrumented_code = []

    lines = copy_src_fd.readlines()

    for i in range(len(lines)):
        instrumented_code.append(lines[i])
        
        current_block = [block for block in parsed_compilation_blocks if block.start_line == i]

        if current_block != []:
            instrumented_code.append(f"#warning COMPILATION_COVERAGE_{current_block[0].block_counter}\n")
    
    copy_src_fd.close()

    instr_copy_src_fd = open(src_path, "r+")

    instr_copy_src_fd.writelines(instrumented_code)

    instr_copy_src_fd.close()



def get_source_compilation_command(app_build_dir : str, lib_name : str, real_src_path : str) -> str | None:

    '''
    Get compilation command from the correct *.o.cmd file of the source file.

    Args:
        app_build_dir(str): Absolute path of the build directory of the app
        lib_name(str): Name of the subfolder in the build directory where the *.o.cmd file should be
        real_src_path(str): Absolute path of the source file

    Returns:
        The compilation command as a string or None if the search was not successful.
    '''

    # the source is a c file, we need .o.cmd extension, but there might be files that do not respect the naming convention
    # iterate through .o.cmd files

    src_file_name = real_src_path.split("/")[-1]

    logger = logging.getLogger(__name__)

    for compile_command_file in os.listdir(f"{app_build_dir}/{lib_name}"):

        if src_file_name[:-2] in compile_command_file and ".o.cmd" in compile_command_file:
            
            logger.debug(f"Searching compilation command for {src_file_name} in {compile_command_file}")

            cmd_file_fd = open(f"{app_build_dir}/{lib_name}/{compile_command_file}", "r")

            # ignore the "" from the start of the command
            make_command = cmd_file_fd.readline()[2:]

            make_tokens = make_command.split()

            try:
                gcc_source_flag_idx = make_tokens.index("-c")
                if real_src_path == make_tokens[gcc_source_flag_idx + 1]:
                    cmd_file_fd.close()
                    return make_command
                else:
                    logger.debug(f"-c flag found but source is not correct: {real_src_path} VS {make_tokens[gcc_source_flag_idx + 1]}")
                    cmd_file_fd.close()
            except:
                logger.debug("-c flag not found. Continue...")
                cmd_file_fd.close()
        
    return None

def try_compilers_for_src_path(command : str, o_cmd_file_abs_path : str) -> str | None:


    # NEW_COMPILER: add new instance to the list so that we can enrich the search for compilation command
    # compiler abstract should be singleton and implement CompilerInterface 
    supported_compilers : list[CompilerInterface] = [ GCC() ]

    for compiler in supported_compilers:
        src_path = compiler.find_source_file(command, o_cmd_file_abs_path)

        if src_path != None:
            return src_path
        
    return None