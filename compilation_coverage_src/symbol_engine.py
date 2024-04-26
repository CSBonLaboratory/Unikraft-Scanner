from __future__ import annotations
import logging
from helpers.helpers import remove_comments
from helpers.CompilationBlock import CompilationBlock

def find_children(total_blocks : list[CompilationBlock]):

    '''
    Builds the compilation block hierarchy by adding children for all nodes

    :param total_blocks: All compilation blocks found out by the symbol_engine, do not use on triggered ones.
    '''

    for i in range(len(total_blocks) - 1):

        for j in range(i + 1, len(total_blocks)):
            if total_blocks[i].block_counter == total_blocks[j].parent_counter:
                total_blocks[i].children.append(total_blocks[j].block_counter)
    return


def get_full_symbol_condition(start_line : int, lines : list[str]) -> tuple[str, int]:

    i = start_line

    # first line, get rid of #ifdef, #ifndef or #if
    tokens = lines[i].split()[1:]

    full_tokens = []

    # first line is start of multiline
    if tokens[-1] == "\\":

        # parse first line if it is start of multiline or other lines if they are multiline symbol condition
        while tokens[-1] == "\\":
            for t in tokens[ : -1]:
                full_tokens.append(t)
            
            i += 1

            tokens = lines[i].split()

    # parse final line or first line if it is not multiline
    for t in tokens:
        full_tokens.append(t)

    # get the symbol condition
    full_condition = ""
    for tok in full_tokens:
        full_condition += tok
        full_condition += " "
            
    # remove last white space left by the previous for
    full_condition = full_condition[ : -1]

    return full_condition, i + 1

def find_compilation_blocks_and_lines(src_path : str, do_remove_comments : bool) -> tuple[list[CompilationBlock], int]:

    '''
    Parses the C source file and finds compilation blocks (also cases of nested compilation blocks)

    Args:
        src_path(str): Absolute path of the source file
        do_remove_comments(bool): Whether or not to remove C-style comments from the source file

    Returns:
        A tuple where the first element is a list of CompilationBlock instances and 
        the second is the number of lines that are compiled no matter what symbols are used (universal)
    '''
    from coverage import LOGGER_NAME
    logger = logging.getLogger(LOGGER_NAME)

    if do_remove_comments:
        remove_comments(src_path)
    
    src_fd = open(src_path, 'r')

    lines = src_fd.readlines()

    nested_directives = []

    parsed_compilation_blocks = []

    global_counter = 0

    universal_lines = 0

    i = 0

    while i < len(lines):
        
        if (lines[i].find("#ifdef") != -1 or lines[i].find("#if") != -1 or lines[i].find("#ifndef") != -1):

            current_condition, line_idx_after_condition = get_full_symbol_condition(i, lines)

            if lines[i].find("#ifndef") != -1:
                current_condition = "!(" + current_condition + ")"

            # default value for a block if it does not have a parent block
            parent = -1

            # the current #ifdef is not nested, so no parent
            # create a new sublist in the nested_blocks list
            # the end_line is not known yet
            if len(nested_directives) == 0:
                nested_directives.append([CompilationBlock(
                    {
                    "symbol_condition" : current_condition,
                    "start_line" : i + 1,
                    "end_line" : i,
                    "_local_id" : global_counter,
                    "_parent_id" : -1,
                    "lines" :0
                    })])
            
            # the current #ifdef is nested, this means that we need to create a new sublist in the nested_blocks list
            # the parent is the first element in the previous sublist (a previous #ifdef or #ifndef or #if which we didnt parse their #endif yet)
            # the end_line is not known yet
            else:
                parent = nested_directives[-1][0].block_counter
                nested_directives.append([CompilationBlock(
                    {
                    "symbol_condition" : current_condition,
                    "start_line" : i + 1, 
                    "end_line" : i,
                    "_local_id" : global_counter,
                    "_parent_id" : parent,
                    "lines" : 0
                    })])

            logger.debug(f"Found a conditional compilation block starting at line {i + 1} with local counter {global_counter} and parent counter {parent}")
            global_counter += 1
            
            # jump 1 line if its only 1 line of symbol condition else multiple lines if its multiline
            i = line_idx_after_condition
            continue

        elif (lines[i].find("#elif") != -1 or lines[i].find("#else") != -1):
            
            # only #elif may have multiline symbol condition 
            if lines[i].find("#elif") != -1:
                current_condition, line_idx_after_condition = get_full_symbol_condition(i, lines)
            
            # this is for #else
            else:
                line_idx_after_condition = i + 1
                current_condition = "!(" + nested_directives[-1][-1].symbol_condition + ")"

            # since its an #elif, we need to close (specify the end_line) the previous block in the same sublist as this #elif
            nested_directives[-1][-1].end_line = i + 1
            logger.debug(f"Ending a conditional compilation block starting at {nested_directives[-1][-1].start_line}, ending at {nested_directives[-1][-1].end_line} with local counter {nested_directives[-1][-1].block_counter}")

            # the parent of this #elif is the same parent as the first element in the sublist which is a #if, #ifdef or #ifndef
            # the end_line is not known yet
            parent = nested_directives[-1][0].parent_counter
            nested_directives[-1].append(CompilationBlock(
                {
                "symbol_condition" : current_condition, 
                "start_line" : i + 1,
                "end_line" :i,
                "_local_id" : global_counter,
                "_parent_id" : parent,
                "lines" : 0
                }))

            logger.debug(f"Found conditional compilation block starting at line {i + 1} with local counter {global_counter} and parent counter {parent}")
            global_counter += 1

            i = line_idx_after_condition
            continue

        elif lines[i].find("#endif") != -1:

            ending_block = nested_directives[-1][-1]
            ending_block.end_line = i + 1
            logger.debug(f"Ending a conditional compilation block starting at {ending_block.start_line}, ending at {ending_block.end_line} with local counter {ending_block.block_counter}")
            
            for branch_block in nested_directives[-1]:
                parsed_compilation_blocks.append(branch_block)

            nested_directives.pop()
            
            # we just increment the line since #endif does not have multiline scenarios
            i += 1
            continue
        
        # its a line which might contain code
        elif lines[i].split() != []:
            

            # we still have a unparsed compile block, so the line should be added in its scope not in the global one
            if len(nested_directives) > 0:
                nested_directives[-1][-1].lines += 1
            else:
                universal_lines += 1

            i += 1
            continue
        
        # this is a empty whitespace line
        else:
            i += 1


    src_fd.close()

    parsed_compilation_blocks.sort(key = lambda cb : cb.block_counter)
    return (parsed_compilation_blocks, universal_lines)

            
            
                
                

