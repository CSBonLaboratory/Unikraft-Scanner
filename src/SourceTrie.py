
from __future__ import annotations
from typing import Union
from queue import LifoQueue
from helpers.CSourceDocument import CSourceDocument
from helpers.CSourceNoDocument import CSourceNoDocument
from coverage import LOGGER_NAME

PLACEHOLDER_INFO = "    "
PLACEHOLDER_NODE = "  | "
PLACEHOLDER_LEAF = "  +-- "

class SourceTrie:

    next_nodes : list[SourceTrie] = None
    path_token : str = None

    # there may be multiple snapshots of the same compiled source file
    # this scenario is found when viewing multiple Unikraft apps with the `app view` command

    info : list[Union[CSourceDocument, CSourceNoDocument]] = None
    def __init__(self, token : str) -> None:
        # the root will have absolute path up to the UK_WORKDIR
        self.next_nodes = []
        self.path_token = token
        self.info = []
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
            correct_child.info.append(src_doc)
    
    def print_trie_view(self, out_file, compilation_tags : list[str] = [], tabs = 0):

        queue = LifoQueue()

        # put the current SourceTrie node and the current number of tabs (reflecting depth)
        queue.put((self, tabs))

        while not queue.empty():

            current_node, depth = queue.get()

            if current_node.path_token[-2:] == ".c":
                for i in current_node.info:
                    i.print_view(depth + 1, out_file, compilation_tags)
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
                for i in current_node.info:
                    i.print_status(depth + 1, out_file)
            else:
                out_file.write(depth * PLACEHOLDER_NODE + current_node.path_token + "\n")

    
            for child_node in current_node.next_nodes:
                queue.put((child_node, depth + 1))

        
        
