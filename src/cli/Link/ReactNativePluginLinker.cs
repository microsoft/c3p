// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.C3P
{
    /// <summary>
    /// Links platform intermediate files into a React Native plugin.
    /// </summary>
    class ReactNativePluginLinker : PluginLinker
    {
        const string c3pReactNativeAndroidPackage = "com.microsoft.c3p.reactnative";
        const string c3pReactNativePackageName = "c3p-reactnative";

        public override string TargetName
        {
            get
            {
                return PluginInfo.ReactNativeTargetName;
            }
        }

        public override void Run()
        {
            this.Init();
            ClrApi pluginApi = this.MergeApis(this.LoadApis());

            PluginLinker.CopyNativeProjects(this.SourcePath, null, this.TargetOutputPath);

            string scriptOutputPath = Path.Combine(this.TargetOutputPath, "js");
            if (!Directory.Exists(scriptOutputPath))
            {
                Directory.CreateDirectory(scriptOutputPath);
            }

            this.CreateTypeScriptBindings(
                pluginApi, true, true, c3pReactNativePackageName, scriptOutputPath);

            string modulesDirectoryPath = Path.Combine(scriptOutputPath, "node_modules");
            Directory.CreateDirectory(modulesDirectoryPath);
            this.CreateBridgeModuleTypeScriptDefinition(modulesDirectoryPath);
            Utils.ExtractResource("NativeObject.ts", modulesDirectoryPath);
            Utils.ExtractResource("NativeBridge.ts", modulesDirectoryPath);
            Utils.ExtractResource("react-native.d.ts", modulesDirectoryPath);
            Utils.ExtractResource("tsconfig.json", scriptOutputPath);

            this.GeneratePluginModule(pluginApi, scriptOutputPath);

            this.RemoveES6PromiseImports(scriptOutputPath);

            Utils.CompileTypeScript(scriptOutputPath);

            Log.Message(Utils.EnsureTrailingSlash(Path.GetFullPath(this.TargetOutputPath)));
            this.GeneratePackageJson(c3pReactNativePackageName, new[] { "    \"main\": \"js/plugin.js\"" });
            Utils.PackNpmPackage(this.TargetOutputPath);
        }

        void RemoveES6PromiseImports(string scriptOutputPath)
        {
            // React Native supports (most) ES6 language features, particularly Promises.
            string tsconfigFilePath = Path.Combine(scriptOutputPath, "tsconfig.json");
            File.WriteAllText(tsconfigFilePath, File.ReadAllText(tsconfigFilePath).Replace("\"es5\"", "\"es6\""));

            foreach (string tsFilePath in Directory.GetFiles(scriptOutputPath, "*.ts", SearchOption.AllDirectories))
            {
                // This is probably very inefficient, but not worth optimizing for now.
                File.WriteAllLines(tsFilePath, File.ReadAllLines(tsFilePath)
                    .Where(line => line.IndexOf("es6-promise") < 0)
                    .Select(line => line.Replace(", Promise }", "}")));
            }
        }

        void CreateBridgeModuleTypeScriptDefinition(string outputDirectoryPath)
        {
            string bridgeInterfaceName = "NativeAsyncBridge";
            File.WriteAllLines(Path.Combine(outputDirectoryPath, c3pReactNativePackageName + ".d.ts"), new[]
            {

                $"import {{ NativeObject, NativeReference }} from \"./NativeObject\";",
                $"import {{ {bridgeInterfaceName} }} from \"./NativeBridge\";",
                $"declare var bridge: {bridgeInterfaceName};",
                $"export {{ bridge, NativeObject, NativeReference }}; ",
            });
        }

        void GeneratePluginModule(ClrApi pluginApi, string scriptOutputPath)
        {
            using (CodeWriter code = new CodeWriter(Path.Combine(scriptOutputPath, "plugin.ts")))
            {
                code.Code("import { Platform, NativeModules } from \"react-native\";");
                code.Code();

                HashSet<string> allNamespaces = new HashSet<string>();

                if (this.PluginInfo.AndroidPlatform != null)
                {
                    code.Code("if (Platform.OS === \"android\") {");

                    foreach (var namespaceMapping in this.PluginInfo.AndroidPlatform.NamespaceMappings)
                    {
                        code.Code("\tNativeModules.C3P.registerNamespaceMapping(" +
                            $"\"{namespaceMapping.Namespace}\", \"{namespaceMapping.Package}\");");
                        allNamespaces.Add(namespaceMapping.Namespace);
                    }

                    code.Code("}");
                }

                if (this.PluginInfo.IOSPlatform != null)
                {
                    code.Code("if (Platform.OS === \"ios\") {");

                    foreach (var namespaceMapping in this.PluginInfo.IOSPlatform.NamespaceMappings)
                    {
                        code.Code("\tNativeModules.C3P.registerNamespaceMapping(" +
                            $"\"{namespaceMapping.Namespace}\", \"{namespaceMapping.Prefix}\");");
                        allNamespaces.Add(namespaceMapping.Namespace);
                    }

                    code.Code("}");
                }

                if (this.PluginInfo.WindowsPlatform != null)
                {
                    code.Code("if (Platform.OS === \"windows\") {");

                    foreach (var namespaceInfo in this.PluginInfo.WindowsPlatform.IncludeNamespaces)
                    {
                        code.Code("\tNativeModules.C3P.registerNamespaceMapping(" +
                            $"\"{namespaceInfo.Namespace}\");");
                        allNamespaces.Add(namespaceInfo.Namespace);
                    }

                    code.Code("}");
                }

                code.Code();

                foreach (var classInfo in this.PluginInfo.Assembly.Classes.Where(c => c.MarshalByValue == "true"))
                {
                    code.Code($"NativeModules.C3P.registerMarshalByValueClass(\"{classInfo.Name}\");");
                }

                string[] includeNamespaces = allNamespaces.ToArray();

                code.Code();
                foreach (Type pluginType in pluginApi.Assemblies.Single().Types
                    .Where(t => includeNamespaces.Contains(t.Namespace)))
                {
                    code.Code($"import {pluginType.Name} = require(\"./{pluginType.Name}\");");
                }

                code.Code();
                code.Code("export = {");

                foreach (Type pluginType in pluginApi.Assemblies.Single().Types
                    .Where(t => includeNamespaces.Contains(t.Namespace)))
                {
                    code.Code($"\t{pluginType.Name}: {pluginType.Name},");
                }

                code.Code("}");
            }
        }
    }
}

