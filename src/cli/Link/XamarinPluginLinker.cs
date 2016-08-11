// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Microsoft.C3P
{
    /// <summary>
    /// Links platform intermediate files into a Xamarin plugin.
    /// </summary>
    class XamarinPluginLinker : PluginLinker
    {
        static readonly XNamespace MsbuildXmlNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";
        static readonly XNamespace NuspecXmlNamespace = "http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd";

        static class XamarinPlatformIds
        {
            public const string Android = "MonoAndroid10";
            public const string IOS = "Xamarin.iOS10";
            public const string Windows = "UAP10";
            private const string Portable =
                "portable-net45+wp8+wpa81+win8+MonoAndroid10+MonoTouch10+Xamarin.iOS10+UAP10";

            public static string Get(string platformName)
            {
                switch (platformName)
                {
                    case PluginInfo.AndroidPlatformName: return Android;
                    case PluginInfo.IOSPlatformName: return IOS;
                    case PluginInfo.WindowsPlatformName: return Windows;
                    case PluginInfo.PortablePlatformName: return Portable;
                    default: throw new NotSupportedException("Platform not supported: " + platformName);
                }
            }
        }

        public override string TargetName
        {
            get
            {
                return PluginInfo.XamarinTargetName;
            }
        }

        public override void Run()
        {
            this.Init();

            Dictionary<string, ClrApi> platformApis = this.LoadApis();
            ClrApi mergedApi = this.MergeApis(platformApis);
            platformApis.Add(PluginInfo.PortablePlatformName, mergedApi);
            Assembly mergedAssembly = mergedApi.Assemblies.Single();

            string xamarinPlatformId = XamarinPlatformIds.Get(PluginInfo.PortablePlatformName);
            string targetRelativePath =
                Path.Combine("build", xamarinPlatformId, mergedAssembly.Name + ".dll");
            Log.Message("    " + targetRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(this.TargetOutputPath, targetRelativePath)));
            PortableAssemblyEmitter assemblyEmitter = new PortableAssemblyEmitter
            {
                Assembly = mergedAssembly,
                AssemblyFilePath = Path.Combine(this.TargetOutputPath, targetRelativePath),
            };
            assemblyEmitter.Run();

            Dictionary<string, Assembly> platformAssemblies =
                platformApis.ToDictionary(pa => pa.Key, pa => pa.Value.Assemblies.Single());

            this.CopyAssemblies(platformAssemblies);
            this.GenerateNuspec(mergedAssembly, platformAssemblies);
            this.PackNugetPackage();
        }

        void CopyAssemblies(Dictionary<string, Assembly> platformAssemblies)
        {
            string configuration = (this.DebugConfiguration ? "debug" : "release");
            string packageId = this.PluginInfo.Assembly.Name;
            string targetsFileName = packageId + ".targets";
            string msbuildTargetName = "ResolvePluginReference_" + packageId;

            foreach (KeyValuePair<string, Assembly> platformAssembly in platformAssemblies)
            {
                string xamarinPlatformId = XamarinPlatformIds.Get(platformAssembly.Key);

                string targetsFilePath = Path.Combine("build", xamarinPlatformId, targetsFileName);
                Log.Message("    " + targetsFilePath);

                XNamespace ns = MsbuildXmlNamespace;
                XElement targetElement = new XElement(
                    ns + "Target",
                    new XAttribute("Name", msbuildTargetName),
                    new XAttribute("BeforeTargets", "ResolveAssemblyReferences"));

                string[] platformSourcePaths;
                if (platformAssembly.Key == PluginInfo.WindowsPlatformName)
                {
                    platformSourcePaths = new[]
                    {
                        @"windows\bin\x86",
                        @"windows\bin\x64",
                        @"windows\bin\ARM",
                    };
                }
                else
                {
                    platformSourcePaths = new[]
                    {
                        Path.Combine(platformAssembly.Key, "bin"),
                    };
                }

                foreach (string platformSourcePath in platformSourcePaths)
                {
                    string relativeAssemblyPath = Path.Combine(
                        platformSourcePath,
                        configuration,
                        platformAssembly.Value.Name + ".dll");
                    string assemblyPath;
                    if (platformAssembly.Key == PluginInfo.PortablePlatformName)
                    {
                        assemblyPath = Path.Combine(
                            this.TargetOutputPath,
                            "build",
                            XamarinPlatformIds.Get(PluginInfo.PortablePlatformName),
                            platformAssembly.Value.Name + ".dll");
                    }
                    else
                    {
                        assemblyPath = this.FindIntermediateFile(relativeAssemblyPath);
                        if (assemblyPath == null)
                        {
                            throw new InvalidOperationException(
                                "Could not find platform assembly in intermediate path: " +
                                    Path.Combine(relativeAssemblyPath));
                        }
                    }

                    XElement itemGroupElement = new XElement(ns + "ItemGroup");
                    targetElement.Add(itemGroupElement);

                    if (platformAssembly.Key == PluginInfo.WindowsPlatformName)
                    {
                        string platform = platformSourcePath.Replace("windows\\bin\\", String.Empty);
                        itemGroupElement.Add(new XAttribute("Condition", " '$(Platform)' == '" + platform + "' "));
                    }

                    foreach (string extension in new[] { ".dll", ".xml", ".pdb", ".dll.mdb", ".winmd", ".pri" })
                    {
                        foreach (string sourcePath in
                            Directory.GetFiles(Path.GetDirectoryName(assemblyPath), "*" + extension))
                        {
                            string fileName = Path.GetFileName(sourcePath);
                            if (fileName.StartsWith("temp."))
                            {
                                continue;
                            }

                            bool isMainAssembly = String.Equals(
                                fileName,
                                this.PluginInfo.Assembly.Name + ".dll",
                                StringComparison.OrdinalIgnoreCase);

                            string targetRelativePath;
                            if (platformAssembly.Key == PluginInfo.WindowsPlatformName)
                            {
                                string platform = platformSourcePath.Replace("windows\\bin\\", String.Empty);
                                targetRelativePath = Path.Combine("build", xamarinPlatformId, platform, fileName);

                                if (isMainAssembly)
                                {
                                    itemGroupElement.Add(new XElement(
                                        ns + "Reference",
                                        new XAttribute("Include", this.PluginInfo.Assembly.Name),
                                        new XElement(
                                            ns + "HintPath",
                                            @"$(MSBuildThisFileDirectory)" + platform + @"\" + fileName)
                                    ));
                                }
                                else
                                {
                                    itemGroupElement.Add(new XElement(
                                        ns + "None",
                                        new XAttribute(
                                            "Include", @"$(MSBuildThisFileDirectory)" + platform + @"\" + fileName),
                                        new XElement(ns + "DeploymentContent", true)
                                    ));
                                }
                            }
                            else
                            {
                                targetRelativePath = Path.Combine("build", xamarinPlatformId, fileName);

                                if (isMainAssembly)
                                {
                                    itemGroupElement.Add(new XElement(
                                        ns + "Reference",
                                        new XAttribute("Include", this.PluginInfo.Assembly.Name),
                                        new XElement(
                                            ns + "HintPath",
                                            @"$(MSBuildThisFileDirectory)" + fileName)
                                    ));
                                }
                                else
                                {
                                    itemGroupElement.Add(new XElement(
                                        ns + "None",
                                        new XAttribute(
                                            "Include", @"$(MSBuildThisFileDirectory)" + fileName),
                                        new XElement(ns + "DeploymentContent", true)
                                    ));
                                }
                            }

                            if (File.Exists(sourcePath))
                            {
                                Log.Message("    " + targetRelativePath);
                                string targetFullPath = Path.Combine(this.TargetOutputPath, targetRelativePath);

                                if (!Directory.Exists(Path.GetDirectoryName(targetFullPath)))
                                {
                                    Directory.CreateDirectory(Path.GetDirectoryName(targetFullPath));
                                }

                                // The portable assembly was already output in the correct location.
                                if (platformAssembly.Key != PluginInfo.PortablePlatformName)
                                {
                                    File.Copy(sourcePath, targetFullPath, true);
                                }
                            }
                        }
                    }
                }

                XElement propertyGroupElement = new XElement(
                    ns + "PropertyGroup",
                    new XElement(
                        ns + "ResolveAssemblyReferencesDependsOn",
                        "$(ResolveAssemblyReferencesDependsOn);" + msbuildTargetName));

                XDocument targetsDocument = new XDocument(new XElement(
                    ns + "Project", new XAttribute("ToolsVersion", "4.0"), targetElement, propertyGroupElement));
                XmlWriterSettings xmlSettings = new XmlWriterSettings
                {
                    ConformanceLevel = ConformanceLevel.Document,
                    Encoding = Encoding.UTF8,
                    Indent = true
                };
                using (XmlWriter writer = XmlWriter.Create(
                    Path.Combine(this.TargetOutputPath, targetsFilePath), xmlSettings))
                {
                    targetsDocument.WriteTo(writer);
                }
            }
        }

        void GenerateNuspec(Assembly portableAssembly, Dictionary<string, Assembly> platformAssemblies)
        {
            string packageId = this.PluginInfo.Assembly.Name;
            string nuspecFileName = packageId + ".nuspec";
            Log.Message("    " + nuspecFileName);

            XNamespace ns = NuspecXmlNamespace;
            XElement metadataElement = new XElement(
                ns + "metadata",
                new XAttribute("minClientVersion", "2.8.0"),
                new XElement(ns + "id", packageId),
                new XElement(ns + "version", this.PluginInfo.Version),
                new XElement(ns + "title", this.PluginInfo.Name),
                new XElement(ns + "authors", this.PluginInfo.Author),
                new XElement(ns + "description", this.PluginInfo.Description),
                new XElement(ns + "tags", this.PluginInfo.Keywords));

            XElement filesElement = new XElement(ns + "files");

            foreach (string filePath in
                Directory.GetFiles(Path.Combine(this.TargetOutputPath, "build"), "*", SearchOption.AllDirectories))
            {
                string relativeFilePath = filePath.Substring(this.TargetOutputPath.Length + 1);
                XElement fileElement = new XElement(
                    ns + "file",
                    new XAttribute("src", relativeFilePath),
                    new XAttribute("target", relativeFilePath));
                filesElement.Add(fileElement);
            }

            string nuspecFilePath = Path.Combine(this.TargetOutputPath, nuspecFileName);
            XDocument nuspecDocument = new XDocument(
                new XElement(ns + "package", metadataElement, filesElement));
            XmlWriterSettings xmlSettings = new XmlWriterSettings
            {
                ConformanceLevel = ConformanceLevel.Document,
                Encoding = Encoding.UTF8,
                Indent = true
            };
            using (XmlWriter writer = XmlWriter.Create(nuspecFilePath, xmlSettings))
            {
                nuspecDocument.WriteTo(writer);
            }
        }

        void PackNugetPackage()
        {
            string nugetToolPath = Utils.GetNuGetToolPath(Path.Combine(this.TargetOutputPath, "tools"));

            string packageId = this.PluginInfo.Assembly.Name;
            string nuspecFileName = packageId + ".nuspec";

            Utils.Execute(
                nugetToolPath,
                $"pack \"{nuspecFileName}\" -Exclude tools\\* -NoPackageAnalysis -Verbosity detailed",
                this.TargetOutputPath,
                TimeSpan.FromSeconds(10));
        }
    }
}
