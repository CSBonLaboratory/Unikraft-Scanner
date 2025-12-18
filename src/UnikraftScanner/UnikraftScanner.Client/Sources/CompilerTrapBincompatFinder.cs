namespace UnikraftScanner.Client.Sources;

using System.Diagnostics;
using UnikraftScanner.Client.Helpers;

public class CompilerTrapBincompatFinder : CompilerTrapFinder
{
    public string KraftfilePath { get; init; }
    public string KraftTarget { get; init; }
    public string UnikraftCompilerPath { get; init; }
    public string UnikraftLinkerPath { get; init; }
    public BincompatHelper TargetAppRuntime { get; init; }
    public static readonly string ElfBuildScriptFileName = "uk_scanner_trap_build.sh";
    private const string relativeScriptPath = ".unikraft/apps/elfloader/scripts";
    private const string relativeScriptCwdExecutionPath = ".unikraft/apps/elfloader";

    public CompilerTrapBincompatFinder(
        string trapCompilerPath,
        string appPath,
        string resultsFilePath,
        string kraftfilePath,
        string kraftTarget,
        string hostCompilerPath,
        string hostLinkerPath,
        BincompatHelper targetAppRuntime
        ) : base(trapCompilerPath, appPath, resultsFilePath)
    {
        KraftfilePath = kraftfilePath;

        KraftTarget = kraftTarget;

        UnikraftCompilerPath = hostCompilerPath;

        UnikraftLinkerPath = hostLinkerPath;

        TargetAppRuntime = targetAppRuntime;
    }


    public override ResultUnikraftScanner<List<string>> FindSources()
    {

        var prepResult = TargetAppRuntime.PrepareUnikraftApp();

        if (!prepResult.IsSuccess)
        {
            return ResultUnikraftScanner<List<string>>.Failure(prepResult.Error);
        }

        // start the compilation of the whole Unikraft app using makefile commands and using the compiler trap
        string buildScript =
        $@"
        #!/usr/bin/bash
        make CC={base.TrapCompilerPath} LD={base.TrapCompilerPath} distclean
        UK_DEFCONFIG={TargetAppRuntime.KConfigFilePath} make CC={base.TrapCompilerPath} LD={base.TrapCompilerPath} defconfig
        make CC={base.TrapCompilerPath} LD={base.TrapCompilerPath} prepare
        make CC={base.TrapCompilerPath} LD={base.TrapCompilerPath}";

        string elfBuildScriptPath = Path.Combine(AppPath, relativeScriptPath, ElfBuildScriptFileName);

        Directory.SetCurrentDirectory(Path.Combine(AppPath, relativeScriptCwdExecutionPath));

        File.WriteAllText(elfBuildScriptPath, buildScript);

        File.SetUnixFileMode(elfBuildScriptPath, UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.UserRead);

        Process elfScriptAppBuilder = new Process();
        elfScriptAppBuilder.StartInfo.FileName = "bash";
        elfScriptAppBuilder.StartInfo.Arguments = $" {elfBuildScriptPath}";

        elfScriptAppBuilder.StartInfo.RedirectStandardOutput = true;
        elfScriptAppBuilder.StartInfo.RedirectStandardError = true;

        elfScriptAppBuilder.Start();

        string stdout = elfScriptAppBuilder.StandardOutput.ReadToEnd();
        string stderr = elfScriptAppBuilder.StandardError.ReadToEnd();

        elfScriptAppBuilder.WaitForExit();

        if (elfScriptAppBuilder.ExitCode != 0)
        {
            return ResultUnikraftScanner<List<string>>.Failure(

                new ErrorUnikraftScanner<string>(
                    @$"Using make commands to build binary compat app fails:
                    Stderr:{stderr}
                    Stdout:{stdout}",
                    ErrorTypes.MakefileBinCompatProblem
                )
            );
        }

        elfScriptAppBuilder.Close();

        string[] srcAndCmdChunks = File.ReadAllText(ResultsFilePath)
            .Split(new string[]{"\n\n"}, StringSplitOptions.RemoveEmptyEntries)
            .ToArray();
        List<string> ans = new();
        foreach(string chunk in srcAndCmdChunks)
        {
            string[] chunkTokens = chunk.Split('\n');
            if(!chunkTokens[0].Equals("None"))
                ans.Add(chunkTokens[0]);
        }

        return ans;
    }
}
