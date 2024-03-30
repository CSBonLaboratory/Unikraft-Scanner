class CompilerInterface:

    name : str
    langs : list[str]

    def find_source_file(self, command : str, o_cmd_file : str) -> str | None:
        raise NotImplementedError(f"{type(self)} has not implemented is_compiling() method")