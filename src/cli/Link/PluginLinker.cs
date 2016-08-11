// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.C3P
{
    /// <summary>
    /// Base class for subclasses that link platform intermediate files into a plugin for
    /// a particular application framework.
    /// </summary>
    abstract class PluginLinker
    {
        protected const string c3pAndroidPackage = "com.microsoft.c3p";

        protected string windowsLanguage;

        public static ICollection<string> SupportedTargets
        {
            get
            {
                return new[]
                {
                    PluginInfo.CordovaTargetName, PluginInfo.ReactNativeTargetName, PluginInfo.XamarinTargetName
                };
            }
        }

        public static ICollection<string> SupportedPlatforms
        {
            get
            {
                return new[]
                {
                    PluginInfo.AndroidPlatformName, PluginInfo.IOSPlatformName, PluginInfo.WindowsPlatformName
                };
            }
        }

        public static PluginLinker Create(string target)
        {
            if (target == PluginInfo.CordovaTargetName)
            {
                return new CordovaPluginLinker();
            }
            else if (target == PluginInfo.ReactNativeTargetName)
            {
                return new ReactNativePluginLinker();
            }
            else if (target == PluginInfo.XamarinTargetName)
            {
                return new XamarinPluginLinker();
            }
            else
            {
                throw new NotSupportedException("Target not supported: " + target);
            }
        }

        public abstract string TargetName { get; }

        public string SourcePath { get; set; }

        public IList<string> IntermediatePaths { get; set; }

        public string OutputPath { get; set; }

        public bool DebugConfiguration { get; set; }

        public PluginInfo PluginInfo { get; private set; }

        public string TargetOutputPath { get; private set; }

        protected virtual void Init()
        {
            if (String.IsNullOrEmpty(this.SourcePath))
            {
                throw new ArgumentNullException(nameof(SourcePath));
            }
            else if (this.IntermediatePaths == null || this.IntermediatePaths.Count == 0 ||
                this.IntermediatePaths.Any(p => String.IsNullOrEmpty(p)))
            {
                throw new ArgumentNullException(nameof(IntermediatePaths));
            }
            else if (String.IsNullOrEmpty(this.OutputPath))
            {
                throw new ArgumentNullException(nameof(OutputPath));
            }

            if (!Directory.Exists(this.SourcePath))
            {
                throw new DirectoryNotFoundException("Source path not found: " + this.SourcePath);
            }

            // Don't check for existence of the last one because it was not specified explicitly.
            for (int i = 0; i < this.IntermediatePaths.Count - 1; i++)
            {
                if (!Directory.Exists(this.IntermediatePaths[i]))
                {
                    throw new DirectoryNotFoundException("Intermediate path not found: " + this.IntermediatePaths[i]);
                }
            }

            string pluginInfoFilePath = Path.Combine(this.SourcePath, "plugin.xml");
            if (!File.Exists(pluginInfoFilePath))
            {
                throw new FileNotFoundException("Plugin info file not found: " + pluginInfoFilePath);
            }

            using (StreamReader reader = File.OpenText(pluginInfoFilePath))
            {
                this.PluginInfo = PluginInfo.FromXml(reader);
            }

            if (!Directory.Exists(this.OutputPath))
            {
                Directory.CreateDirectory(this.OutputPath);
            }

            this.TargetOutputPath = Path.Combine(this.OutputPath, this.TargetName);
            if (!Directory.Exists(this.TargetOutputPath))
            {
                Directory.CreateDirectory(this.TargetOutputPath);
            }
            else
            {
                Utils.ClearDirectory(this.TargetOutputPath, new string[] { "tools" });
            }

            this.PluginInfo.Id = this.PluginInfo.Id + "-" + this.TargetName;

            Log.Important($"Linking {this.TargetName} plugin at\n" +
                Utils.EnsureTrailingSlash(Path.GetFullPath(this.TargetOutputPath)));
        }

        public abstract void Run();

        protected string FindIntermediateFile(string relativeFilePath)
        {
            foreach (string intermediatePath in this.IntermediatePaths)
            {
                string intermediateFile = Path.Combine(intermediatePath, relativeFilePath);
                if (File.Exists(intermediateFile))
                {
                    return intermediateFile;
                }
            }

            return null;
        }

        protected Dictionary<string, ClrApi> LoadApis()
        {
            Dictionary<string, ClrApi> platformApis = new Dictionary<string, ClrApi>();

            foreach (string platform in PluginLinker.SupportedPlatforms.Where(p => !platformApis.ContainsKey(p)))
            {
                string platformApiFile = this.FindIntermediateFile(Path.Combine(platform, platform + "-api.xml"));
                if (platformApiFile != null)
                {
                    using (StreamReader reader = File.OpenText(platformApiFile))
                    {
                        ClrApi platformApi = ClrApi.FromXml(reader);
                        platformApis.Add(platform, platformApi);

                        if (platform == PluginInfo.WindowsPlatformName)
                        {
                            this.windowsLanguage = platformApi.Language;
                        }
                    }
                }
            }

            if (platformApis.Count == 0)
            {
                throw new InvalidOperationException("No plugin APIs were found. Run the compile step first.");
            }

            return platformApis;
        }

        protected ClrApi MergeApis(Dictionary<string, ClrApi> platformApis)
        {
            PluginApiMerger merger = new PluginApiMerger
            {
                PluginInfo = this.PluginInfo,
                PlatformApis = platformApis,
            };
            merger.Run();

            foreach (string platform in PluginLinker.SupportedPlatforms)
            {
                this.RemovePlatformIfNotAvailable(merger.MergedApi, platform);
            }

            return merger.MergedApi;
        }

        void RemovePlatformIfNotAvailable(ClrApi api, string platform)
        {
            PluginInfo.PlatformInfo platformInfo = this.PluginInfo.Platforms.FirstOrDefault(p => p.Name == platform);
            if (!api.Platforms.Contains(platform) && platformInfo != null)
            {
                this.PluginInfo.Platforms.Remove(platformInfo);
            }
        }

        protected static void CopyFiles(
            string sourcePath, string targetPath, bool preserveDirectories, Predicate<string> filter)
        {
            string fullSourcePath = Path.GetFullPath(sourcePath);
            if (!fullSourcePath.EndsWith("" + Path.DirectorySeparatorChar))
            {
                fullSourcePath += Path.DirectorySeparatorChar;
            }

            Log.Message(Utils.EnsureTrailingSlash(Path.GetFullPath(targetPath)));
            foreach (string sourceFilePath in Directory.EnumerateFiles(fullSourcePath, "*", SearchOption.AllDirectories))
            {
                string relativePath = sourceFilePath.Substring(fullSourcePath.Length);

                if (filter == null || filter(relativePath))
                {
                    string targetFilePath;
                    if (preserveDirectories)
                    {
                        Log.Message("    " + relativePath);
                        targetFilePath = Path.Combine(targetPath, relativePath);

                        string targetDirectory = Path.GetDirectoryName(targetFilePath);
                        if (!Directory.Exists(targetDirectory))
                        {
                            Directory.CreateDirectory(targetDirectory);
                        }
                    }
                    else
                    {
                        string fileName = Path.GetFileName(relativePath);
                        Log.Message("    " + fileName);
                        targetFilePath = Path.Combine(targetPath, fileName);
                    }

                    File.Copy(sourceFilePath, targetFilePath, true);
                }
            }
        }

        protected static void CopyFiles(
            string sourcePath,
            string targetPath,
            bool preserveDirectories,
            ICollection<string> includeExtensions = null,
            ICollection<string> excludeDirectories = null)
        {
            PluginLinker.CopyFiles(sourcePath, targetPath, preserveDirectories, relativePath =>
            {
                if (excludeDirectories != null)
                {
                    char slash = Path.DirectorySeparatorChar;
                    foreach (string excludeDirectory in excludeDirectories)
                    {
                        if (relativePath.StartsWith(excludeDirectory + slash, StringComparison.OrdinalIgnoreCase) ||
                            relativePath.IndexOf(slash + excludeDirectory + slash, StringComparison.OrdinalIgnoreCase) > 0)
                        {
                            return false;
                        }
                    }
                }

                if (includeExtensions != null)
                {
                    foreach (string includeExtension in includeExtensions)
                    {
                        if (relativePath.EndsWith(includeExtension, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }

                    return false;
                }

                return true;
            });
        }

        protected static ICollection<string> GetFileExtensionsForNativeCode(string platform)
        {
            switch (platform)
            {
                case PluginInfo.AndroidPlatformName:
                    return new[] { ".java", ".js" };
                case PluginInfo.IOSPlatformName:
                    return new[] { ".m", ".h", ".storyboard", ".entitlements", ".js" };
                case PluginInfo.WindowsPlatformName:
                    // Windows source files will be copied as part of the project, not separately.
                    return new string[0];
                default:
                    throw new ArgumentException("Unsupported platform: " + platform);
            }
        }

        internal static void CopyNativeCode(string sourcePath, string[] sourceSubdirectories, string targetPath)
        {
            foreach (string platform in new[]
            {
                PluginInfo.AndroidPlatformName,
                PluginInfo.IOSPlatformName,
            })
            {
                string platformSourcePath = Path.Combine(sourcePath, platform);
                if (!Directory.Exists(platformSourcePath))
                {
                    continue;
                }

                string platformTargetPath = Path.Combine(targetPath, platform);
                if (!Directory.Exists(platformTargetPath))
                {
                    Directory.CreateDirectory(platformTargetPath);
                }

                ICollection<string> includeExtensions = GetFileExtensionsForNativeCode(platform);
                ICollection<string> excludeDirectories = new List<string>(new[] { "build" });

                if (sourceSubdirectories != null)
                {
                    foreach (string sourceSubdirectory in sourceSubdirectories)
                    {
                        string sourceSubPath = Path.Combine(platformSourcePath, sourceSubdirectory);
                        PluginLinker.CopyFiles(
                            sourceSubPath, platformTargetPath, false, includeExtensions, excludeDirectories);
                    }
                }
                else
                {
                    foreach (string frameworkSourceDirectory in Directory.GetDirectories(platformSourcePath, "*.framework"))
                    {
                        string frameworkName = Path.GetFileName(frameworkSourceDirectory);
                        excludeDirectories.Add(frameworkName);

                        string frameworkTargetDirectory = Path.Combine(platformTargetPath, frameworkName);
                        if (!Directory.Exists(frameworkTargetDirectory))
                        {
                            Directory.CreateDirectory(frameworkTargetDirectory);
                        }

                        PluginLinker.CopyFiles(frameworkSourceDirectory, frameworkTargetDirectory, true);
                    }

                    PluginLinker.CopyFiles(
                        platformSourcePath, platformTargetPath, false, includeExtensions, excludeDirectories);
                }
            }
        }

        internal static void CopyNativeProjects(string sourcePath, string[] includeProjects, string targetPath)
        {
            foreach (string platform in new[]
            {
                PluginInfo.AndroidPlatformName,
                PluginInfo.IOSPlatformName,
                PluginInfo.WindowsPlatformName
            })
            {
                string platformSourcePath = Path.Combine(sourcePath, platform);
                if (!Directory.Exists(platformSourcePath))
                {
                    continue;
                }

                string platformTargetPath = Path.Combine(targetPath, platform);
                if (!Directory.Exists(platformTargetPath))
                {
                    Directory.CreateDirectory(platformTargetPath);
                }

                if (platform == PluginInfo.AndroidPlatformName)
                {
                    Log.Message(Utils.EnsureTrailingSlash(Path.GetFullPath(platformTargetPath)));
                    foreach (string projectFile in new[]
                    {
                        "build.gradle",
                        "gradle.properties",
                        "gradlew",
                        "gradlew.bat",
                        "settings.gradle",
                    })
                    {
                        Log.Message("    " + projectFile);
                        File.Copy(
                            Path.Combine(platformSourcePath, projectFile),
                            Path.Combine(platformTargetPath, projectFile),
                            true);
                    }

                    foreach (string projectDirectory in Directory.GetDirectories(platformSourcePath))
                    {
                        string directoryName = Path.GetFileName(projectDirectory);
                        if (directoryName != "build" && directoryName != ".idea" && directoryName != ".gradle" &&
                            (directoryName == "gradle" || includeProjects == null ||
                            includeProjects.Any(p => p.Equals(directoryName, StringComparison.OrdinalIgnoreCase))))
                        {
                            PluginLinker.CopyProjectDirectory(
                                projectDirectory, Path.Combine(platformTargetPath, directoryName));
                        }
                    }
                }
                else if (platform == PluginInfo.IOSPlatformName)
                {
                    foreach (string projectDirectory in Directory.GetDirectories(platformSourcePath))
                    {
                        string directoryName = Path.GetFileName(projectDirectory);
                        string projectName = directoryName;

                        if (projectName.EndsWith(".xcodeproj"))
                        {
                            projectName = projectName.Substring(0, projectName.Length - ".xcodeproj".Length);
                        }

                        if (projectName.StartsWith("C3P") && projectName != "C3P")
                        {
                            projectName = projectName.Substring("C3P".Length);
                        }

                        if (directoryName != "build" && directoryName != "sharpie-build" && (includeProjects == null ||
                            includeProjects.Any(p => p.Equals(projectName, StringComparison.OrdinalIgnoreCase))))
                        {
                            PluginLinker.CopyProjectDirectory(
                                projectDirectory, Path.Combine(platformTargetPath, directoryName));
                        }
                    }
                }
                else if (platform == PluginInfo.WindowsPlatformName)
                {
                    // TODO: Copy Windows project files.
                }
            }
        }

        protected static void CopyProjectDirectory(string sourcePath, string targetPath)
        {
            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }

            string[] sourceFiles = Directory.GetFiles(sourcePath);
            if (sourceFiles.Length > 0)
            {
                Log.Message(Utils.EnsureTrailingSlash(targetPath));

                foreach (string sourceFile in sourceFiles)
                {
                    string fileName = Path.GetFileName(sourceFile);
                    if (fileName != ".DS_Store" &&
                        !fileName.EndsWith(".iml") &&
                        !fileName.EndsWith(".sln") &&
                        !fileName.EndsWith(".sdf") &&
                        !fileName.EndsWith(".VC.db") &&
                        !fileName.EndsWith(".VC.opendb") &&
                        !fileName.EndsWith(".ipch"))
                    {
                        string targetFile = Path.Combine(targetPath, fileName);
                        Log.Message("    " + fileName);
                        File.Copy(sourceFile, targetFile, true);
                    }
                }
            }

            foreach (string sourceSubDirectory in Directory.GetDirectories(sourcePath))
            {
                string subDirectoryName = Path.GetFileName(sourceSubDirectory);
                switch (subDirectoryName)
                {
                    case "build":
                    case "xcuserdata":
                    case ".gradle":
                    case ".vs":
                    case "bin":
                    case "obj":
                    case "ipch":
                        continue;
                }

                string targetSubDirectory = Path.Combine(targetPath, subDirectoryName);
                CopyProjectDirectory(sourceSubDirectory, targetSubDirectory);
            }
        }

        protected void CreateTypeScriptBindings(
            ClrApi pluginApi, bool forceAsync, bool es6, string bridgeModuleName, string scriptOutputPath)
        {
            foreach (Assembly apiAssembly in pluginApi.Assemblies)
            {
                TypeScriptEmitter tsEmitter = new TypeScriptEmitter
                {
                    Assembly = apiAssembly,
                    OutputPath = scriptOutputPath,
                    ForceAsyncAPIs = forceAsync,
                    ES6 = es6,
                    BridgeModuleName = bridgeModuleName,
                    PluginInfo = this.PluginInfo,
                };
                tsEmitter.Run();
            }
        }

        protected void GeneratePackageJson(string dependencyName, string[] additionalLines = null)
        {
            Log.Message("    package.json");

            string keywordsJson =
                "[" + String.Join(",", this.PluginInfo.KeywordsArray.Select(k => '"' + k + '"')) + "]";

            string[] lines = new[] {
                "{",
                $"    \"name\": \"{this.PluginInfo.Id}\",",
                $"    \"version\": \"{this.PluginInfo.Version}\",",
                $"    \"description\": \"{this.PluginInfo.Description}\",",
                $"    \"keywords\": {keywordsJson},",
                $"    \"author\": \"{this.PluginInfo.Author}\",",
                $"    \"license\": \"{this.PluginInfo.License}\",",
                $"    \"dependencies\": {{",
                $"        \"{dependencyName}\": \"*\"",
                $"    }}" + (additionalLines?.Length > 0 ? "," : ""),
            }.Concat(additionalLines ?? new string[0]).Concat(new[] { "}" }).ToArray();

            File.WriteAllLines(Path.Combine(this.TargetOutputPath, "package.json"), lines);
        }
    }
}
