// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Microsoft.C3P
{
    /// <summary>
    /// Generates typescript bindings for the cross-platform native APIs.
    /// </summary>
    /// <remarks>
    /// Type TypeScript bindings depend on the C3P runtime JavaScript library
    /// native class libraries for the target platforms, which are merged into the
    /// application via a separate plugin.
    /// </remarks>
    class TypeScriptEmitter
    {
        /// <summary>
        /// Parameters and options for how the plugin is to be built.
        /// </summary>
        public PluginInfo PluginInfo { get; set; }

        /// <summary>
        /// Assembly containing details about the APIs to be bound.
        /// </summary>
        public Assembly Assembly { get; set; }

        /// <summary>
        /// Path to a directory where the TypeScript code files are to be written.
        /// </summary>
        public string OutputPath { get; set; }

        /// <summary>
        /// True if all properties and methods (for marshal-by-reference types) should be
        /// automatically transformed to asyncronous (returning Promises).
        /// </summary>
        public bool ForceAsyncAPIs { get; set; }

        /// <summary>
        /// True if the target JavaScript environment supports ES6 functionality like Promises.
        /// </summary>
        public bool ES6 { get; set; }

        /// <summary>
        /// Name of the JavaScript module to be imported that provides bridging APIs.
        /// </summary>
        public string BridgeModuleName { get; set; }

        public void Run()
        {
            if (this.PluginInfo == null)
            {
                throw new ArgumentNullException(nameof(PluginInfo));
            }
            else if (this.Assembly == null)
            {
                throw new ArgumentNullException(nameof(Assembly));
            }
            else if (String.IsNullOrEmpty(this.OutputPath))
            {
                throw new ArgumentNullException(nameof(OutputPath));
            }

            if (!Directory.Exists(this.OutputPath))
            {
                throw new DirectoryNotFoundException(
                    "TypeScript output directory path not found: " + this.OutputPath);
            }

            this.GenerateTypeScriptBindings();
        }

        void GenerateTypeScriptBindings()
        {
            Log.Important("Generating TypeScript bindings at\n" + Utils.EnsureTrailingSlash(this.OutputPath));

            // Skip Xamarin-supporting types in the C3P namespace.
            Type[] types = this.Assembly.Types.Where(
                t => t.Namespace != typeof(TypeScriptEmitter).Namespace).ToArray();

            foreach (Type type in types)
            {
                string tsFileName = type.Name + ".ts";
                Log.Message("    " + tsFileName);
                using (CodeWriter code = new CodeWriter(Path.Combine(this.OutputPath, tsFileName)))
                {
                    IEnumerable<Type> otherTypes = types.Where(t => t != type);
                    this.WriteTypeScriptBindingsForType(type, otherTypes, code);
                }
            }
        }

        void WriteTypeScriptBindingsForType(
            Type type,
            IEnumerable<Type> importTypes,
            CodeWriter code)
        {
            string simpleTypeName;
            if (!type.IsNested)
            {
                if (!(type is Enum))
                {
                    foreach (Type importType in importTypes)
                    {
                        code.Code($"import {importType.Name} = require(\"./{importType.Name}\");");
                    }

                    if (!(type is Interface))
                    {
                        code.Code();
                        code.Code($"import {{ bridge, NativeObject, NativeReference{(this.ES6 ? "" : ", Promise")} }} " +
                            $"from \"{this.BridgeModuleName}\";");
                    }
                    else if (!this.ES6)
                    {
                        code.Code();
                        code.Code($"import {{ Promise }} from \"{this.BridgeModuleName}\";");
                    }
                }

                code.Code();

                simpleTypeName = type.Name;
            }
            else
            {
                // TODO: Support multi-level nesting
                simpleTypeName = type.Name.Replace('+', '.');
            }

            simpleTypeName = simpleTypeName.Substring(simpleTypeName.LastIndexOf('.') + 1);

            PluginInfo.AssemblyClassInfo classInfo = this.PluginInfo.Assembly.Classes
                .FirstOrDefault(c => c.Name == type.Name || c.Name == type.FullName);
            bool marshalByValue = (classInfo != null && classInfo.MarshalByValue == "true");

            if (type is Class || type is Struct)
            {
                string extendsTypeName = (marshalByValue ? "NativeObject" : "NativeReference");
                code.Code($"{(type.IsNested ? "export " : "")}class {simpleTypeName} extends {extendsTypeName} {{");
                code.Code($"\tstatic type: string = \"{type.FullName}\";");
            }
            else if (type is Interface)
            {
                code.Code($"{(type.IsNested ? "export " : "")}interface {simpleTypeName} {{");
            }
            else if (type is Enum)
            {
                code.Code($"{(type.IsNested ? "export " : "")}enum {simpleTypeName} {{");
            }
            else
            {
                throw new NotSupportedException("Type type not supported: " + type.GetType().Name);
            }

            CodeWriter members = code.Indent();
            Func<Property, bool> isStructProperty = p =>
                p.GetMethod != null && !p.IsStatic;

            if (type is Enum)
            {
                foreach (EnumValue field in type.Members.Cast<EnumValue>().OrderBy(f => (int)f.Value))
                {
                    this.WriteTypeScriptBindingForField(type, field, members);
                }
            }
            else if (marshalByValue)
            {
                // Give the C3P JS marshaller hints about how to convert certain marshal-by-value fields.
                IEnumerable<Property> properties = type.Members.OfType<Property>().Where(isStructProperty);
                string[] guidFields = properties.Where(p => p.PropertyType == "System.Guid")
                    .Select(p => p.Name).ToArray();
                if (guidFields.Length > 0)
                {
                    members.Code("static typeConversions: any = { " +
                        String.Join(", ", guidFields.Select(f => $"\"{Uncapitalize(f)}\": \"uuid\"")) + " };");
                }

                bool isFirstField = true;
                foreach (Property property in properties)
                {
                    if (isFirstField)
                    {
                        members.Code();
                        isFirstField = false;
                    }

                    this.WriteTypeScriptBindingForField(type, property, members);
                }
            }

            if (marshalByValue)
            {
                members.Code();
                members.Code(
                    "constructor() {",
                    $"\tsuper({simpleTypeName}.type);",
                    "}");
            }
            else if (type is Class)
            {
                string implicitContextArgument = null;

                foreach (Constructor constructor in type.Members.OfType<Constructor>())
                {
                    members.Code();
                    this.WriteTypeScriptBindingForConstructor(type, constructor, members);

                    if (implicitContextArgument == null)
                    {
                        implicitContextArgument = GetImplicitContextArgument(constructor);
                    }
                }

                string argsWithContext = (implicitContextArgument == null ? "args" :
                    "[" + implicitContextArgument + "].concat(args)");

                members.Code();
                members.Code("constructor(handle: Promise<number>);");
                members.Code();
                members.Code(
                    $"constructor(...args: any[]) {{",
                    $"\tsuper(",
                    $"\t\t{simpleTypeName}.type,",
                    $"\t\t(args.length === 1 && args[0] instanceof Promise ? args[0] :",
                    $"\t\t\tbridge.createInstance({type.Name}.type, {argsWithContext})));",
                    $"}}");
                members.Code();
                members.Code("dispose(): Promise<void> {");
                members.Code("\tvar releaseNativeInstance: () => Promise<void> = ");
                members.Code("\t\tbridge.releaseInstance.bind(undefined, this.type, this.handle);");
                members.Code("\treturn super.dispose().then(function () { return releaseNativeInstance(); });");
                members.Code("}");
            }

            foreach (Property property in type.Members.OfType<Property>()
                .Where(p => !marshalByValue || !isStructProperty(p)))
            {
                members.Code();
                this.WriteTypeScriptBindingsForProperty(type, property, members);
            }

            foreach (Event eventMember in type.Members.OfType<Event>())
            {
                members.Code();
                this.WriteTypeScriptBindingForEvent(type, eventMember, members);
            }

            foreach (Method method in type.Members.OfType<Method>())
            {
                members.Code();
                this.WriteTypeScriptBindingForMethod(type, method, members);
            }

            code.Code("}");

            if (!(type is Enum) && !(type is Interface))
            {
                code.Code($"bridge.registerType({simpleTypeName}.type, <any>{simpleTypeName});");
            }

            if (type.Members.OfType<Type>().Count() != 0)
            {
                code.Code();
                code.Code($"module {type.Name} {{");

                foreach (Type nestedType in type.Members.OfType<Type>())
                {
                    code.Code();
                    this.WriteTypeScriptBindingsForType(nestedType, null, code.Indent());
                }

                code.Code("}");
            }

            if (!type.IsNested)
            {
                code.Code();
                code.Code($"export = {type.Name};");
            }
        }

        void WriteTypeScriptBindingForField(Type declaringType, Member fieldOrProperty, CodeWriter code)
        {
            EnumValue enumValue = fieldOrProperty as EnumValue;
            Field field = fieldOrProperty as Field;
            Property property = fieldOrProperty as Property;

            if (enumValue != null)
            {
                code.Code($"{Uncapitalize(enumValue.Name)} = {enumValue.Value},");
            }
            else if (field != null)
            {
                string typeName = this.GetJavaScriptTypeName(field.FieldType, declaringType);
                code.Code($"{Uncapitalize(field.Name)}: {typeName};");
            }
            else if (property != null)
            {
                string typeName = this.GetJavaScriptTypeName(property.PropertyType, declaringType);
                string modifier = property.SetMethod == null ? "readonly " : "";
                code.Code($"{modifier}{Uncapitalize(property.Name)}: {typeName};");
            }
        }

        void WriteTypeScriptBindingForConstructor(
            Type declaringType, Constructor constructor, CodeWriter code)
        {
            string parameters = String.Join(", ", constructor.Parameters.Select(
                    p => p.Name + ": " + this.GetJavaScriptTypeName(p.ParameterType, declaringType)));

            code.Code($"constructor({parameters});");
        }

        void WriteTypeScriptBindingsForProperty(Type declaringType, Property property, CodeWriter code)
        {
            CodeWriter body = code.Indent();
            string propertyName = property.Name;
            if (propertyName.StartsWith("Is"))
            {
                propertyName = propertyName.Substring(2);
            }

            bool isInterfaceMember = declaringType is Interface;

            string simpleName = declaringType.Name.Substring(declaringType.Name.IndexOf('+') + 1);
            if (property.GetMethod != null)
            {
                string propertyDeclaration;
                string returnType = this.GetJavaScriptTypeName(property.PropertyType, declaringType);
                if (this.ForceAsyncAPIs)
                {
                    propertyDeclaration =
                        (property.Name.StartsWith("Is") ? Uncapitalize(property.Name) : "get" + property.Name) +
                        "Async";
                    returnType = MakePromiseTypeName(returnType);
                }
                else
                {
                    propertyDeclaration = "get " + Uncapitalize(property.Name);
                }

                code.Code((property.IsStatic ? "static " : String.Empty) +
                    $"{propertyDeclaration}(): {returnType}" + (isInterfaceMember ? ";" : "{"));
                if (!isInterfaceMember)
                {
                    string retCast = "";
                    if (property.PropertyType == "System.Boolean")
                    {
                        retCast = ".then(result => !!result)";
                    }
                    else if (property.PropertyType == "System.Guid")
                    {
                        retCast = ".then(result => (typeof(result) === \"string\" ? " +
                            "result.toUpperCase() : result && result.value))";
                    }
                    else if (property.PropertyType == "System.Uri")
                    {
                        retCast = ".then(result => (typeof(result) === \"string\" ? " +
                            "result : result && result.value))";
                    }

                    if (property.IsStatic)
                    {
                        body.Code("return bridge.getStaticProperty(" +
                            $"{simpleName}.type, \"{propertyName}\"){retCast};");
                    }
                    else
                    {
                        body.Code($"return bridge.getProperty(this, \"{propertyName}\"){retCast};");
                    }

                    code.Code("}");
                }
            }

            if (property.SetMethod != null)
            {
                string propertyDeclaration;
                string returnType;
                string returnStatement;
                if (this.ForceAsyncAPIs)
                {
                    propertyDeclaration = "set" + propertyName + "Async";
                    returnType = ": " + MakePromiseTypeName("void");
                    returnStatement = "return ";
                }
                else
                {
                    propertyDeclaration = "set " + Uncapitalize(property.Name);
                    returnType = String.Empty;
                    returnStatement = String.Empty;
                }

                string typeName = this.GetJavaScriptTypeName(property.PropertyType, declaringType);
                code.Code((property.IsStatic ? "static " : String.Empty) +
                    $"{propertyDeclaration}(value: {typeName}){returnType}" + (isInterfaceMember ? ";" : "{"));
                if (!isInterfaceMember)
                {
                    string value = "value";
                    if (property.PropertyType == "System.Guid")
                    {
                        value = "(typeof(value) === \"string\" ? { \"type\": \"<uuid>\", \"value\": value } : null)";
                    }
                    else if (property.PropertyType == "System.Uri")
                    {
                        value = "(typeof(value) === \"string\" ? { \"type\": \"<uri>\", \"value\": value } : null)";
                    }

                    if (property.IsStatic)
                    {
                        body.Code($"{returnStatement}bridge.setStaticProperty(" +
                            $"{simpleName}.type, \"{propertyName}\", {value});");
                    }
                    else
                    {
                        body.Code($"{ returnStatement}bridge.setProperty(this, \"{propertyName}\", {value});");
                    }

                    code.Code("}");
                }
            }
        }

        void WriteTypeScriptBindingForMethod(Type declaringType, Method method, CodeWriter code)
        {
            string methodName = Uncapitalize(method.Name);
            string parameters = String.Join(", ", method.Parameters.Select(
                    p => p.Name + ": " + this.GetJavaScriptTypeName(p.ParameterType, declaringType)));
            string returnType = this.GetJavaScriptTypeName(method.ReturnType, declaringType);
            if (this.ForceAsyncAPIs)
            {
                returnType = MakePromiseTypeName(returnType);

                if (!methodName.EndsWith("Async"))
                {
                    methodName += "Async";
                }
            }

            bool isInterfaceMethod = (declaringType is Interface);
            code.Code((method.IsStatic ? "static " : String.Empty) +
                $"{methodName}({parameters}): {returnType}" +
                (isInterfaceMethod ? ";" : " {"));

            if (!isInterfaceMethod)
            {
                CodeWriter body = code.Indent();

                string retCast = "";
                if (method.ReturnType == "System.Boolean" ||
                    method.ReturnType == "System.Threading.Tasks.Task<System.Boolean>")
                {
                    retCast = ".then(result => !!result)";
                }
                else if (method.ReturnType == "System.Void" ||
                    method.ReturnType == "System.Threading.Tasks.Task")
                {
                    retCast = ".then(result => undefined)";
                }
                else if (method.ReturnType == "System.Guid" ||
                    method.ReturnType == "System.Threading.Tasks.Task<System.Guid>")
                {
                    retCast = ".then(result => (typeof(result) === \"string\" ? " +
                        "result.toUpperCase() : result && result.value))";
                }

                string arguments = String.Join(", ", method.Parameters.Select(p =>
                    p.ParameterType == "System.Guid" ?
                        $"(typeof({p.Name}) === \"string\" ? {{ \"type\": \"<uuid>\", \"value\": {p.Name} }} : null)" :
                    p.ParameterType == "System.Uri" ?
                        $"(typeof({p.Name}) === \"string\" ? {{ \"type\": \"<uri>\", \"value\": {p.Name} }} : null)" :
                    p.Name));
                string implicitContextArgument = GetImplicitContextArgument(method);
                if (implicitContextArgument != null)
                {
                    arguments = (arguments.Length > 0 ? implicitContextArgument + ", " + arguments : implicitContextArgument);
                }

                if (method.IsStatic)
                {
                    string simpleName = declaringType.Name.Substring(declaringType.Name.IndexOf('+') + 1);
                    body.Code("return bridge.invokeStaticMethod(" +
                        $"{simpleName}.type, \"{method.Name}\", [{arguments}]){retCast};");
                }
                else
                {
                    body.Code($"return bridge.invokeMethod(this, \"{method.Name}\", [{arguments}]){retCast};");
                }

                code.Code("}");
            }
        }

        static string GetImplicitContextArgument(Member member)
        {
            string implicitContextKind = member.CustomAttributes?
                .Where(a => a.AttributeType == typeof(ImplicitContextAttribute).FullName)
                .Select(a => a.Arguments.Single().Value)
                .FirstOrDefault();
            if (implicitContextKind == ImplicitContextAttribute.Application)
            {
                return "NativeReference.implicitAppContext";
            }
            else if (implicitContextKind == ImplicitContextAttribute.Window)
            {
                return "NativeReference.implicitWindowContext";
            }

            return null;
        }

        void WriteTypeScriptBindingForEvent(Type declaringType, Event eventMember, CodeWriter code)
        {
            bool isInterfaceMember = (declaringType is Interface);
            string eventHandlerPrefix = "System.EventHandler<";
            if (!eventMember.EventHandlerType.StartsWith(eventHandlerPrefix))
            {
                throw new NotSupportedException("Unsupported event handler type: " + eventMember.EventHandlerType);
            }

            string addMethodName = $"add{eventMember.Name}Listener";
            string removeMethodName = $"remove{eventMember.Name}Listener";
            string returnType = "void";

            if (this.ForceAsyncAPIs)
            {
                addMethodName += "Async";
                removeMethodName += "Async";
                returnType = "Promise<void>";
            }

            string eventType = eventMember.EventHandlerType.Substring(
                eventHandlerPrefix.Length, eventMember.EventHandlerType.Length - 1 - eventHandlerPrefix.Length);

            code.Code((eventMember.IsStatic ? "static " : String.Empty) +
                $"{addMethodName}(listener: (e: {GetJavaScriptTypeName(eventType, declaringType)}) => void)" +
                $": {returnType}" + (isInterfaceMember ? ";" : " {"));

            string simpleName = declaringType.Name.Substring(declaringType.Name.IndexOf('+') + 1);
            if (!isInterfaceMember)
            {
                if (eventMember.IsStatic)
                {
                    code.Code("\treturn bridge.addStaticEventListener(" +
                        $"{simpleName}.type, \"{eventMember.Name}\", listener);");
                }
                else
                {
                    code.Code($"\treturn bridge.addEventListener(this, \"{eventMember.Name}\", listener);");
                }

                code.Code("}");
            }

            code.Code();
            code.Code((eventMember.IsStatic ? "static " : String.Empty) +
                $"{removeMethodName}(listener: (e: {GetJavaScriptTypeName(eventType, declaringType)}) => void)" +
                $": {returnType}" + (isInterfaceMember ? ";" : " {"));

            if (!isInterfaceMember)
            {
                if (eventMember.IsStatic)
                {
                    code.Code("\treturn bridge.removeStaticEventListener(" +
                        $"{simpleName}.type, \"{eventMember.Name}\", listener);");
                }
                else
                {
                    code.Code($"\treturn bridge.removeEventListener(this, \"{eventMember.Name}\", listener);");
                }

                code.Code("}");
            }
        }

        string GetJavaScriptTypeName(string typeFullName, Type declaringType)
        {
            if (String.IsNullOrEmpty(typeFullName))
            {
                throw new ArgumentNullException(nameof(typeFullName));
            }

            if (typeFullName.EndsWith("&"))
            {
                throw new NotSupportedException("Out parameters are not supported. (Parameter type: " + typeFullName +
                    "; declaring type: " + declaringType.Name + ")");
            }

            if (typeFullName.StartsWith("System.Nullable<"))
            {
                typeFullName = typeFullName.Substring("System.Nullable<".Length);
                typeFullName = typeFullName.Substring(0, typeFullName.Length - 1);
            }

            switch (typeFullName)
            {
                case "System.Single":
                case "System.Double":
                case "System.Byte":
                case "System.Int16":
                case "System.Int32":
                case "System.Int64":
                case "System.UInt16":
                case "System.UInt32":
                case "System.UInt64": return "number";
                case "System.Single[]":
                case "System.Double[]":
                case "System.Int16[]":
                case "System.Int32[]":
                case "System.Int64[]":
                case "System.UInt16[]":
                case "System.UInt32[]":
                case "System.UInt64[]": return "number[]";
                case "System.DateTimeOffset": return "Object";
                case "System.Guid":
                case "System.Uri":
                case "System.String": return "string";
                case "System.Boolean": return "boolean";
                case "System.Void": return "void";
                case "System.Object": return "any";
                case "System.Collections.Generic.IList": return "Array";
            }

            int lastDot = typeFullName.LastIndexOf('.');
            string typeNamespace = (lastDot > 0 ? typeFullName.Substring(0, lastDot) : null);
            string typeName = typeFullName.Substring(lastDot + 1);

            if (typeNamespace != declaringType.Namespace)
            {
                if (typeFullName.StartsWith("System.Threading.Tasks.Task"))
                {
                    typeName = typeFullName.Replace("System.Threading.Tasks.Task", "Promise");
                }
                else if (typeFullName.StartsWith("System.Action"))
                {
                    // TODO: Convert callbacks to Tasks in the adapter layer.
                    typeName = "any";
                }
                else
                {
                    typeName = typeFullName.Replace("+", ".");
                }
            }
            else
            {
                typeName = typeName.Replace('+', '.');
            }

            if (typeName.EndsWith(">"))
            {
                int ltIndex = typeName.IndexOf('<');
                string genericArguments = typeName.Substring(ltIndex + 1, typeName.Length - ltIndex - 2);
                IEnumerable<string> genericArgumentTypeFullNames = genericArguments.Split(',').Select(t => t.Trim());
                IEnumerable<string> genericArgumentTypeNames = genericArgumentTypeFullNames.Select(
                    t => this.GetJavaScriptTypeName(t, declaringType));

                string baseTypeName = typeName.Substring(0, ltIndex);
                baseTypeName = this.GetJavaScriptTypeName(baseTypeName, declaringType);
                if (baseTypeName == "Promise<void>")
                {
                    baseTypeName = "Promise";
                }

                typeName = baseTypeName + "<" + String.Join(", ", genericArgumentTypeNames) + ">";
            }
            else if (typeName.EndsWith("]"))
            {
                if (typeName.IndexOfAny(new char[] { ',', '*' }) >= 0)
                {
                    throw new NotSupportedException("TypeScript does not support multi-dimensional arrays.");
                }
            }
            else if (typeName == "Promise")
            {
                typeName += "<void>";
            }

            return typeName;
        }

        static string MakePromiseTypeName(string typeName)
        {
            if (!typeName.StartsWith("Promise<"))
            {
                typeName = "Promise<" + typeName + ">";
            }

            return typeName;
        }

        static string Uncapitalize(string memberName)
        {
            return Char.ToLowerInvariant(memberName[0]) + memberName.Substring(1);
        }
    }
}
