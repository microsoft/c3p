// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Microsoft.C3P
{
    /// <summary>
    /// Loads the public API details from a CLR assembly and supports serialization of the API details to/from XML.
    /// </summary>
    [XmlRoot("api")]
    public class ClrApi
    {
        [XmlAttribute("platforms")]
        public string Platform { get; set; }

        [XmlIgnore]
        public IEnumerable<string> Platforms
        {
            get
            {
                if (String.IsNullOrEmpty(this.Platform))
                {
                    return new string[0];
                }

                return this.Platform.Split(',');
            }
        }

        [XmlAttribute("language")]
        public string Language { get; set; }

        [XmlElement("assembly")]
        public List<Assembly> Assemblies { get; set; }

        public static ClrApi FromXml(TextReader textReader)
        {
            XmlReader reader = XmlReader.Create(
                textReader,
                new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Document });
            ClrApi clrApi = (ClrApi)new XmlSerializer(typeof(ClrApi)).Deserialize(reader);
            return clrApi;
        }

        public void ToXml(TextWriter writer)
        {
            string defaultNamespace = String.Empty;
            XmlWriter xmlWriter = XmlWriter.Create(
                writer,
                new XmlWriterSettings
                {
                    ConformanceLevel = ConformanceLevel.Document,
                    Encoding = Encoding.UTF8,
                    Indent = true
                });

            XmlSerializer serializer = new XmlSerializer(typeof(ClrApi), defaultNamespace);
            XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
            namespaces.Add(String.Empty, defaultNamespace);
            serializer.Serialize(xmlWriter, this, namespaces);
        }

        public static Assembly LoadFrom(
            string assemblyFilePath,
            ICollection<string> referenceAssemblyPaths,
            ICollection<string> includeNamespaces = null)
        {
            if (String.IsNullOrEmpty(assemblyFilePath))
            {
                throw new ArgumentNullException(nameof(assemblyFilePath));
            }
            else if (!File.Exists(assemblyFilePath))
            {
                throw new FileNotFoundException("Assembly file not found.", assemblyFilePath);
            }

            if (referenceAssemblyPaths != null)
            {
                foreach (string referenceAssemblyPath in referenceAssemblyPaths)
                {
                    if (!File.Exists(referenceAssemblyPath))
                    {
                        throw new FileNotFoundException("Reference assembly not found.", referenceAssemblyPath);
                    }
                }
            }

            Assembly assembly = null;
            string exceptionText = null;

            try
            {
                AssemblyLoader loader = new AssemblyLoader
                {
                    AssemblyFilePath = assemblyFilePath,
                    ReferenceAssemblyPaths = (referenceAssemblyPaths?.ToArray() ?? new string[0]),
                    IncludeNamespaces = includeNamespaces?.ToArray(),
                };

                // It would be better to do this loading in a separate AppDomain, but
                // AppDomain support is buggy in Mono on OS X.
                ////AppDomain tempDomain = AppDomain.CreateDomain("ClrApi.AssemblyLoader", null, new AppDomainSetup());
                ////tempDomain.DoCallBack(loader.Load);
                loader.Load();

                assembly = loader.LoadedAssembly;
                exceptionText = loader.Exception;

                ////AppDomain.Unload(tempDomain);
            }
            catch (Exception ex)
            {
                exceptionText = ex.ToString();
            }

            if (exceptionText != null)
            {
                throw new FileLoadException(
                    "Failed to load assembly",
                    assemblyFilePath,
                    new Exception("Inner exception details: " + exceptionText));
            }

            return assembly;
        }

        private class AssemblyLoader : MarshalByRefObject
        {
            public string AssemblyFilePath { get; set; }

            string AssemblyIl { get; set; }

            public string[] ReferenceAssemblyPaths { get; set; }

            public string[] IncludeNamespaces { get; set; }

            public Assembly LoadedAssembly { get; private set; }

            public string Exception { get; private set; }

            public void Load()
            {
                // When loading for reflection only, it's impossible to get certain things like enum values.
                // So this IL disassembly is used as a supplement to the reflection.
                string ildasmPath;
                string ildasmArgs;
                if (Utils.IsRunningOnMacOS)
                {
                    ildasmPath = "ikdasm";
                    ildasmArgs = "\"" + this.AssemblyFilePath + "\"";
                }
                else
                {
                    ildasmPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) +
                        @"\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools\ildasm.exe";
                    if (!File.Exists(ildasmPath))
                    {
                        throw new FileNotFoundException("ILDASM tool not found at: " + ildasmPath);
                    }

                    ildasmArgs = "/TEXT \"" + this.AssemblyFilePath + "\"";
                }

                StringWriter ilWriter = new StringWriter();
                Utils.Execute(
                    ildasmPath,
                    ildasmArgs,
                    Environment.CurrentDirectory,
                    TimeSpan.FromSeconds(10),
                    line => ilWriter.WriteLine(line));

                try
                {
                    this.AssemblyIl = ilWriter.ToString();

                    List<System.Reflection.Assembly> referenceAssemblies = new List<System.Reflection.Assembly>();
                    foreach (string referenceAssemblyPath in this.ReferenceAssemblyPaths)
                    {
                        var referenceAssembly = System.Reflection.Assembly.ReflectionOnlyLoadFrom(referenceAssemblyPath);
                        referenceAssemblies.Add(referenceAssembly);

                        Log.Verbose("  Referencing " + referenceAssembly.FullName +
                            " from " + referenceAssemblyPath);
                    }

                    AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += (sender, e) =>
                    {
                        System.Reflection.Assembly referenceAssembly =
                            referenceAssemblies.SingleOrDefault(a => a.FullName == e.Name);

                        if (referenceAssembly == null)
                        {
                            throw new TypeLoadException("Reference assembly not provided: " + e.Name);
                        }

                        return referenceAssembly;
                    };

                    string[] winrtSearchPaths =
                        this.ReferenceAssemblyPaths.Select(p => Path.GetDirectoryName(p)).ToArray();

                    WindowsRuntimeMetadata.ReflectionOnlyNamespaceResolve += (sender, e) =>
                    {
                        string path =
                            WindowsRuntimeMetadata.ResolveNamespace(e.NamespaceName, winrtSearchPaths).FirstOrDefault();
                        if (path != null)
                        {
                            e.ResolvedAssemblies.Add(System.Reflection.Assembly.ReflectionOnlyLoadFrom(path));
                        }
                    };

                    var refAssembly = System.Reflection.Assembly.ReflectionOnlyLoadFrom(this.AssemblyFilePath);
                    Assembly assembly = new Assembly
                    {
                        Name = refAssembly.GetName().Name,
                        Version = refAssembly.GetName().Version,
                        CodeBase = refAssembly.CodeBase,
                        Types = new List<Type>(),
                    };
                    LoadTypes(refAssembly, assembly, this.IncludeNamespaces);
                    this.LoadedAssembly = assembly;
                }
                catch (Exception ex)
                {
                    // Don't let any exceptions leak through the cross-app-domain call,
                    // because they might include non-serializable types. Instead, just
                    // report the exception as text.
                    this.Exception = ex.ToString();
                }
            }

            void LoadTypes(System.Reflection.Assembly refAssembly, Assembly assembly, string[] includeNamespaces)
            {
                foreach (System.Type refType in refAssembly.ExportedTypes.Where(t => !t.IsNested))
                {
                    if (includeNamespaces == null || includeNamespaces.Contains(refType.Namespace))
                    {
                        Type type = LoadType(refType, includeNamespaces != null);
                        if (type != null)
                        {
                            assembly.Types.Add(type);
                        }
                    }
                }
            }

            Type LoadType(System.Type refType, bool loadAttributes)
            {
                Type type;
                TypeAttributes typeAttributes = refType.Attributes;

                if (refType.IsClass)
                {
                    if (refType.BaseType?.Name == "Activity")
                    {
                        // For some reason, Android types that extend Activity fail to load for reflection on OS X.
                        // They don't need to be reflected anyway since they are always skipped by the adapter.
                        return null;
                    }

                    if (typeof(MulticastDelegate).IsAssignableFrom(refType.BaseType))
                    {
                        Method invokeMethod = (Method)LoadMember(refType.GetMethod("Invoke"), loadAttributes);

                        Delegate delegateType = new Delegate
                        {
                            ReturnType = invokeMethod.ReturnType,
                            Parameters = invokeMethod.Parameters,
                        };
                        type = delegateType;
                    }
                    else
                    {
                        // A class is detected as static if it is sealed and abstract, or if there are
                        // no instance member properties, methods, or events.
                        bool isStatic = true;
                        if (!(refType.IsSealed && refType.IsAbstract))
                        {
                            foreach (MemberInfo member in
                                refType.GetMembers(BindingFlags.Public | BindingFlags.Instance))
                            {
                                if (member.DeclaringType.Name != "Object" &&
                                    member.DeclaringType.Name != "NSObject" &&
                                    member.Name != "ClassHandle" &&
                                    member.Name != "get_ClassHandle")
                                {
                                    isStatic = false;
                                    break;
                                }
                            }
                        }

                        if (isStatic)
                        {
                            typeAttributes |= TypeAttributes.Sealed | TypeAttributes.Abstract;
                        }

                        Class classType = new Class
                        {
                            IsStatic = isStatic,
                            IsSealed = isStatic || refType.IsSealed,
                            IsAbstract = isStatic || refType.IsAbstract,
                            ExtendsType = GetTypeFullName(refType.BaseType),
                            IsGenericType = refType.IsGenericTypeDefinition,
                        };
                        classType.ImplementsTypes = refType.GetInterfaces().Select(i => GetTypeFullName(i)).ToList();
                        classType.GenericArgumentNames = (refType.IsGenericTypeDefinition ?
                                refType.GetGenericArguments().Select(t => t.Name).ToList() : null);
                        type = classType;
                    }
                }
                else if (refType.IsInterface)
                {
                    Interface interfaceType = new Interface();
                    interfaceType.ExtendsTypes = refType.GetInterfaces().Select(i => GetTypeFullName(i)).ToList();
                    interfaceType.GenericArgumentNames = (refType.IsGenericTypeDefinition ?
                            refType.GetGenericArguments().Select(t => t.Name).ToList() : null);
                    type = interfaceType;
                }
                else if (refType.IsEnum)
                {
                    Enum enumType = new Enum
                    {
                        UnderlyingType = GetTypeFullName(refType.GetEnumUnderlyingType()),
                    };
                    type = enumType;
                }
                else if (refType.IsValueType)
                {
                    Struct structType = new Struct();
                    structType.ImplementsTypes = refType.GetInterfaces().Select(i => GetTypeFullName(i)).ToList();
                    structType.GenericArgumentNames = (refType.IsGenericTypeDefinition ?
                            refType.GetGenericArguments().Select(t => t.Name).ToList() : null);
                    type = structType;
                }
                else
                {
                    throw new NotSupportedException("Unsupported type: " + GetTypeFullName(refType));
                }

                type.Name = GetTypeName(refType);
                type.Namespace = refType.Namespace;
                type.Attributes = typeAttributes;

                if (loadAttributes)
                {
                    type.CustomAttributes = LoadAttributes(refType.CustomAttributes);
                }

                if (!(type is Delegate))
                {
                    LoadMembers(refType, type, loadAttributes);
                }

                return type;
            }

            void LoadMembers(System.Type refType, Type type, bool loadAttributes)
            {
                type.Members = new List<Member>();

                BindingFlags bindingFlags =
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

                foreach (System.Reflection.MemberInfo refMember in refType.GetMembers(bindingFlags))
                {
                    Member member = LoadMember(refMember, loadAttributes);
                    if (member != null)
                    {
                        type.Members.Add(member);
                    }
                }
            }

            Member LoadMember(System.Reflection.MemberInfo refMember, bool loadAttributes)
            {
                Member member;

                if (refMember.MemberType == MemberTypes.NestedType)
                {
                    member = LoadType((System.Type)refMember, loadAttributes);
                }
                else if (refMember.MemberType == MemberTypes.Field)
                {
                    if (refMember.DeclaringType.IsEnum)
                    {
                        member = LoadEnumValue((FieldInfo)refMember);
                    }
                    else
                    {
                        member = LoadField((FieldInfo)refMember);
                    }
                }
                else if (refMember.MemberType == MemberTypes.Property)
                {
                    member = LoadProperty((PropertyInfo)refMember);
                }
                else if (refMember.MemberType == MemberTypes.Constructor)
                {
                    member = LoadConstructor((ConstructorInfo)refMember);
                }
                else if (refMember.MemberType == MemberTypes.Method)
                {
                    if (((MethodInfo)refMember).IsSpecialName)
                    {
                        member = null;
                    }
                    else
                    {
                        member = LoadMethod((MethodInfo)refMember);
                    }
                }
                else if (refMember.MemberType == MemberTypes.Event)
                {
                    member = LoadEvent((EventInfo)refMember);
                }
                else
                {
                    throw new NotSupportedException(
                        "Unsupported member type: " + refMember.MemberType + " " +
                        GetTypeFullName(refMember.DeclaringType) + "." + refMember.Name);
                }

                if (loadAttributes && member != null)
                {
                    member.CustomAttributes = LoadAttributes(refMember.GetCustomAttributesData());
                }

                return member;
            }

            Field LoadField(System.Reflection.FieldInfo refField)
            {
                if (refField.IsSpecialName)
                {
                    return null;
                }

                string value = null;
                if (refField.IsStatic && refField.IsLiteral)
                {
                    string declaration = ".field public static literal int32 " + refField.Name + " = ";
                    int declarationIndex = this.AssemblyIl.IndexOf(declaration);
                    if (declarationIndex >= 0)
                    {
                        int valueStart = this.AssemblyIl.IndexOf("(0x", declarationIndex + declaration.Length) + 3;
                        int valueEnd = this.AssemblyIl.IndexOf(')', valueStart);
                        string valueString = this.AssemblyIl.Substring(valueStart, valueEnd - valueStart);
                        value = Int64.Parse(valueString, NumberStyles.HexNumber).ToString();
                    }
                }

                Field field = new Field
                {
                    Name = refField.Name,
                    FieldType = GetTypeFullName(refField.FieldType),
                    IsStatic = refField.IsStatic,
                    Value = value,
                    Attributes = refField.Attributes,
                };
                return field;
            }

            EnumValue LoadEnumValue(System.Reflection.FieldInfo refField)
            {
                if (refField.IsSpecialName)
                {
                    return null;
                }

                long value = 0;
                string declaration = ".field public static literal valuetype " + refField.DeclaringType.FullName +
                    " " + refField.Name + " = ";
                int declarationIndex = this.AssemblyIl.IndexOf(declaration);
                if (declarationIndex >= 0)
                {
                    int valueStart = this.AssemblyIl.IndexOf("(0x", declarationIndex + declaration.Length) + 3;
                    int valueEnd = this.AssemblyIl.IndexOf(')', valueStart);
                    string valueString = this.AssemblyIl.Substring(valueStart, valueEnd - valueStart);
                    value = Int64.Parse(valueString, NumberStyles.HexNumber);
                }

                EnumValue field = new EnumValue
                {
                    Name = refField.Name,
                    Value = value,
                };
                return field;
            }

            Property LoadProperty(System.Reflection.PropertyInfo refProperty)
            {
                bool isStatic =
                    (refProperty.CanRead && refProperty.GetGetMethod().IsStatic) ||
                    (refProperty.CanRead && refProperty.GetGetMethod().IsStatic);
                System.Reflection.MethodInfo getMethod =
                    (refProperty.CanRead ? refProperty.GetGetMethod(false) : null);
                System.Reflection.MethodInfo setMethod =
                    (refProperty.CanWrite ? refProperty.GetSetMethod(false) : null);
                Property property = new Property
                {
                    Name = refProperty.Name,
                    IsStatic = isStatic,
                    PropertyType = GetTypeFullName(refProperty.PropertyType),
                    GetMethod = (getMethod != null ? LoadMethod(getMethod) : null),
                    SetMethod = (setMethod != null ? LoadMethod(setMethod) : null),
                    Attributes = refProperty.Attributes,
                };
                property.IndexParameters = refProperty.GetIndexParameters().Select(p => LoadParameter(p)).ToList();
                return property;
            }

            Constructor LoadConstructor(System.Reflection.ConstructorInfo refConstructor)
            {
                Constructor constructor = new Constructor
                {
                    Name = refConstructor.DeclaringType.Name,
                    CallingConvention = refConstructor.CallingConvention,
                    Attributes = refConstructor.Attributes,
                };
                constructor.Parameters = refConstructor.GetParameters().Select(p => LoadParameter(p)).ToList();
                return constructor;
            }

            Method LoadMethod(System.Reflection.MethodInfo refMethod)
            {
                Method method = new Method
                {
                    Name = refMethod.Name,
                    IsStatic = refMethod.IsStatic,
                    IsAbstract = refMethod.IsAbstract,
                    ReturnType = GetTypeFullName(refMethod.ReturnType),
                    CallingConvention = refMethod.CallingConvention,
                    Attributes = refMethod.Attributes,
                };
                method.Parameters = refMethod.GetParameters().Select(p => LoadParameter(p)).ToList();
                return method;
            }

            Parameter LoadParameter(System.Reflection.ParameterInfo refParameter)
            {
                Parameter parameter = new Parameter
                {
                    Name = refParameter.Name,
                    ParameterType = GetTypeFullName(refParameter.ParameterType),
                    HasDefaultValue = false, // refParameter.HasDefaultValue,
                    DefaultValue = null, //(refParameter.HasDefaultValue ? refParameter.DefaultValue : null),
                    Attributes = refParameter.Attributes,
                };
                return parameter;
            }

            Event LoadEvent(System.Reflection.EventInfo refEvent)
            {
                Event eventObject = new Event
                {
                    Name = refEvent.Name,
                    IsStatic = refEvent.GetAddMethod().IsStatic,
                    EventHandlerType = GetTypeFullName(refEvent.EventHandlerType),
                    Attributes = refEvent.Attributes,
                };
                return eventObject;
            }

            List<Attribute> LoadAttributes(IEnumerable<CustomAttributeData> refAttributes)
            {
                if (refAttributes == null)
                {
                    return new List<Attribute>();
                }

                return refAttributes.Select(a => LoadAttribute(a)).Where(a => a != null).ToList();
            }

            Attribute LoadAttribute(CustomAttributeData refAttribute)
            {
                string type = refAttribute.AttributeType.FullName;
                if (!type.StartsWith(typeof(ClrApi).Namespace + "."))
                {
                    // Non-C3P custom attributes are ignored for now.
                    return null;
                }

                Attribute attribute = new Attribute
                {
                    AttributeType = refAttribute.AttributeType.FullName,
                    Arguments = refAttribute.ConstructorArguments.Select(a => LoadAttributeArgument(type, a))
                        .Concat(refAttribute.NamedArguments.Select(a => LoadAttributeArgument(type, a))).ToList(),
                };
                return attribute;
            }

            AttributeArgument LoadAttributeArgument(
                string attributeTypeFullName,
                CustomAttributeTypedArgument refArgument)
            {
                try
                {
                    AttributeArgument argument = new AttributeArgument
                    {
                        ArgumentType = refArgument.ArgumentType.FullName,
                        Value = (string)refArgument.Value,
                    };
                    return argument;
                }
                catch (InvalidCastException ex)
                {
                    throw new NotSupportedException(
                        "Attribute values with types other than string are not supported. " +
                        "Attribute: " + attributeTypeFullName, ex);
                }
            }

            AttributeArgument LoadAttributeArgument(
                string attributeTypeFullName,
                CustomAttributeNamedArgument refArgument)
            {
                try
                {
                    AttributeArgument argument = new AttributeArgument
                    {
                        Name = refArgument.MemberName,
                        ArgumentType = refArgument.TypedValue.ArgumentType.FullName,
                        Value = (string)refArgument.TypedValue.Value,
                    };
                    return argument;
                }
                catch (InvalidCastException ex)
                {
                    throw new NotSupportedException(
                        "Attribute values with types other than string are not supported. " +
                        "Attribute: " + attributeTypeFullName, ex);
                }
            }

            static string GetTypeName(System.Type refType)
            {
                if (refType.IsNested)
                {
                    return GetTypeName(refType.DeclaringType) + '+' + refType.Name;
                }
                else if (refType.IsGenericTypeDefinition)
                {
                    return refType.Name.Substring(0, refType.Name.IndexOf('`'));
                }
                else
                {
                    return refType.Name;
                }
            }

            static string GetTypeFullName(System.Type refType)
            {
                if (refType.IsConstructedGenericType)
                {
                    string baseName = refType.GetGenericTypeDefinition().FullName;
                    baseName = baseName.Substring(0, baseName.IndexOf('`'));
                    string arguments =
                        String.Join(", ", refType.GetGenericArguments().Select(t => GetTypeFullName(t)));
                    return $"{baseName}<{arguments}>";
                }
                else
                {
                    return refType.FullName;
                }
            }
        }
    }
}
