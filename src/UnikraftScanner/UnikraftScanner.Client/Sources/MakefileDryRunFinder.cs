namespace UnikraftScanner.Client.Sources;

using UnikraftScanner.Client.Helpers;
using System.Diagnostics;
using System.Text.RegularExpressions;

public class MakefileDryRunFinder : ISourceFinder
{
    public string KraftfilePath { get; init; }
    public string AppPath { get; init; }
    public string KraftTarget { get; init; }
    public BincompatHelper TargetAppRuntime { get; init; }
    public static readonly string buildScriptFileName = "uk_scanner_make_dryrun_build.sh";
    public const string dumpFileName = "uk_scanner_make_dump.txt";
    private const string relativeScriptPath = ".unikraft/apps/elfloader/scripts";
    private const string relativeScriptCwdExecutionPath = ".unikraft/apps/elfloader";
    public string MakefileDumpFilePath {get; init; }
    public MakefileDryRunFinder(
        string appPath,
        string kraftfilePath,
        string kraftTarget,
        BincompatHelper targetAppRuntime
        )
    {

        KraftfilePath = kraftfilePath;

        KraftTarget = kraftTarget;

        AppPath = appPath;

        MakefileDumpFilePath = Path.Combine(AppPath, dumpFileName);

        TargetAppRuntime = targetAppRuntime;
    }
    public ResultUnikraftScanner<List<string>> FindSources()
    {
        var prepResult = TargetAppRuntime.PrepareUnikraftApp();

        if (!prepResult.IsSuccess)
        {
            return ResultUnikraftScanner<List<string>>.Failure(prepResult.Error);
        }

        Directory.SetCurrentDirectory(Path.Combine(AppPath, relativeScriptCwdExecutionPath));

        string buildScript =
        $@"make distclean 
        echo '~~~~~~~~~~~~~~~~~~~~~~ DEFCONFIG ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~'
        UK_DEFCONFIG={TargetAppRuntime.KConfigFilePath} make defconfig
        echo '++++++++++++++++++++++ PREPARE ++++++++++++++++++++++++++++++++++++++++'
        make -n prepare
        echo '---------------------- MAIN ---------------------------------------------------'
        make -n -j $(nproc)";

        string buildScriptPath = Path.Combine(AppPath, relativeScriptPath, buildScriptFileName);

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
            return ResultUnikraftScanner<List<string>>.Failure(

                new ErrorUnikraftScanner<string>(
                    @$"Using make commands to build binary compat app fails:
                    Stderr:{stderr}
                    Stdout:{stdout}",
                    ErrorTypes.MakefileBinCompatProblem
                )
            );
        }

        File.WriteAllText(MakefileDumpFilePath, stdout);

        makefileAppBuilder.Close();

        List<string> ans = new();
        foreach(string line in File.ReadLines(MakefileDumpFilePath))
        {
            Match m = Regex.Match(
                line,
                @"printf\s+'\s+%-7s\s+%s\\n'\s+'CC'\s+'(?<LibName>[a-zA-Z0-9_.$!-]+)':'\s+(?<ObjFile>[a-zA-Z0-9_.$!-]+)'\s+&&\s+(?<Shell>[a-zA-Z0-9_.$~!/\-]+)\s+(?<CmdFilePath>[a-zA-Z0-9_.$!~/\-]+)"
            );

            if (m.Success)
            {
                string libname = m.Groups["LibName"].Value;
                string objFile = m.Groups["ObjFile"].Value;
        
                string cmdFileData = File.ReadAllText(m.Groups["CmdFilePath"].Value);

                string[] tokens = Regex.Split(cmdFileData, @"\s+");
                string sourceFilePath;
                for(int i = 0; i < tokens.Length; i++)
                {
                    if (tokens[i].Equals("-c"))
                    {
                        ans.Add(tokens[i + 1]);
                        break;
                    }
                }
            }
        }

        return (ResultUnikraftScanner<List<string>>)ans;
    }
}
