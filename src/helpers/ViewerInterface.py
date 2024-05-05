class ViewerInterface:
    def print_view(self):
        raise NotImplementedError(f"{type(self)} has not implemented print_view() method")