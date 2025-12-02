namespace UnikraftScanner.Client;
using System.Diagnostics;
using UnikraftScanner.Client.Helpers;
public class BincompatHelper
{
    public string KraftfilePath { get; set; }
    public string KraftTarget { get; set; }
    public string? KConfigFilePath { get; set; }
    public string AppPath { get; set; }
    public string BuildScriptFileName = "uk_scanner_build.sh";
    private bool prepTriggered = false;

    public BincompatHelper(string kraftfilePath, string kraftTarget, string appPath, string buildScriptFileName, string? kconfigFilePath = null)
    {
        KraftfilePath = kraftfilePath;
        KraftTarget = kraftTarget;
        AppPath = appPath;
        BuildScriptFileName = buildScriptFileName;
        KConfigFilePath = kconfigFilePath;
    }
    protected bool CheckElfloader(string appPath)
    {
        Directory.SetCurrentDirectory(appPath);
        string elfLoaderPath = Path.Combine(appPath, ".unikraft/apps/elfloader");
        if (!Directory.Exists(elfLoaderPath))
        {
            return false;
        }

        return true;
    }

    protected bool IsFetchedWithGit(string cwd)
    {
        Directory.SetCurrentDirectory(cwd);
        Process gitRepoOrigin = new Process();
        gitRepoOrigin.StartInfo.FileName = "git";
        gitRepoOrigin.StartInfo.Arguments = " rev-parse --show-toplevel";
        gitRepoOrigin.StartInfo.RedirectStandardOutput = true;
        gitRepoOrigin.StartInfo.RedirectStandardError = true;

        gitRepoOrigin.Start();

        string stdout = gitRepoOrigin.StandardOutput.ReadToEnd();
        string stderr = gitRepoOrigin.StandardError.ReadToEnd();

        gitRepoOrigin.WaitForExit();

        if (gitRepoOrigin.ExitCode != 0)
        {
            return false;
        }

        gitRepoOrigin.Close();

        // remove the ending /n
        if (!stdout[..^1].Equals(Directory.GetCurrentDirectory()))
        {
            return false;
        }

        return true;
    }

    // protected bool GitRemoteOrigin(string cwd, string remoteGitOrigin)
    // {
    //     Directory.SetCurrentDirectory(cwd);
    //     Process gitFetchUrl = new Process();
    //     gitFetchUrl.StartInfo.FileName = "git";
    //     gitFetchUrl.StartInfo.Arguments = " config --get remote.origin.url";
    //     gitFetchUrl.StartInfo.RedirectStandardOutput = true;
    //     gitFetchUrl.StartInfo.RedirectStandardError = true;

    //     gitFetchUrl.Start();

    //     string stdout = gitFetchUrl.StandardOutput.ReadToEnd();
    //     string stderr = gitFetchUrl.StandardError.ReadToEnd();

    //     gitFetchUrl.WaitForExit();

    //     if (gitFetchUrl.ExitCode != 0)
    //     {
    //         return false;
    //     }

    //     // remove the ending /n
    //     if (!stdout[..^1].Equals(remoteGitOrigin))
    //     {
    //         return false;
    //     }

    //     gitFetchUrl.Close();

    //     return true;
    // }

    protected ResultUnikraftScanner<bool> CheckFetchUsingGitAndSetup(string elfloaderPath)
    {

        // for app-elfloader
        Directory.SetCurrentDirectory(elfloaderPath);

        if (!IsFetchedWithGit(elfloaderPath))
            return ResultUnikraftScanner<bool>.Failure(
                new ErrorUnikraftScanner<string>($"APP CORE: {elfloaderPath} not fetched using git", ErrorTypes.NoFetchWithGit)
            );

        string symbolicDirectory = Path.Combine(elfloaderPath, "workdir");

        Directory.CreateDirectory(symbolicDirectory);
        Directory.CreateDirectory(Path.Combine(symbolicDirectory, "libs"));

        string fetchedLibsPath = Path.Combine(elfloaderPath, "../../libs");

        string fetchedUnikraftPath = Path.Combine(elfloaderPath, "../../unikraft");

        Directory.SetCurrentDirectory(fetchedLibsPath);

        // for libs
        foreach (string libPathKraft in Directory.GetDirectories(Directory.GetCurrentDirectory()))
        {
            string libName = new DirectoryInfo(libPathKraft).Name;

            if (!IsFetchedWithGit(libPathKraft))
                return ResultUnikraftScanner<bool>.Failure(
                    new ErrorUnikraftScanner<string>($"LIB {libName}: {libPathKraft} not fetched using git", ErrorTypes.NoFetchWithGit)
            );
            
            File.CreateSymbolicLink(Path.Combine(symbolicDirectory, "libs", libName), libPathKraft);
        }

        // for unikraft core
        string unikraftCorePathKraft = Path.Combine(Directory.GetCurrentDirectory(), "../../unikraft");

        if (!IsFetchedWithGit(unikraftCorePathKraft))
            return ResultUnikraftScanner<bool>.Failure(
                    new ErrorUnikraftScanner<string>($"UNIKRAFT BASE: {unikraftCorePathKraft} not fetched using git", ErrorTypes.NoFetchWithGit)
            );

        File.CreateSymbolicLink(Path.Combine(symbolicDirectory, "unikraft"), fetchedUnikraftPath);

        return (ResultUnikraftScanner<bool>)true;
    }

    protected string PromptConfigFile()
    {
        string[] potentialConfigsPaths = new FileInfo(KraftfilePath).Directory
            .GetFiles()
            .Where(f => f.Name.Contains(".config"))
            .Select(f => f.FullName)
            .ToArray();

        int order = 0;
        Console.WriteLine("Choose what config file to be used:");
        foreach (string configFileOption in potentialConfigsPaths)
        {
            Console.WriteLine($"{order}: {configFileOption}");
            order++;
        }

        Console.Write("Option: ");
        int opt = int.Parse(Console.ReadLine());
        Console.Write("\n");

        return potentialConfigsPaths[opt];

    }

    public ResultUnikraftScanner<bool> PrepareUnikraftApp()
    {

        if (prepTriggered)
            return (ResultUnikraftScanner<bool>)true;

        prepTriggered = true;
        
        Directory.SetCurrentDirectory(AppPath);

        // ##target## can be --target <name> / --plat <p> --arch <a> tuple
        const string kraftbuildTemplateCmd = "build -g --no-cache --no-update --no-prompt --log-level trace --log-type basic ##target##";

        Process kraftBuilder = new Process();
        kraftBuilder.StartInfo.FileName = "kraft";
        kraftBuilder.StartInfo.Arguments = $" {kraftbuildTemplateCmd}".Replace("##target##", KraftTarget);
        kraftBuilder.StartInfo.RedirectStandardError = true;

        kraftBuilder.Start();
        
        string stderr = kraftBuilder.StandardError.ReadToEnd();

        kraftBuilder.WaitForExit();

        if (kraftBuilder.ExitCode != 0)
        {
            return ResultUnikraftScanner<bool>.Failure(

                new ErrorUnikraftScanner<string>(
                    @$"Building Unikraft app at {Directory.GetCurrentDirectory()} using command \
                    `{kraftBuilder.StartInfo.Arguments}` exited with code {kraftBuilder.ExitCode}\n{stderr}",
                    ErrorTypes.KraftBuildProblem
                )
            );
        }

        kraftBuilder.Close();


        if (!CheckElfloader(AppPath))
        {
            return ResultUnikraftScanner<bool>.Failure(

                new ErrorUnikraftScanner<string>(
                    @$"Elfloader is not present at {Path.Combine(AppPath, ".unikraft/apps/elfloader")}",
                    ErrorTypes.ElfloaderFetchProblem
                )
            );
        }

        var res = CheckFetchUsingGitAndSetup(Path.Combine(AppPath, ".unikraft/apps/elfloader"));
        if (!res.IsSuccess)
        {
            return res;
        }

        if (KConfigFilePath == null)
        {
            KConfigFilePath = PromptConfigFile();
        }

        return (ResultUnikraftScanner<bool>)true;
    }
}
