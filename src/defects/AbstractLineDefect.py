from helpers.MongoEntityInterface import MongoEntityInterface
from helpers.ViewerInterface import ViewerInterface
from helpers.StatusInterface import StatusInterface


class AbstractLineDefect(MongoEntityInterface, ViewerInterface, StatusInterface):
    line_number : int

    source_path : str

    compilation_tag : str

    id : str

    defects_provider : str

    def to_mongo_dict(self) -> dict:
        raise NotImplementedError("to_mongo_dict() is not implemented !")