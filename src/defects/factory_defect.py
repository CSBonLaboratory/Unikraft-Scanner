from defects.AbstractLineDefect import AbstractLineDefect
from defects.CoverityDefect import CoverityDefect

def factory_defect(defect_dict : dict) -> AbstractLineDefect:


    # NEW_DEFECT: add here factory instance creation
    
    if defect_dict['provider'] == CoverityDefect.defect_provider:
        return CoverityDefect(defect_dict)
    
    raise NotImplementedError("No known provider found")