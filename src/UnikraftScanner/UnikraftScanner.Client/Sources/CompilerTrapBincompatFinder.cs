namespace UnikraftScanner.Client.Sources;

using System.Diagnostics;
using UnikraftScanner.Client.Helpers;

public class CompilerTrapBincompatFinder : CompilerTrapFinder
{
    public string KraftfilePath { get; set; }

    public string KraftTarget { get; set; }

    public string UnikraftCompilerPath { get; set; }

    public string UnikraftLinkerPath { get; set; }

    public BincompatHelper TargetAppRuntime { get; set; }

    public static readonly string BuildScriptFileName = "uk_scanner_trap_build.sh";
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


    public override ResultUnikraftScanner<string[]> FindSources()
    {

        var prepResult = TargetAppRuntime.PrepareUnikraftApp();

        if (!prepResult.IsSuccess)
        {
            return ResultUnikraftScanner<string[]>.Failure(prepResult.Error);
        }

        // start the compilation of the whole Unikraft app using makefile commands and using the compiler trap
        string buildScript =
        $@"
        #!/usr/bin/bash
        make distclean
        UK_DEFCONFIG={TargetAppRuntime.KConfigFilePath} make defconfig
        make prepare
        make CC={base.TrapCompilerPath} LD={base.TrapCompilerPath}";

        string buildScriptPath = Path.Combine(AppPath, BuildScriptFileName);

        File.WriteAllText(buildScriptPath, buildScript);

        File.SetUnixFileMode(buildScriptPath, UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.UserRead);

        Process makefileAppBuilder = new Process();
        makefileAppBuilder.StartInfo.FileName = "bash";
        makefileAppBuilder.StartInfo.Arguments = $" {buildScriptPath}";

        makefileAppBuilder.StartInfo.RedirectStandardOutput = true;
        makefileAppBuilder.StartInfo.RedirectStandardError = true;

        makefileAppBuilder.Start();

        string stdout = makefileAppBuilder.StandardOutput.ReadToEnd();
        string stderr = makefileAppBuilder.StandardError.ReadToEnd();

        makefileAppBuilder.WaitForExit();

        if (makefileAppBuilder.ExitCode != 0)
        {
            return ResultUnikraftScanner<string[]>.Failure(

                new ErrorUnikraftScanner<string>(
                    @$"Using make commands to build binary compat app fails:
                    Stderr:{stderr}
                    Stdout:{stdout}",
                    ErrorTypes.MakefileBinCompatProblem
                )
            );
        }

        makefileAppBuilder.Close();

        return File.ReadLines(ResultsFilePath).ToArray();
    }
}
