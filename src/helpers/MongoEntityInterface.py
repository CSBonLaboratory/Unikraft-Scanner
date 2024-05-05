class MongoEntityInterface:
    def to_mongo_dict(self) -> dict:
        raise NotImplementedError(f"{type(self)} has not implemented to_mongo_dict")