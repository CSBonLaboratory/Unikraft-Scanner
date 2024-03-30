class StatusInterface:
    def print_status(self):
        raise NotImplementedError(f"{type(self)} has not implemented print_status() method")