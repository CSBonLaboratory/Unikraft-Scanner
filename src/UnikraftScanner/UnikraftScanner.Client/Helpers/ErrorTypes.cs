namespace UnikraftScanner.Client;

public enum ErrorTypes
{
    WrongPreprocessorDirective,
    UnknowDirectiveName,
    CompilationInPluginFailure,
    UnknownCompilerFound,
    CFamillyCompilerNotFound,
    WrongPluginPhase,
    KraftBuildProblem,
    ElfloaderFetchProblem,
    NoFetchWithGit,
    MakefileBinCompatProblem
}
