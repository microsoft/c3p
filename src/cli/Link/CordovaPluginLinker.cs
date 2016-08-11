// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.C3P
{
    /// <summary>
    /// Links platform intermediate files into a Cordova plugin.
    /// </summary>
    class CordovaPluginLinker : PluginLinker
    {
        internal const string cordovaPluginXmlNamespace = "http://apache.org/cordova/ns/plugins/1.0";
        const string c3pNamespace = "Microsoft.C3P";
        const string c3pCordovaNamespace = "Microsoft.C3P.Cordova";
        const string c3pCordovaAndroidPackage = "com.microsoft.c3p.cordova";
        const string cordovaPluginAndroidClass = c3pCordovaAndroidPackage + ".C3PCordovaPlugin";
        const string c3pIOSPrefix = "C3P";
        const string c3pCordovaIOSPrefix = "C3PC";
        const string cordovaPluginIOSClass = c3pCordovaIOSPrefix + "CordovaPlugin";
        const string cordovaPluginWindowsClass = c3pCordovaNamespace + ".C3PCordovaPlugin";
        const string c3pPluginId = "c3p-cordova";
        const string c3pCordovaPackageName = "c3p-cordova";
        const string c3pCordovaModuleName = "CordovaNativeBridge";
        const string c3pServiceName = "C3P";

        public override string TargetName
        {
            get
            {
                return PluginInfo.CordovaTargetName;
            }
        }

        public override void Run()
        {
            this.Init();
            ClrApi pluginApi = this.MergeApis(this.LoadApis());

            PluginLinker.CopyNativeCode(this.SourcePath, null, this.TargetOutputPath);

            if (this.PluginInfo.WindowsPlatform != null)
            {
                string windowsSourcePath = Path.Combine(
                    this.SourcePath,
                    "windows" + (this.windowsLanguage != null ? "-" + this.windowsLanguage : null));
                if (Directory.Exists(windowsSourcePath))
                {
                    string windowsOutputPath = Path.Combine(this.TargetOutputPath, "windows");
                    if (!Directory.Exists(windowsOutputPath))
                    {
                        Directory.CreateDirectory(windowsOutputPath);
                    }

                    PluginLinker.CopyProjectDirectory(windowsSourcePath, windowsOutputPath);
                }
            }

            CordovaPluginLinker.CollectSourceFilesInfo(this.TargetOutputPath, this.PluginInfo);
            this.CreateAndroidConfigFileInfo();
            this.CreateIOSConfigFileInfo();
            this.CreateWindowsConfigFileInfo();

            this.AddProjectReferences();

            string scriptOutputPath = Path.Combine(this.TargetOutputPath, "www");
            if (Directory.Exists(scriptOutputPath))
            {
                Directory.Delete(scriptOutputPath, true);
            }

            Directory.CreateDirectory(scriptOutputPath);
            this.CreateTypeScriptBindings(
                pluginApi, true, false, c3pPluginId + "." + c3pCordovaModuleName, scriptOutputPath);

            string modulesDirectoryPath = Path.Combine(scriptOutputPath, "node_modules");
            Directory.CreateDirectory(modulesDirectoryPath);

            // This is messy, because Cordova's implementation of module resolution
            // doesn't respect the 'main' property from the package.json.
            this.CreateBridgeModuleTypeScriptDefinition(Path.Combine(
                modulesDirectoryPath, c3pPluginId + "." + c3pCordovaModuleName + ".d.ts"));
            Utils.ExtractResource(
                "es6-promise.d.ts",
                modulesDirectoryPath,
                c3pPluginId + "." + "es6-promise.d.ts");

            foreach (string tsFile in new[] { "NativeObject.ts", "NativeBridge.ts" })
            {
                Utils.ExtractResource(tsFile, modulesDirectoryPath, c3pPluginId + "." + tsFile);
                string fixTsCode = File.ReadAllText(Path.Combine(
                    modulesDirectoryPath, c3pPluginId + "." + tsFile));
                fixTsCode = fixTsCode.Replace("\"es6-promise\"", "\"" + c3pPluginId + ".es6-promise\"")
                    .Replace("\"./NativeObject\"", "\"" + c3pPluginId + ".NativeObject\"");
                File.WriteAllText(
                    Path.Combine(modulesDirectoryPath, c3pPluginId + "." + tsFile), fixTsCode);
            }

            Utils.ExtractResource("tsconfig.json", scriptOutputPath);
            Utils.CompileTypeScript(scriptOutputPath);

            Log.Message(Utils.EnsureTrailingSlash(Path.GetFullPath(this.TargetOutputPath)));
            this.GeneratePluginXml(scriptOutputPath);
            this.GeneratePackageJson(c3pCordovaPackageName);
            Utils.PackNpmPackage(this.TargetOutputPath);
        }

        void CreateBridgeModuleTypeScriptDefinition(string outputFilePath)
        {
            string bridgeInterfaceName = "NativeAsyncBridge";
            File.WriteAllLines(outputFilePath, new[]
            {
                $"import {{ Promise }} from \"{c3pPluginId}.es6-promise\";",
                $"import {{ NativeObject, NativeReference }} from \"{c3pPluginId}.NativeObject\";",
                $"import {{ {bridgeInterfaceName} }} from \"{c3pPluginId}.NativeBridge\";",
                $"declare var bridge: {bridgeInterfaceName};",
                $"export {{ bridge, NativeObject, NativeReference, Promise }}; ",
            });
        }

        void GeneratePluginXml(string scriptOutputPath)
        {
            Log.Message("    plugin.xml");

            foreach (string scriptFile in Directory.GetFiles(scriptOutputPath, "*.js"))
            {
                string scriptModuleName = Path.GetFileNameWithoutExtension(scriptFile);
                if (!this.PluginInfo.JavaScriptModules.Any(
                    m => String.Equals(m.Name, scriptModuleName, StringComparison.OrdinalIgnoreCase)))
                {
                    this.PluginInfo.JavaScriptModules.Add(new PluginInfo.JavaScriptModuleInfo
                    {
                        Name = scriptModuleName,
                    });
                }
            }

            foreach (PluginInfo.JavaScriptModuleInfo jsModule in this.PluginInfo.JavaScriptModules)
            {
                jsModule.Source = "www/" + jsModule.Name + ".js";
                if (jsModule.Clobbers == null && this.PluginInfo.Assembly.JavaScriptTarget != null &&
                    !jsModule.Name.StartsWith("C3P") && !jsModule.Name.StartsWith("es6"))
                {
                    jsModule.Clobbers = new PluginInfo.ClobbersInfo
                    {
                        Target = this.PluginInfo.Assembly.JavaScriptTarget + "." + jsModule.Name,
                    };
                }
            }

            foreach (PluginInfo.PlatformInfo platform in this.PluginInfo.Platforms)
            {
                platform.NamespaceMappings = null;
                platform.IncludeNamespaces = null;
                platform.Enums = null;

                // TODO: Convert Android lib-file elements (for auto-detected .so files) to source-file elements.
                platform.LibFiles = null;
            }

            this.PluginInfo.Assembly = null;

            using (StreamWriter writer = File.CreateText(Path.Combine(this.TargetOutputPath, "plugin.xml")))
            {
                this.PluginInfo.ToXml(writer, cordovaPluginXmlNamespace);
            }
        }

        internal static void CollectSourceFilesInfo(string sourceDirectoryPath, PluginInfo pluginInfo)
        {
            foreach (string platform in new[]
            {
                PluginInfo.AndroidPlatformName,
                PluginInfo.IOSPlatformName,
                PluginInfo.WindowsPlatformName
            })
            {
                PluginInfo.PlatformInfo platformInfo =
                    pluginInfo.Platforms.FirstOrDefault(p => p.Name == platform);
                if (platformInfo == null)
                {
                    continue;
                }

                string platformSourcePath = Path.Combine(sourceDirectoryPath, platform);
                if (Directory.Exists(platformSourcePath))
                {
                    ICollection<string> extensions = PluginLinker.GetFileExtensionsForNativeCode(platform);
                    foreach (string sourceFilePath in Directory.GetFiles(platformSourcePath))
                    {
                        string relativeFilePath = Path.GetFullPath(sourceFilePath)
                            .Substring(Path.GetFullPath(sourceDirectoryPath).Length + 1);
                        if (extensions.Any(e => sourceFilePath.EndsWith(e, StringComparison.OrdinalIgnoreCase)))
                        {
                            PluginInfo.SourceFileInfo sourceFileInfo =
                                new PluginInfo.SourceFileInfo
                                {
                                    SourceFilePath = relativeFilePath.Replace("\\", "/"),
                                };

                            if (sourceFilePath.EndsWith(".java", StringComparison.OrdinalIgnoreCase))
                            {
                                string javaPackage = CordovaPluginLinker.GetJavaSourceFilePackage(sourceFilePath);
                                if (javaPackage != null)
                                {
                                    sourceFileInfo.TargetDirectoryPath = "src/" + javaPackage.Replace('.', '/');
                                }
                            }

                            if (!platformInfo.ResourceFiles.Any(r => String.Equals(
                                r.SourceFilePath, sourceFileInfo.SourceFilePath, StringComparison.OrdinalIgnoreCase)))
                            {
                                platformInfo.SourceFiles.Add(sourceFileInfo);
                            }
                        }
                    }
                }
            }
        }

        void CreateAndroidConfigFileInfo()
        {
            if (this.PluginInfo.AndroidPlatform != null)
            {
                PluginInfo.ConfigFeatureInfo pluginFeatureInfo = new PluginInfo.ConfigFeatureInfo
                {
                    Name = c3pServiceName,
                    Params = new List<PluginInfo.ConfigParamInfo>(new[]
                    {
                        new PluginInfo.ConfigParamInfo
                        {
                            Name = "android-package",
                            Value = cordovaPluginAndroidClass,
                        },
                        new PluginInfo.ConfigParamInfo
                        {
                            Name = "plugin-namespace:" + c3pAndroidPackage,
                            Value = c3pNamespace,
                        },
                        new PluginInfo.ConfigParamInfo
                        {
                            Name = "plugin-namespace:" + c3pCordovaAndroidPackage,
                            Value = c3pCordovaNamespace,
                        },
                    }),
                };

                foreach (PluginInfo.NamespaceMappingInfo nsMapping
                    in this.PluginInfo.AndroidPlatform.NamespaceMappings)
                {
                    if (String.IsNullOrEmpty(nsMapping.Package))
                    {
                        throw new InvalidOperationException(
                            "Missing package for Android namespace mapping: " + nsMapping.Namespace);
                    }

                    pluginFeatureInfo.Params.Add(new PluginInfo.ConfigParamInfo
                    {
                        Name = "plugin-namespace:" + nsMapping.Package,
                        Value = nsMapping.Namespace,
                    });
                }

                foreach (PluginInfo.AssemblyClassInfo classInfo in this.PluginInfo.Assembly.Classes
                    .Where(c => c.MarshalByValue == "true"))
                {
                    pluginFeatureInfo.Params.Add(new PluginInfo.ConfigParamInfo
                    {
                        Name = "plugin-class:" + classInfo.Name,
                        Value = "marshal-by-value",
                    });
                }

                this.PluginInfo.AndroidPlatform.ConfigFiles.Add(new PluginInfo.ConfigFileInfo
                {
                    Target = "res/xml/config.xml",
                    Parent = "/*",
                    Features = new List<PluginInfo.ConfigFeatureInfo>(new[] { pluginFeatureInfo }),
                });
            }
        }

        void CreateIOSConfigFileInfo()
        {
            if (this.PluginInfo.IOSPlatform != null)
            {
                PluginInfo.ConfigFeatureInfo pluginFeatureInfo = new PluginInfo.ConfigFeatureInfo
                {
                    Name = c3pServiceName,
                    Params = new List<PluginInfo.ConfigParamInfo>(new[]
                    {
                        new PluginInfo.ConfigParamInfo
                        {
                            Name = "ios-package",
                            Value = cordovaPluginIOSClass,
                        },
                        new PluginInfo.ConfigParamInfo
                        {
                            Name = "plugin-namespace:" + c3pIOSPrefix,
                            Value = c3pNamespace,
                        },
                        new PluginInfo.ConfigParamInfo
                        {
                            Name = "plugin-namespace:" + c3pCordovaIOSPrefix,
                            Value = c3pCordovaNamespace,
                        },
                    }),
                };

                foreach (PluginInfo.NamespaceMappingInfo nsMapping
                    in this.PluginInfo.IOSPlatform.NamespaceMappings)
                {
                    if (String.IsNullOrEmpty(nsMapping.Prefix))
                    {
                        throw new InvalidOperationException(
                            "Missing prefix for IOS namespace mapping: " + nsMapping.Namespace);
                    }

                    pluginFeatureInfo.Params.Add(new PluginInfo.ConfigParamInfo
                    {
                        Name = "plugin-namespace:" + nsMapping.Prefix,
                        Value = nsMapping.Namespace,
                    });
                }

                foreach (PluginInfo.AssemblyClassInfo classInfo in this.PluginInfo.Assembly.Classes
                    .Where(c => c.MarshalByValue == "true"))
                {
                    pluginFeatureInfo.Params.Add(new PluginInfo.ConfigParamInfo
                    {
                        Name = "plugin-class:" + classInfo.Name,
                        Value = "marshal-by-value",
                    });
                }

                this.PluginInfo.IOSPlatform.ConfigFiles.Add(new PluginInfo.ConfigFileInfo
                {
                    Target = "config.xml",
                    Parent = "/*",
                    Features = new List<PluginInfo.ConfigFeatureInfo>(new[] { pluginFeatureInfo }),
                });
            }
        }

        void CreateWindowsConfigFileInfo()
        {
            if (this.PluginInfo.WindowsPlatform != null)
            {
                PluginInfo.ConfigFeatureInfo pluginFeatureInfo = new PluginInfo.ConfigFeatureInfo
                {
                    Name = c3pServiceName,
                    Params = new List<PluginInfo.ConfigParamInfo>(new[]
                    {
                        // TODO: Use some alternate means to configure the list of proxied namespaces.
                        new PluginInfo.ConfigParamInfo
                        {
                            Name = "plugin-namespace:" + c3pNamespace,
                            Value = String.Empty,
                        },
                        new PluginInfo.ConfigParamInfo
                        {
                            Name = "plugin-namespace:" + c3pCordovaNamespace,
                            Value = String.Empty,
                        },
                    }),
                };

                foreach (PluginInfo.NamespaceMappingInfo nsMapping
                    in this.PluginInfo.WindowsPlatform.NamespaceMappings)
                {
                    if (!String.IsNullOrEmpty(nsMapping.Namespace))
                    {
                        pluginFeatureInfo.Params.Add(new PluginInfo.ConfigParamInfo
                        {
                            Name = "plugin-namespace:" + nsMapping.Namespace,
                            Value = String.Empty,
                        });
                    }
                }

                foreach (PluginInfo.AssemblyClassInfo classInfo in this.PluginInfo.Assembly.Classes
                    .Where(c => c.MarshalByValue == "true"))
                {
                    pluginFeatureInfo.Params.Add(new PluginInfo.ConfigParamInfo
                    {
                        Name = "plugin-class:" + classInfo.Name,
                        Value = "marshal-by-value",
                    });
                }

                this.PluginInfo.WindowsPlatform.ConfigFiles.Add(new PluginInfo.ConfigFileInfo
                {
                    Target = "config.xml",
                    Parent = "/*",
                    Features = new List<PluginInfo.ConfigFeatureInfo>(new[] { pluginFeatureInfo }),
                });
            }
        }

        static string GetJavaSourceFilePackage(string javaFilePath)
        {
            string[] lines = File.ReadAllLines(javaFilePath);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("package ", StringComparison.Ordinal) &&
                    lines[i].EndsWith(";"))
                {
                    string package = lines[i].Substring(0, lines[i].IndexOf(';')).Substring("package ".Length);
                    return package.Trim();
                }
            }

            return null;
        }

        void AddProjectReferences()
        {
            if (this.PluginInfo.WindowsPlatform != null)
            {
                string nativeOutputPath = Path.Combine(this.TargetOutputPath, "windows");
                foreach (string projectFilePattern in new[] { "*.csproj", "*.vcxproj" })
                {
                    foreach (string projectFile in Directory.GetFiles(
                        nativeOutputPath, projectFilePattern, SearchOption.AllDirectories))
                    {
                        string relativePath = "windows/" + projectFile.Substring(nativeOutputPath.Length + 1);
                        this.PluginInfo.WindowsPlatform.Frameworks.Add(new PluginInfo.FrameworkInfo
                        {
                            Source = relativePath,
                            Type = "projectReference",
                            Custom = "true",
                        });
                    }
                }
            }
        }
    }
}
