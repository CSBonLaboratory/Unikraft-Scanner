
import subprocess
import logging
import os
import re
import hashlib
from enum import Enum
from helpers.CompilationBlock import CompilationBlock
from compilers.CompilerInterface import CompilerInterface
from compilers.GCC import GCC
import urllib.request

    

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

def hash_no_git_commit(real_src_path) -> str:
    sha1 = hashlib.sha1()

    with open(real_src_path, 'rb') as f:
        while True:
            data = f.read(4096)
            if not data:
                break
            sha1.update(data)

    return sha1.hexdigest()



class SourceVersionInfo():
    class SourceStatus(Enum):
        NEW = 0
        EXISTING = 1
        UNKNOWN = 2
    
    status : SourceStatus = SourceStatus.UNKNOWN
    git_file_commit_hash : str = None
    git_repo_commit_hash : str = None

 
class CoverageStatus(Enum):
    NOTHING = 0
    PARTIAL = 1
    TOTAL = 2


    
def get_source_git_commit_hashes(real_src_path : str) -> list[str] | None:

    '''
    Retrieves the lastest git commit hash associated with this file

    Args:
        real_src_path(str): Absolute path of the source file

    Returns:
        A list of 2 elements where the first is the latest git commit of the source file while the second is the latest git commit of the entire repo
    '''

    from coverage import LOGGER_NAME
    logger = logging.getLogger(LOGGER_NAME)

    src_tokens = real_src_path.split("/")

    src_sub_path = "/".join(src_tokens[:-1])

    src_name = src_tokens[-1]

    ans = []
    
    proc = subprocess.Popen(f"cd {src_sub_path} && git log -n 1 --pretty=format:%H {src_name}", shell=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)

    latest_commit_id_raw, err = proc.communicate()

    latest_commit_id = latest_commit_id_raw.decode()

    if proc.returncode != 0:
        logger.critical(f"Error while getting git commit hash for {real_src_path}, results in exit code {proc.returncode}: {err.decode()}")
        return None
    
    logger.debug(f"Got git file commit hash for {real_src_path} : {latest_commit_id}")

    proc.terminate()

    ans.append(latest_commit_id)

    proc = subprocess.Popen(f"cd {src_sub_path} && git log -n 1 --pretty=format:%H ", shell=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)

    repo_latest_commit_raw, err = proc.communicate()

    repo_latest_commit = repo_latest_commit_raw.decode()

    if proc.returncode != 0:
        logger.critical(f"Error while getting latest repo git commit hash, return code {proc.returncode} and error: {err.decode()}")
        return None

    logger.debug(f"Repo latest git commit hash for {real_src_path}: {repo_latest_commit}")

    proc.terminate()

    ans.append(repo_latest_commit)

    return ans




def find_latest_schema_remote_version() -> int:

    response = urllib.request.urlopen("https://github.com/CSBonLaboratory/Unikraft-Scanner/tree/master/src/tool_configs")

    html_tool_configs = response.read().decode(response.headers.get_content_charset())

    latest_version = max([int(v) for v in re.findall(r'config_(\d+).yaml', html_tool_configs)])

    return latest_version

def find_latest_schema_local_version() -> int:

    return max([int(re.findall(r"config_(\d+).yaml", f)[0]) for f in os.listdir("tool_configs/") if os.path.isfile(os.path.join("tool_configs/", f))])


def trigger_compilation_blocks(activation_cmd : str, preproc_log_file : str, src_path : str) -> list[int]:

    '''
    Opens a shell process that executes the compilation command of a source file.

    Args:
        activation_cmd(str): Compilation command

        preproc_log_file(str): Absolute path to a file that will store preprocessor stderr such as warnings and errors

        src_path(str): Absolute path to the analyzed source file
    
    Returns:
        A list of indexes of compilation blocks which were activated after rerunning the command.

    '''
    from coverage import LOGGER_NAME
    logger = logging.getLogger(LOGGER_NAME)

    proc = subprocess.Popen(activation_cmd, shell=True, stderr = subprocess.PIPE)

    _, warnings_raw = proc.communicate()
    
    warnings = warnings_raw.decode()

    # Write C preprocessor warnings. It can be warnings or errors
    f = open(preproc_log_file,"a")
    f.write(f"------------------------------------------------------------ {src_path} -----------------------------------------")
    f.write(warnings)
    f.close()

    # return the indexes of compilation blocks found in warning directives
    activated_blocks = [int(block_match) for block_match in re.findall("warning: #warning COMPILATION_COVERAGE_([0-9]+)", warnings)]

    logger.debug(f"Compilation blocks triggered are {activated_blocks}")

    return activated_blocks


def find_real_source_file(src_path : str, app_build_dir : str, lib_name : str) -> str:


    from coverage import LOGGER_NAME
    logger = logging.getLogger(LOGGER_NAME)

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

    from coverage import LOGGER_NAME
    logger = logging.getLogger(LOGGER_NAME)

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


def prepare_coverity_defects_for_db(defect : dict, compilation_tag : str) -> dict:

    # just remove the / from the beggining of the path, Coverity adds its since the archive submited is considered to be the whole filesystem
    defect['File'] = defect['File'][1 : ]

    # add compilation tag to the defect dict
    defect['compilation_tag'] = compilation_tag

    return defect