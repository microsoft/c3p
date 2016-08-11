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
    /// Compiles Android Java code into an intermediate form suitable for linking into a C3P plugin.
    /// </summary>
    class AndroidApiCompiler : ApiCompiler
    {
        const string MonoAndroidDll = "Mono.Android.dll";

        string bindingsProject;
        string javadocPath;
        string moduleName;

        static class MsbuildProperties
        {
            public const string AssemblyName = "C3PAndroidBindingsAssemblyName";
            public const string JavadocPath = "C3PAndroidBindingsJavadocPath";
        }

        static class ProjectFiles
        {
            public const string Project = "AndroidBindings.csproj";
            public const string AsyncWrappers = "AsyncWrappers.cs";
            public const string AssemblyInfo = "AssemblyInfo.cs";
            public const string Metadata = "Metadata.xml";
            public const string EnumFields = "EnumFields.xml";
            public const string EnumMethods = "EnumMethods.xml";
        }

        public override string PlatformName
        {
            get
            {
                return PluginInfo.AndroidPlatformName;
            }
        }

        protected override void Init()
        {
            base.Init();

            this.javadocPath = Path.Combine(this.PlatformIntermediatePath, "javadoc");
            if (!Directory.Exists(this.javadocPath))
            {
                Directory.CreateDirectory(this.javadocPath);
            }

            this.bindingsProject = Path.Combine(this.PlatformIntermediatePath, ProjectFiles.Project);
        }

        public override void Run()
        {
            this.Init();

            this.moduleName = this.GetModuleName();

            if (!this.SkipNativeBuild)
            {
                this.CompilePluginAndroidNativeCode();
            }

            this.GenerateAndroidCSharpBindingsProject(false);
            ICollection<string> referencePaths = this.BuildAndroidCSharpBindingsProject(false);
            this.GenerateAndroidCSharpBindingsProject(true, referencePaths);
            this.BuildAndroidCSharpBindingsProject(true);

            this.ExportApi(referencePaths);
        }

        string GetModuleName()
        {
            string settingsGradleFile = Path.Combine(this.PlatformSourcePath, "settings.gradle");
            if (!File.Exists(settingsGradleFile))
            {
                throw new FileNotFoundException("Android settings.gradle file not found", settingsGradleFile);
            }

            string includeLine = File.ReadAllLines(settingsGradleFile).SingleOrDefault(l => l.StartsWith("include '"));
            if (includeLine == null || !includeLine.EndsWith("'"))
            {
                throw new InvalidOperationException("Failed to parse Android settings.gradle file.");
            }

            if (includeLine.IndexOf(',') >= 0)
            {
                throw new NotSupportedException("Multiple Android modules in settings.gradle are not supported.");
            }

            return includeLine.Split('\'')[1].Substring(1);
        }

        void CompilePluginAndroidNativeCode()
        {
            Utils.Execute(
                "gradlew.bat",
                (this.DebugConfiguration ? "assembleDebug" : "assembleRelease") + " --quiet",
                this.PlatformSourcePath,
                TimeSpan.FromSeconds(120));

            // TODO: Use a gradle JavaDoc task, where it should know all the dependencies to include?
            Utils.ClearDirectory(this.javadocPath);

            string javadocToolPath;
            string androidJarPath;
            if (Utils.IsRunningOnMacOS)
            {
                javadocToolPath = "/usr/bin/javadoc";
                androidJarPath = $"/Users/{Environment.UserName}/Library/Android/sdk/platforms/android-19/android.jar";
            }
            else
            {
                string jdkDir = Environment.GetEnvironmentVariable("JAVA_HOME");
                if (String.IsNullOrEmpty(jdkDir) || !Directory.Exists(jdkDir))
                {
                    throw new FileNotFoundException("Could not find JAVA_HOME");
                }

                javadocToolPath = Path.Combine(jdkDir, @"bin\javadoc.exe");

                string androidHome = Environment.GetEnvironmentVariable("ANDROID_HOME");
                if (String.IsNullOrEmpty(androidHome))
                {
                    throw new FileNotFoundException("Could not find ANDROID_HOME.");
                }

                androidJarPath = Path.Combine(androidHome, @"platforms\android-19\android.jar");
            }

            if (!File.Exists(javadocToolPath))
            {
                throw new FileNotFoundException("Could not find javadoc tool at " + javadocToolPath);
            }

            if (!File.Exists(androidJarPath))
            {
                throw new FileNotFoundException("Could not find android.jar at " + androidJarPath);
            }

            List<string> libJarPaths = new List<string>();
            libJarPaths.Add(androidJarPath);

            string libSearchPath = Path.Combine(this.moduleName, "build", "intermediates", "exploded-aar");
            if (Directory.Exists(Path.Combine(this.PlatformSourcePath, libSearchPath)))
            {
                libJarPaths.AddRange(Directory.GetFiles(
                    Path.Combine(this.PlatformSourcePath, libSearchPath), "classes.jar", SearchOption.AllDirectories));
            }

            List<string> packages = this.PluginInfo.AndroidPlatform.NamespaceMappings.Select(m => m.Package).ToList();

            string sourcePath = Path.Combine(this.moduleName, "src", "main", "java");
            Utils.Execute(
                javadocToolPath,
                $"-d \"{Path.GetFullPath(this.javadocPath)}\" " +
                $"-sourcepath \"{sourcePath}\" " +
                $"-subpackages \"{String.Join(Path.PathSeparator.ToString(), packages)}\" " +
                $"-bootclasspath \"{String.Join(Path.PathSeparator.ToString(), libJarPaths)}\" " +
                "-nocomment",
                this.PlatformSourcePath,
                TimeSpan.FromSeconds(60));

            FixJavadocOutput(this.javadocPath);
        }

        /// <summary>
        /// The javadoc output format changed slightly between JDK 1.7 and JDK 1.8, unfortunately in a way that
        /// broke the Xamarin binding tool that extracts parameter names from the javadoc. This modifies the
        /// 1.8-style output to look like 1.7 just enough for the process to work again.
        /// </summary>
        static void FixJavadocOutput(string javadocDirectoryPath)
        {
            foreach (string htmlFile in Directory.GetFiles(javadocDirectoryPath, "*.html", SearchOption.AllDirectories))
            {
                string[] lines = File.ReadAllLines(htmlFile);
                bool edited = false;
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (line.StartsWith("<td class=\"colOne\"><code><span class=\"memberNameLink\">") ||
                        line.StartsWith("<td class=\"colLast\"><code><span class=\"memberNameLink\">"))
                    {
                        int firstHyphen = line.IndexOf('-');
                        int lastHyphen = line.LastIndexOf('-');
                        if (firstHyphen < lastHyphen)
                        {
                            line = (line.Substring(0, firstHyphen) + '(' +
                                line.Substring(firstHyphen + 1, lastHyphen - firstHyphen - 1) + ')' +
                                line.Substring(lastHyphen + 1))
                                .Replace("-", ", ");
                        }

                        lines[i] = line.Replace("<span class=\"memberNameLink\">", "<strong>")
                            .Replace("</span>", "</strong>");
                        edited = true;
                    }
                }

                if (edited)
                {
                    File.WriteAllLines(htmlFile, lines);
                }
            }
        }

        void GenerateAndroidCSharpBindingsProject(bool includeAdapters, ICollection<string> referencePaths = null)
        {
            Log.Important("Generating Android C# bindings project at\n" + this.bindingsProject);

            string project = Utils.ExtractResourceText(ProjectFiles.Project);
            string assemblyName = this.PluginInfo.Assembly.Name;
            string tempAssemblyName = "temp";

            project = project.Replace($"$({MsbuildProperties.JavadocPath})", Path.GetFullPath(this.javadocPath));
            project = project.Replace(
                $"$({MsbuildProperties.AssemblyName})", (includeAdapters ? assemblyName : tempAssemblyName));
            project = project.Replace (
                "<ItemGroup Label=\"Jars\">", "<ItemGroup Label=\"Jars\">\n" + this.GetMsbuildJarItems());
            project = project.Replace (
                "<ItemGroup Label=\"Libs\">", "<ItemGroup Label=\"Libs\">\n" + this.GetMsbuildLibItems());

            File.WriteAllText(this.bindingsProject, project);

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

            string transformsPath = Path.Combine(this.PlatformIntermediatePath, "Transforms");
            if (!Directory.Exists(transformsPath))
            {
                Directory.CreateDirectory(transformsPath);
            }

            if (includeAdapters)
            {
                string apiXmlPath = Path.Combine(
                    this.PlatformIntermediatePath, "obj", (this.DebugConfiguration ? "Debug" : "Release"), "api.xml");
                if (!File.Exists(apiXmlPath))
                {
                    throw new FileNotFoundException("Exported Java API XML file not found at " + apiXmlPath);
                }

                JavaApi javaApi;
                using (StreamReader reader = File.OpenText(apiXmlPath))
                {
                    javaApi = JavaApi.FromXml(reader);
                }

                string tempPath = Path.Combine(
                    Path.GetDirectoryName(this.BuiltBindingsAssemblyPath), tempAssemblyName + ".dll");
                Assembly androidApi = ClrApi.LoadFrom(
                    tempPath,
                    referencePaths);
                AndroidApiAdapter adapter = new AndroidApiAdapter();
                adapter.PluginInfo = this.PluginInfo;
                adapter.JavaApi = javaApi;
                adapter.OutputDirectoryPath = adaptersPath;
                adapter.GenerateAdapterCodeForApi(androidApi);
            }

            this.GenerateBindingMetadata(transformsPath);

            string enumFields = "<enum-field-mappings></enum-field-mappings>";
            File.WriteAllText(Path.Combine(transformsPath, ProjectFiles.EnumFields), enumFields);

            string enumMethods = "<enum-method-mappings></enum-method-mappings>";
            File.WriteAllText(Path.Combine(transformsPath, ProjectFiles.EnumMethods), enumMethods);
        }

        string GetMsbuildJarItems()
        {
            string jarsDirectoryPath = Path.Combine(this.PlatformIntermediatePath, "Jars");
            if (!Directory.Exists(jarsDirectoryPath))
            {
                Directory.CreateDirectory(jarsDirectoryPath);
            }

            string configuration = (this.DebugConfiguration ? "debug" : "release");
            string jarPath = Path.Combine(
                this.PlatformSourcePath, this.moduleName, @"build", "intermediates", "bundles", configuration, "classes.jar");
            File.Copy(jarPath, Path.Combine(jarsDirectoryPath, this.PluginInfo.Assembly.Name + ".jar"), true);
            string jarItems = $"    <EmbeddedJar Include=\"Jars\\{this.PluginInfo.Assembly.Name}.jar\" />\n";

            string libSearchPath = Path.Combine(this.moduleName, "build", "intermediates", "exploded-aar");
            if (Directory.Exists(Path.Combine(this.PlatformSourcePath, libSearchPath)))
            {
                foreach (string libJarPath in Directory.GetFiles(
                    Path.Combine(this.PlatformSourcePath, libSearchPath), "classes.jar", SearchOption.AllDirectories))
                {
                    string relativeLibJarPath = libJarPath.Substring(
                        Path.Combine(this.PlatformSourcePath, libSearchPath).Length + 1);
                    string[] pathParts = relativeLibJarPath.Split(Path.DirectorySeparatorChar);
                    if (pathParts.Length == 5)
                    {
                        string libName = pathParts[1] + '-' + pathParts[2];
                        File.Copy(
                            Path.Combine(this.PlatformSourcePath, libSearchPath, relativeLibJarPath),
                            Path.Combine(jarsDirectoryPath, libName + ".jar"),
                            true);
                        jarItems += $"    <EmbeddedReferenceJar Include=\"Jars\\{libName}.jar\" />\n";
                    }
                }
            }

            return jarItems;
        }

        string GetMsbuildLibItems()
        {
            // Include any shared libs from [module]/build/intermediates/binaries/debug/lib/[abi]/*.so

            string libsDirectoryPath = Path.Combine(this.PlatformIntermediatePath, "Libs");
            if (!Directory.Exists(libsDirectoryPath))
            {
                Directory.CreateDirectory(libsDirectoryPath);
            }

            string libItems = String.Empty;

            string configuration = (this.DebugConfiguration ? "debug" : "release");
            string intermediatesPath =
                Path.Combine(this.PlatformSourcePath, this.moduleName, @"build", "intermediates");
            string[] libSourcePaths = new[]
            {
                Path.Combine(intermediatesPath, "binaries", configuration, "lib"),
                Path.Combine(intermediatesPath, "ndk", configuration, "lib"),
            };

            foreach (string libSourcePath in libSourcePaths)
            {
                if (Directory.Exists(libSourcePath))
                {
                    foreach (string libFile in Directory.GetFiles(libSourcePath, "*.so", SearchOption.AllDirectories))
                    {
                        if (new FileInfo(libFile).Length == 0)
                        {
                            // 0-sized lib files may be used to make gradle happy when building for other ABIs.
                            // Ignore them here.
                            continue;
                        }

                        string libName = Path.GetFileName(libFile);
                        string abi = Path.GetFileName(Path.GetDirectoryName(libFile));

                        if (!Directory.Exists(Path.Combine(libsDirectoryPath, abi)))
                        {
                            Directory.CreateDirectory(Path.Combine(libsDirectoryPath, abi));
                        }

                        if (!File.Exists(Path.Combine(libsDirectoryPath, abi, libName)))
                        {
                            File.Copy(libFile, Path.Combine(libsDirectoryPath, abi, libName), true);
                            libItems += $"    <EmbeddedNativeLibrary Include=\"Libs\\{abi}\\{libName}\" />\n";

                            this.PluginInfo.AndroidPlatform.LibFiles.Add(new PluginInfo.SourceFileInfo
                            {
                                SourceFilePath = Path.Combine(this.PlatformName, "libs", abi, libName),
                                TargetDirectoryPath = Path.Combine("libs", abi),
                            });
                        }
                    }
                }
            }

            return libItems;
        }

        void GenerateBindingMetadata(string transformsPath)
        {
            XElement metadataRoot = new XElement("metadata");

            // Prevent the Xamarin tool from doing automatic namespace mapping.
            foreach (PluginInfo.NamespaceMappingInfo namespaceMapping
                in this.PluginInfo.AndroidPlatform.NamespaceMappings)
            {
                metadataRoot.Add(new XElement(
                    "attr",
                    new XAttribute("path", $"/api/package[@name='{namespaceMapping.Package}']"),
                    new XAttribute("name", "managedName"),
                    namespaceMapping.Package));
            }

            // Exclude any classes that don't have public or protected visibility.
            // (The binding tool should exclude them automatically, but it doesn't.)
            metadataRoot.Add(new XElement(
                "remove-node",
                new XAttribute("path", $"/api/package/class[@visibility='']")));

            XmlWriterSettings xmlSettings = new XmlWriterSettings
            {
                ConformanceLevel = ConformanceLevel.Document,
                Encoding = Encoding.UTF8,
                Indent = true
            };

            string metadataFilePath = Path.Combine(transformsPath, ProjectFiles.Metadata);
            XDocument metadataDocument = new XDocument(metadataRoot);
            using (XmlWriter writer = XmlWriter.Create(metadataFilePath, xmlSettings))
            {
                metadataDocument.WriteTo(writer);
            }
        }

        ICollection<string> BuildAndroidCSharpBindingsProject(bool isFinal)
        {
            string configuration = (this.DebugConfiguration ? "Debug" : "Release");
            ICollection<string> referencePaths =
                Utils.BuildProjectAndReturnReferenceAssemblies(this.PlatformIntermediatePath, configuration);

            string assemblyName = this.PluginInfo.Assembly.Name;
            this.BuiltBindingsAssemblyPath =
                Path.Combine(this.PlatformIntermediatePath, "bin", configuration, assemblyName + ".dll");

            if (isFinal)
            {
                if (!File.Exists(this.BuiltBindingsAssemblyPath))
                {
                    throw new FileNotFoundException(
                        "Bindings assembly not found at: " + this.BuiltBindingsAssemblyPath);
                }

                Log.Important("Built Android C# bindings at\n" + this.BuiltBindingsAssemblyPath);
            }

            return referencePaths;
        }

        public static string GetMonoAndroidReferencePath()
        {
            string monoAndroidReferenceParentPath;
            string monoAndroidReferencePath = null;
            if (Utils.IsRunningOnMacOS)
            {
                monoAndroidReferenceParentPath = "/Library/Frameworks/Xamarin.Android.framework/Versions";
                if (Directory.Exists(monoAndroidReferenceParentPath))
                {
                    string parentPath2 =
                        Directory.GetDirectories(monoAndroidReferenceParentPath).OrderBy(d => d).LastOrDefault();
                    if (parentPath2 != null)
                    {
                        string parentPath3 = Path.Combine(parentPath2, "lib/xbuild-frameworks/MonoAndroid");
                        if (Directory.Exists(parentPath3))
                        {
                            monoAndroidReferencePath =
                                Directory.GetDirectories(parentPath3).OrderBy(d => d).LastOrDefault();
                        }
                    }
                }
            }
            else
            {
                monoAndroidReferenceParentPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    @"Reference Assemblies\Microsoft\Framework\MonoAndroid");
                if (Directory.Exists(monoAndroidReferenceParentPath))
                {
                    monoAndroidReferencePath =
                        Directory.GetDirectories(monoAndroidReferenceParentPath).OrderBy(d => d).LastOrDefault();
                }
            }

            if (monoAndroidReferencePath == null ||
                !File.Exists(Path.Combine(monoAndroidReferencePath, MonoAndroidDll)))
            {
                throw new FileNotFoundException("Mono.Android.dll not found under " + monoAndroidReferenceParentPath);
            }

            return Path.Combine(monoAndroidReferencePath, MonoAndroidDll);
        }

        void ExportApi(ICollection<string> referencePaths)
        {
            string[] includeNamespaces = this.PluginInfo.AndroidPlatform.NamespaceMappings.Select(m => m.Namespace)
                .Concat(new[] { typeof(AndroidApiCompiler).Namespace }).ToArray();
            Assembly apiAssembly = ClrApi.LoadFrom(
                this.BuiltBindingsAssemblyPath,
                referencePaths,
                includeNamespaces);

            ClrApi androidApi = new ClrApi
            {
                Platform = PluginInfo.AndroidPlatformName,
                Assemblies = new List<Assembly>(new[] { apiAssembly }),
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
