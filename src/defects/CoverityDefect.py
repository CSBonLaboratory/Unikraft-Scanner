from defects.AbstractLineDefect import AbstractLineDefect
from io import TextIOWrapper
from colorama import Back
class CoverityDefect(AbstractLineDefect):

    impact : str

    coverity_checker : str

    category : str
    
    defect_provider  = "Coverity"

    others : dict

    def __init__(self, cov_dict : dict) -> None:

        super().__init__()
        
        self.source_path = cov_dict['displayFile']

        self.compilation_tag = cov_dict['compilation_tag']

        if cov_dict['lineNumber'] == "Various":
            self.line_number = -1
        else:
            self.line_number = int(cov_dict['lineNumber'])

        self.impact = cov_dict['displayImpact']
        self.category = cov_dict['displayCategory']
        self.coverity_checker = cov_dict['checker']

        del cov_dict['displayFile']
        del cov_dict['lineNumber']
        del cov_dict['displayImpact']
        del cov_dict['displayCategory']
        del cov_dict['checker']
        del cov_dict['compilation_tag']

        self.others = cov_dict

    def to_mongo_dict(self) -> dict:
        
        mongo_dict = {}

        mongo_dict['provider'] = self.defect_provider
        mongo_dict['displayFile'] = self.source_path
        mongo_dict['lineNumber'] = self.line_number
        mongo_dict['displayImpact'] = self.impact
        mongo_dict['displayCategory'] = self.category
        mongo_dict['checker'] = self.coverity_checker
        mongo_dict['compilation_tag'] = self.compilation_tag

        mongo_dict.update(self.others)

        return mongo_dict
    
    def print_view(self, tabs : int, out_file : TextIOWrapper) -> None:

        from SourceTrie import PLACEHOLDER_NODE, PLACEHOLDER_INFO
        
        out_file.write(tabs * PLACEHOLDER_NODE + PLACEHOLDER_INFO + f"{Back.RED}Provider : {self.defect_provider}{Back.RESET}\n")

        out_file.write(tabs * PLACEHOLDER_NODE + PLACEHOLDER_INFO + f"{Back.RED}Category : {self.category}{Back.RESET}\n")

        out_file.write(tabs * PLACEHOLDER_NODE + PLACEHOLDER_INFO + f"{Back.RED}Impact : {self.impact}{Back.RESET}\n")

        out_file.write(tabs * PLACEHOLDER_NODE + PLACEHOLDER_INFO + f"{Back.RED}Line number : {self.line_number}{Back.RESET}\n")

        out_file.write(tabs * PLACEHOLDER_NODE + PLACEHOLDER_INFO + f"{Back.RED}Coverity checker : {self.coverity_checker}{Back.RESET}\n")

        out_file.write("\n")

