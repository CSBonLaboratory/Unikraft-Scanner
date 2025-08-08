namespace UnikraftScanner.Client;

public class PluginOptions
{
    public string CompilerPath {get; init;}
    public string PluginPath {get; init;}
    public string InterceptionResultsFilePath_PluginParam {get; init;}
    public bool? RetainExcludedBlocks_PluginParam { get; set;}

    public PluginOptions(
        string compilerPath,
        string pluginPath,
        string interceptionResFilePath
        )
    {
        CompilerPath = compilerPath;
        PluginPath = pluginPath;
        InterceptionResultsFilePath_PluginParam = interceptionResFilePath;

        // true for discovery stage when we try to find all blocks
        // false for triggering stage when we find only blocks that are compiled with the given preprocessor defined symbols
        // be careful, toggling is exclusively your responsiblity depending on the stage, otherwise the null will throw an error along the way
        RetainExcludedBlocks_PluginParam = null;

    }

}
