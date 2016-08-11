// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.C3P
{
    /// <summary>
    /// Base class for subclasses that compile native code into an intermediate form suitable
    /// for linking into a C3P plugin.
    /// </summary>
    abstract class ApiCompiler
    {
        public static ICollection<string> SupportedPlatforms
        {
            get
            {
                if (Utils.IsRunningOnMacOS)
                {
                    return new[] { PluginInfo.AndroidPlatformName, PluginInfo.IOSPlatformName };
                }
                else
                {
                    return new[] { PluginInfo.AndroidPlatformName, PluginInfo.WindowsPlatformName };
                }
            }
        }

        public static ApiCompiler Create(string platform)
        {
            if (platform == PluginInfo.AndroidPlatformName)
            {
                return new AndroidApiCompiler();
            }
            else if (platform == PluginInfo.IOSPlatformName && Utils.IsRunningOnMacOS)
            {
                return new IOSApiCompiler();
            }
            else if (platform == PluginInfo.WindowsPlatformName && !Utils.IsRunningOnMacOS)
            {
                return new WindowsApiCompiler();
            }
            else
            {
                throw new NotSupportedException("Platform not supported: " + platform);
            }
        }

        public abstract string PlatformName { get; }

        public string Language { get; set; }

        public string SourcePath { get; set; }

        public bool DebugConfiguration { get; set; }

        public string IntermediatePath { get; set; }

        public bool SkipNativeBuild { get; set; }

        public PluginInfo PluginInfo { get; private set; }

        public string PlatformSourcePath { get; private set; }

        public string PlatformIntermediatePath { get; private set; }

        public string BuiltBindingsAssemblyPath { get; protected set; }

        protected virtual void Init()
        {
            if (String.IsNullOrEmpty(this.SourcePath))
            {
                throw new ArgumentNullException(nameof(SourcePath));
            }
            else if (String.IsNullOrEmpty(this.IntermediatePath))
            {
                throw new ArgumentNullException(nameof(IntermediatePath));
            }

            if (!Directory.Exists(this.SourcePath))
            {
                throw new DirectoryNotFoundException("Source path not found: " + this.SourcePath);
            }

            this.PlatformSourcePath = Path.Combine(this.SourcePath, this.PlatformName);
            if (!String.IsNullOrEmpty(this.Language))
            {
                this.PlatformSourcePath += "-" + this.Language;
            }

            if (!Directory.Exists(this.PlatformSourcePath))
            {
                throw new DirectoryNotFoundException("Platform source path not found: " + this.PlatformSourcePath);
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

            if (!Directory.Exists(this.IntermediatePath))
            {
                Directory.CreateDirectory(this.IntermediatePath);
            }

            this.PlatformIntermediatePath = Path.Combine(this.IntermediatePath, this.PlatformName);
            if (!Directory.Exists(this.PlatformIntermediatePath))
            {
                Directory.CreateDirectory(this.PlatformIntermediatePath);
            }
            else
            {
                Utils.ClearDirectory(this.PlatformIntermediatePath, new string[] { "javadoc", "tools" });
            }

            PluginInfo.PlatformInfo platformInfo =
                this.PluginInfo.Platforms.SingleOrDefault(p => p.Name == this.PlatformName);
            if (platformInfo == null)
            {
                throw new InvalidOperationException(
                    "Platform info for " + this.PlatformName + " platform is missing from plugin.xml.");
            }

            Log.Important($"Compiling {this.PlatformName} APIs at\n" +
                Utils.EnsureTrailingSlash(Path.GetFullPath(this.PlatformIntermediatePath)));
        }

        public abstract void Run();

        protected void GenerateAssemblyInfo(
            string assemblyInfoFilePath, string[] additionalLines = null)
        {
            if (this.PluginInfo.Assembly == null ||
                String.IsNullOrEmpty(this.PluginInfo.Assembly.Name))
            {
                throw new InvalidDataException(
                    "The plugin.xml file must include a single assembly element with a name attribute.");
            }

            string name = this.PluginInfo.Assembly.Name;
            string description = this.PluginInfo.Description ?? String.Empty;
            string author = this.PluginInfo.Author ?? String.Empty;
            string version = (this.PluginInfo.Version ?? "1.0.0");

            string[] assemblyInfo = new string[] {
                "using System.Reflection;",
                $"[assembly: AssemblyTitle(\"{name}\")]",
                $"[assembly: AssemblyDescription(\"{description}\")]",
                $"[assembly: AssemblyCompany(\"{author}\")]",
                $"[assembly: AssemblyVersion(\"{version}\")]",
            };

            if (additionalLines != null)
            {
                assemblyInfo = assemblyInfo.Concat(additionalLines).ToArray();
            }

            File.WriteAllLines(assemblyInfoFilePath, assemblyInfo);
        }
    }
}
