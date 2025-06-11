namespace UnikraftScanner.Client;

public class InterceptorOptions
{
    public string CompilerPath {get; init;}
    public string PluginPath {get; init;}
    public string InterceptionResultsFilePath {get; init;}
    public InterceptorOptions(
        string compilerPath,
        string pluginPath,
        string interceptionResFilePath
        ){

        CompilerPath = compilerPath;
        PluginPath = pluginPath;
        InterceptionResultsFilePath = interceptionResFilePath;

    }

}
