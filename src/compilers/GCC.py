from compilers.CompilerInterface import CompilerInterface
import logging

logger = logging.getLogger(__name__)
class GCC(CompilerInterface):


    # singleton
    def __new__(cls) -> None:
        if not hasattr(cls, 'instance'):
            cls.instance = super(GCC, cls).__new__(cls)
        return cls.instance
    

    def find_source_file(self, command: str, o_cmd_file : str) -> str | None:
        
        command_tokens = command.split()

        if "gcc" not in command_tokens:
            logger.debug(f"No gcc token found {o_cmd_file}")
            return None
        
        try:
            gcc_source_flag_idx = command_tokens.index("-c")

            src_path = command_tokens[gcc_source_flag_idx + 1]

            # ignore this source if a C compiler is used to compile a non-C source
            if src_path[-2 : ] != ".c":
                logger.debug(f"Ignore. C compiler for non-C source {src_path} in {o_cmd_file}")
                return None

        # there is no -c flag to specify the source
        except ValueError:
            logger.debug(f"Ignore. No -c flag for source provided in {o_cmd_file}")
            return None
        
        return src_path


