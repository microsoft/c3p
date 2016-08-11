// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.C3P
{
    /// <summary>
    /// Compiles Windows C++ or C# code into an intermediate form suitable for linking into a C3P plugin.
    /// </summary>
    class WindowsApiCompiler : ApiCompiler
    {
        static class MsbuildProperties
        {
            public const string AssemblyName = "C3PWindowsBindingsAssemblyName";
            public const string PluginAssemblyName = "C3PWindowsPluginAssemblyName";
            public const string PluginAssemblyPath = "C3PWindowsPluginAssemblyPath";
        }

        static class ProjectFiles
        {
            public const string Project = "WindowsBindings.csproj";
            public const string AssemblyInfo = "AssemblyInfo.cs";
            public const string ProjectJson = "project.json";
        }

        string pluginProjectFilePath;
        string pluginMetadataFilePath;
        string bindingsProject;

        public override string PlatformName
        {
            get
            {
                return PluginInfo.WindowsPlatformName;
            }
        }

        protected override void Init()
        {
            base.Init();

            this.pluginProjectFilePath = Directory.GetFiles(
                this.PlatformSourcePath, "*.csproj").SingleOrDefault();
            if (pluginProjectFilePath == null)
            {
                this.pluginProjectFilePath = Directory.GetFiles(
                    this.PlatformSourcePath, "*.vcxproj").SingleOrDefault();
                if (pluginProjectFilePath == null)
                {
                    throw new FileNotFoundException("C# or C++ project not found at " + this.PlatformSourcePath);
                }
            }

            this.bindingsProject = Path.Combine(this.PlatformIntermediatePath, ProjectFiles.Project);
        }

        public override void Run()
        {
            this.Init();

            this.BuildPluginWindowsProject();

            this.GenerateWindowsAdapterProject();
            this.BuildWindowsAdapterProject();

            this.ExportApi();
        }

        void BuildPluginWindowsProject()
        {
            string configuration = (this.DebugConfiguration ? "Debug" : "Release");
            if (!this.SkipNativeBuild)
            {
                foreach (string platform in new[] { "x86", "x64", "ARM" })
                {
                    Utils.BuildProject(this.PlatformSourcePath, configuration, platform);
                }
            }

            string assemblyName = this.PluginInfo.Assembly.Name;
            string buildOutputPath = Path.Combine(this.PlatformSourcePath, "bin", "x86", configuration);
            this.pluginMetadataFilePath = Directory.GetFiles(buildOutputPath, "*.winmd")
                .OrderBy(f => File.GetLastWriteTimeUtc(f)).Last();
            if (!File.Exists(this.pluginMetadataFilePath))
            {
                throw new FileNotFoundException("Plugin metadata file not found at: " + buildOutputPath);
            }

            Log.Important("Built WinRT plugin component at\n" + this.pluginMetadataFilePath);
        }

        void GenerateWindowsAdapterProject()
        {
            Log.Important("Generating Windows C# bindings project at\n" + this.bindingsProject);

            string project = Utils.ExtractResourceText(ProjectFiles.Project);
            project = project.Replace($"$({MsbuildProperties.AssemblyName})", this.PluginInfo.Assembly.Name);
            project = project.Replace(
                $"$({MsbuildProperties.PluginAssemblyName})",
                Path.GetFileNameWithoutExtension(this.pluginMetadataFilePath));
            project = project.Replace(
                $"$({MsbuildProperties.PluginAssemblyPath})",
                this.pluginMetadataFilePath.Replace("x86", "$(Platform)"));

            File.WriteAllText(this.bindingsProject, project);

            Utils.ExtractResource(ProjectFiles.ProjectJson, this.PlatformIntermediatePath);

            string propertiesPath = Path.Combine(this.PlatformIntermediatePath, "Properties");
            if (!Directory.Exists(propertiesPath))
            {
                Directory.CreateDirectory(propertiesPath);
            }

            this.GenerateAssemblyInfo(Path.Combine(propertiesPath, ProjectFiles.AssemblyInfo));

            string adaptersPath = Path.Combine(this.PlatformIntermediatePath, "Adapters");
            if (!Directory.Exists(adaptersPath))
            {
                Directory.CreateDirectory(adaptersPath);
            }

            string refPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) +
                @"\Reference Assemblies\Microsoft\Framework\";
            string[] referenceAssemblyPaths = new string[]
            {
                refPath + @".NETFramework\v4.5\System.dll",
                refPath + @".NETFramework\v4.5\Facades\System.Runtime.dll",
                refPath + @".NETFramework\v4.6.1\Facades\System.Diagnostics.Debug.dll",
                refPath + @".NETFramework\v4.6.1\Facades\System.Runtime.dll",
                refPath + @".NETFramework\v4.6.1\Facades\System.Threading.Tasks.dll",
                refPath + @".NETCore\v4.5\System.Runtime.WindowsRuntime.dll",
                refPath + @".NETCore\v4.5\System.Runtime.WindowsRuntime.UI.Xaml.dll",
                refPath + @".NETCore\v4.5.1\System.Runtime.InteropServices.WindowsRuntime.dll",
            };

            Assembly androidApi = ClrApi.LoadFrom(this.pluginMetadataFilePath, referenceAssemblyPaths);
            WindowsApiAdapter adapter = new WindowsApiAdapter();
            adapter.PluginInfo = this.PluginInfo;
            adapter.OutputDirectoryPath = adaptersPath;
            adapter.GenerateAdapterCodeForApi(androidApi);
        }

        void BuildWindowsAdapterProject()
        {
            string nugetToolPath = Utils.GetNuGetToolPath(Path.Combine(this.PlatformIntermediatePath, "tools"));
            Utils.Execute(
                nugetToolPath,
                "restore " + ProjectFiles.ProjectJson,
                this.PlatformIntermediatePath,
                TimeSpan.FromSeconds(15));

            string configuration = (this.DebugConfiguration ? "Debug" : "Release");
            foreach (string platform in new[] { "x86", "x64", "ARM" })
            {
                Utils.BuildProject(this.PlatformIntermediatePath, configuration, platform);
            }

            string assemblyName = this.PluginInfo.Assembly.Name;
            this.BuiltBindingsAssemblyPath =
                Path.Combine(this.PlatformIntermediatePath, "bin", "x86", configuration, assemblyName + ".dll");
            if (!File.Exists(this.BuiltBindingsAssemblyPath))
            {
                throw new FileNotFoundException("Bindings assembly not found at: " + this.BuiltBindingsAssemblyPath);
            }

            Log.Important("Built Windows bindings assembly at\n" + this.BuiltBindingsAssemblyPath);
        }

        void ExportApi()
        {
            string refPath = @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework";
            string[] referenceAssemblyPaths = new string[]
            {
                refPath + @"\v4.5\System.dll",
                refPath + @"\v4.5\Facades\System.Runtime.dll",
                refPath + @"\v4.6.1\Facades\System.Diagnostics.Debug.dll",
                refPath + @"\v4.6.1\Facades\System.Runtime.dll",
                refPath + @"\v4.6.1\Facades\System.Threading.Tasks.dll",
                this.pluginMetadataFilePath,
            };
            string[] includeNamespaces = this.PluginInfo.WindowsPlatform.IncludeNamespaces?.Select(m => m.Namespace)
                .Concat(new[] { typeof(WindowsApiCompiler).Namespace }).ToArray();

            Assembly apiAssembly = ClrApi.LoadFrom(
                this.BuiltBindingsAssemblyPath, referenceAssemblyPaths, includeNamespaces);

            ClrApi androidApi = new ClrApi
            {
                Platform = PluginInfo.WindowsPlatformName,
                Assemblies = new List<Assembly>(new[] { apiAssembly }),
                Language = this.Language,
            };

            string apiXmlPath = Path.GetFullPath(Path.Combine(
                this.PlatformIntermediatePath, this.PlatformName + "-api.xml"));
            using (StreamWriter writer = File.CreateText(apiXmlPath))
            {
                androidApi.ToXml(writer);
            }

            Log.Important($"Exported {this.PlatformName} API metadata to {apiXmlPath}");
        }
    }
}
