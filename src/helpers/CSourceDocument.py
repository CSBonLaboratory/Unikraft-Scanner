from dataclasses import dataclass
from helpers.CSourceNoDocument import CSourceNoDocument
from helpers.MongoEntityInterface import MongoEntityInterface
from helpers.helpers import CoverageStatus, SourceVersionStrategy, GitCommitStrategy, SHA1Strategy
from helpers.CompilationBlock import CompilationBlock
from bson.objectid import ObjectId
from colorama import Fore
from queue import LifoQueue
from helpers.ViewerInterface import ViewerInterface
from helpers.StatusInterface import StatusInterface
from defects.factory_defect import factory_defect
from defects.AbstractLineDefect import AbstractLineDefect
from io import TextIOWrapper


@dataclass(init=False)
class CSourceDocument(CSourceNoDocument, MongoEntityInterface, ViewerInterface, StatusInterface):

    triggered_compilations : list[ObjectId]
    
    source_version : SourceVersionStrategy

    defects : list[AbstractLineDefect]

    # dict where key is the compilation tag and value is number of compiled lines in this document
    compiled_stats : dict

    def __init__(self, mongo_dict : dict) -> None:
        
        # some attributes are part of a CSourceNoDocument
        super().__init__(mongo_dict)

        if "triggered_compilations" in mongo_dict:
            self.triggered_compilations = mongo_dict["triggered_compilations"]
        else:
            self.triggered_compilations = []
        
        if "git_commit_id" in mongo_dict:
            self.source_version = GitCommitStrategy()
            self.source_version.version_value = mongo_dict["git_commit_id"]
        elif "sha1_id" in mongo_dict:
            self.source_version = SHA1Strategy()
            self.source_version.version_value = mongo_dict["sha1_id"]
        
        self.lib = mongo_dict["lib"]

        if "compiled_stats" in mongo_dict:
            self.compiled_stats = mongo_dict["compiled_stats"]
        else:
            self.compiled_stats = {}

        if 'defects' in mongo_dict:
            self.defects = [factory_defect(defect_dict) for defect_dict in mongo_dict['defects']]
        else:
            self.defects = []


    def print_view(self, tabs : int, out_file : TextIOWrapper, compilation_tags : list[str]) -> None:
         
        from SourceTrie import PLACEHOLDER_INFO, PLACEHOLDER_LEAF, PLACEHOLDER_NODE

        # print compile stats (total line, compiled lines, ratio) for every compilation that involved this file
        complete = True
        ans = ""
        for tag, compiled_lines in self.compiled_stats.items():

            if compiled_lines < self.total_lines:
                ratio = compiled_lines / self.total_lines
                ans += (tabs - 1) * PLACEHOLDER_NODE + PLACEHOLDER_INFO + f"Compilation stats({Fore.RED}{tag}{Fore.RESET}): Compiled:{Fore.RED}{compiled_lines}{Fore.RESET}, Total: {self.total_lines}, Ratio:{Fore.RED}{'{:.2%}'.format(ratio)}{Fore.RESET}\n"
                complete = False
                    
            else:
                ans += (tabs - 1) * PLACEHOLDER_NODE + PLACEHOLDER_INFO + f"Compilation stats({Fore.GREEN}{tag}{Fore.RESET}): Compiled:{Fore.GREEN}{compiled_lines}{Fore.RESET}, Total: {self.total_lines}, Ratio:{Fore.GREEN}100%{Fore.RESET}\n"
        
        # print source name based on results from compile stats, put information at the start of string
        if complete:
            ans = (tabs - 2) * PLACEHOLDER_NODE + PLACEHOLDER_LEAF + f"{Fore.GREEN}{self.source_path}:{Fore.RESET}\n" + ans
        else:
            ans = (tabs - 2) * PLACEHOLDER_NODE + PLACEHOLDER_LEAF + f"{Fore.RED}{self.source_path}:{Fore.RESET}\n" + ans


        ans += (tabs - 1) * PLACEHOLDER_NODE + PLACEHOLDER_INFO + "Library: " + self.lib + "\n"
        ans += (tabs - 1) * PLACEHOLDER_NODE + PLACEHOLDER_INFO + f"Triggered compilations/apps:{self.triggered_compilations}\n"
                    
        # print the type of source version strategy (git commit hash or sha1 etc.)

        ans += (tabs - 1) * PLACEHOLDER_NODE + PLACEHOLDER_INFO + f"{self.source_version.version_key} : {self.source_version.version_value}" + "\n"

        out_file.write(ans)

        compile_blocks : list[CompilationBlock] = self.compile_blocks

        out_file.write((tabs - 1) * PLACEHOLDER_NODE + PLACEHOLDER_INFO + "Compilation blocks\n")

        # print defects occuring in universal lines (outside any compilation block)
        for defect in self.defects:
            if defect.compilation_tag in compilation_tags:
                defect.print_view(tabs - 1, out_file)

        # DFS printing of blocks
        queue = LifoQueue()

        for cb in reversed(compile_blocks):
            if cb.parent_counter == -1:
                queue.put((cb, tabs))

        while not queue.empty():

            current_block, depth = queue.get()

            current_block.print_view(tabs - 1, depth, out_file, compilation_tags)

            for child_local_id in reversed(current_block.children):
                queue.put((compile_blocks[child_local_id], depth + 1))
                

    def print_status(self, tabs : int, out_file : TextIOWrapper) -> None:

        from SourceTrie import PLACEHOLDER_INFO, PLACEHOLDER_LEAF, PLACEHOLDER_NODE

        # print compile stats (total line, compiled lines, ratio) for every compilation that involved this file
        complete = CoverageStatus.NOTHING
        ans = ""
        for tag, compiled_lines in self.compiled_stats.items():

            if compiled_lines < self.total_lines:
                ratio = compiled_lines / self.total_lines
                ans += (tabs - 1) * PLACEHOLDER_NODE + PLACEHOLDER_INFO + f"Compilation stats({Fore.RED}{tag}{Fore.RESET}): Compiled:{Fore.RED}{compiled_lines}{Fore.RESET}, Total: {self.total_lines}, Ratio:{Fore.RED}{'{:.2%}'.format(ratio)}{Fore.RESET}\n"
                if complete != CoverageStatus.TOTAL:
                    complete = CoverageStatus.PARTIAL
                    
            else:
                ans += (tabs - 1) * PLACEHOLDER_NODE + PLACEHOLDER_INFO + f"Compilation stats({Fore.GREEN}{tag}{Fore.RESET}): Compiled:{Fore.GREEN}{compiled_lines}{Fore.RESET}, Total: {self.total_lines}, Ratio:{Fore.GREEN}100%{Fore.RESET}\n"

        # print source name based on results from compile stats, put information at the start of the string
        if complete == CoverageStatus.NOTHING:
            ans = (tabs - 2) * PLACEHOLDER_NODE + PLACEHOLDER_LEAF + f"{Fore.RED}{self.source_path}:{Fore.RESET}\n" + ans

        elif complete == CoverageStatus.PARTIAL:
            ans = (tabs - 2) * PLACEHOLDER_NODE + PLACEHOLDER_LEAF + f"{Fore.YELLOW}{self.source_path}:{Fore.RESET}\n" + ans

        else:
            ans = (tabs - 2) * PLACEHOLDER_NODE + PLACEHOLDER_LEAF + f"{Fore.GREEN}{self.source_path}:{Fore.RESET}\n" + ans
        
        ans += (tabs - 1) * PLACEHOLDER_NODE + PLACEHOLDER_INFO + "Library: " + self.lib + "\n"
        ans += (tabs - 1) * PLACEHOLDER_NODE + PLACEHOLDER_INFO + f"Triggered compilations/apps:{self.triggered_compilations}\n"
                    
        # print the type of source version strategy (git commit hash or sha1 etc.)
        ans += (tabs - 1) * PLACEHOLDER_NODE + PLACEHOLDER_INFO + f"{self.source_version.version_key} : {self.source_version.version_value}" + "\n"

        out_file.write(ans)

        out_file.write((tabs - 1) * PLACEHOLDER_NODE + PLACEHOLDER_INFO + "Compilation blocks\n")

        compile_blocks : list[CompilationBlock] = self.compile_blocks

        # DFS printing of blocks
        queue = LifoQueue()

        for cb in reversed(compile_blocks):
            if cb.parent_counter == -1:
                queue.put((cb, tabs))

        while not queue.empty():

            current, depth = queue.get()

            current.print_block_status(tabs - 1, depth, out_file)

            for child_local_id in reversed(current.children):
                queue.put((compile_blocks[child_local_id], depth + 1))


    def to_mongo_dict(self) -> dict:
        
        ans = {}

        ans["source_path"] = self.source_path
        
        ans["triggered_compilations"] = self.triggered_compilations
        
        ans.update(self.source_version.to_mongo_dict())

        ans["lib"] = self.lib

        ans["compile_blocks"] = [cb.to_mongo_dict() for cb in self.compile_blocks]

        ans["universal_lines"] = self.universal_lines

        ans["total_lines"] = self.total_lines

        ans['compiled_stats'] = self.compiled_stats

        ans['defects'] = [d.to_mongo_dict() for d in self.defects]

        return ans