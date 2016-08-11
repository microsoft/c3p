// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System.IO;

namespace Microsoft.C3P
{
    /// <summary>
    /// Packs the C3P Cordova runtime library into a Cordova plugin that other C3P Cordova plugins depend on.
    /// </summary>
    class CordovaPluginLibPackager : PluginLibPackager
    {
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

            PluginLinker.CopyNativeCode(this.SourcePath, new[] { "c3p", "cordova" }, this.TargetOutputPath);

            string scriptOutputPath = Path.Combine(this.TargetOutputPath, "www");
            this.CopyLibTypeScript(scriptOutputPath);

            File.Copy(
                Path.Combine(this.SourcePath, "ts", "node_modules", "cordova.d.ts"),
                Path.Combine(scriptOutputPath, "cordova.d.ts"));
            File.Copy(
                Path.Combine(this.SourcePath, "ts", "node_modules", "es6-promise.d.ts"),
                Path.Combine(scriptOutputPath, "es6-promise.d.ts"));

            this.FixTSModuleReferences(scriptOutputPath, false);
            Utils.CompileTypeScript(scriptOutputPath);

            this.GeneratePluginXml(this.TargetOutputPath, scriptOutputPath);
            this.CopyPackageJson();

            Utils.PackNpmPackage(this.TargetOutputPath);
        }

        void GeneratePluginXml(string nativeOutputPath, string scriptOutputPath)
        {
            Log.Message("    plugin.xml");

            PluginInfo pluginInfo;
            string pluginXmlPath = Path.Combine(this.SourcePath, "ts", this.TargetName, "plugin.xml");
            using (StreamReader reader = File.OpenText(pluginXmlPath))
            {
                pluginInfo = PluginInfo.FromXml(reader, PluginInfo.C3PNamespaceUri);
            }

            CordovaPluginLinker.CollectSourceFilesInfo(nativeOutputPath, pluginInfo);

            foreach (string scriptFile in Directory.GetFiles(scriptOutputPath, "*.js"))
            {
                string scriptModuleName = Path.GetFileNameWithoutExtension(scriptFile);
                PluginInfo.JavaScriptModuleInfo jsModule = new PluginInfo.JavaScriptModuleInfo
                {
                    Name = scriptModuleName,
                    Source = "www/" + scriptModuleName + ".js",
                };

                if (scriptModuleName == "CordovaWindowsBridge")
                {
                    jsModule.Runs = "true";
                    pluginInfo.WindowsPlatform.JavaScriptModules.Add(jsModule);
                }
                else
                {
                    pluginInfo.JavaScriptModules.Add(jsModule);
                }
            }

            using (StreamWriter writer = File.CreateText(Path.Combine(this.TargetOutputPath, "plugin.xml")))
            {
                pluginInfo.ToXml(writer, CordovaPluginLinker.cordovaPluginXmlNamespace);
            }
        }
    }
}
