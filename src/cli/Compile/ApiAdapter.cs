// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.C3P
{
    /// <summary>
    /// Base class for subclasses that generate adapter code to convert platform-specific APIs
    /// into standard types and patterns that are the same across all platforms.
    /// </summary>
    abstract class ApiAdapter
    {
        HashSet<string> enumTypes = new HashSet<string>();

        protected Dictionary<string, Type> listenerMap = new Dictionary<string, Type>();

        protected abstract List<PluginInfo.NamespaceMappingInfo> NamespaceMappings { get; }
        protected virtual List<PluginInfo.EnumInfo> EnumMappings { get { return null; } }

        protected virtual string PluginAlias { get { return String.Empty; } }

        public PluginInfo PluginInfo { get; set; }

        public string OutputDirectoryPath { get; set; }

        public virtual void GenerateAdapterCodeForApi(Assembly api)
        {
            if (api == null)
            {
                throw new ArgumentNullException(nameof(api));
            }

            if (String.IsNullOrEmpty(this.OutputDirectoryPath))
            {
                throw new ArgumentNullException(nameof(OutputDirectoryPath));
            }

            if (this.PluginInfo == null)
            {
                throw new ArgumentNullException(nameof(PluginInfo));
            }

            if (this.NamespaceMappings == null)
            {
                throw new ArgumentNullException(nameof(NamespaceMappings));
            }

            this.IndexApi(api);

            foreach (Type type in api.Types)
            {
                if (this.IsIgnoredType(type))
                {
                    continue;
                }

                string mappedName = this.GetNameFromTypeFullName(type.FullName);
                string mappedNamespace = this.GetNamespaceFromTypeFullName(type.FullName);
                string mappedTypeFullName = mappedNamespace + "." + mappedName;

                using (CodeWriter code = new CodeWriter(Path.Combine(this.OutputDirectoryPath, mappedName + ".cs")))
                {
                    string alias = this.PluginAlias;
                    if (!String.IsNullOrEmpty(alias))
                    {
                        code.Code($"extern alias {alias.Replace("::", "")};");
                        code.Code();
                    }

                    code.Code($"namespace {mappedNamespace}");
                    code.Code("{");

                    this.WriteType(type, mappedTypeFullName, code.Indent());

                    code.Code("}");
                }
            }

            Utils.ExtractResource("ImplicitContextAttribute.cs", this.OutputDirectoryPath);
        }

        protected void IndexApi(Assembly api)
        {
            foreach (Type type in api.Types)
            {
                this.IndexType(type);
            }
        }

        protected virtual void IndexType(Type type)
        {
            if (type is Enum)
            {
                this.enumTypes.Add(type.FullName);
            }

            Class theClass = type as Class;
            if (theClass != null)
            {
                foreach (Type innerType in theClass.Members.OfType<Type>())
                {
                    this.IndexType(innerType);
                }
            }
        }

        protected void WriteType(Type type, string typeFullName, CodeWriter code)
        {
            Class theClass = type as Class;
            Interface theInterface = type as Interface;
            Enum theEnum = type as Enum;
            Delegate theDelegate = type as Delegate;

            if (theClass != null)
            {
                if (this.IsEnumClass(theClass))
                {
                    this.WriteEnum(theClass, typeFullName, code);
                }
                else
                {
                    this.WriteClass(theClass, typeFullName, code);
                }
            }
            else if (theInterface != null)
            {
                this.WriteInterface(theInterface, typeFullName, code);
            }
            else if (theEnum != null)
            {
                this.WriteEnum(theEnum, typeFullName, code);
            }
            else if (theDelegate != null)
            {
                this.WriteDelegate(theDelegate, typeFullName, code);
            }
        }

        void WriteClass(Class theClass, string typeFullName, CodeWriter code)
        {
            string baseClassName = this.GetBaseClassName(theClass);
            string[] interfaceNames = this.GetInterfaceNames(theClass, typeFullName);
            string modifier = (theClass.IsStatic ? "static " : theClass.IsAbstract ? "abstract " : "");
            code.Code($"public {modifier}class {GetNameFromTypeFullName(typeFullName)}" +
                (baseClassName != null ? " : " + GetTypeName(baseClassName, typeFullName) : "") +
                (interfaceNames.Length > 0 ? (baseClassName != null ? ", " : " : ") +
                    String.Join(", ", interfaceNames.Select(i => GetTypeName(i, typeFullName))) : ""));
            code.Code("{");

            CodeWriter memberWriter = code.Indent();

            string boundTypeFullName = theClass.FullName.Replace('+', '.');

            bool isMarshalByValueType = this.IsMarshalByValueType(theClass, typeFullName);
            if (!isMarshalByValueType && !theClass.IsAbstract && !theClass.IsStatic)
            {
                memberWriter.Code($"private {this.PluginAlias}{boundTypeFullName} forward;");
                memberWriter.Code($"internal {this.PluginAlias}{boundTypeFullName} Forward");
                memberWriter.Code("{");
                memberWriter.Code("\tget");
                memberWriter.Code("\t{");
                memberWriter.Code("\t\tif (this.forward == null) throw new System.ObjectDisposedException(" +
                    $"nameof({GetNameFromTypeFullName(typeFullName)}));");
                memberWriter.Code("\t\treturn forward;");
                memberWriter.Code("\t}");
                memberWriter.Code("}");

                foreach (Constructor constructor in theClass.Members.OfType<Constructor>()
                    .Where(c => !IsIgnoredMember(theClass, c))
                    .OrderBy(c => c.Parameters.Count))
                {
                    memberWriter.Code();
                    this.WriteConstructor(constructor, typeFullName, boundTypeFullName, memberWriter);
                }

                memberWriter.Code();
                memberWriter.Code("public void Dispose()");
                memberWriter.Code("{");

                memberWriter.Code("\tif (forward != null)");
                memberWriter.Code("\t{");
                if (theClass.ImplementsTypes.Contains("System.IDisposable"))
                {
                    memberWriter.Code("\t\tforward.Dispose();");
                }
                memberWriter.Code("\t\tforward = null;");
                memberWriter.Code("\t}");

                memberWriter.Code("}");
            }
            else if (!theClass.IsStatic)
            {
                string visibility = IsOneWay(theClass) ? "internal" : "public";
                memberWriter.Code($"{visibility} {GetNameFromTypeFullName(typeFullName)}()");
                memberWriter.Code("{");
                memberWriter.Code("}");
            }

            foreach (Property property in theClass.Members.OfType<Property>()
                .Where(p => !this.IsIgnoredMember(theClass, p)).OrderBy(p => p.IsStatic ? 0 : 1))
            {
                memberWriter.Code();
                this.WriteProperty(property, typeFullName, boundTypeFullName, false, isMarshalByValueType, memberWriter);
            }

            foreach (Method method in theClass.Members.OfType<Method>()
                .Where(m => !IsAddEventMethod(m) && !IsRemoveEventMethod(m) && !IsIgnoredMember(theClass, m))
                .OrderBy(m => m.IsStatic ? 0 : 1))
            {
                memberWriter.Code();
                this.WriteMethod(method, typeFullName, boundTypeFullName, false, isMarshalByValueType, memberWriter);
            }

            foreach (Method eventMethod in theClass.Members.OfType<Method>()
                .Where(e => IsAddEventMethod(e) && !IsIgnoredMember(theClass, e)).OrderBy(m => m.IsStatic ? 0 : 1))
            {
                memberWriter.Code();
                this.WriteEvent(eventMethod, typeFullName, boundTypeFullName, false, memberWriter);
            }

            foreach (Event eventMember in theClass.Members.OfType<Event>()
                .Where(e => !IsIgnoredMember(theClass, e)).OrderBy(m => m.IsStatic ? 0 : 1))
            {
                memberWriter.Code();
                this.WriteEvent(eventMember, typeFullName, boundTypeFullName, false, memberWriter);
            }

            this.WriteInnerTypes(theClass, typeFullName, memberWriter);

            memberWriter.Code();
            this.WriteImplicitConversions(theClass, boundTypeFullName, typeFullName, isMarshalByValueType, memberWriter);

            code.Code("}");
        }

        protected virtual void WriteInnerTypes(Class outerClass, string outerClassFullName, CodeWriter code)
        {
            foreach (Type type in outerClass.Members.OfType<Type>())
            {
                if (this.IsIgnoredType(type))
                {
                    continue;
                }

                code.Code();

                string innerTypeName = type.Name.Substring(type.Name.IndexOf('+') + 1);
                this.WriteType(type, outerClassFullName + "+" + innerTypeName, code);
            }
        }

        void WriteInterface(Interface theInterface, string typeFullName, CodeWriter code)
        {
            string[] interfaceNames = this.GetInterfaceNames(theInterface, typeFullName);
            code.Code($"public interface {GetNameFromTypeFullName(typeFullName)}" +
                (interfaceNames.Length > 0 ? " : " +
                 String.Join(", ", interfaceNames.Select(i => GetTypeName(i, typeFullName))) : ""));
            code.Code("{");

            CodeWriter memberWriter = code.Indent();

            bool first = true;
            foreach (Property property in theInterface.Members.OfType<Property>()
                .Where(p => !this.IsIgnoredMember(theInterface, p)))
            {
                if (!first)
                {
                    memberWriter.Code();
                }
                first = false;
                this.WriteProperty(property, typeFullName, null, true, false, memberWriter);
            }

            foreach (Method method in theInterface.Members.OfType<Method>()
                .Where(m => !IsAddEventMethod(m) && !IsRemoveEventMethod(m) && !IsIgnoredMember(theInterface, m)))
            {
                if (!first)
                {
                    memberWriter.Code();
                }
                first = false;
                this.WriteMethod(method, typeFullName, null, true, false, memberWriter);
            }

            foreach (Method eventMethod in theInterface.Members.OfType<Method>()
                .Where(e => IsAddEventMethod(e) && !IsIgnoredMember(theInterface, e)).OrderBy(m => m.IsStatic ? 0 : 1))
            {
                memberWriter.Code();
                this.WriteEvent(eventMethod, typeFullName, null, true, memberWriter);
            }

            code.Code("}");
        }

        void WriteEnum(Class enumClass, string typeFullName, CodeWriter code)
        {
            string typeName = GetNameFromTypeFullName(typeFullName);
            string boundTypeFullName = enumClass.FullName.Replace('+', '.');

            code.Code($"public enum {typeName}");
            code.Code("{");

            foreach (Field field in enumClass.Members.OfType<Field>().OrderBy(f => f.Value))
            {
                string fieldName = field.Name;
                if (fieldName.StartsWith(typeName))
                {
                    fieldName = fieldName.Substring(typeName.Length);
                }

                code.Code($"\t{fieldName} = (int){boundTypeFullName}.{field.Name},");
            }

            code.Code("}");
        }

        void WriteEnum(Enum theEnum, string typeFullName, CodeWriter code)
        {
            string typeName = GetNameFromTypeFullName(typeFullName);
            string boundTypeFullName = theEnum.FullName.Replace('+', '.');

            code.Code($"public enum {typeName}");
            code.Code("{");

            foreach (EnumValue field in theEnum.Members.OfType<EnumValue>().OrderBy(f => f.Value))
            {
                string fieldName = field.Name;
                if (fieldName.StartsWith(typeName))
                {
                    fieldName = fieldName.Substring(typeName.Length);
                }

                code.Code($"\t{fieldName} = (int){this.PluginAlias}{boundTypeFullName}.{field.Name},");
            }

            code.Code("}");
        }

        void WriteDelegate(Delegate theDelegate, string typeFullName, CodeWriter code)
        {
            string parameters = String.Join(", ", theDelegate.Parameters.Select(
                p => GetTypeName(p.ParameterType, typeFullName) + " " + p.Name));
            code.Code($"public delegate {GetTypeName(theDelegate.ReturnType, typeFullName)} " +
                $"{theDelegate.Name}({parameters});");
        }

        void WriteConstructor(
            Constructor constructor,
            string declaringTypeFullName,
            string boundTypeFullName,
            CodeWriter code)
        {
            if (this.IsOutErrorMember(constructor))
            {
                this.WriteOutErrorConstructor(constructor, declaringTypeFullName, boundTypeFullName, code);
            }
            else
            {
                string context = this.GetImplicitContext(constructor, code);
                int skip = context != null ? 1 : 0;
                string parameters = String.Join(", ", constructor.Parameters.Skip(skip).Select(
                    (p, i) => GetParameterTypeName(p.ParameterType, declaringTypeFullName, null, i) + " " + p.Name));
                code.Code($"public {GetNameFromTypeFullName(declaringTypeFullName)}({parameters})");
                code.Code("{");

                if (context != null && constructor.Parameters.Count > 1)
                {
                    context += ", ";
                }

                string arguments = (context != null ? context : String.Empty) +
                    String.Join(", ", constructor.Parameters.Skip(skip).Select(
                        (p,i) => this.CastArgument(
                            p.Name,
                            this.GetParameterTypeName(p.ParameterType, declaringTypeFullName, null, i),
                            p.ParameterType)));

                code.Code($"\tforward = new {this.PluginAlias}{boundTypeFullName}({arguments});");

                code.Code("}");
            }
        }

        protected virtual string GetImplicitContext(Member member, CodeWriter code)
        {
            return null;
        }

        void WriteProperty(
            Property property,
            string declaringTypeFullName,
            string boundTypeFullName,
            bool isInterfaceMember,
            bool isMarshalByValueType,
            CodeWriter code)
        {
            string propertyName = property.Name;

            Method getMethod = property.GetMethod;
            Method setMethod = property.SetMethod;
            string propertyType = GetMemberTypeName(property.PropertyType, declaringTypeFullName, propertyName);

            if (propertyType == "bool" && !propertyName.StartsWith("Is"))
            {
                propertyName = "Is" + propertyName;
            }

            bool isAbstract = isInterfaceMember || (getMethod?.IsAbstract ?? false) || (setMethod?.IsAbstract ?? false);
            if (isAbstract)
            {
                code.Code($"{(isInterfaceMember ? "" : "public abstract ")}{propertyType} " +
                    $"{property.Name} {{ {(getMethod != null ? "get; " : "")}{(setMethod != null ? "set; " : "")}}}");
            }
            else
            {
                string forward = (property.IsStatic ? this.PluginAlias + boundTypeFullName : "Forward");

                code.Code($"public {(property.IsStatic ? "static " : "")}{propertyType} {propertyName}");
                code.Code("{");

                if (isMarshalByValueType)
                {
                    code.Code("\tget;");
                    code.Code(setMethod != null ? "\tset;" : "\tprivate set;");
                }
                else
                {
                    string mappedPropertyType = this.MapTypeName(property.PropertyType) ?? property.PropertyType;
                    if (getMethod != null)
                    {
                        this.WritePropertyGetter(property, propertyType, forward, code.Indent());
                    }

                    if (setMethod != null)
                    {
                        this.WritePropertySetter(property, mappedPropertyType, forward, code.Indent());
                    }
                }

                code.Code("}");
            }
        }

        protected virtual void WritePropertyGetter(
            Property property, string mappedPropertyType, string forward, CodeWriter code)
        {
            code.Code("get");
            code.Code("{");
            code.Code($"\treturn " +
                this.CastArgument($"{forward}.{property.Name}", property.PropertyType, mappedPropertyType) + ";");
            code.Code("}");
        }

        protected virtual void WritePropertySetter(
            Property property, string mappedPropertyType, string forward, CodeWriter code)
        {
            code.Code("set");
            code.Code("{");
            code.Code($"\t{forward}.{property.Name} = " +
                this.CastArgument("value", mappedPropertyType, property.PropertyType, true) + ";");
            code.Code("}");
        }

        void WriteMethod(
            Method method,
            string declaringTypeFullName,
            string boundTypeFullName,
            bool isInterfaceMember,
            bool isMarshalByValueType,
            CodeWriter code)
        {
            if (this.IsAsyncMethod(method))
            {
                this.WriteAsyncMethod(method, declaringTypeFullName, boundTypeFullName, isInterfaceMember, code);
            }
            else if (this.IsOutErrorMember(method))
            {
                this.WriteOutErrorMethod(
                    method, declaringTypeFullName, boundTypeFullName, isInterfaceMember, isMarshalByValueType, code);
            }
            else
            {
                string context = this.GetImplicitContext(method, code);
                int skip = context != null ? 1 : 0;

                bool isAbstract = isInterfaceMember || method.IsAbstract;
                string parameters = String.Join(", ", method.Parameters.Skip(skip).Select(
                    (p, i) => GetParameterTypeName(p.ParameterType, declaringTypeFullName, method.Name, i) +
                        " " + p.Name));
                code.Code($"{(isInterfaceMember ? "" : "public ")}{(method.IsStatic ? "static " : "")}" +
                    $"{(isAbstract && !isInterfaceMember ? "abstract " : "")}" +
                    $"{GetMemberTypeName(method.ReturnType, declaringTypeFullName, method.Name)} " +
                    $"{method.Name}({parameters}){(isAbstract ? ";" : "")}");

                if (!isAbstract)
                {
                    code.Code("{");

                    if (isMarshalByValueType && !method.IsStatic)
                    {
                        code.Code($"\t{this.PluginAlias}{boundTypeFullName} Forward = " +
                            $"new {this.PluginAlias}{boundTypeFullName}();");
                        code.Code("\tthis.CopyValuesTo(Forward);");
                    }

                    if (context != null && method.Parameters.Count > 1)
                    {
                        context += ", ";
                    }

                    string arguments = (context != null ? context : String.Empty) +
                        String.Join(", ", method.Parameters.Skip(skip).Select(
                            (p, i) => this.CastArgument(
                                p.Name,
                                this.GetParameterTypeName(p.ParameterType, declaringTypeFullName, method.Name, i),
                                p.ParameterType,
                                true)));
                    string forward = (method.IsStatic ? this.PluginAlias + boundTypeFullName : "Forward");
                    if (method.ReturnType == "System.Void")
                    {
                        code.Code($"\t{forward}.{method.Name}({arguments});");

                        if (isMarshalByValueType && !method.IsStatic)
                        {
                            code.Code("\tthis.CopyValuesFrom(Forward);");
                        }
                    }
                    else
                    {
                        code.Code($"\tvar result = {forward}.{method.Name}({arguments});");

                        if (isMarshalByValueType && !method.IsStatic)
                        {
                            code.Code("\tthis.CopyValuesFrom(Forward);");
                        }

                        code.Code("\treturn " + this.CastArgument(
                            $"result",
                            method.ReturnType,
                            this.GetMemberTypeName(method.ReturnType, declaringTypeFullName, method.Name)) + ";");
                    }
                    code.Code("}");
                }
            }
        }

        protected virtual string CastArgument(
            string argument, string fromTypeName, string toTypeName, bool alias = false)
        {
            const string nullablePrefix = "System.Nullable<";
            if (fromTypeName.StartsWith(nullablePrefix))
            {
                fromTypeName = fromTypeName.Substring(
                    nullablePrefix.Length, fromTypeName.Length - nullablePrefix.Length - 1) + '?';
            }
            if (toTypeName.StartsWith(nullablePrefix))
            {
                toTypeName = toTypeName.Substring(
                    nullablePrefix.Length, toTypeName.Length - nullablePrefix.Length - 1) + '?';
            }

            if (this.enumTypes.Contains(fromTypeName) || this.enumTypes.Contains(toTypeName) ||
                toTypeName.IndexOf("::") > 0 && this.enumTypes.Contains(toTypeName.Substring(toTypeName.IndexOf("::") + 2)))
            {
                return $"({this.GetAliasedTypeName(toTypeName.Replace('+', '.'), alias)}){argument}";
            }
            else if ((fromTypeName.EndsWith("[]") || fromTypeName.StartsWith("System.Collections.Generic.IList<")) &&
                (toTypeName.EndsWith("[]") || toTypeName.StartsWith("System.Collections.Generic.IList<")))
            {
                string toComponentName;
                if (toTypeName.EndsWith("[]"))
                {
                    toComponentName = toTypeName.Substring(0, toTypeName.Length - 2);
                }
                else
                {
                    toComponentName = toTypeName.Substring("System.Collections.Generic.IList<".Length);
                    toComponentName = toComponentName.Substring(0, toComponentName.Length - 1);
                }

                return "System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(" +
                    $"{argument}, item => ({this.GetAliasedTypeName(toComponentName, alias)})item))";
            }
            else if (fromTypeName.StartsWith("System.Collections.IList<") &&
                toTypeName.StartsWith("System.Collections.Generic.IList<"))
            {
                // System.Collections.IList isn't actually generic, but the caller had additional
                // information about the actual item type, to facilitate this cast to a generic list.
                string fromComponentName = fromTypeName.Substring("System.Collections.IList<".Length);
                fromComponentName = fromComponentName.Substring(0, fromComponentName.Length - 1);
                string toComponentName = toTypeName.Substring("System.Collections.Generic.IList<".Length);
                toComponentName = toComponentName.Substring(0, toComponentName.Length - 1);

                return "System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(" +
                    $"System.Linq.Enumerable.Cast<System.Object>({argument}), " +
                    $"item => ({this.GetAliasedTypeName(toComponentName, alias)})({fromComponentName})item))";
            }
            else if ((fromTypeName == "System.Int32" || toTypeName == "System.Int32") && this.EnumMappings != null)
            {
                return $"({toTypeName}){argument}";
            }
            else if (fromTypeName == "bool?" && toTypeName == "bool")
            {
                return $"{argument} ?? false";
            }
            else if ((fromTypeName == "short?" && toTypeName == "short") ||
                    (fromTypeName == "int?" && toTypeName == "int") ||
                    (fromTypeName == "long?" && toTypeName == "long") ||
                    (fromTypeName == "float?" && toTypeName == "float") ||
                    (fromTypeName == "double?" && toTypeName == "double"))
            {
                return $"{argument} ?? 0";
            }
            else if (fromTypeName == "System.Guid?" && toTypeName == "System.Guid")
            {
                return $"{argument} ?? System.Guid.Empty";
            }
            else
            {
                return argument;
            }
        }

        protected virtual bool IsEnumClass(Class theClass)
        {
            return false;
        }

        protected virtual bool IsOutErrorMember(Member constructorOrMethod)
        {
            return false;
        }

        protected virtual void WriteOutErrorConstructor(
            Constructor constructor,
            string declaringTypeFullName,
            string boundTypeFullName,
            CodeWriter code)
        {
            throw new NotImplementedException();
        }

        protected virtual void WriteOutErrorMethod(
            Method method,
            string declaringTypeFullName,
            string boundTypeFullName,
            bool isInterfaceMember,
            bool isMarshalByValueType,
            CodeWriter code)
        {
            throw new NotImplementedException();
        }

        protected abstract bool IsAsyncMethod(Method method);

        protected virtual void WriteAsyncMethod(
            Method method,
            string declaringTypeFullName,
            string boundTypeFullName,
            bool isInterfaceMember,
            CodeWriter code)
        {
            throw new NotImplementedException();
        }

        protected virtual void WriteEvent(
            Method eventMethod,
            string declaringTypeFullName,
            string boundTypeFullName,
            bool isInterfaceMember,
            CodeWriter code)
        {
            string boundEventTypeFullName =
                this.GetBoundEventTypeForListenerType(eventMethod.Parameters[0].ParameterType);
            string eventName = eventMethod.Name.Substring("Add".Length, eventMethod.Name.Length - "AddListener".Length);
            string forward = (eventMethod.IsStatic ? boundTypeFullName : "Forward");
            code.Code($"{(isInterfaceMember ? "" : "public ")}" +
                $"{(eventMethod.IsAbstract && !isInterfaceMember ? "abstract " : "")}" +
                $"{(eventMethod.IsStatic ? "static " : "")}event " +
                $"System.EventHandler<{GetTypeName(boundEventTypeFullName, declaringTypeFullName)}> " +
                $"{eventName}{(isInterfaceMember || eventMethod.IsAbstract ? ";" : "")}");
            if (!isInterfaceMember && !eventMethod.IsAbstract)
            {
                code.Code("{");
                this.WriteEventAdder(eventName, eventMethod, declaringTypeFullName, forward, code.Indent());
                this.WriteEventRemover(eventName, eventMethod, declaringTypeFullName, forward, code.Indent());
                code.Code("}");
            }
        }

        protected virtual void WriteEvent(
            Event eventMember,
            string declaringTypeFullName,
            string boundTypeFullName,
            bool isInterfaceMember,
            CodeWriter code)
        {
            string boundEventTypeFullName = this.GetBoundEventTypeForListenerType(eventMember.EventHandlerType);
            string forward = (eventMember.IsStatic ? this.PluginAlias + boundTypeFullName : "Forward");
            code.Code($"{(isInterfaceMember ? "" : "public ")}" +
                $"{(eventMember.IsStatic ? "static " : "")}event " +
                $"System.EventHandler<{GetTypeName(boundEventTypeFullName, declaringTypeFullName)}> " +
                $"{eventMember.Name}{(isInterfaceMember ? ";" : "")}");
            if (!isInterfaceMember)
            {
                code.Code("{");
                this.WriteEventAdder(eventMember.Name, eventMember, declaringTypeFullName, forward, code.Indent());
                this.WriteEventRemover(eventMember.Name, eventMember, declaringTypeFullName, forward, code.Indent());
                code.Code("}");
            }
        }

        protected virtual void WriteEventAdder(
            string eventName, Member eventMember, string declaringTypeFullName, string forward, CodeWriter code)
        {
            Method eventMethod = eventMember as Method;
            Event eventEvent = eventMember as Event;
            bool isStatic = (eventMethod != null && eventMethod.IsStatic) ||
                (eventEvent != null && eventEvent.IsStatic);
            string sender = isStatic ? "typeof(" + declaringTypeFullName + ")" : "this";

            code.Code("add");
            code.Code("{");
            code.Code($"\tif (__{eventName}ListenerMap.ContainsKey(value)) return;");
            code.Code($"\tvar listener = new __{eventName}Listener {{ Sender = {sender}, Handler = value }};");
            code.Code($"\t{forward}.Add{eventName}Listener(listener);");
                        code.Code($"\t__{eventName}ListenerMap[value] = listener;");
            code.Code("}");
        }

        protected virtual void WriteEventRemover(
            string eventName, Member eventMember, string declaringTypeFullName, string forward, CodeWriter code)
        {
            code.Code("remove");
            code.Code("{");
            code.Code($"\t__{eventName}Listener listener;");
            code.Code($"\tif (__{eventName}ListenerMap.TryGetValue(value, out listener))");
            code.Code("\t{");
            code.Code($"\t\t{forward}.Remove{eventName}Listener(listener);");
            code.Code($"\t\t__{eventName}ListenerMap.Remove(value);");
            code.Code("\t}");
            code.Code("}");
        }

        protected string GetBoundEventTypeForListenerType(string listenerTypeFullName)
        {
            string eventHandlerPrefix = "System.EventHandler<";
            if (listenerTypeFullName.StartsWith(eventHandlerPrefix))
            {
                string eventTypeFullName = listenerTypeFullName.Substring(
                    eventHandlerPrefix.Length, listenerTypeFullName.Length - eventHandlerPrefix.Length - 1);
                return eventTypeFullName;
            }

            Type listenerType;
            if (!this.listenerMap.TryGetValue(listenerTypeFullName, out listenerType))
            {
                throw new InvalidOperationException(
                    "Listener interface or delegate type not found: " + listenerTypeFullName);
            }

            return this.GetBoundEventTypeForListenerType(listenerType);
        }

        protected virtual string GetBoundEventTypeForListenerType(Type listenerType)
        {
            throw new NotSupportedException();
        }

        protected virtual bool IsIgnoredType(Type type)
        {
            Delegate theDelegate = type as Delegate;
            if (theDelegate != null && IsEventListenerDelegate(theDelegate))
            {
                // Ignore event listener delegates. The public API will use EventHandler<> instead.
                return true;
            }

            return false;
        }

        protected virtual bool IsIgnoredMember(Type declaringType, Member member)
        {
            return (member is Method && (member.Name == "Equals" || member.Name == "GetHashCode"));
        }

        protected static bool IsEventListenerDelegate(Delegate theDelegate)
        {
            return theDelegate.Name.EndsWith("Listener") &&
            theDelegate.Parameters.Count == 2 &&
                theDelegate.Parameters[0].ParameterType.EndsWith("Object");
        }

        void WriteImplicitConversions(
            Class theClass,
            string boundTypeFullName,
            string declaringTypeFullName,
            bool isMarshalByValueType,
            CodeWriter code)
        {
            if (isMarshalByValueType)
            {
                code.Code($"internal {GetNameFromTypeFullName(declaringTypeFullName)}" +
                    $"({this.PluginAlias}{boundTypeFullName} source)");
                code.Code("{");
                code.Code("\tthis.CopyValuesFrom(source);");
                code.Code("}");
                code.Code();

                if (!IsOneWay(theClass))
                {
                    code.Code("public static implicit operator " +
                        $"{this.PluginAlias}{boundTypeFullName}({GetNameFromTypeFullName(declaringTypeFullName)} value)");
                    code.Code("{");
                    code.Code($"\t{this.PluginAlias}{boundTypeFullName} copy = " +
                        $"new {this.PluginAlias}{boundTypeFullName}();");
                    code.Code("\tvalue.CopyValuesTo(copy);");
                    code.Code("\treturn copy;");
                    code.Code("}");
                    code.Code();
                }

                code.Code("public static implicit operator " +
                    $"{GetNameFromTypeFullName(declaringTypeFullName)}({this.PluginAlias}{boundTypeFullName} value)");
                code.Code("{");
                code.Code($"\t{GetNameFromTypeFullName(declaringTypeFullName)} copy = " +
                    $"new {GetNameFromTypeFullName(declaringTypeFullName)}();");
                code.Code("\tcopy.CopyValuesFrom(value);");
                code.Code("\treturn copy;");
                code.Code("}");
                code.Code();

                code.Code($"void CopyValuesFrom({this.PluginAlias}{boundTypeFullName} value)");
                code.Code("{");

                foreach (Property property in theClass.Members.OfType<Property>()
                    .Where(property => !property.IsStatic && property.Name != "ClassHandle"))
                {
                    string thisPropertyName = property.Name;
                    if (property.PropertyType == "System.Boolean" && !thisPropertyName.StartsWith("Is") &&
                        !thisPropertyName.StartsWith("Has") && !thisPropertyName.StartsWith("Use"))
                    {
                        thisPropertyName = "Is" + thisPropertyName;
                    }

                    string thisPropertyType =
                        this.GetMemberTypeName(property.PropertyType, declaringTypeFullName, thisPropertyName);
                    code.Code($"\tthis.{thisPropertyName} = " + this.CastArgument(
                        $"value.{property.Name}", property.PropertyType, thisPropertyType) + ";");
                }

                code.Code("}");

                if (!IsOneWay(theClass))
                {
                    code.Code();
                    code.Code($"void CopyValuesTo({this.PluginAlias}{boundTypeFullName} value)");
                    code.Code("{");

                    foreach (Property property in theClass.Members.OfType<Property>()
                        .Where(property => !property.IsStatic && property.Name != "ClassHandle"))
                    {
                        string thisPropertyName = property.Name;
                        if (property.PropertyType == "System.Boolean" && !thisPropertyName.StartsWith("Is") &&
                            !thisPropertyName.StartsWith("Has") && !thisPropertyName.StartsWith("Use"))
                        {
                            thisPropertyName = "Is" + thisPropertyName;
                        }

                        string thisPropertyType =
                            this.GetMemberTypeName(property.PropertyType, declaringTypeFullName, thisPropertyName);
                        string otherPropertyType = property.PropertyType;
                        if (this.enumTypes.Contains(otherPropertyType))
                        {
                            otherPropertyType = this.PluginAlias + otherPropertyType;
                        }
                        code.Code($"\tvalue.{property.Name} = " + this.CastArgument(
                            $"this.{thisPropertyName}", thisPropertyType, otherPropertyType) + ";");
                    }

                    code.Code("}");
                }
            }
            else if (!theClass.IsAbstract)
            {
                code.Code($"internal {GetNameFromTypeFullName(declaringTypeFullName)}" +
                    $"({this.PluginAlias}{boundTypeFullName} forward)");
                code.Code("{");
                code.Code("\tthis.forward = forward;");
                code.Code("}");
                code.Code();
                code.Code("public static implicit operator " +
                    $"{this.PluginAlias}{boundTypeFullName}({GetNameFromTypeFullName(declaringTypeFullName)} value)");
                code.Code("{");
                code.Code("\treturn value?.Forward;");
                code.Code("}");
                code.Code();
                code.Code("public static implicit operator " +
                    $"{GetNameFromTypeFullName(declaringTypeFullName)}({this.PluginAlias}{boundTypeFullName} value)");
                code.Code("{");
                code.Code("\treturn value != null ? " +
                    $"new {GetNameFromTypeFullName(declaringTypeFullName)}(value) : null;");
                code.Code("}");
            }
        }

        protected bool IsOneWay(Class theClass)
        {
            // A type can only be proxied in one direction if it does not have a public parameter-less constructor.
            // Also, events are naturally one-way.
            return theClass.Name.EndsWith("Event") ||
                !theClass.Members.OfType<Constructor>().Any(c => c.Parameters.Count == 0);
        }

        protected bool IsAddEventMethod(Method method)
        {
            if (!method.Name.StartsWith("Add") ||
                !method.Name.EndsWith("Listener") ||
                method.ReturnType != "System.Void" ||
                method.Parameters.Count != 1)
            {
                return false;
            }

            Parameter param = method.Parameters[0];
            string parameterTypeName = this.GetNameFromTypeFullName(param.ParameterType);
            return parameterTypeName.EndsWith("Listener");
        }

        protected bool IsRemoveEventMethod(Method method)
        {
            if (!method.Name.StartsWith("Remove") ||
                !method.Name.EndsWith("Listener") ||
                method.ReturnType != "System.Void" ||
                method.Parameters.Count != 1)
            {
                return false;
            }

            Parameter param = method.Parameters[0];
            string parameterTypeName = this.GetNameFromTypeFullName(param.ParameterType);
            return parameterTypeName.EndsWith("Listener");
        }

        protected virtual string GetNamespaceFromTypeFullName(string typeFullName)
        {
            int lastDot = typeFullName.LastIndexOf('.');
            string typeNamespace = (lastDot > 0 ? typeFullName.Substring(0, lastDot) : String.Empty);
            return typeNamespace;
        }

        protected virtual string GetNameFromTypeFullName(string typeFullName)
        {
            int lastDotOrPlus = typeFullName.LastIndexOfAny(new char[] { '.', '+' });
            string typeName = typeFullName.Substring(lastDotOrPlus + 1);
            return typeName;
        }

        protected string GetBaseClassName(Class theClass)
        {
            if (theClass.Name.EndsWith("Event"))
            {
                return "System.EventArgs";
            }

            foreach (Type listenerType in this.listenerMap.Values)
            {
                Interface listenerInterface = listenerType as Interface;
                Delegate listenerDelegate = listenerType as Delegate;
                if (listenerInterface != null)
                {
                    if (listenerInterface.Members.OfType<Method>().Any(
                        m => m.Parameters.Any(p => p.ParameterType == theClass.FullName)))
                    {
                        return "System.EventArgs";
                    }
                }
                else if (listenerDelegate != null)
                {
                    if (listenerDelegate.Parameters.Any(p => p.ParameterType == theClass.FullName))
                    {
                        return "System.EventArgs";
                    }
                }
            }

            return null;
        }

        protected virtual string[] GetInterfaceNames(Type type, string typeFullName)
        {
            return this.IsMarshalByValueType(type, typeFullName) ||
                (type is Class && ((Class)type).IsStatic) ?
                new string[0] : new[] { "System.IDisposable" };
        }

        protected bool IsMarshalByValueType(Type type, string typeFullName)
        {
            Class classType = type as Class;
            if (classType != null && this.GetBaseClassName(classType) == "System.EventArgs")
            {
                return true;
            }

            string typeName = typeFullName.Substring(GetNamespaceFromTypeFullName(typeFullName).Length + 1);
            PluginInfo.AssemblyClassInfo classInfo = this.PluginInfo.Assembly.Classes.FirstOrDefault(
                c => c.Name == typeName || c.Name == typeFullName);
            bool marshalByValue = (classInfo != null && classInfo.MarshalByValue == "true");
            return marshalByValue;
        }

        protected string GetMemberTypeName(
            string boundTypeFullName, string declaringTypeFullName, string memberName)
        {
            string simpleClassName = this.GetNameFromTypeFullName(declaringTypeFullName);
            foreach (PluginInfo.AssemblyClassInfo classInfo in this.PluginInfo.Assembly.Classes)
            {
                if (classInfo.Name == simpleClassName && classInfo.TypeBindings != null)
                {
                    foreach (PluginInfo.TypeBindingInfo typeBinding in classInfo.TypeBindings)
                    {
                        if (typeBinding.Member == memberName && typeBinding.ParameterIndex == null &&
                            typeBinding.Type != null)
                        {
                            return GetTypeName(typeBinding.Type, declaringTypeFullName);
                        }
                    }
                }
            }

            return GetTypeName(boundTypeFullName, declaringTypeFullName);
        }

        protected string GetParameterTypeName(
            string boundTypeFullName, string declaringTypeFullName, string memberName, int parameterIndex)
        {
            string simpleClassName = this.GetNameFromTypeFullName(declaringTypeFullName);
            foreach (PluginInfo.AssemblyClassInfo classInfo in this.PluginInfo.Assembly.Classes)
            {
                if (classInfo.Name == simpleClassName && classInfo.TypeBindings != null)
                {
                    foreach (PluginInfo.TypeBindingInfo typeBinding in classInfo.TypeBindings)
                    {
                        if (typeBinding.Member == memberName &&
                            typeBinding.ParameterIndex == parameterIndex.ToString() &&
                            typeBinding.Type != null)
                        {
                            return GetTypeName(typeBinding.Type, declaringTypeFullName);
                        }
                    }
                }
            }

            return GetTypeName(boundTypeFullName, declaringTypeFullName);
        }

        protected string GetAliasedTypeName(string typeFullName, bool isPluginType = true)
        {
            if (isPluginType && !String.IsNullOrEmpty(this.PluginAlias) &&
                this.NamespaceMappings.Any(n => typeFullName.StartsWith(n.Namespace + ".")))
            {
                return this.PluginAlias + typeFullName;
            }

            return typeFullName;
        }

        protected virtual string GetTypeName(string boundTypeFullName, string declaringTypeFullName)
        {
            string arrayPart = String.Empty;
            string genericPart = String.Empty;
            string nullablePart = String.Empty;

            if (boundTypeFullName.EndsWith("?"))
            {
                nullablePart = "?";
                boundTypeFullName = boundTypeFullName.Substring(0, boundTypeFullName.Length - 1);
            }

            if (boundTypeFullName.EndsWith("[]"))
            {
                arrayPart = "[]";
                boundTypeFullName = boundTypeFullName.Substring(0, boundTypeFullName.Length - 2);
            }
            else if (boundTypeFullName.EndsWith(">"))
            {
                int ltIndex = boundTypeFullName.IndexOf('<');
                if (boundTypeFullName.EndsWith(">>"))
                {
                    // TODO: This code doesn't handle multiple generic arguments in the outer type.
                    // So it can work with a List<Dictionary<,>> but not a Dictionary<,List<>>.
                    string genericArgument = boundTypeFullName
                        .Substring(ltIndex + 1, boundTypeFullName.Length - ltIndex - 2);
                    genericArgument = GetTypeName(genericArgument, declaringTypeFullName);
                    genericPart = "<" + genericArgument + ">";
                }
                else
                {
                    string[] genericArguments = boundTypeFullName
                        .Substring(ltIndex + 1, boundTypeFullName.Length - ltIndex - 2)
                        .Split(',').Select(a => a.Trim()).ToArray();
                    for (int i = 0; i < genericArguments.Length; i++)
                    {
                        genericArguments[i] = GetTypeName(genericArguments[i], declaringTypeFullName);
                    }

                    genericPart = "<" + String.Join(", ", genericArguments) + ">";
                }
                boundTypeFullName = boundTypeFullName.Substring(0, ltIndex);
            }

            string typeName = this.MapTypeName(boundTypeFullName);
            if (typeName == null)
            {
                string boundTypeNamespace = this.GetNamespaceFromTypeFullName(boundTypeFullName);
                string declaringTypeNamespace = this.GetNamespaceFromTypeFullName(declaringTypeFullName);

                if (!String.IsNullOrEmpty(boundTypeNamespace))
                {
                    foreach (PluginInfo.NamespaceMappingInfo namespaceMapping in this.NamespaceMappings)
                    {
                        if (namespaceMapping.Package == boundTypeNamespace ||
                            namespaceMapping.Namespace == boundTypeNamespace)
                        {
                            typeName = GetNameFromTypeFullName(boundTypeFullName);
                            if (namespaceMapping.Namespace != declaringTypeNamespace)
                            {
                                typeName = namespaceMapping.Namespace + "." + typeName;
                                break;
                            }
                        }
                    }
                }

                if (typeName == null)
                {
                    typeName = boundTypeFullName;
                }

                if (typeName.StartsWith(GetNameFromTypeFullName(declaringTypeFullName) + "+"))
                {
                    typeName = typeName.Substring(typeName.IndexOf('+') + 1);
                }
                else
                {
                    typeName = typeName.Replace('+', '.');
                }
            }

            typeName += genericPart + arrayPart + nullablePart;
            return typeName;
        }

        protected virtual string MapTypeName(string typeFullName)
        {
            switch (typeFullName)
            {
                case "System.Boolean": return "bool";
                case "System.Byte": return "byte";
                case "System.Char": return "char";
                case "System.Double": return "double";
                case "System.Float": return "float";
                case "System.Int16": return "short";
                case "System.Int32": return "int";
                case "System.Int64": return "long";
                case "System.Object": return "object";
                case "System.SByte": return "sbyte";
                case "System.String": return "string";
                case "System.UInt16": return "ushort";
                case "System.UInt32": return "uint";
                case "System.UInt64": return "ulong";
                case "System.Void": return "void";
                default: return null;
            }
        }
    }
}
