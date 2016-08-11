// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.C3P
{
    /// <summary>
    /// A collection of static utility methods used throughout the tool codebase.
    /// </summary>
    static class Utils
    {
        /// <summary>
        /// Checks if the program is currently executing on Mac OS X.
        /// </summary>
        public static bool IsRunningOnMacOS
        {
            get
            {
                // Note the Mono runtime identifies Mac OS X with PlatformID.Unix rather than PlatformID.MacOSX.
                return Environment.OSVersion.Platform == PlatformID.Unix;
            }
        }

        /// <summary>
        /// Removes files and subdirectories from a directory without removing the directory itself.
        /// </summary>
        /// <param name="directoryPath">Path to the directory.</param>
        /// <param name="keepDirectories">Optional list of top-level subdirectories to keep.</param>
        public static void ClearDirectory(string directoryPath, string[] keepDirectories = null)
        {
            try
            {
                foreach (string subDirectoryPath in Directory.GetDirectories(directoryPath))
                {
                    if (keepDirectories == null || !keepDirectories.Contains(Path.GetFileName(subDirectoryPath)))
                    {
                        Directory.Delete(subDirectoryPath, true);
                    }
                }

                foreach (string filePath in Directory.GetFiles(directoryPath))
                {
                    File.Delete(filePath);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new IOException("Failed to clear directory: " + directoryPath, ex);
            }
            catch (IOException ex)
            {
                throw new IOException("Failed to clear directory: " + directoryPath, ex);
            }
        }

        /// <summary>
        /// Ensures that a directory path ends with a separator character (backslash or slash
        /// depending on the current platform.)
        /// </summary>
        public static string EnsureTrailingSlash(string path)
        {
            if (String.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (path[path.Length - 1] != Path.DirectorySeparatorChar)
            {
                path += Path.DirectorySeparatorChar;
            }

            return path;
        }

        /// <summary>
        /// Executes a command.
        /// </summary>
        /// <param name="commandPath">Path to the command executable.</param>
        /// <param name="commandArguments">Arguments to the command.</param>
        /// <param name="workingDirectory">Working directory.</param>
        /// <param name="timeout">Time after which a TimeoutException will be thrown
        /// if the command does not finish in time.</param>
        /// <param name="writeOutputLine">Optional callback to handle stdout lines. By default the
        /// lines are written to the console.</param>
        /// <param name="writeErrorLine">Optional callback to handle stderr lines. By default the
        /// lines are written to the console.</param>
        public static void Execute(
            string commandPath,
            string commandArguments,
            string workingDirectory,
            TimeSpan timeout,
            Action<string> writeOutputLine = null,
            Action<string> writeErrorLine = null)
        {
            if (writeOutputLine == null)
            {
                writeOutputLine = line => Log.Message("{0}", line);
            }
            if (writeErrorLine == null)
            {
                writeErrorLine = line => Log.Message("{0}", line);
            }

            string arguments = commandArguments;
            string commandName = Path.GetFileName(commandPath);
            string commandExt = Path.GetExtension(commandPath);
            if (commandExt == ".bat" || commandExt == ".cmd")
            {
                if (Utils.IsRunningOnMacOS)
                {
                    string commandDirectory = Path.GetDirectoryName(commandPath);
                    if (String.IsNullOrEmpty(commandDirectory))
                    {
                        commandDirectory = Path.GetFullPath(workingDirectory);
                    }

                    commandName = Path.GetFileNameWithoutExtension(commandName);
                    commandPath = Path.Combine(commandDirectory, commandName);
                }
                else
                {
                    commandArguments = "/c \"" + commandPath + "\" " + commandArguments;
                    commandPath = Path.Combine(Environment.SystemDirectory, "cmd.exe");
                }
            }

            Log.Important("{0} {1}", commandName, arguments);

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.FileName = commandPath;
            startInfo.Arguments = commandArguments;
            startInfo.WorkingDirectory = Path.GetFullPath(workingDirectory);
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            Process process = Process.Start(startInfo);
            process.OutputDataReceived += (sender, args) => { if (args.Data != null) writeOutputLine(args.Data); };
            process.BeginOutputReadLine();
            process.ErrorDataReceived += (sender, args) => { if (args.Data != null) writeErrorLine(args.Data); };
            process.BeginErrorReadLine();

            int timeoutMs = (int)timeout.TotalMilliseconds;
            bool completed = process.WaitForExit(timeoutMs);
            if (!completed)
            {
                process.Kill();
                throw new TimeoutException(String.Format(
                    "Process timed out after {0} ms: {1} {2}", timeoutMs, commandName, arguments));
            }

            // On Windows, calling WaitForExit() with no timeout ensures all stdout messages are processed.
            process.WaitForExit();

            // On Mac, there doesn't seem to be a reliable way to wait for stdout to flush.
            if (Utils.IsRunningOnMacOS)
            {
                System.Threading.Thread.Sleep(1000);
            }

            if (process.ExitCode != 0)
            {
                // Wait for a bit to allow all the process output to flush.
                System.Threading.Thread.Sleep(1000);

                throw new ApplicationException(String.Format(
                    "Process exited with code {0}: {1} {2}", process.ExitCode, commandName, arguments));
            }
        }

        /// <summary>
        /// Builds a project with MSBuild or Mono XBuild.
        /// </summary>
        /// <param name="projectDirectoryPath">Path to the project directory.</param>
        /// <param name="configuration">Project configuration to build, such as Debug or Release.</param>
        /// <param name="platform">Optional project platform to build, such as x86, x64, or ARM.</param>
        /// <param name="writeOutputLine">Optional callback to handle stdout lines. By default the
        /// lines are written to the console.</param>
        /// <param name="writeErrorLine">Optional callback to handle stderr lines. By default the
        /// lines are written to the console.</param>
        public static void BuildProject(
            string projectDirectoryPath,
            string configuration,
            string platform = null,
            Action<string> writeOutputLine = null,
            Action<string> writeErrorLine = null)
        {
            string buildToolPath;
            if (Utils.IsRunningOnMacOS)
            {
                buildToolPath = "/Library/Frameworks/Mono.framework/Versions/Current/bin/xbuild";
                if (!File.Exists (buildToolPath))
                {
                    throw new FileNotFoundException ("XBuild tool not found at " + buildToolPath);
                }
            }
            else
            {

                buildToolPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    @"MSBuild\14.0\Bin\MSBuild.exe");
                if (!File.Exists(buildToolPath))
                {
                    throw new FileNotFoundException("MSBuild 14 not found at " + buildToolPath);
                }
            }

            string arguments = "/p:Configuration=" + configuration;
            if (platform != null) {
                arguments += " /p:Platform=" + platform;
            }

            Utils.Execute(
                buildToolPath,
                arguments,
                projectDirectoryPath,
                TimeSpan.FromSeconds(120),
                writeOutputLine,
                writeErrorLine);
        }

        /// <summary>
        /// Builds a project with MSBuild or Mono XBuild and detects the assemblies that are
        /// referenced during the build.
        /// </summary>
        /// <param name="projectDirectoryPath">Path to the project directory.</param>
        /// <param name="configuration">Project configuration to build, such as Debug or Release.</param>
        /// <param name="platform">Optional project platform to build, such as x86, x64, or ARM.</param>
        public static ICollection<string> BuildProjectAndReturnReferenceAssemblies(
            string projectDirectoryPath,
            string configuration,
            string platform = null)
        {
            // Detect the reference assemblies used in the build so that they can be
            // correctly referenced later when reflecting over the built assembly.

            HashSet<string> referencePaths = new HashSet<string>();
            Regex unquotedReferenceRegex = new Regex("/reference:([^ \"]+)( |$)");
            Regex quotedReferenceRegex = new Regex("/reference:\"([^\"]+)\"");

            Utils.BuildProject(projectDirectoryPath, configuration, platform, line =>
            {
                Log.Message("{0}", line);

                foreach (Match match in unquotedReferenceRegex.Matches(line))
                {
                    referencePaths.Add(match.Groups[1].Value);
                }
                foreach (Match match in quotedReferenceRegex.Matches(line))
                {
                    referencePaths.Add(match.Groups[1].Value);
                }
            });

            return referencePaths;
        }

        /// <summary>
        /// Compiles TypeScript code.
        /// </summary>
        /// <param name="tsSourceDirectoryPath">Root directory of the TypeScript code to compile.
        /// A tsconfig.json file should exist in this directory.</param>
        public static void CompileTypeScript(string tsSourceDirectoryPath)
        {
            string tscPath;
            if (Utils.IsRunningOnMacOS)
            {
                tscPath = "/usr/local/bin/tsc";
            }
            else
            {
                tscPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "npm", "tsc.cmd");
            }
            if (!File.Exists(tscPath))
            {
                throw new FileNotFoundException("TypeScript compiler tool not found.", tscPath);
            }

            Utils.Execute(tscPath, null, tsSourceDirectoryPath, TimeSpan.FromSeconds(30));
        }

        /// <summary>
        /// Packs an NPM package.
        /// </summary>
        /// <param name="packageDirectoryPath">Root directory of the NPM package source files.
        /// A package.json file should exist in this directory.</param>
        public static void PackNpmPackage(string packageDirectoryPath)
        {
            Utils.Execute(
                (Utils.IsRunningOnMacOS ? "/usr/local/bin/npm" : "npm.cmd"),
                "pack",
                packageDirectoryPath,
                TimeSpan.FromSeconds(120));
        }

        /// <summary>
        /// Extracts an embedded resource as a string.
        /// </summary>
        public static string ExtractResourceText(string resourceName)
        {
            string fullResourceName = typeof(Utils).Namespace + ".Resources." + resourceName;
            using (Stream stream = System.Reflection.Assembly.GetExecutingAssembly()
                .GetManifestResourceStream(fullResourceName))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException("Embedded resource not found: " + resourceName);
                }

                string text = new StreamReader(stream).ReadToEnd();
                return text;
            }
        }

        /// <summary>
        /// Extracts an embedded resource to a file.
        /// </summary>
        public static void ExtractResource(string resourceName, string directoryPath, string fileName = null)
        {
            string fullResourceName = typeof(Utils).Namespace + ".Resources." + resourceName;

            using (Stream stream = System.Reflection.Assembly.GetExecutingAssembly()
                .GetManifestResourceStream(fullResourceName))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException("Embedded resource not found: " + resourceName);
                }

                string contents = new StreamReader(stream).ReadToEnd();
                File.WriteAllText(Path.Combine(directoryPath, fileName ?? resourceName), contents);
            }
        }

        /// <summary>
        /// Gets the path to the NuGet tool, downloading the tool if necessary.
        /// </summary>
        public static string GetNuGetToolPath(string downloadDirectory)
        {
            if (Utils.IsRunningOnMacOS)
            {
                // Assume the NuGet CLI was already installed along with the Xamarin tools.
                return "/usr/local/bin/nuget";
            }

            if (!Directory.Exists(downloadDirectory))
            {
                Directory.CreateDirectory(downloadDirectory);
            }

            string nugetToolPath = Path.Combine(downloadDirectory, "nuget.exe");
            if (!File.Exists(nugetToolPath))
            {
                Log.Important("Downloading nuget.exe");
                string nugetToolUri = "https://dist.nuget.org/win-x86-commandline/v3.3.0/nuget.exe";
                Utils.Execute(
                    "powershell.exe",
                    "-NoProfile -Command \"& {Invoke-WebRequest -Uri '" + nugetToolUri + "' -OutFile nuget.exe}\"",
                    downloadDirectory,
                    TimeSpan.FromSeconds(60));
            }

            return nugetToolPath;
        }
    }
}
