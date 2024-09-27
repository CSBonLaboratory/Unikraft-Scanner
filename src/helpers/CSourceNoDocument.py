from functools import reduce
from colorama import Fore
from queue import LifoQueue
from dataclasses import dataclass
from helpers.CompilationBlock import CompilationBlock
from helpers.StatusInterface import StatusInterface
from io import TextIOWrapper

@dataclass(init=False)
class CSourceNoDocument(StatusInterface):

    source_path : str

    universal_lines : int

    git_file_commit_hash : str

    git_repo_commit_hash : str

    hash_no_git : str

    compile_blocks : list[CompilationBlock] = None

    lib : str = None

    # universal_lines + all lines from compilation blocks
    total_lines : int = None

    def __init__(self, info : dict) -> None:

        self.source_path = info['source_path']
        
        self.universal_lines = info['universal_lines']

        self.git_file_commit_hash = info['git_file_commit_hash']

        self.git_repo_commit_hash = info['git_repo_commit_hash']

        if 'hash_no_git_commit' in info:
            self.hash_no_git = info['hash_no_git_commit']
        
        if 'compile_blocks' in info:
            self.compile_blocks = [CompilationBlock(block_dict) for block_dict in info['compile_blocks']]
        
        if "total_lines" not in info:
            self.total_lines = self.universal_lines
            self.total_lines = reduce(lambda acc, cb : acc + cb.lines, self.compile_blocks, self.total_lines)
            
        else:
            self.total_lines = info["total_lines"]

        if "lib" in info:
            self.lib = info["lib"]

    def print_status(self, tabs : int, out_file : TextIOWrapper):

        from SourceTrie import PLACEHOLDER_NODE, PLACEHOLDER_LEAF

        ans =  (tabs - 2) * PLACEHOLDER_NODE + PLACEHOLDER_LEAF + f"{Fore.RED}{self.source_path}:{Fore.RESET}\n"

        ans += (tabs - 1) * PLACEHOLDER_NODE + f"Total lines : {self.total_lines}\n"

        if self.lib != None:
            ans += (tabs - 1) * PLACEHOLDER_NODE + f"Lib : {self.lib}\n"  

        ans += (tabs - 1) * PLACEHOLDER_NODE + f"Universal lines : {self.universal_lines}\n"

        if self.hash_no_git != None:
            ans += (tabs - 1) * PLACEHOLDER_NODE + f"{Fore.RED}File is not tracked by any git repo. Source file history may be unreliable{Fore.RESET}. SHA-1 : {self.hash_no_git}"
        else:
            ans += (tabs - 1) * PLACEHOLDER_NODE + f"Git commit file hash: {self.git_file_commit_hash}"
            ans += (tabs - 1) * PLACEHOLDER_NODE + f"Git commit repo hash: {self.git_repo_commit_hash}"

        out_file.write(ans)

        # DFS printing of blocks
        queue = LifoQueue()

        for cb in reversed(self.compile_blocks):
            if cb.parent_counter == -1:
                queue.put((cb, tabs))

        while not queue.empty():

            current, depth = queue.get()

            current.print_block_status(tabs - 1, depth, out_file)

            for child_local_id in reversed(current.children):
                queue.put((self.compile_blocks[child_local_id], depth + 1))