// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Microsoft.C3P
{
    /// <summary>
    /// Represents a C3P or Cordova plugin.xml file and supports serialization to/from XML.
    /// </summary>
    [XmlRoot("plugin")]
    public class PluginInfo
    {
        public const string AndroidPlatformName = "android";
        public const string IOSPlatformName = "ios";
        public const string WindowsPlatformName = "windows";
        public const string PortablePlatformName = "portable";

        public const string CordovaTargetName = "cordova";
        public const string ReactNativeTargetName = "reactnative";
        public const string XamarinTargetName = "xamarin";

        public const string C3PNamespaceUri = "http://schemas.microsoft.com/c3p/0.1";
        const string AndroidNamespaceUri = "http://schemas.android.com/apk/res/android";

        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlAttribute("version")]
        public string Version { get; set; }

        [XmlElement("name")]
        public string Name { get; set; }

        [XmlElement("description")]
        public string Description { get; set; }

        [XmlElement("author")]
        public string Author { get; set; }

        [XmlElement("keywords")]
        public string Keywords { get; set; }

        [XmlIgnore]
        public string[] KeywordsArray
        {
            get
            {
                if (String.IsNullOrEmpty(this.Keywords))
                {
                    return new string[0];
                }
                else
                {
                    return this.Keywords.Split(',').Select(k => k.Trim()).ToArray();
                }
            }
        }

        [XmlElement("license")]
        public string License { get; set; }

        public class AssemblyInfo
        {
            [XmlAttribute("name")]
            public string Name { get; set; }

            [XmlAttribute("js-target")]
            public string JavaScriptTarget { get; set; }

            [XmlElement("class")]
            public List<AssemblyClassInfo> Classes { get; set; }
        }

        [XmlElement("assembly")]
        public AssemblyInfo Assembly { get; set; }

        public class AssemblyClassInfo
        {
            [XmlAttribute("name")]
            public string Name { get; set; }

            [XmlAttribute("marshal-by-value")]
            public string MarshalByValue { get; set; }

            [XmlElement("type-binding")]
            public List<TypeBindingInfo> TypeBindings { get; set; }
        }

        public class JavaScriptModuleInfo
        {
            [XmlAttribute("name")]
            public string Name { get; set; }

            [XmlAttribute("src")]
            public string Source { get; set; }

            [XmlElement("clobbers")]
            public ClobbersInfo Clobbers { get; set; }

            [XmlElement("runs")]
            public string Runs { get; set; }
        }

        [XmlElement("js-module")]
        public List<JavaScriptModuleInfo> JavaScriptModules { get; set; }

        public class ClobbersInfo
        {
            [XmlAttribute("target")]
            public string Target { get; set; }

            [XmlAttribute("instance")]
            public string Instance { get; set; }
        }

        public class PlatformInfo
        {
            [XmlAttribute("name")]
            public string Name { get; set; }

            [XmlElement("js-module")]
            public List<JavaScriptModuleInfo> JavaScriptModules { get; set; }

            [XmlElement("config-file")]
            public List<ConfigFileInfo> ConfigFiles { get; set; }

            [XmlElement("header-file")]
            public List<SourceFileInfo> HeaderFiles { get; set; }

            [XmlElement("source-file")]
            public List<SourceFileInfo> SourceFiles { get; set; }

            [XmlElement("resource-file")]
            public List<SourceFileInfo> ResourceFiles { get; set; }

            [XmlElement ("lib-file")]
            public List<SourceFileInfo> LibFiles { get; set; }

            [XmlElement("include")]
            public List<NamespaceMappingInfo> IncludeNamespaces { get; set; }

            [XmlElement("namespace-mapping")]
            public List<NamespaceMappingInfo> NamespaceMappings { get; set; }

            [XmlElement("enum")]
            public List<EnumInfo> Enums { get; set; }

            [XmlElement("framework")]
            public List<FrameworkInfo> Frameworks { get; set; }

            [XmlElement("hook")]
            public List<HookInfo> Hooks { get; set; }
        }

        public class SourceFileInfo
        {
            [XmlAttribute("src")]
            public string SourceFilePath { get; set; }

            [XmlAttribute("target-dir")]
            public string TargetDirectoryPath { get; set; }
        }

        [XmlElement("platform")]
        public List<PlatformInfo> Platforms { get; set; }

        [XmlIgnore]
        public PlatformInfo AndroidPlatform
        {
            get
            {
                if (this.Platforms == null)
                {
                    return null;
                }

                return this.Platforms.FirstOrDefault(p => p.Name == AndroidPlatformName);
            }
        }

        [XmlIgnore]
        public PlatformInfo IOSPlatform
        {
            get
            {
                if (this.Platforms == null)
                {
                    return null;
                }

                return this.Platforms.FirstOrDefault(p => p.Name == IOSPlatformName);
            }
        }

        [XmlIgnore]
        public PlatformInfo WindowsPlatform
        {
            get
            {
                if (this.Platforms == null)
                {
                    return null;
                }

                return this.Platforms.FirstOrDefault(p => p.Name == WindowsPlatformName);
            }
        }

        public class ConfigFileInfo
        {
            [XmlAttribute("target")]
            public string Target { get; set; }

            [XmlAttribute("parent")]
            public string Parent { get; set; }

            [XmlElement("feature")]
            public List<ConfigFeatureInfo> Features { get; set; }

            [XmlAnyElement]
            public List<XmlElement> AdditionalElements { get; set; }
        }

        public class ConfigFeatureInfo
        {
            [XmlAttribute("name")]
            public string Name { get; set; }


            [XmlElement("param")]
            public List<ConfigParamInfo> Params { get; set; }
        }

        public class ConfigParamInfo
        {
            [XmlAttribute("name")]
            public string Name { get; set; }

            [XmlAttribute("value")]
            public string Value { get; set; }
        }

        public class NamespaceMappingInfo
        {
            [XmlAttribute("prefix")]
            public string Prefix { get; set; }

            [XmlAttribute("package")]
            public string Package { get; set; }

            [XmlAttribute("namespace")]
            public string Namespace { get; set; }
        }

        public class EnumInfo
        {
            [XmlAttribute("name")]
            public string Name { get; set; }
        }

        public class TypeBindingInfo
        {
            [XmlAttribute("member")]
            public string Member { get; set; }

            [XmlAttribute("parameterIndex")]
            public string ParameterIndex { get; set; }

            [XmlAttribute("type")]
            public string Type { get; set; }
        }

        public class FrameworkInfo
        {
            [XmlAttribute("src")]
            public string Source { get; set; }

            [XmlAttribute("custom")]
            public string Custom { get; set; }

            [XmlAttribute("type")]
            public string Type { get; set; }
        }

        public class HookInfo
        {
            [XmlAttribute("type")]
            public string Type { get; set; }

            [XmlAttribute("src")]
            public string Source { get; set; }
        }

        public static PluginInfo FromXml(TextReader reader, string defaultNamespace = null)
        {
            XmlReader xmlReader = XmlReader.Create(
                reader,
                new XmlReaderSettings
                {
                    ConformanceLevel = ConformanceLevel.Document
                });
            defaultNamespace = defaultNamespace ?? C3PNamespaceUri;
            XmlSerializer serializer = new XmlSerializer(typeof(PluginInfo), defaultNamespace);
            PluginInfo pluginInfo = (PluginInfo)serializer.Deserialize(xmlReader);
            return pluginInfo;
        }

        public void ToXml(TextWriter writer, string defaultNamespace = null)
        {
            StringWriter tempWriter = new StringWriter();

            XmlWriter xmlWriter = XmlWriter.Create(
                tempWriter,
                new XmlWriterSettings
                {
                    ConformanceLevel = ConformanceLevel.Document,
                    Encoding = Encoding.UTF8,
                    Indent = true,
                    IndentChars = "    ",
                });

            XmlSerializer serializer = new XmlSerializer(typeof(PluginInfo), C3PNamespaceUri);
            XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
            namespaces.Add(String.Empty, C3PNamespaceUri);
            if (this.FindAndroidNamespace())
            {
                namespaces.Add("android", AndroidNamespaceUri);
            }

            serializer.Serialize(xmlWriter, this, namespaces);

            // Replace the C3P namespace with the requested namespace. This is using ordinary
            // string replacement because it's extremely difficult to do using the XML object model.
            string xml = tempWriter.ToString();
            if (defaultNamespace != null)
            {
                xml = xml.Replace(C3PNamespaceUri, defaultNamespace);
            }
            writer.Write(xml);
        }

        bool FindAndroidNamespace()
        {
            return this.Platforms != null && this.Platforms.Any(p => p.ConfigFiles != null &&
                p.ConfigFiles.Any(c => c.AdditionalElements != null &&
                    c.AdditionalElements.Any(e =>
                        e.ChildNodes.Cast<XmlNode>().Any(n => n.NamespaceURI == AndroidNamespaceUri) ||
                        e.Attributes.Cast<XmlAttribute>().Any(a => a.NamespaceURI == AndroidNamespaceUri))));
        }
    }
}
