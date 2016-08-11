// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.C3P
{
    /// <summary>
    /// Compiles iOS Obj-C code into an intermediate form suitable for linking into a C3P plugin.
    /// </summary>
    class IOSApiCompiler : ApiCompiler
    {
        internal const string bindingNamespace = "objc";

        const string xamarinIOSReferencePath =
            "/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/lib/mono/Xamarin.iOS/Xamarin.iOS.dll";

        static class Resources
        {
            public const string Project = "IOSBindings.csproj";
        }

        static class MsbuildProperties
        {
            public const string AssemblyName = "C3PIOSBindingsAssemblyName";
            public const string NativeLibFileName = "C3PIOSBindingsNativeLibFileName";
        }

        static class ProjectFiles
        {
            public const string Project = "IOSBindings.csproj";
            public const string ApiDefinitions = "ApiDefinitions.cs";
            public const string StructsAndEnums = "StructsAndEnums.cs";
            public const string AssemblyInfo = "AssemblyInfo.cs";
        }

        string pluginProjectFilePath;
        string bindingsProjectFilePath;
        string nativeLibFileName;

        public override string PlatformName
        {
            get
            {
                return PluginInfo.IOSPlatformName;
            }
        }

        public override void Run()
        {
            this.Init();

            if (!this.SkipNativeBuild)
            {
                // This step is not necessary because the sharpie tool will always compile first.
                //this.CompilePluginIOSNativeCode();
            }

            this.pluginProjectFilePath = Directory.GetDirectories(
                this.PlatformSourcePath, "*.xcodeproj").SingleOrDefault();
            if (pluginProjectFilePath == null)
            {
                throw new FileNotFoundException("Xcode project not found at " + this.PlatformSourcePath);
            }

            this.BuildIOSNativeProject();

            this.bindingsProjectFilePath = Path.Combine(this.PlatformIntermediatePath, ProjectFiles.Project);

            this.GenerateCSharpBindings();
            this.GenerateIOSCSharpBindingsProject(false);
            ICollection<string> referencePaths = this.BuildIOSCSharpBindingsProject(false);
            this.GenerateIOSCSharpBindingsProject(true, referencePaths);
            this.BuildIOSCSharpBindingsProject(true);

            this.ExportApi(referencePaths);
        }

        void BuildIOSNativeProject()
        {
            string projectFilePath = Path.GetFullPath(this.pluginProjectFilePath);
            string configuration = (this.DebugConfiguration ? "Debug" : "Release");

            string[][] sdksArchs = new[]
            {
                new[] { "iphonesimulator", "i386" },
                new[] { "iphonesimulator", "x86_64" },
                new[] { "iphoneos", "armv7" },
                new[] { "iphoneos", "armv7s" },
                new[] { "iphoneos", "arm64" },
            };

            string allArchLibs = "";
            foreach (string[] sdkArch in sdksArchs)
            {
                if (!this.SkipNativeBuild)
                {
                    Utils.Execute(
                        "/usr/bin/xcodebuild",
                        $"-project \"{projectFilePath}\" -sdk {sdkArch[0]} -arch {sdkArch[1]} " +
                            $"-configuration {configuration} clean build",
                        this.PlatformSourcePath,
                        TimeSpan.FromSeconds(60));
                }

                string buildOutputPath = Path.Combine(
                    this.PlatformSourcePath, "build", configuration + "-" + sdkArch[0]);
                string libFilePath = Directory.GetFiles(buildOutputPath, "lib*.a").SingleOrDefault();
                if (libFilePath == null)
                {
                    throw new InvalidOperationException("lib*.a file not found at " + buildOutputPath);
                }

                if (this.nativeLibFileName == null)
                {
                    this.nativeLibFileName = Path.GetFileName(libFilePath);
                }
                else if (this.nativeLibFileName != Path.GetFileName(libFilePath))
                {
                    throw new InvalidOperationException("Static lib files have inconsistent names: " +
                        this.nativeLibFileName + ", " + Path.GetFileName(libFilePath));
                }

                string archLibFileName =
                    Path.GetFileNameWithoutExtension(this.nativeLibFileName) + "-" + sdkArch[1] + ".a";
                string archLibFilePath = Path.Combine(Path.GetFullPath(this.PlatformIntermediatePath), archLibFileName);
                if (!this.SkipNativeBuild)
                {
                    File.Copy(libFilePath, archLibFilePath, true);
                }
                else if (!File.Exists(archLibFilePath))
                {
                    throw new InvalidOperationException("Built lib file not found at " + archLibFilePath);
                }

                allArchLibs += " " + archLibFileName;
            }

            Utils.Execute(
                "/usr/bin/xcrun",
                $"-sdk iphoneos lipo -create -output {this.nativeLibFileName}" + allArchLibs,
                this.PlatformIntermediatePath,
                TimeSpan.FromSeconds(15));
        }

        void GenerateCSharpBindings()
        {
            PluginInfo.PlatformInfo iosPlatformInfo = this.PluginInfo.IOSPlatform;
            if (iosPlatformInfo == null)
            {
                throw new InvalidOperationException("iOS platform is missing in plugin.xml");
            }

            string pluginProjectName = Path.GetFileNameWithoutExtension(this.pluginProjectFilePath);
            string outputPath = Path.GetFullPath(this.PlatformIntermediatePath);

            string iosSdkName = this.GetSharpieIosSdkName();

            Utils.Execute(
                "/usr/local/bin/sharpie",
                $"-tlm-do-not-submit bind -n {bindingNamespace} " +
                $"{pluginProjectName}/{pluginProjectName}.h " +
                $"-sdk {iosSdkName} " +
                $"-o \"{outputPath}\"",
                this.PlatformSourcePath,
                TimeSpan.FromSeconds(60));

            this.FixGeneratedCSharpCode();
        }

        string GetSharpieIosSdkName()
        {
            string iosSdkName = null;

            Utils.Execute(
                "/usr/local/bin/sharpie",
                "xcode -sdks",
                this.PlatformSourcePath,
                TimeSpan.FromSeconds(10),
                line =>
                {
                    if (line.StartsWith("sdk: iphoneos"))
                    {
                        iosSdkName = line.Split(' ')[1];
                    }
                });

            if (iosSdkName == null)
            {
                throw new InvalidOperationException("Failed to get iOS SDK name from Objective Sharpie.");
            }

            return iosSdkName;
        }

        void FixGeneratedCSharpCode()
        {
            string projectName = Path.GetFileNameWithoutExtension(this.pluginProjectFilePath);
            string[] apiDefinitions = File.ReadAllLines(
                Path.Combine(this.PlatformIntermediatePath, ProjectFiles.ApiDefinitions));
            for (int i = 0; i < apiDefinitions.Length; i++)
            {
                if (apiDefinitions[i] ==$"using {projectName};")
                {
                    // Why does Objective Sharpie output this? It doesn't seem to have any purpose.
                    apiDefinitions[i] = String.Empty;
                }
                else if (apiDefinitions[i].Contains("[Verify ("))
                {
                    // Undo MethodToProperty transformations where the method name does not start with "get".
                    if (apiDefinitions[i].Contains("(MethodToProperty)") &&
                        apiDefinitions[i + 1].EndsWith("{ get; }") &&
                        !apiDefinitions[i - 1].Contains("Export (\"get"))
                    {
                        apiDefinitions[i + 1] = apiDefinitions[i + 1].Replace("{ get; }", "();");
                    }

                    // Comment out all [Verify] attributes, since they block compilation of autogenerated code.
                    // Most incorrect API mappings should be corrected by the C3P adapter layer anyway.
                    apiDefinitions[i] = apiDefinitions[i].Replace("[Verify (", "//[Verify (");
                }
                else if (apiDefinitions[i].Contains("NSArray<") && apiDefinitions[i].IndexOf("//") < 0)
                {
                    // Objective Sharpie has a bug in which it uses NSArray<T> instead of T[]
                    // when the array is inside a callback.
                    int nsArrayIndex = apiDefinitions[i].IndexOf("NSArray<");
                    int ltIndex = nsArrayIndex + "NSArray".Length;
                    int gtIndex = apiDefinitions[i].IndexOf('>', nsArrayIndex);
                    string innerType = apiDefinitions[i].Substring(ltIndex + 1, gtIndex - ltIndex - 1);
                    apiDefinitions[i] = apiDefinitions[i].Substring(0, nsArrayIndex) +
                        innerType + "[]" + apiDefinitions[i].Substring(gtIndex + 1);
                }
                else if (apiDefinitions[i].Contains("NSUUID"))
                {
                    // Objective Sharpie generates NSUUID type bindings with the wrong casing!
                    apiDefinitions[i] = apiDefinitions[i].Replace("NSUUID", "NSUuid");
                }
            }
            File.WriteAllLines(
                Path.Combine(this.PlatformIntermediatePath, ProjectFiles.ApiDefinitions), apiDefinitions);

            if (File.Exists(Path.Combine(this.PlatformIntermediatePath, ProjectFiles.StructsAndEnums)))
            {
                string[] structsAndEnums = File.ReadAllLines(
                                           Path.Combine(this.PlatformIntermediatePath, ProjectFiles.StructsAndEnums));
                for (int i = 0; i < structsAndEnums.Length; i++)
                {
                    if (structsAndEnums[i].EndsWith(" : nint"))
                    {
                        structsAndEnums[i] =
                        structsAndEnums[i].Substring(0, structsAndEnums[i].Length - " : nint".Length) + " : long";
                    }
                }
                File.WriteAllLines(
                    Path.Combine(this.PlatformIntermediatePath, ProjectFiles.StructsAndEnums), structsAndEnums);
            }
        }

        void GenerateIOSCSharpBindingsProject(bool includeAdapters, ICollection<string> referencePaths = null)
        {
            Log.Important("Generating iOS C# bindings project at\n" + this.bindingsProjectFilePath);

            string project = Utils.ExtractResourceText(Resources.Project);
            string assemblyName = this.PluginInfo.Assembly.Name;
            string tempAssemblyName = "temp";

            project = project.Replace(
                $"$({MsbuildProperties.AssemblyName})", (includeAdapters ? assemblyName : tempAssemblyName));
            project = project.Replace($"$({MsbuildProperties.NativeLibFileName})", this.nativeLibFileName);

            File.WriteAllText(this.bindingsProjectFilePath, project);

            string propertiesPath = Path.Combine(this.PlatformIntermediatePath, "Properties");
            if (!Directory.Exists(propertiesPath))
            {
                Directory.CreateDirectory(propertiesPath);
            }

            this.GenerateAssemblyInfo(
                Path.Combine(propertiesPath, ProjectFiles.AssemblyInfo),
                new string[]
                {
                    "[assembly: ObjCRuntime.LinkWith(" +
                        $"\"{this.nativeLibFileName}\", SmartLink = true, ForceLoad = true)]",
                });

            string adaptersPath = Path.Combine(this.PlatformIntermediatePath, "Adapters");
            if (!Directory.Exists(adaptersPath))
            {
                Directory.CreateDirectory(adaptersPath);
            }

            if (includeAdapters)
            {
                string tempPath = Path.Combine(
                    Path.GetDirectoryName(this.BuiltBindingsAssemblyPath), tempAssemblyName + ".dll");
                Assembly iosApi = ClrApi.LoadFrom(tempPath, referencePaths);
                IOSApiAdapter adapter = new IOSApiAdapter();
                adapter.PluginInfo = this.PluginInfo;
                adapter.OutputDirectoryPath = adaptersPath;
                adapter.GenerateAdapterCodeForApi(iosApi);
            }
        }

        ICollection<string> BuildIOSCSharpBindingsProject(bool isFinal)
        {
            string configuration = (this.DebugConfiguration ? "Debug" : "Release");
            ICollection<string> referencePaths =
                Utils.BuildProjectAndReturnReferenceAssemblies(this.PlatformIntermediatePath, configuration);

            string assemblyName = this.PluginInfo.Assembly.Name;
            this.BuiltBindingsAssemblyPath = Path.Combine(
                this.PlatformIntermediatePath, "bin", configuration, assemblyName + ".dll");

            if (isFinal)
            {
                if (!File.Exists(this.BuiltBindingsAssemblyPath))
                {
                    throw new FileNotFoundException(
                        "Bindings assembly not found at: " + this.BuiltBindingsAssemblyPath);
                }

                Log.Important("Built iOS C# bindings at\n" + this.BuiltBindingsAssemblyPath);
            }

            return referencePaths;
        }

        void ExportApi(ICollection<string> referencePaths)
        {
            string[] includeNamespaces = this.PluginInfo.IOSPlatform.NamespaceMappings.Select(m => m.Namespace)
                .Concat(new[] { typeof(AndroidApiCompiler).Namespace }).ToArray();

            Assembly apiAssembly = ClrApi.LoadFrom(
                this.BuiltBindingsAssemblyPath, referencePaths, includeNamespaces);

            ClrApi iosApi = new ClrApi
            {
                Platform = PluginInfo.IOSPlatformName,
                Assemblies = new List<Assembly>(new[] { apiAssembly }),
            };

            string apiXmlPath = Path.GetFullPath(Path.Combine(
                this.PlatformIntermediatePath, this.PlatformName + "-api.xml"));
            using (StreamWriter writer = File.CreateText(apiXmlPath))
            {
                iosApi.ToXml(writer);
            }

            Log.Important($"Exported {this.PlatformName} API metadata to {apiXmlPath}");
        }
    }
}
