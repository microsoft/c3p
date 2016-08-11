// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.IO;

namespace Microsoft.C3P
{
    /// <summary>
    /// Base class for subclasses that pack the C3P runtime library code into a plugin
    /// that other C3P plugins depend on.
    /// </summary>
    abstract class PluginLibPackager
    {
        public static PluginLibPackager Create(string target)
        {
            if (target == PluginInfo.CordovaTargetName)
            {
                return new CordovaPluginLibPackager();
            }
            else if (target == PluginInfo.ReactNativeTargetName)
            {
                return new ReactNativePluginLibPackager();
            }
            else
            {
                throw new NotSupportedException("Target not supported: " + target);
            }
        }

        public abstract string TargetName { get; }

        public string SourcePath { get; set; }

        public string OutputPath { get; set; }

        public string TargetOutputPath { get; private set; }

        protected virtual void Init()
        {
            if (String.IsNullOrEmpty(this.SourcePath))
            {
                throw new ArgumentNullException(nameof(SourcePath));
            }
            else if (String.IsNullOrEmpty(this.OutputPath))
            {
                throw new ArgumentNullException(nameof(OutputPath));
            }

            if (!Directory.Exists(this.SourcePath))
            {
                throw new DirectoryNotFoundException("Source path not found: " + this.SourcePath);
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
                Utils.ClearDirectory(this.TargetOutputPath);
            }

            Log.Important($"Packing c3p-{this.TargetName} at\n" +
                Utils.EnsureTrailingSlash(Path.GetFullPath(this.TargetOutputPath)));
        }

        public abstract void Run();

        protected void CopyPackageJson()
        {
            Log.Message("    package.json");
            File.Copy(
                Path.Combine(this.SourcePath, "ts", this.TargetName, "package.json"),
                Path.Combine(this.TargetOutputPath, "package.json"));

            // Update the file write time so it can be used with incremental builds.
            File.SetLastWriteTime(Path.Combine(this.TargetOutputPath, "package.json"), DateTime.Now);
        }

        protected void CopyLibTypeScript(string scriptTargetPath)
        {
            Log.Message(Utils.EnsureTrailingSlash(Path.GetFullPath(scriptTargetPath)));

            if (!Directory.Exists(scriptTargetPath))
            {
                Directory.CreateDirectory(scriptTargetPath);
            }

            foreach (string moduleName in new[] { "c3p", this.TargetName })
            {
                foreach (string tsFile in Directory.GetFiles(Path.Combine(this.SourcePath, "ts", moduleName)))
                {
                    string fileName = Path.GetFileName(tsFile);
                    if (fileName.EndsWith(".ts") || fileName.EndsWith(".js") || fileName.EndsWith(".js.map"))
                    {
                        Log.Message("    " + Path.Combine(Path.GetFileName(scriptTargetPath), fileName));
                        File.Copy(tsFile, Path.Combine(scriptTargetPath, fileName));
                    }
                }
            }

            Log.Message("    " + Path.Combine(Path.GetFileName(scriptTargetPath), "tsconfig.json"));
            File.Copy(
                Path.Combine(this.SourcePath, "ts", "tsconfig.json"),
                Path.Combine(scriptTargetPath, "tsconfig.json"));
        }

        protected void FixTSModuleReferences(string scriptOutputPath, bool es6)
        {
            foreach (string tsFile in Directory.GetFiles(scriptOutputPath, "*.ts"))
            {
                string[] lines = File.ReadAllLines(tsFile);
                bool modified = false;

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].IndexOf("from \"../C3P/") >= 0)
                    {
                        lines[i] = lines[i].Replace("from \"../C3P/", "from \"./");
                        modified = true;
                    }
                    else if (lines[i].IndexOf("from \"es6-promise\"") >= 0)
                    {
                        if (es6)
                        {
                            lines[i] = String.Empty;
                        }
                        else
                        {
                            lines[i] = lines[i].Replace("from \"es6-promise\"", "from \"./es6-promise\"");
                        }
                        modified = true;
                    }
                    else if (lines[i].IndexOf("from \"cordova\"") >= 0)
                    {
                        lines[i] = lines[i].Replace("from \"cordova\"", "from \"./cordova\"");
                        modified = true;
                    }
                }

                if (modified)
                {
                    File.WriteAllLines(tsFile, lines);
                }
            }
        }
    }
}
