// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Options;

namespace Microsoft.C3P
{
    /// <summary>
    /// Main entry point for the C3P command-line tool for compiling and linking
    /// cross-platform cross-mobile-app-framework plugins.
    /// </summary>
    public class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                Arguments parsedArgs = new Arguments();
                if (!ParseArgs(args, parsedArgs) || parsedArgs.ShowHelp ||
                    (parsedArgs.CompilePlatform == null && parsedArgs.LinkTarget == null && parsedArgs.Pack == null))
                {
                    ShowHelp(parsedArgs.ShowHelp);
                    return parsedArgs.ShowHelp ? 0 : 1;
                }

                Log.IsVerboseOutputEnabled = parsedArgs.VerboseConsoleOutput;

                if (parsedArgs.CompilePlatform != null)
                {
                    string[] compilePlatformParts = parsedArgs.CompilePlatform.Split('-');
                    ApiCompiler apiCompiler = ApiCompiler.Create(compilePlatformParts[0]);
                    apiCompiler.SourcePath = parsedArgs.SourcePath;
                    apiCompiler.DebugConfiguration = !parsedArgs.ReleaseConfiguration;
                    apiCompiler.IntermediatePath = parsedArgs.IntermediatePaths.FirstOrDefault();
                    apiCompiler.SkipNativeBuild = parsedArgs.SkipNativeBuild;

                    if (compilePlatformParts.Length == 2)
                    {
                        apiCompiler.Language = compilePlatformParts[1];
                    }

                    apiCompiler.Run();
                }

                if (parsedArgs.LinkTarget != null)
                {
                    PluginLinker pluginLinker = PluginLinker.Create(parsedArgs.LinkTarget);
                    pluginLinker.SourcePath = parsedArgs.SourcePath;
                    pluginLinker.IntermediatePaths = parsedArgs.IntermediatePaths;
                    pluginLinker.OutputPath = parsedArgs.OutputPath;
                    pluginLinker.DebugConfiguration = !parsedArgs.ReleaseConfiguration;
                    pluginLinker.Run();
                }

                if (parsedArgs.Pack != null)
                {
                    PluginLibPackager packager = PluginLibPackager.Create(parsedArgs.Pack);
                    packager.SourcePath = parsedArgs.SourcePath;
                    packager.OutputPath = parsedArgs.OutputPath;
                    packager.Run();
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(ex);
                Console.ResetColor();
                return 1;
            }
            finally
            {
#if DEBUG
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("Press enter to quit.");
                    Console.ResetColor();
                    Console.ReadLine();
                }
#endif
            }
        }

        /// <summary>
        /// Prints basic or detailed usage help to the console.
        /// </summary>
        static void ShowHelp(bool detailed)
        {
            System.Reflection.Assembly currentAssembly = System.Reflection.Assembly.GetExecutingAssembly();
            string cmd = currentAssembly.GetName().Name;
            AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)
                currentAssembly.GetCustomAttribute(typeof(AssemblyTitleAttribute));
            AssemblyDescriptionAttribute descriptionAttribute = (AssemblyDescriptionAttribute)
                currentAssembly.GetCustomAttribute(typeof(AssemblyDescriptionAttribute));
            Version version = currentAssembly.GetName().Version;
            Log.Important($"{titleAttribute.Title} v{version}");
            Log.Message(descriptionAttribute.Description);
            Log.Message("");

            if (!detailed)
            {
                Log.Message($"    {cmd} compile <platform> [-s <source path>] [-i <int. path>] [options]");
                Log.Message("");
                Log.Message($"    {cmd} link <target> [-s <source path>] [-i <int. path> ...] ");
                Log.Message("        [-o <output path>] [options]");
                Log.Message("");
                Log.Message("For more help:");
                Log.Message($"    {cmd} --help");
                Log.Message("");
            }
            else
            {
                Log.IsVerboseOutputEnabled = true;
                Log.Important("Compile APIs");
                Log.Message("The compile phase compiles plugin APIs based on a native code project");
                Log.Message("for one platform.");
                Log.Message("");
                Log.Message($"    {cmd} compile <platform> [-s <source path>] [-i <int. path>] [options]");
                Log.Message("");
                Log.Message("    Platform ............ " + String.Join(" | ", ApiCompiler.SupportedPlatforms));
                Log.Message("    Source path ......... root directory of plugin source code,");
                Log.Message("                          containing plugin.xml and platform-specific subdirs,");
                Log.Message("                          defaults to current directory");
                Log.Message("    Intermediate path ... root directory where compiled APIs are placed,");
                Log.Message("                          defaults to <source path>/build");
                Log.Message("");
                Log.Important("Link Plugins");
                Log.Message("The link phase validates and merges plugin APIs from multiple compiled ");
                Log.Message("platforms and generates a plugin for one target application framework.");
                Log.Message("");
                Log.Message($"    {cmd} link <target> [-s <source path>] [-i <int. path> -i ...] ");
                Log.Message("        [-o <output path>] [options]");
                Log.Message("");
                Log.Message("    Target .............. " + String.Join(" | ", PluginLinker.SupportedTargets));
                Log.Message("    Source path ......... root directory of plugin source code,");
                Log.Message("                          containing plugin.xml and platform-specific subdirs,");
                Log.Message("                          defaults to current directory");
                Log.Message("    Intermediate path ... root directory where compiled APIs are obtained,");
                Log.Message("                          may be specified multiple times,");
                Log.Message("                          defaults to <source path>/build");
                Log.Message("    Output path ......... root directory where built plugins are placed,");
                Log.Message("                          defaults to <source path>/build");
                Log.Message("");
                Log.Important("Options");
                Log.Message("");
                Log.Message("    -n, --nobuild ....... skip build of native-code project");
                Log.Message("                          (assume it was already built separately)");
                Log.Message("    -d, --debug ......... compile code in debug configuration (default)");
                Log.Message("    -r, --release ....... compile code in release configuration");
                Log.Message("    -v, --verbose ....... show verbose console output");
                Log.Message("    -h, --help .......... show this help text");
                Log.Message("");
                Log.Important("Example");
                Log.Message("This example shows the five-step process of compiling and linking a plugin");
                Log.Message("for three platforms and three target app frameworks. Note the ios and windows");
                Log.Message("compile steps require different host operating systems, so they may be");
                Log.Message("performed separately, and then the intermediate apis can be linked together.");
                Log.Message("");
                Log.Verbose("    (On a Mac OS X host)");
                Log.Message($"    cd /Path/To/MyPlugin");
                Log.Message($"    {cmd} compile ios");
                Log.Message("");
                Log.Verbose("    (On a Windows host)");
                Log.Message($"    cd C:\\Path\\To\\MyPlugin");
                Log.Message($"    {cmd} compile android");
                Log.Message($"    {cmd} compile windows");
                Log.Message("");
                Log.Verbose("    (On the same Windows host, accessing the Mac host via a network path)");
                Log.Message($"    {cmd} link cordova -i \\\\mymac\\home\\MyPlugin\\build");
                Log.Message($"    {cmd} link xamarin -i \\\\mymac\\home\\MyPlugin\\build");
                Log.Message($"    {cmd} link reactnative -i \\\\mymac\\home\\MyPlugin\\build");
                Log.Message("");
            }
        }

        /// <summary>
        /// Parses the command-line arguments using the Mono.Options library.
        /// </summary>
        /// <returns>True if arguments were successfully parsed, false if there was a syntax
        /// or semantic error.</returns>
        static bool ParseArgs(string[] args, Arguments parsedArgs)
        {
            // The option parser doesn't support options that don't have a special-char prefix.
            // So add a prefix to the first argument if necessary.
            if (args.Length > 0 && !args[0].StartsWith("-") && !args[0].StartsWith("/"))
            {
                args[0] = '-' + args[0];
            }

            OptionSet options = new OptionSet
            {
                { "compile=", v => parsedArgs.CompilePlatform = v.ToLowerInvariant() },
                { "link=", v => parsedArgs.LinkTarget = v.ToLowerInvariant() },
                { "pack=", v => parsedArgs.Pack = v.ToLowerInvariant() },

                { "s|src=", v => parsedArgs.SourcePath = v },
                { "i|int=", v => parsedArgs.IntermediatePaths.Add(v) },
                { "o|out=", v => parsedArgs.OutputPath = v },

                { "n|nobuild", v => parsedArgs.SkipNativeBuild = true },
                { "d|debug", v => parsedArgs.ReleaseConfiguration = false },
                { "r|release", v => parsedArgs.ReleaseConfiguration = true },
                { "v|verbose", v => parsedArgs.VerboseConsoleOutput = true },
                { "h|?|help", v => parsedArgs.ShowHelp = true },
            };

            List<string> unprocessedArgs;
            try
            {
                unprocessedArgs = options.Parse(args);
            }
            catch (OptionException e)
            {
                Log.Error(e.Message);
                Log.Error("");
                return false;
            }

            if (unprocessedArgs.Count > 0)
            {
                Log.Error("Unrecognized command-line option(s): " + String.Join(" ", unprocessedArgs));
                Log.Error("");
                return false;
            }

            if (parsedArgs.CompilePlatform == PluginInfo.IOSPlatformName && !Utils.IsRunningOnMacOS)
            {
                Log.Error($"Compiling the {PluginInfo.IOSPlatformName} platform is not supported on Windows.");
                Log.Error($"(Plugin APIs compiled on Mac OS X can be linked on Windows.)");
                return false;
            }
            else if (parsedArgs.CompilePlatform == PluginInfo.WindowsPlatformName && Utils.IsRunningOnMacOS)
            {
                Log.Error($"Compiling the {PluginInfo.WindowsPlatformName} platform is not supported on Mac OS X.");
                Log.Error($"(Plugin APIs compiled on Windows can be linked on Mac OS X.)");
                return false;
            }

            if (parsedArgs.CompilePlatform != null &&
                !ApiCompiler.SupportedPlatforms.Contains(parsedArgs.CompilePlatform.Split('-')[0]))
            {
                Log.Error("Unsupported platform: " + parsedArgs.CompilePlatform);
                Log.Error("Available platforms: " + String.Join(", ", ApiCompiler.SupportedPlatforms));
                Log.Error("");
                return false;
            }

            if (parsedArgs.LinkTarget != null &&
                !PluginLinker.SupportedTargets.Contains(parsedArgs.LinkTarget))
            {
                Log.Error("Unsupported target application framework: " + parsedArgs.LinkTarget);
                Log.Error("Available targets: " + String.Join(", ", PluginLinker.SupportedTargets));
                Log.Error("");
                return false;
            }

            if (parsedArgs.SourcePath == null)
            {
                if (parsedArgs.Pack == null)
                {
                    if (File.Exists(Path.Combine(Environment.CurrentDirectory, "plugin.xml")))
                    {
                        parsedArgs.SourcePath = Environment.CurrentDirectory;
                    }
                    else
                    {
                        Log.Error("No plugin.xml file was found in the current directory.");
                        Log.Error("Change to the plugin source directory or use -s to specify the path.");
                        Log.Error("");
                        return false;
                    }
                }
                else
                {
                    if (Directory.Exists(Path.Combine(Environment.CurrentDirectory, "ts")))
                    {
                        parsedArgs.SourcePath = Environment.CurrentDirectory;
                    }
                    else
                    {
                        Log.Error("No C3P lib source code was found in the current directory.");
                        Log.Error("Change to the C3P src/lib directory or use -s to specify the path.");
                        Log.Error("");
                        return false;
                    }
                }
            }

            if (parsedArgs.IntermediatePaths.Count == 0)
            {
                parsedArgs.IntermediatePaths.Add(Path.Combine(parsedArgs.SourcePath, "build"));
            }

            if (parsedArgs.OutputPath == null)
            {
                parsedArgs.OutputPath = Path.Combine(parsedArgs.SourcePath, "build");
            }

            if (parsedArgs.Pack != null && (parsedArgs.CompilePlatform != null || parsedArgs.LinkTarget != null))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Holds options parsed from the command-line arguments.
        /// </summary>
        class Arguments
        {
            public string CompilePlatform { get; set; }
            public string LinkTarget { get; set; }
            public string Pack { get; set; }

            public string SourcePath { get; set; }
            public List<string> IntermediatePaths { get; } = new List<string>();
            public string OutputPath { get; set; }

            public bool SkipNativeBuild { get; set; }
            public bool ReleaseConfiguration { get; set; }
            public bool VerboseConsoleOutput { get; set; }
            public bool ShowHelp { get; set; }
        }
    }
}
