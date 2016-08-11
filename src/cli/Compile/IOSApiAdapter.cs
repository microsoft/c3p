// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.C3P
{
    /// <summary>
    /// Generates adapter code to convert iOS Obj-C APIs into standard types and patterns
    /// that are the same across all platforms.
    /// </summary>
    class IOSApiAdapter : ApiAdapter
    {
        Dictionary<string, List<Type>> innerTypeMap = new Dictionary<string, List<Type>>();

        protected override List<PluginInfo.NamespaceMappingInfo> NamespaceMappings
        {
            get
            {
                return this.PluginInfo.IOSPlatform?.NamespaceMappings;
            }
        }

        public override void GenerateAdapterCodeForApi(Assembly api)
        {
            base.GenerateAdapterCodeForApi(api);
            Utils.ExtractResource("IOSPluginContext.cs", this.OutputDirectoryPath);
        }

        protected override void IndexType(Type type)
        {
            base.IndexType(type);

            Class theClass = type as Class;
            Delegate theDelegate = type as Delegate;

            if (theDelegate != null && IsEventListenerDelegate(theDelegate))
            {
                this.listenerMap.Add(type.FullName, type);
            }

            int separatorIndex = type.FullName.LastIndexOf('_');
            if (separatorIndex > type.FullName.LastIndexOf('.'))
            {
                string outerName = type.FullName.Substring(0, separatorIndex);
                List<Type> innerTypes;
                if (!this.innerTypeMap.TryGetValue(outerName, out innerTypes))
                {
                    innerTypes = new List<Type>();
                    this.innerTypeMap.Add(outerName, innerTypes);
                }

                innerTypes.Add(type);
            }

            if (theClass != null)
            {
                this.FixStaticProperties(theClass);
            }
        }

        void FixStaticProperties(Class theClass)
        {
            foreach (Property property in theClass.Members.OfType<Property>()
                .Where(p => p.IsStatic && p.SetMethod == null).ToArray())
            {
                Method setMethod = theClass.Members.OfType<Method>().FirstOrDefault(
                  m => m.Name == "Set" + property.Name && m.Parameters.Count == 1 &&
                      m.Parameters[0].ParameterType == property.PropertyType);
                if (setMethod != null)
                {
                    property.SetMethod = setMethod;
                    theClass.Members.Remove(setMethod);
                }
            }

            foreach (Method setMethod in theClass.Members.OfType<Method>()
                .Where(m => m.IsStatic && m.Parameters.Count == 1 && m.Name.StartsWith("Set")).ToArray())
            {
                string propertyName = setMethod.Name.Substring(3);
                string propertyType = setMethod.Parameters[0].ParameterType;

                Method getMethod = theClass.Members.OfType<Method>().FirstOrDefault(
                    m => m.Name == propertyName && m.Parameters.Count == 0 &&
                    m.ReturnType == propertyType);
                if (getMethod != null)
                {
                    Property property = new Property
                    {
                        Name = propertyName,
                        PropertyType = propertyType,
                        IsStatic = true,
                        GetMethod = getMethod,
                        SetMethod = setMethod,
                    };
                    theClass.Members.Add(property);
                    theClass.Members.Remove(getMethod);
                    theClass.Members.Remove(setMethod);
                }
            }
        }

        protected override void WriteInnerTypes(Class outerClass, string outerClassFullName, CodeWriter code)
        {
            base.WriteInnerTypes(outerClass, outerClassFullName, code);

            List<Type> inferredInnerTypes;
            if (this.innerTypeMap.TryGetValue(outerClass.FullName, out inferredInnerTypes))
            {
                foreach (Type innerType in inferredInnerTypes)
                {
                    string innerName = innerType.Name.Substring(innerType.Name.LastIndexOf('_') + 1);

                    code.Code();
                    this.WriteType(innerType, outerClassFullName + "+" + innerName, code);
                }
            }
        }

        protected override void WritePropertyGetter(
            Property property, string mappedPropertyType, string forward, CodeWriter code)
        {
            // The Objective Sharpie binder /sometimes/ binds static property getters as normal methods.
            if (!property.GetMethod.Name.StartsWith("get_"))
            {
                code.Code("get");
                code.Code("{");
                code.Code($"\treturn " +
                    this.CastArgument(
                        $"{forward}.{property.GetMethod.Name}()",
                        property.PropertyType, mappedPropertyType) +
                    ";");
                code.Code("}");
            }
            else
            {
                base.WritePropertyGetter(property, mappedPropertyType, forward, code);
            }
        }

        protected override void WritePropertySetter(
            Property property, string mappedPropertyType, string forward, CodeWriter code)
        {
            // The Objective Sharpie binder /sometimes/ binds static property setters as normal methods.
            if (!property.SetMethod.Name.StartsWith("set_"))
            {
                code.Code("set");
                code.Code("{");
                code.Code($"\t{forward}.{property.SetMethod.Name}(" +
                    this.CastArgument("value", mappedPropertyType, property.PropertyType) + ");");
                code.Code("}");
            }
            else
            {
                base.WritePropertySetter(property, mappedPropertyType, forward, code);
            }
        }

        protected override bool IsOutErrorMember(Member constructorOrMethod)
        {
            Parameter lastParameter;

            Constructor constructor = constructorOrMethod as Constructor;
            if (constructor != null)
            {
                lastParameter = constructor.Parameters.LastOrDefault();
            }
            else
            {
                Method method = (Method)constructorOrMethod;
                lastParameter = method.Parameters.LastOrDefault();
            }

            return lastParameter != null && lastParameter.ParameterType == "Foundation.NSError&";
        }

        protected override void WriteOutErrorConstructor(
            Constructor constructor,
            string declaringTypeFullName,
            string boundTypeFullName,
            CodeWriter code)
        {
            string context = this.GetImplicitContext(constructor, code);
            int skip = context != null ? 1 : 0;
            string parameters = String.Join(", ", constructor.Parameters.Skip(skip)
                .Take(constructor.Parameters.Count - 1 - skip).Select(
                (p, i) => GetParameterTypeName(p.ParameterType, declaringTypeFullName, null, i) + " " + p.Name));
            string visibility = declaringTypeFullName.EndsWith("Event") ? "internal" : "public";
            code.Code($"{visibility} {GetNameFromTypeFullName(declaringTypeFullName)}({parameters})");
            code.Code("{");
            code.Code("\tFoundation.NSError error;");

            if (context != null && constructor.Parameters.Count > 1)
            {
                context += ", ";
            }

            string arguments = (context != null ? context : String.Empty) +
                String.Join(", ", constructor.Parameters.Skip(skip)
                    .Take(constructor.Parameters.Count - 1 - skip).Select(
                    (p,i) => this.CastArgument(
                        p.Name,
                        this.GetParameterTypeName(p.ParameterType, declaringTypeFullName, null, i),
                        p.ParameterType))) + (constructor.Parameters.Count > 1 + skip ? ", " : "") + "out error";

            code.Code($"\tforward = new {boundTypeFullName}({arguments});");
            code.Code("\tif (error != null) throw new Foundation.NSErrorException(error);");

            code.Code("}");
        }

        protected override void WriteOutErrorMethod(
            Method method,
            string declaringTypeFullName,
            string boundTypeFullName,
            bool isInterfaceMember,
            bool isMarshalByValueType,
            CodeWriter code)
        {
            string methodName = method.Name;
            if (method.Parameters.Count == 1)
            {
                if (methodName.EndsWith("WithError"))
                {
                    methodName = methodName.Substring(0, methodName.Length - "WithError".Length);
                }
                else if (methodName.EndsWith("Error"))
                {
                    methodName = methodName.Substring(0, methodName.Length - "Error".Length);
                }
            }

            string context = this.GetImplicitContext(method, code);
            int skip = context != null ? 1 : 0;

            bool isAbstract = isInterfaceMember || method.IsAbstract;
            string parameters = String.Join(", ", method.Parameters.Skip(skip)
                .Take(method.Parameters.Count - 1 - skip).Select(
                (p, i) => GetParameterTypeName(p.ParameterType, declaringTypeFullName, method.Name, i) + " " + p.Name));
            code.Code($"{(isInterfaceMember ? "" : "public ")}{(method.IsStatic ? "static " : "")}" +
                $"{(isAbstract && !isInterfaceMember ? "abstract " : "")}" +
                $"{GetMemberTypeName(method.ReturnType, declaringTypeFullName, method.Name)} " +
                $"{methodName}({parameters}){(isAbstract ? ";" : "")}");

            if (!isAbstract)
            {
                code.Code("{");
                code.Code("\tFoundation.NSError error;");

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
                    String.Join(", ", method.Parameters.Skip(skip)
                        .Take(method.Parameters.Count - 1 - skip).Select(
                        (p, i) => this.CastArgument(
                            p.Name,
                            this.GetParameterTypeName(p.ParameterType, declaringTypeFullName, method.Name, i),
                            p.ParameterType))) + (method.Parameters.Count > 1 + skip ? ", " : "") + "out error";
                string forward = (method.IsStatic ? boundTypeFullName : "Forward");

                if (method.ReturnType == "System.Void")
                {
                    code.Code($"\t{forward}.{method.Name}({arguments});");
                    code.Code("\tif (error != null) throw new Foundation.NSErrorException(error);");


                    if (isMarshalByValueType && !method.IsStatic)
                    {
                        code.Code("\tthis.CopyValuesFrom(Forward);");
                    }
                }
                else
                {
                    code.Code($"\tvar result = " +
                        this.CastArgument(
                            $"{forward}.{method.Name}({arguments})",
                            method.ReturnType, this.GetMemberTypeName(method.ReturnType, declaringTypeFullName, method.Name)) +
                            ";");
                    code.Code("\tif (error != null) throw new Foundation.NSErrorException(error);");

                    if (isMarshalByValueType && !method.IsStatic)
                    {
                        code.Code("\tthis.CopyValuesFrom(Forward);");
                    }

                    code.Code("\treturn result;");
                }

                code.Code("}");
            }
        }

        protected override bool IsAsyncMethod(Method method)
        {
            string lastParameterType = method.Parameters.LastOrDefault()?.ParameterType;
            if (lastParameterType == null)
            {
                return false;
            }

            return lastParameterType.StartsWith("System.Action<");
        }

        protected override void WriteAsyncMethod(
            Method method,
            string declaringTypeFullName,
            string boundTypeFullName,
            bool isInterfaceMember,
            CodeWriter code)
        {
            string methodName = method.Name;
            if ((method.Parameters.Count == 1 || method.Parameters.Count == 2) &&
                method.Parameters.All(p => p.ParameterType.StartsWith("System.Action")))
            {
                if (methodName.EndsWith("AndThen"))
                {
                    methodName = methodName.Substring(0, methodName.Length - "AndThen".Length);
                }
                else if (methodName.EndsWith("Then"))
                {
                    methodName = methodName.Substring(0, methodName.Length - "Then".Length);
                }
                else if (methodName.EndsWith("WithResult"))
                {
                    methodName = methodName.Substring(0, methodName.Length - "WithResult".Length);
                }
                else if (methodName.EndsWith("Result"))
                {
                    methodName = methodName.Substring(0, methodName.Length - "Result".Length);
                }
            }

            string context = this.GetImplicitContext(method, code);
            int skip = context != null ? 1 : 0;

            bool hasCatchCallback = false;
            int callbackParameterIndex = method.Parameters.Count - 1;
            if (method.Parameters[callbackParameterIndex].ParameterType == "System.Action<Foundation.NSError>")
            {
                --callbackParameterIndex;
                hasCatchCallback = true;
            }

            if (callbackParameterIndex < 0 ||
                !method.Parameters[callbackParameterIndex].ParameterType.StartsWith("System.Action"))
            {
                throw new InvalidOperationException("Invalid async method signature: " + method);
            }

            bool hasReturnValue = false;
            string returnValueType = "void";
            string returnType = "System.Threading.Tasks.Task";
            string thenCallbackType = method.Parameters[callbackParameterIndex].ParameterType;
            string callbackParameterType = null;
            int ltIndex = thenCallbackType.IndexOf('<');
            if (ltIndex > 0)
            {
                hasReturnValue = true;
                callbackParameterType =
                    thenCallbackType.Substring(ltIndex + 1, thenCallbackType.Length - ltIndex - 2);
                returnValueType = GetMemberTypeName(callbackParameterType, declaringTypeFullName, method.Name);
                returnType += "<" + returnValueType + ">";
            }

            bool isAbstract = isInterfaceMember || method.IsAbstract;
            int parameterCount = method.Parameters.Count - (hasCatchCallback ? 2 : 1) - skip;
            string parameters = String.Join(", ", method.Parameters.Skip(skip).Take(parameterCount).Select(
                (p, i) => GetParameterTypeName(p.ParameterType, declaringTypeFullName, method.Name, i) + " " + p.Name));
            code.Code($"{(isInterfaceMember ? "" : "public ")}{(method.IsStatic ? "static " : "")}" +
                $"{(isAbstract && !isInterfaceMember ? "abstract" : "async")} {returnType} " +
                $"{methodName}({parameters}){(isAbstract ? ";" : "")}");

            if (!isAbstract)
            {
                code.Code("{");
                if (hasReturnValue)
                {
                    code.Code($"\tvar asyncResult = default({returnValueType});");
                }

                code.Code("\tvar completion = new System.Threading.Tasks.TaskCompletionSource<bool>();");
                code.Code($"\t{thenCallbackType} success = ({(hasReturnValue ? "result" : "")}) =>");

                if (hasReturnValue)
                {
                    code.Code("\t{");
                    code.Code($"\t\tasyncResult = " +
                        this.CastArgument("result", callbackParameterType, returnValueType) + ";");
                    code.Code("\t\tcompletion.SetResult(true);");
                    code.Code("\t};");
                }
                else
                {
                    code.Code("\t\tcompletion.SetResult(true);");
                }

                if (hasCatchCallback)
                {
                    code.Code("\tSystem.Action<Foundation.NSError> failure = (error) =>");
                    code.Code("\t\tcompletion.SetException(new Foundation.NSErrorException(error));");
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
                            p.ParameterType)));
                string forward = (method.IsStatic ? boundTypeFullName : "Forward");
                code.Code($"\t{forward}.{method.Name}({arguments});");

                code.Code("\tawait completion.Task;");

                if (hasReturnValue)
                {
                    code.Code("\treturn asyncResult;");
                }

                code.Code("}");
            }
        }

        protected override void WriteEvent(
            Method eventMethod,
            string declaringTypeFullName,
            string boundTypeFullName,
            bool isInterfaceMember,
            CodeWriter code)
        {
            if (!isInterfaceMember && !eventMethod.IsAbstract)
            {
                string listenerTypeFullName = eventMethod.Parameters[0].ParameterType;
                Type listenerType;
                if (!this.listenerMap.TryGetValue(listenerTypeFullName, out listenerType))
                {
                    throw new InvalidOperationException(
                        "Listener delegate type not found: " + listenerTypeFullName);
                }

                string boundEventTypeFullName = this.GetBoundEventTypeForListenerType(listenerType);
                string eventTypeName = GetTypeName(boundEventTypeFullName, declaringTypeFullName);
                string eventName = eventMethod.Name.Substring(
                    "Add".Length, eventMethod.Name.Length - "AddListener".Length);
                string listenerMapType = "System.Collections.Generic.Dictionary<" +
                    $"System.EventHandler<{eventTypeName}>, {listenerTypeFullName}>";
                code.Code($"{(eventMethod.IsStatic ? "static " : "")}{listenerMapType} __{eventName}ListenerMap =");
                code.Code($"\tnew {listenerMapType}();");
                code.Code();
            }

            base.WriteEvent(eventMethod, declaringTypeFullName, boundTypeFullName, isInterfaceMember, code);
        }

        protected override void WriteEventAdder(
            string eventName, Member eventMember, string declaringTypeFullName, string forward, CodeWriter code)
        {
            Method eventMethod = eventMember as Method;
            Event eventEvent = eventMember as Event;
            bool isStatic = (eventMethod != null && eventMethod.IsStatic) ||
                (eventEvent != null && eventEvent.IsStatic);

            string listenerTypeFullName = (eventMethod != null ? eventMethod.Parameters[0].ParameterType :
                eventEvent != null ? eventEvent.EventHandlerType : "???");
            string sender = isStatic ?
                "typeof(" + GetTypeName(declaringTypeFullName, declaringTypeFullName) + ")" : "this";

            code.Code("add");
            code.Code("{");
            code.Code($"\tif (__{eventName}ListenerMap.ContainsKey(value)) return;");
            code.Code($"\t{listenerTypeFullName} listener = (sender, e) => {{");

            // TODO: IOS event listeners currently fail to get removed at the native code layer,
            // because the Xamarin binding layer creates unique blocks.
            // This check for presence in the map is a workaround until a better solution is found.
            code.Code($"\t\tif (__{eventName}ListenerMap.ContainsKey(value)) value({sender}, e);");

            code.Code("\t};");
            code.Code($"\t{forward}.Add{eventName}Listener(listener);");
            code.Code($"\t__{eventName}ListenerMap[value] = listener;");
            code.Code("}");
        }

        protected override void WriteEventRemover(
            string eventName, Member eventMember, string declaringTypeFullName, string forward, CodeWriter code)
        {
            Method eventMethod = eventMember as Method;
            Event eventEvent = eventMember as Event;

            string listenerTypeFullName = (eventMethod != null ? eventMethod.Parameters[0].ParameterType :
                eventEvent != null ? eventEvent.EventHandlerType : "???");

            code.Code("remove");
            code.Code("{");
            code.Code($"\t{listenerTypeFullName} listener;");
            code.Code($"\tif (__{eventName}ListenerMap.TryGetValue(value, out listener))");
            code.Code("\t{");
            code.Code($"\t\t{forward}.Remove{eventName}Listener(listener);");
            code.Code($"\t\t__{eventName}ListenerMap.Remove(value);");
            code.Code("\t}");
            code.Code("}");
        }

        protected override string GetImplicitContext(Member member, CodeWriter code)
        {
            Constructor constructor = member as Constructor;
            Method method = member as Method;

            if ((constructor != null && constructor.Parameters.Count > 0 &&
                constructor.Parameters[0].ParameterType == "UIKit.UIApplication") ||
                (method != null && method.Parameters.Count > 0 &&
                method.Parameters[0].ParameterType == "UIKit.UIApplication"))
            {
                code.Code($"[{typeof(ImplicitContextAttribute).FullName}(\"{ImplicitContextAttribute.Application}\")]");
                return typeof(IOSApiAdapter).Namespace + ".PluginContext.Application";
            }
            else if ((constructor != null && constructor.Parameters.Count > 0 &&
                constructor.Parameters[0].ParameterType == "UIKit.UIWindow") ||
                (method != null && method.Parameters.Count > 0 &&
                    method.Parameters[0].ParameterType == "UIKit.UIWindow"))
            {
                code.Code($"[{typeof(ImplicitContextAttribute).FullName}(\"{ImplicitContextAttribute.Window}\")]");
                return typeof(IOSApiAdapter).Namespace + ".PluginContext.CurrentWindow";
            }

            return null;
        }

        static bool IsEventListenerInterface(Interface theInterface)
        {
            if (!theInterface.Name.EndsWith("Listener") ||
                theInterface.Members.Count != 1)
            {
                return false;
            }

            Method method = theInterface.Members[0] as Method;
            return method != null &&
                method.ReturnType == "System.Void" &&
                method.Parameters.Count == 2 &&
                method.Parameters[1].ParameterType == "Foundation.NSObject" &&
                method.Parameters[0].ParameterType.EndsWith("Event");
        }

        static bool IsEventListenerClass(Class theClass)
        {
            if (!theClass.IsAbstract || !theClass.Name.EndsWith("Listener"))
            {
                return false;
            }

            if (theClass.ImplementsTypes == null || !theClass.ImplementsTypes.Any(i => i.EndsWith("Listener")) ||
                theClass.Members.Count != 1 || !(theClass.Members[0] is Method))
            {
                return false;
            }

            Method method = (Method)theClass.Members[0];
            return method.Parameters.Count == 2 &&
                method.Parameters[0].ParameterType.EndsWith("Event") &&
                method.Parameters[1].ParameterType == "Foundation.NSObject";
        }

        protected override bool IsIgnoredType(Type type)
        {
            if (base.IsIgnoredType(type))
            {
                return true;
            }

            int separatorIndex = type.FullName.LastIndexOf('_');
            if (separatorIndex > type.FullName.LastIndexOf('.'))
            {
                // Inner types inferred via underscores will be handled specially.
                return true;
            }

            if ((type is Class && IsEventListenerClass((Class)type)) ||
                (type is Interface && IsEventListenerInterface((Interface)type)))
            {
                return true;
            }

            return false;
        }

        protected override bool IsIgnoredMember(Type declaringType, Member member)
        {
            if (base.IsIgnoredMember(declaringType, member))
            {
                return true;
            }

            Constructor constructor = member as Constructor;
            Property property = member as Property;
            Method method = member as Method;

            if (property != null && !property.IsStatic &&
                property.Name == "ClassHandle" && property.PropertyType == "System.IntPtr")
            {
                return true;
            }

            // Check if the delcaring class implements the UIApplicationDelegate protocol.
            Class declaringClass = declaringType as Class;
            if (declaringClass != null &&
                declaringClass.ImplementsTypes.Contains("UIKit.IUIApplicationDelegate"))
            {
                // Any methods with a UIApplication as the first parameter should be excluded from the API.
                // (They may be called via a different mechanism.)
                if (method != null && method.Parameters.Count > 0 &&
                    method.Parameters[0].ParameterType == "UIKit.UIApplication")
                {
                    return true;
                }

                // Any properties of type UIApplication should also be excluded.
                // (They may only be for internal use.)
                if (property != null && (property.PropertyType == "UIKit.UIApplication"))
                {
                    return true;
                }
            }

            // If this member is a parameter-less constructor, exclude it if there is also a constructor
            // that takes an application context.
            if (constructor != null)
            {
                if (constructor != null && constructor.Parameters.Count == 0 &&
                    declaringType.Members.OfType<Constructor>().Any(
                        c => c.Parameters.Count == 1 && c.Parameters[0].ParameterType == "UIKit.UIApplication"))
                {
                    return true;
                }
            }

            if (method != null)
            {
                // A static "initialize" method is automatically called by the Obj-C runtime,
                // so it doesn't need to be exposed in the adapter layer.
                if (method.IsStatic && method.Name == "Initialize")
                {
                    return true;
                }
            }

            return false;
        }

        protected override string GetBoundEventTypeForListenerType(Type listenerType)
        {
            string eventType = null;

            Delegate listenerDelegate = listenerType as Delegate;
            if (listenerDelegate != null)
            {
                // IsEventListenerDelegate already validated that the delegate has 2 parameters
                // with the second one being the event object.
                eventType = listenerDelegate.Parameters[1].ParameterType;
            }

            return eventType;
        }

        static string GetObjCClassPrefix(string objCClassName)
        {
            for (int i = 1; i < objCClassName.Length; i++)
            {
                if (!Char.IsUpper(objCClassName[i]) && !Char.IsDigit(objCClassName[i]) &&
                    Char.IsUpper(objCClassName[i - 1]))
                {
                    return objCClassName.Substring(0, i - 1);
                }
            }

            return String.Empty;
        }

        protected override string GetNamespaceFromTypeFullName(string typeFullName)
        {
            string namespaceName = base.GetNamespaceFromTypeFullName(typeFullName);
            if (namespaceName != IOSApiCompiler.bindingNamespace)
            {
                return namespaceName;
            }

            string className = typeFullName.Substring(IOSApiCompiler.bindingNamespace.Length + 1);
            string prefix = GetObjCClassPrefix(className);
            foreach (PluginInfo.NamespaceMappingInfo nsMapping in this.NamespaceMappings)
            {
                if (String.Equals(nsMapping.Prefix, prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return nsMapping.Namespace;
                }
            }

            throw new InvalidOperationException("No namespace mapping was found for Obj-C prefix: " + prefix +
                " (Class: " + typeFullName + ")");
        }

        protected override string GetNameFromTypeFullName(string typeFullName)
        {
            string namespaceName = base.GetNamespaceFromTypeFullName(typeFullName);
            if (namespaceName != IOSApiCompiler.bindingNamespace)
            {
                return base.GetNameFromTypeFullName(typeFullName);
            }

            string className = typeFullName.Substring(IOSApiCompiler.bindingNamespace.Length + 1);
            string prefix = GetObjCClassPrefix(className);
            return className.Substring(prefix.Length);
        }

        protected override string GetTypeName(string boundTypeFullName, string declaringTypeFullName)
        {
            string typeName = base.GetTypeName(boundTypeFullName, declaringTypeFullName);
            int separatorIndex = typeName.LastIndexOf('_');
            if (separatorIndex > typeName.LastIndexOf('.'))
            {
                string outerName = typeName.Substring(0, separatorIndex);
                string innerName = typeName.Substring(separatorIndex + 1);
                if (outerName == GetNameFromTypeFullName(declaringTypeFullName))
                {
                    return innerName;
                }
                else
                {
                    return outerName + '.' + innerName;
                }
            }

            if (typeName.EndsWith("[]"))
            {
                // NSArray should be bound as IList, not directly as an array.
                return "System.Collections.Generic.IList<" + typeName.Substring(0, typeName.Length - 2) + ">";
            }

            return typeName;
        }

        protected override string MapTypeName(string typeFullName)
        {
            switch (typeFullName)
            {
                case "Foundation.NSString": return "string";
                case "Foundation.NSDictionary": return "System.Collections.Generic.IDictionary";
                case "Foundation.NSUuid": return "System.Guid";
                case "Foundation.NSUrl": return "System.Uri";
                case "Foundation.NSDate": return "System.DateTimeOffset?";
                default: return base.MapTypeName(typeFullName);
            }
        }

        protected override string CastArgument(
            string argument, string fromTypeName, string toTypeName, bool alias = false)
        {
            if (fromTypeName.StartsWith("Foundation.NSDictionary<") &&
                toTypeName.StartsWith("System.Collections.Generic.IDictionary<"))
            {
                // TODO: This doesn't handle nested generic types with multiple generic arguments.
                string fromGenericArguments = fromTypeName.Substring(fromTypeName.IndexOf('<') + 1);
                fromGenericArguments = fromGenericArguments.Substring(0, fromGenericArguments.Length - 1);
                string fromKeyType = fromGenericArguments.Split(',')[0].Trim();
                string fromValueType = fromGenericArguments.Split(',')[1].Trim();
                string toGenericArguments = toTypeName.Substring(toTypeName.IndexOf('<') + 1);
                toGenericArguments = toGenericArguments.Substring(0, toGenericArguments.Length - 1);
                string toKeyType = toGenericArguments.Split(',')[0].Trim();
                string toValueType = toGenericArguments.Split(',')[1].Trim();

                return "System.Linq.Enumerable.ToDictionary<System.Collections.Generic.KeyValuePair<" +
                    $"{fromKeyType}, {fromValueType}>, {toKeyType}, {toValueType}>({argument}, " +
                    "kv => " + CastArgument("kv.Key", fromKeyType, toKeyType) + ", " +
                    "kv => " + CastArgument("kv.Value", fromValueType, toValueType) + ")";
            }
            else if (fromTypeName.StartsWith("System.Collections.Generic.IDictionary<") &&
                toTypeName.StartsWith("Foundation.NSDictionary<"))
            {
                // TODO: This doesn't handle nested generic types with multiple generic arguments.
                string fromGenericArguments = fromTypeName.Substring(fromTypeName.IndexOf('<') + 1);
                fromGenericArguments = fromGenericArguments.Substring(0, fromGenericArguments.Length - 1);
                string fromKeyType = fromGenericArguments.Split(',')[0].Trim();
                string fromValueType = fromGenericArguments.Split(',')[1].Trim();
                string toGenericArguments = toTypeName.Substring(toTypeName.IndexOf('<') + 1);
                toGenericArguments = toGenericArguments.Substring(0, toGenericArguments.Length - 1);
                string toKeyType = toGenericArguments.Split(',')[0].Trim();
                string toValueType = toGenericArguments.Split(',')[1].Trim();

                return $"new Foundation.NSDictionary<{toKeyType}, {toValueType}>(" +
                    "System.Linq.Enumerable.ToArray(" +
                        $"System.Linq.Enumerable.Select(({argument}).Keys, k => ({toKeyType})" +
                        CastArgument("k", fromKeyType, toKeyType) + "))," +
                    "System.Linq.Enumerable.ToArray(" +
                        $"System.Linq.Enumerable.Select(({argument}).Keys, k => ({toKeyType})" +
                        CastArgument($"{argument}[k]", fromValueType, toValueType) + ")))";
            }
            else if (fromTypeName == "Foundation.NSUuid" && toTypeName == "System.Guid")
            {
                return $"({argument} == null ? System.Guid.Empty : new System.Guid({argument}.GetBytes()))";
            }
            else if (fromTypeName == "System.Guid" && toTypeName == "Foundation.NSUuid")
            {
                return $"new Foundation.NSUuid({argument}.ToString())";
            }
            else if (fromTypeName == "Foundation.NSUuid" && toTypeName == "System.Guid?")
            {
                return $"({argument} == null ? (System.Guid?)null : new System.Guid({argument}.GetBytes()))";
            }
            else if (fromTypeName == "System.Guid?" && toTypeName == "Foundation.NSUuid")
            {
                return $"({argument} == null ? null : new Foundation.NSUuid({argument}.ToString()))";
            }
            else if (fromTypeName == "int?" && toTypeName == "Foundation.NSNumber")
            {
                return $"({argument} == null ? null : Foundation.NSNumber.FromInt32({argument}.Value))";
            }
            else if (fromTypeName == "Foundation.NSNumber" && toTypeName == "int?")
            {
                return $"({argument} == null ? (int?)null : {argument}.Int32Value)";
            }
            else if (fromTypeName == "int" && toTypeName == "Foundation.NSNumber")
            {
                return $"Foundation.NSNumber.FromInt32({argument}.Value)";
            }
            else if (fromTypeName == "Foundation.NSNumber" && toTypeName == "int")
            {
                return $"({argument} == null ? 0 : {argument}.Int32Value)";
            }
            else if (fromTypeName == "double?" && toTypeName == "Foundation.NSNumber")
            {
                return $"({argument} == null ? null : Foundation.NSNumber.FromDouble({argument}.Value))";
            }
            else if (fromTypeName == "Foundation.NSNumber" && toTypeName == "double?")
            {
                return $"({argument} == null ? (double?)null : {argument}.DoubleValue)";
            }
            else if (fromTypeName == "double" && toTypeName == "Foundation.NSNumber")
            {
                return $"Foundation.NSNumber.FromDouble({argument}.Value)";
            }
            else if (fromTypeName == "Foundation.NSNumber" && toTypeName == "double")
            {
                return $"({argument} == null ? 0 : {argument}.DoubleValue)";
            }
            else if (fromTypeName == "bool?" && toTypeName == "Foundation.NSNumber")
            {
                return $"({argument} == null ? null : Foundation.NSNumber.FromBoolean({argument}.Value))";
            }
            else if (fromTypeName == "Foundation.NSNumber" && toTypeName == "bool?")
            {
                return $"({argument} == null ? (bool?)null : {argument}.BoolValue)";
            }
            else if (fromTypeName == "bool" && toTypeName == "Foundation.NSNumber")
            {
                return $"Foundation.NSNumber.FromBoolean({argument}.Value)";
            }
            else if (fromTypeName == "Foundation.NSNumber" && toTypeName == "bool")
            {
                return $"({argument} == null ? false : {argument}.BoolValue)";
            }
            else if (fromTypeName == "Foundation.NSDate" && toTypeName == "System.DateTimeOffset?")
            {
                return $"{argument} == null ? (System.DateTimeOffset?)null :" +
                    $"new System.DateTimeOffset((System.DateTime){argument})";
            }
            else if (fromTypeName == "System.DateTimeOffset?" && toTypeName == "Foundation.NSDate")
            {
                return $"{argument} == null ? null : (Foundation.NSDate){argument}.Value.LocalDateTime";
            }
            else
            {
                return base.CastArgument(argument, fromTypeName, toTypeName, alias);
            }
        }
    }
}

