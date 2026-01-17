namespace UnikraftScanner.Client.Core;

using Microsoft.Extensions.Configuration;
using System.CommandLine;
using System.CommandLine.Parsing;

/*


Validators =
                                    {
                                        (OptionResult res) =>
                                        {
                                            string value = res.GetValueOrDefault<string>();
                                            if (string.IsNullOrWhiteSpace(value))
                                            {
                                                res.AddError("Path cannot be empty.");
                                                return;
                                            }

                                            if (!Path.IsPathRooted(value))
                                            {
                                                res.AddError("Path must be an absolute path.");
                                                return;
                                            }

                                            if (File.Exists(value) && !Directory.Exists(value))
                                            {
                                                res.AddError("Path is not a directory");
                                            }
                                        }
                                    }
*/


public class ScannerApp
{
    public static void Main(string args)
    {
        var rootCmd = new RootCommand()
        {
            Options =
            {
                new Option<string>("--config")
                {
                    Aliases = {"-c"},
                    Description = "Path to a configuration file required for UnikraftScanner",
                    Required = true,
                }.AcceptLegalFileNamesOnly(),

                new Option<string>("--log")
                {
                    Aliases = {"-l"},
                    Description = "Log level",
                    Required = true,
                }.AcceptOnlyFromAmong(["Trace", "Debug", "Information", "Warning", "Error", "Critical"])
            },
            
            Subcommands =
            {
                new Command("app")
                {

                    Subcommands =
                    {
                        new Command("register")
                        {
                            Description = "Command used to register a new Unikraft app in the static code analysis platform and also perform analysis using various algorithms",
                            Options =
                            {
                                new Option<string>("--app-path")
                                {
                                    Aliases = {"-a"},
                                    Description = "Abosulte path to directory of a Unikraft app that contains a Kraftfile",
                                    Required = true,
                                    
                                    // will need unit test for testing absolute path
                                }.AcceptLegalFilePathsOnly(),

                                // integration test for unique names
                                new Option<string>("--name")
                                {
                                    Aliases = {"-n"},
                                    Description = "Unique name of the scanning process of a Unikraft app. For multiple words use double quotes",
                                    Required = true
                                },

                                // unit test for arch + plat / target 
                                new Option<string?>("--arch")
                                {
                                    Aliases = {"-m"},
                                    Description = "Kraft architecture. Same values used as in `kraft build` command (x86_64/arm64/arm)",
                                    DefaultValueFactory = (ArgumentResult _) => null
                                }.AcceptOnlyFromAmong(["x86_64", "arm64", "arm"]),

                                new Option<string?>("--plat")
                                {
                                    Aliases = {"-p"},
                                    Description = "Kraft platform. Same values as in `kraft build` command (fc/qemu/xen)",
                                    DefaultValueFactory = (ArgumentResult _) => null
                                }.AcceptOnlyFromAmong(["fc", "qemu", "xen"]),

                                new Option<string?>("--target")
                                {
                                    Aliases = {"-t"},
                                    Description = "Target found in the Kraftfile used for building. Same option as in `kraft build`",
                                    DefaultValueFactory = (ArgumentResult _) => null
                                },

                                new Option<string?>("--other")
                                {
                                    Aliases = {"-o"},
                                    Description = "Other flags/options appended to the final underlying `kraft build -g --no-cache --no-update` + your plat_arch/target"
                                },

                                new Option<bool>("--coverity")
                                {
                                    Description = "Use Coverity static code analysis algorithm",
                                    DefaultValueFactory = (ArgumentResult _) => false
                                },
                            },

                            Validators =
                            {
                                (CommandResult cmdRes) =>
                                {
                                    string? arch = cmdRes.GetValue<string?>("--arch");
                                    string? plat = cmdRes.GetValue<string?>("--plat");
                                    string? target = cmdRes.GetValue<string?>("--target");
                                    if(target != null || (arch != null && plat != null))
                                        return;

                                    cmdRes.AddError("Arch and plat must both have values otherwise must use target");
                                }
                            }
                        },

                        new Command("enrich")
                        {
                            Description = "Used to enrich a previously submited registration/analysis with other information from newly added analysis algorithms",
                            Options =
                            {
                                new Option<string>("--name")
                                {
                                    Aliases = {"-n"},
                                    Description = "Unique name of the previously submited registration/scanning process of a Unikraft app. For multiple words use double quotes",
                                    Required = true
                                },

                                new Option<bool>("--coverity")
                                {
                                    Description = "Use Coverity static code analysis algorithm",
                                    DefaultValueFactory = (ArgumentResult _) => false
                                },
                            }
                        }
                    }
                },

                new Command("view")
                {
                    Description = "Print rudimentary view of analysis coverage and vulnerabilty results of a specific app registration",
                    Options =
                    {
                        new Option<string>("--name")
                        {
                            Aliases = {"-n"},
                            Description = "Unique name of the previously submited registration/scanning process of a Unikraft app. For multiple words use double quotes",
                            Required = true
                        },
                    }
                }
            }
        };    
    }
}