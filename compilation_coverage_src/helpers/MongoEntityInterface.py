class MongoEntityInterface:
    def to_mongo_dict(self) -> dict:
        raise NotImplementedError("Type has not implemented to_mongo_dict() method")