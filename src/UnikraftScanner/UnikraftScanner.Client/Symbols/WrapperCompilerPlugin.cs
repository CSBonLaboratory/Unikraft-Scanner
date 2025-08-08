namespace UnikraftScanner.Client.Symbols;
using System.IO;
using System.Diagnostics;

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
        _plugin.Arguments += $" -Xclang ConditionalBlockFinder";

        // // pass where to put results about intercepted plugin blocks after plugin execution
        _plugin.Arguments += $" -Xclang -plugin-arg-ConditionalBlockFinder";
        _plugin.Arguments += $" -Xclang {opts.InterceptionResultsFilePath_PluginParam}";

        // // should plugin parse conditional blocks inside a block that was evaluated as false
        // // for now, do not exclude them since we need to find all conditional blocks
        // // we will exclude them at the second stage when we need to find which ones are compiled (evaluated to true)
        _plugin.Arguments += $" -Xclang -plugin-arg-ConditionalBlockFinder";
        _plugin.Arguments += $" -Xclang {opts.RetainExcludedBlocks_PluginParam}";

        Console.WriteLine(_plugin.Arguments);
        _plugin.CreateNoWindow = true;
        _plugin.RedirectStandardError = true;
        _plugin.RedirectStandardOutput = true;

        _plugin.UseShellExecute = false;

    }
    
    public string[]? ExecutePlugin()
    {
        var generalInterceptor = Process.Start(_plugin);

        string interceptOut = generalInterceptor.StandardOutput.ReadToEnd();
        Console.WriteLine(generalInterceptor.StandardError.ReadToEnd());

        generalInterceptor.WaitForExit();

        if (generalInterceptor.ExitCode != 0)
            return null;

        string pluginResults = File.ReadAllText(_opts.InterceptionResultsFilePath_PluginParam);

        // blocks are split by an empty line
        return pluginResults.Split("\n\n").Where(e => e != "").ToArray();
    }
}