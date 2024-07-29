from __future__ import annotations
from dataclasses import dataclass
from helpers.MongoEntityInterface import MongoEntityInterface
from helpers.ViewerInterface import ViewerInterface
from helpers.StatusInterface import StatusInterface
from bson.objectid import ObjectId
from colorama import Fore
from functools import reduce
from io import TextIOWrapper
from defects.AbstractLineDefect import AbstractLineDefect
from defects.factory_defect import factory_defect

@dataclass(init=False)
class CompilationBlock(MongoEntityInterface, ViewerInterface, StatusInterface):

    symbol_condition : str = None
    triggered_compilations : list[ObjectId]
    start_line : int
    end_line : int
    block_counter : int
    parent_counter : int
    lines : int
    children : list[int]

    defects : list[AbstractLineDefect]
    

    def __init__(self, mongo_dict : dict) -> None:
        
        self.symbol_condition = mongo_dict["symbol_condition"]
        
        if "triggered_compilations" in mongo_dict:
            self.triggered_compilations = mongo_dict["triggered_compilations"]
        else:
            self.triggered_compilations = []
        
        self.start_line = mongo_dict["start_line"]
        self.end_line = mongo_dict["end_line"]

        self.block_counter = mongo_dict["_local_id"]
        self.parent_counter = mongo_dict["_parent_id"]
        self.lines = mongo_dict["lines"]

        if "children" in mongo_dict:
            self.children = mongo_dict["children"]
        else:
            self.children = []

        if 'defects' in mongo_dict:
            self.defects = [factory_defect(defect_dict) for defect_dict in mongo_dict['defects']]
        else:
            self.defects = []

    def to_mongo_dict(self) -> dict:

        ans = {}

        ans["symbol_condition"] = self.symbol_condition
        ans["triggered_compilations"] = self.triggered_compilations
        ans["start_line"] = self.start_line
        ans["end_line"] = self.end_line
        ans["_local_id"] = self.block_counter
        ans["_parent_id"] = self.parent_counter
        ans["lines"] = self.lines
        ans["children"] = self.children

        ans['defects'] = [d.to_mongo_dict() for d in self.defects]
    
        return ans
    

    def print_view(self, base : int, depth : int, out_file : TextIOWrapper, compilation_tags : list[str]) -> None:

        from SourceTrie import PLACEHOLDER_INFO, PLACEHOLDER_NODE
        
        # no compilation has ever triggered this compile block
        if self.triggered_compilations == []:
            out_file.write(base * PLACEHOLDER_NODE + (depth - base) * PLACEHOLDER_INFO + Fore.RED + self.symbol_condition + Fore.RESET + "\n")

        # one or more of the chosen compilations have triggered this compile block
        elif reduce(lambda acc, compile_name : acc | True if compile_name in self.triggered_compilations else acc | False, compilation_tags, False):
            out_file.write(base * PLACEHOLDER_NODE + (depth - base) * PLACEHOLDER_INFO + Fore.GREEN + self.symbol_condition + Fore.RESET + "\n")

        # one or more compilations that HAVE NOT BEEN CHOSEN have triggered this compile block
        else:
            out_file.write(base * PLACEHOLDER_NODE + (depth - base) * PLACEHOLDER_INFO + Fore.YELLOW + self.symbol_condition + Fore.RESET + "\n")
                
            
        out_file.write(base * PLACEHOLDER_NODE + (depth - base) * PLACEHOLDER_INFO + f"Start line: {self.start_line}\n")

        out_file.write(base * PLACEHOLDER_NODE + (depth - base) * PLACEHOLDER_INFO + f"End line: {self.end_line}\n")

        out_file.write(base * PLACEHOLDER_NODE + (depth - base) * PLACEHOLDER_INFO + f"Block counter: {self.block_counter}\n")

        out_file.write(base * PLACEHOLDER_NODE + (depth - base) * PLACEHOLDER_INFO + f"Triggered compilations/apps: {self.triggered_compilations}\n")

        for defect in self.defects:
            if defect.compilation_tag in compilation_tags:
                defect.print_view(depth, out_file)
            
        out_file.write(base * PLACEHOLDER_NODE + "\n")



    def print_status(self, base : int, depth : int, out_file : TextIOWrapper) -> None:

        from SourceTrie import PLACEHOLDER_INFO, PLACEHOLDER_NODE

        if self.triggered_compilations == []:
            out_file.write(base * PLACEHOLDER_NODE + (depth - base) * PLACEHOLDER_INFO + Fore.RED + self.symbol_condition + Fore.RESET + "\n")
        else:
            out_file.write(base * PLACEHOLDER_NODE + (depth - base) * PLACEHOLDER_INFO + Fore.GREEN + self.symbol_condition + Fore.RESET + "\n")
        
        out_file.write(base * PLACEHOLDER_NODE + (depth - base) * PLACEHOLDER_INFO + f"Start line: {self.start_line}\n")

        out_file.write(base * PLACEHOLDER_NODE + (depth - base) * PLACEHOLDER_INFO + f"End line: {self.end_line}\n")

        out_file.write(base * PLACEHOLDER_NODE + (depth - base) * PLACEHOLDER_INFO + f"Block counter: {self.block_counter}\n")

        out_file.write(base * PLACEHOLDER_NODE + (depth - base) * PLACEHOLDER_INFO + f"Triggered compilations/apps: {self.triggered_compilations}\n")
            
        out_file.write(base * PLACEHOLDER_NODE + "\n")