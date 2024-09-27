from defects.AbstractLineDefect import AbstractLineDefect
from io import TextIOWrapper
from colorama import Back, Fore
from defects.StaticAnalyzers import StaticAnalyzers
class CoverityDefect(AbstractLineDefect):

    impact : str

    coverity_checker : str

    category : str
    
    defect_provider  = StaticAnalyzers.COVERITY

    others : dict

    def __init__(self, cov_dict : dict) -> None:

        super().__init__()

        print(f"\t {cov_dict}")
        self.source_path = cov_dict['File']

        self.id = self.defect_provider.value + "_" + str(cov_dict['CID'])

        self.compilation_tag = cov_dict['compilation_tag']

        if cov_dict['Line Number'] == "Various":
            self.line_number = -1
        else:
            self.line_number = int(cov_dict['Line Number'])

        self.impact = cov_dict['Impact']
        self.category = cov_dict['Category']
        self.coverity_checker = cov_dict['Checker']
        
        del cov_dict['CID']
        del cov_dict['File']
        del cov_dict['Line Number']
        del cov_dict['Impact']
        del cov_dict['Category']
        del cov_dict['Checker']
        del cov_dict['compilation_tag']

        self.others = cov_dict

    def to_mongo_dict(self) -> dict:
        import coverage
        import logging
        logger = logging.getLogger(coverage.LOGGER_NAME)
        
        mongo_dict = {}

        mongo_dict['CID'] = self.id
        mongo_dict['Provider'] = self.defect_provider.value
        mongo_dict['File'] = self.source_path
        mongo_dict['Line Number'] = self.line_number
        mongo_dict['Impact'] = self.impact
        mongo_dict['Category'] = self.category
        mongo_dict['Checker'] = self.coverity_checker
        mongo_dict['compilation_tag'] = self.compilation_tag

        mongo_dict.update(self.others)

        return mongo_dict
    
    def print_view(self, tabs : int, out_file : TextIOWrapper) -> None:

        from SourceTrie import PLACEHOLDER_NODE, PLACEHOLDER_INFO
        
        out_file.write(tabs * PLACEHOLDER_NODE + PLACEHOLDER_INFO + f"{Fore.RED}Provider : {self.defect_provider.value}{Fore.RESET}\n")

        out_file.write(tabs * PLACEHOLDER_NODE + PLACEHOLDER_INFO + f"{Fore.RED}Category : {self.category}{Fore.RESET}\n")

        out_file.write(tabs * PLACEHOLDER_NODE + PLACEHOLDER_INFO + f"{Fore.RED}Impact : {self.impact}{Fore.RESET}\n")

        out_file.write(tabs * PLACEHOLDER_NODE + PLACEHOLDER_INFO + f"{Fore.RED}Line number : {self.line_number}{Fore.RESET}\n")

        out_file.write(tabs * PLACEHOLDER_NODE + PLACEHOLDER_INFO + f"{Fore.RED}Coverity checker : {self.coverity_checker}{Fore.RESET}\n")

        out_file.write("\n")

