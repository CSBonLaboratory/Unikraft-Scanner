namespace UnikraftScanner.Client.Symbols;
using System.IO;
using System.Diagnostics;
using UnikraftScanner.Client.Helpers;

public enum PluginStage
{
    Discovery,
    Trigger
}

public record PluginOptions(
    string CompilerPath,
    string PluginPath,
    string PluginName,
    string InterceptionResultsFilePath_External_PluginParam,
    PluginStage Stage_RetainExcludedBlocks_Internal_PluginParam
    );


// Wrapper class that passes arguments and controls the C++ Clang plugin
internal class CompilerPlugin
{
    private ProcessStartInfo _plugin { get; init; }
    private PluginOptions _opts { get; init; }
    public CompilerPlugin(string fullCompilationCommand, PluginOptions opts)
    {
        // given the normal compilation command of a C-family source file, enrich it with clang plugin flags and prepare plugin process

        // ignore the compiler provided
        _plugin = new ProcessStartInfo(opts.CompilerPath);

        // the compiler compatible with the plugin mechanism is already present in the process recipe
        // the fullCommand has also the same compiler (when we are using the unit tests) or the compiler used by Unikraft for normal source compilation
        // so, remove the compiler
        string fullFlags = fullCompilationCommand[fullCompilationCommand.IndexOf(' ')..];

        _opts = opts;

        _plugin.Arguments = fullFlags;

        // // inform clang that it needs to execute our plugin
        // // https://clang.llvm.org/docs/ClangPlugins.html
        // // plugin name is hardcoded here and in the plugin source code (BlockInterceptorPlugin.cxx) at static FrontendPluginRegistry decclaration
        _plugin.Arguments += " -Xclang -load";
        _plugin.Arguments += $" -Xclang {opts.PluginPath}";
        _plugin.Arguments += " -Xclang -plugin";
        _plugin.Arguments += $" -Xclang {opts.PluginName}";

        // // pass where to put results about intercepted plugin blocks after plugin execution
        _plugin.Arguments += $" -Xclang -plugin-arg-{opts.PluginName}";
        _plugin.Arguments += $" -Xclang {opts.InterceptionResultsFilePath_External_PluginParam}";

        // // should plugin parse conditional blocks inside a block that was evaluated as false
        // // for now, do not exclude them since we need to find all conditional blocks
        // // we will exclude them at the second stage when we need to find which ones are compiled
        _plugin.Arguments += $" -Xclang -plugin-arg-{opts.PluginName}";
        _plugin.Arguments += $" -Xclang {opts.Stage_RetainExcludedBlocks_Internal_PluginParam}";

        Console.WriteLine(_plugin.Arguments);
        _plugin.CreateNoWindow = true;
        _plugin.RedirectStandardError = true;
        _plugin.RedirectStandardOutput = true;

        _plugin.UseShellExecute = false;

    }

    
    public ResultUnikraftScanner<string[]> ExecutePlugin()
    {

        var generalInterceptor = Process.Start(_plugin);

        string interceptOut = generalInterceptor.StandardOutput.ReadToEnd();
        string stderr = generalInterceptor.StandardError.ReadToEnd();

        generalInterceptor.WaitForExit();

        if (generalInterceptor.ExitCode != 0)
        {
            return ResultUnikraftScanner<string[]>.Failure(

                new ErrorUnikraftScanner<string>(
                    $"Plugin execution failed with exit code {generalInterceptor.ExitCode}\n{stderr}",
                    ErrorTypes.CompilationInPluginFailure
                )
            );
        }

        Console.WriteLine(_opts.InterceptionResultsFilePath_External_PluginParam);
        string pluginResults = File.ReadAllText(_opts.InterceptionResultsFilePath_External_PluginParam);

        // no compilation blocks, so all code will be compiled no matter what
        if (pluginResults.Length == 0)
            return (ResultUnikraftScanner<string[]>)Array.Empty<string>();

        // blocks are split by an empty line
        return (ResultUnikraftScanner<string[]>)pluginResults.Split("\n\n").Where(e => e != "").ToArray();
    }
}