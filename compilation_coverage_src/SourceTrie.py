
from __future__ import annotations
from typing import Union
from queue import LifoQueue
from helpers.CSourceDocument import CSourceDocument
from helpers.CSourceNoDocument import CSourceNoDocument

PLACEHOLDER_INFO = "    "
PLACEHOLDER_NODE = "  | "
PLACEHOLDER_LEAF = "  +-- "

class SourceTrie:

    next_nodes : list[SourceTrie] = None
    path_token : str = None
    info : Union[CSourceDocument, CSourceNoDocument] = None
    def __init__(self, token : str) -> None:
        # the root will have absolute path up to the UK_WORKDIR
        self.next_nodes = []
        self.path_token = token
    def add_node(self, srcs_tokens : list[str], src_doc : Union[CSourceDocument, CSourceNoDocument]) -> None:
        
        correct_child = None

        for child in self.next_nodes:
            if child.path_token == srcs_tokens[0]:
                correct_child = child
                break
            
        
        if correct_child == None:
            correct_child = SourceTrie(srcs_tokens[0])
            self.next_nodes.append(correct_child)

        if len(srcs_tokens) > 1:
            correct_child.add_node(srcs_tokens[1:], src_doc)
        else:
            correct_child.info = src_doc
    
    def print_trie_view(self, out_file, compilation_tags : list[str] = [], tabs = 0):

        queue = LifoQueue()

        # put the current SourceTrie node and the current number of tabs (reflecting depth)
        queue.put((self, tabs))

        while not queue.empty():

            current_node, depth = queue.get()

            if current_node.path_token[-2:] == ".c":
                current_node.info.print_view(depth + 1, out_file, compilation_tags)
            else:
                out_file.write(depth * PLACEHOLDER_NODE + current_node.path_token + "\n")

    
            for child_node in current_node.next_nodes:
                queue.put((child_node, depth + 1))

    def print_trie_status(self, out_file, tabs = 0):

        queue = LifoQueue()

        # put the current SourceTrie node and the current number of tabs (reflecting depth)
        queue.put((self, tabs))

        while not queue.empty():

            current_node, depth = queue.get()

            if current_node.path_token[-2:] == ".c":
                current_node.info.print_status(depth + 1, out_file)
            else:
                out_file.write(depth * PLACEHOLDER_NODE + current_node.path_token + "\n")

    
            for child_node in current_node.next_nodes:
                queue.put((child_node, depth + 1))
        

# class SrcsTrieGlobalStatus(SrcsTrie):

#     def add_node(self, srcs_tokens: list[str], src_doc: Union[SourceDocument, SourceNoDocument]) -> None:

#         return super().add_node(srcs_tokens, src_doc)
    
#     def print_source(self, tabs : int, out_file):

        
#         # this means that the source file from from the global status has not been compiled yet
#         if "compiled_stats" not in self.info:
#             ans = (tabs - 2) * PLACEHOLDER_NODE + PLACEHOLDER_LEAF + f"{Fore.RED}{self.info['source_path']}:{Fore.RESET}\n" + ans
        
#         # source is also in the database, it has been compiled before
#         else:
#             coverage = CoverageStatus.PARTIAL
#             for tag, compiled_lines in self.info["compiled_stats"].items():
#                 if compiled_lines == self.info["total_lines"]:
#                     coverage = CoverageStatus.TOTAL
#                     ans += (tabs - 1) * PLACEHOLDER_NODE + PLACEHOLDER_INFO + f"Compilation stats({Fore.GREEN}{tag}{Fore.RESET}): Compiled:{Fore.GREEN}{compiled_lines}{Fore.RESET}, Total: {self.info['total_lines']}, Ratio:{Fore.GREEN}100%{Fore.RESET}\n"
#                 else:
#                     ratio = compiled_lines / self.info["total_lines"]
#                     ans += (tabs - 1) * PLACEHOLDER_NODE + PLACEHOLDER_INFO + f"Compilation stats({Fore.YELLOW}{tag}{Fore.RESET}): Compiled:{Fore.YELLOW}{compiled_lines}{Fore.RESET}, Total: {self.info['total_lines']}, Ratio:{Fore.YELLOW}{'{:.2%}'.format(ratio)}{Fore.RESET}\n"


#             if coverage == CoverageStatus.PARTIAL:
#                 ans = (tabs - 2) * PLACEHOLDER_NODE + PLACEHOLDER_LEAF + f"{Fore.YELLOW}{self.info['source_path']}:{Fore.RESET}\n" + ans
#             elif coverage == CoverageStatus.TOTAL:
#                 ans = (tabs - 2) * PLACEHOLDER_NODE + PLACEHOLDER_LEAF + f"{Fore.GREEN}{self.info['source_path']}:{Fore.RESET}\n" + ans

#         if "triggered_compilations" in self.info:

        
        
