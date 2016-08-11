// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System.IO;

namespace Microsoft.C3P
{
    /// <summary>
    /// Packs the C3P React Native runtime library into a React Native plugin that other
    /// C3P React Native plugins depend on.
    /// </summary>
    class ReactNativePluginLibPackager : PluginLibPackager
    {
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

            PluginLinker.CopyNativeProjects(this.SourcePath, new[] { "c3p", "reactnative" }, this.TargetOutputPath);

            string scriptOutputPath = Path.Combine(this.TargetOutputPath, "js");
            this.CopyLibTypeScript(scriptOutputPath);

            string modulesOutputPath = Path.Combine(scriptOutputPath, "node_modules");
            Directory.CreateDirectory(modulesOutputPath);
            File.Copy(
                Path.Combine(this.SourcePath, "ts", "node_modules", "react-native.d.ts"),
                Path.Combine(modulesOutputPath, "react-native.d.ts"));

            // React Native supports (most) ES6 language features, particularly Promises.
            string tsconfigFilePath = Path.Combine(scriptOutputPath, "tsconfig.json");
            File.WriteAllText(tsconfigFilePath, File.ReadAllText(tsconfigFilePath).Replace("\"es5\"", "\"es6\""));

            this.FixTSModuleReferences(scriptOutputPath, true);
            Utils.CompileTypeScript(scriptOutputPath);

            this.CopyPackageJson();

            Utils.PackNpmPackage(this.TargetOutputPath);
        }
    }
}
