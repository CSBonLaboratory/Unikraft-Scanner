from defects.AbstractLineDefect import AbstractLineDefect
from defects.CoverityDefect import CoverityDefect
from defects.StaticAnalyzers import StaticAnalyzers



def factory_defect(defect_dict : dict, analyzer_type : StaticAnalyzers) -> AbstractLineDefect:


    # NEW_DEFECT: add here factory instance creation
    
    if defect_dict['Provider'] == StaticAnalyzers.COVERITY.value:
        return CoverityDefect(defect_dict)
    
    raise NotImplementedError("No known provider found")