// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.C3P
{
    /// <summary>
    /// Generates adapter code to convert Android Java APIs into standard types and patterns
    /// that are the same across all platforms.
    /// </summary>
    class AndroidApiAdapter : ApiAdapter
    {

        public JavaApi JavaApi
        {
            get;
            set;
        }

        protected override List<PluginInfo.NamespaceMappingInfo> NamespaceMappings
        {
            get
            {
                return this.PluginInfo.AndroidPlatform?.NamespaceMappings;
            }
        }

        protected override List<PluginInfo.EnumInfo> EnumMappings
        {
            get
            {
                return this.PluginInfo.AndroidPlatform?.Enums;
            }
        }

        public override void GenerateAdapterCodeForApi(Assembly api)
        {
            base.GenerateAdapterCodeForApi(api);
            Utils.ExtractResource("AndroidPluginContext.cs", this.OutputDirectoryPath);
        }

        protected override void IndexType(Type type)
        {
            base.IndexType(type);

            Interface theInterface = type as Interface;
            if (theInterface != null && IsEventListenerInterface(theInterface))
            {
                this.listenerMap.Add(type.FullName, type);
            }
        }

        protected override bool IsEnumClass(Class theClass)
        {
            Constructor[] constructors = theClass.Members.OfType<Constructor>().ToArray();
            if (constructors.Length != 1 || constructors[0].Parameters.Count != 0)
            {
                return false;
            }

            Field[] fields = theClass.Members.OfType<Field>().ToArray();
            if (fields.Length == 0 || theClass.Members.Count != constructors.Length + fields.Length)
            {
                return false;
            }

            return fields.All(f => f.IsStatic && f.Value != null);
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
                method.Parameters.Count == 1 &&
                (method.Parameters[0].ParameterType.EndsWith("Event") ||
                 method.Parameters[0].ParameterType.EndsWith("EventArgs"));
        }

        protected override string GetImplicitContext(Member member, CodeWriter code)
        {
            Constructor constructor = member as Constructor;
            Method method = member as Method;

            if ((constructor != null && constructor.Parameters.Count > 0 &&
                constructor.Parameters[0].ParameterType == "Android.App.Application") ||
                (method != null && method.Parameters.Count > 0 &&
                 method.Parameters[0].ParameterType == "Android.App.Application"))
            {
                code.Code($"[{typeof(ImplicitContextAttribute).FullName}(\"{ImplicitContextAttribute.Application}\")]");
                return typeof(AndroidApiAdapter).Namespace + ".PluginContext.Application";
            }
            else if ((constructor != null && constructor.Parameters.Count > 0 &&
                constructor.Parameters[0].ParameterType == "Android.App.Activity") ||
                (method != null && method.Parameters.Count > 0 &&
                    method.Parameters[0].ParameterType == "Android.App.Activity"))
            {
                code.Code($"[{typeof(ImplicitContextAttribute).FullName}(\"{ImplicitContextAttribute.Window}\")]");
                return typeof(AndroidApiAdapter).Namespace + ".PluginContext.CurrentActivity";
            }

            return null;
        }

        protected override bool IsAsyncMethod(Method method)
        {
            return method.ReturnType == "Java.Util.Concurrent.IFuture";
        }

        protected override void WriteAsyncMethod(
            Method method,
            string declaringTypeFullName,
            string boundTypeFullName,
            bool isInterfaceMember,
            CodeWriter code)
        {
            string asyncReturnType = this.FindAsyncReturnType(boundTypeFullName, method.Name, declaringTypeFullName);
            string mappedAsyncReturnType = this.GetMemberTypeName(asyncReturnType, declaringTypeFullName, method.Name);
            string xamarinReturnType = this.MapTypeName(asyncReturnType);

            if (asyncReturnType == "java.util.UUID")
            {
                // TODO: This special case shouldn't be necessary here.
                xamarinReturnType = "Java.Util.UUID";
            }

            string castFromType = xamarinReturnType;
            if (xamarinReturnType == null)
            {
                if (asyncReturnType.StartsWith("java.util.List<"))
                {
                    // Java lists inside a Future don't get automatically converted to
                    // the generic IList<> by Xamarin.
                    xamarinReturnType = "System.Collections.IList";
                    string itemType = asyncReturnType.Substring(asyncReturnType.IndexOf('<') + 1);
                    itemType = itemType.Substring(0, itemType.Length - 1);
                    castFromType = xamarinReturnType + "<" + (this.MapTypeName(itemType) ?? itemType) + ">";
                }
                else
                {
                    xamarinReturnType = mappedAsyncReturnType;
                    castFromType = xamarinReturnType;
                }
            }

            string context = this.GetImplicitContext(method, code);
            int skip = context != null ? 1 : 0;

            bool isAbstract = isInterfaceMember || method.IsAbstract;
            string parameters = String.Join(", ", method.Parameters.Skip(skip).Select(
                (p, i) => GetParameterTypeName(p.ParameterType, declaringTypeFullName, method.Name, i) +
                    " " + p.Name));
            code.Code($"{(isInterfaceMember ? "" : "public ")}{(method.IsStatic ? "static " : "")}" +
                $"{(isAbstract && !isInterfaceMember ? "abstract" : "async")} " +
                "System.Threading.Tasks.Task" +
                (mappedAsyncReturnType != "void" ? "<" + mappedAsyncReturnType + ">" : "") +
                $" {method.Name}({parameters}){(isAbstract ? ";" : "")}");

            if (!isAbstract)
            {
                code.Code("{");

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
                code.Code($"\tvar future = {forward}.{method.Name}({arguments});");

                if (mappedAsyncReturnType == "void")
                {
                    code.Code("\tawait Java.Util.Concurrent.IFutureExtensions.GetAsync(future);");
                }
                else
                {
                    code.Code($"\tvar result = ({xamarinReturnType})" +
                        "await Java.Util.Concurrent.IFutureExtensions.GetAsync(future);");
                    code.Code($"\treturn " + this.CastArgument($"result", castFromType, mappedAsyncReturnType) + ";");
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
                string listenerInterfaceFullName = eventMethod.Parameters[0].ParameterType;
                Type listenerType;
                if (!this.listenerMap.TryGetValue(listenerInterfaceFullName, out listenerType))
                {
                    throw new InvalidOperationException(
                        "Listener interface type not found: " + listenerInterfaceFullName);
                }

                string boundEventTypeFullName = this.GetBoundEventTypeForListenerType(listenerInterfaceFullName);
                string eventTypeName = GetTypeName(boundEventTypeFullName, declaringTypeFullName);
                string eventName = eventMethod.Name.Substring(
                    "Add".Length, eventMethod.Name.Length - "AddListener".Length);
                this.WriteEventListener(
                    eventName, boundEventTypeFullName, eventTypeName, listenerType, code);
                string listenerMapType = "System.Collections.Generic.Dictionary<" +
                    $"System.EventHandler<{eventTypeName}>, __{eventName}Listener>";
                code.Code($"{(eventMethod.IsStatic ? "static " : "")}{listenerMapType} __{eventName}ListenerMap =");
                code.Code($"\tnew {listenerMapType}();");
                code.Code();
            }

            base.WriteEvent(eventMethod, declaringTypeFullName, boundTypeFullName, isInterfaceMember, code);
        }

        void WriteEventListener(
            string eventName,
            string boundEventTypeFullName,
            string eventTypeName,
            Type listenerType,
            CodeWriter code)
        {
            string listenerMethodName = listenerType.Members.Cast<Method>().Single().Name;

            code.Code($"class __{eventName}Listener : global::Java.Lang.Object, {listenerType.FullName}");
            code.Code("{");
            code.Code("\tpublic object Sender { get; set; }");
            code.Code($"\tpublic System.EventHandler<{eventTypeName}> Handler {{ get; set; }}");
            code.Code($"\tpublic void {listenerMethodName}({boundEventTypeFullName} e)");
            code.Code("\t{");
            code.Code("\t\tHandler(Sender, e);");
            code.Code("\t}");
            code.Code("}");
            code.Code();
        }

        protected override bool IsIgnoredType(Type type)
        {
            Class javaClass = this.FindJavaApiClass(type.Namespace, type.Name.Replace("+", "."));
            Class theClass = type as Class;

            return base.IsIgnoredType(type) ||
                (javaClass != null && javaClass.Visibility != "public") ||
                type.Name == "BuildConfig" || type.Name == "__TypeRegistrations" ||
                (theClass != null && IsRedundantEventClass(theClass)) ||
                (theClass?.ExtendsType == "Android.App.Activity") ||
                (theClass?.ExtendsType == "Android.App.Fragment") ||
                (type is Interface && IsEventListenerInterface((Interface)type));
        }

        protected override bool IsIgnoredMember(Type declaringType, Member member)
        {
            if (base.IsIgnoredMember(declaringType, member))
            {
                return true;
            }

            Constructor constructor = member as Constructor;
            Method method = member as Method;
            Property property = member as Property;

            if (constructor != null && ((Class)declaringType).ExtendsType == "Java.Util.EventObject")
            {
                // This is a constructor for an event object. The constructor that takes a single source
                // object should be ignored, since the source is handled separately in the adapted API.
                return constructor.Parameters.Count == 1 &&
                    constructor.Parameters[0].ParameterType == "Java.Lang.Object";
            }

            // Check if the delcaring class implements the ActivityLifecycleCallbacks interface.
            Class declaringClass = declaringType as Class;
            if (declaringClass != null &&
                declaringClass.ImplementsTypes.Contains("Android.App.Application+IActivityLifecycleCallbacks"))
            {
                // Any methods with an Activity as the first parameter should be excluded from the API.
                // (They may be called via a different mechanism.)
                if (method != null && method.Parameters.Count > 0 &&
                    method.Parameters[0].ParameterType == "Android.App.Activity")
                {
                    return true;
                }

                // Any properties of type Activity or Context should also be excluded.
                // (They may only be for internal use.)
                if (property != null &&
                    (property.PropertyType == "Android.App.Activity" ||
                     property.PropertyType == "Android.Content.Context"))
                {
                    return true;
                }
            }

            // If this member is a parameter-less constructor, exclude it if there is also a constructor
            // that takes an Activity or Context.
            if (constructor != null && constructor.Parameters.Count == 0 &&
                declaringType.Members.OfType<Constructor>().Any(c => c.Parameters.Count == 1 &&
                    (c.Parameters[0].ParameterType == "Android.App.Context" ||
                        c.Parameters[0].ParameterType == "Android.Content.Context")))
            {
                return true;
            }

            if (method != null && method.Name == "OnActivityResult" && method.Parameters.Count == 3)
            {
                // The onActivityResult method must be public to allow intercepting activity results,
                // but it is not exposed in the plugin API.
                return true;
            }

            return false;
        }

        static bool IsRedundantEventClass(Class theClass)
        {
            return theClass.Name.EndsWith("EventArgs") &&
                theClass.Members.Count == 3 &&
                theClass.Members.OfType<Constructor>().Count() == 1 &&
                theClass.Members.OfType<Property>().Count() == 2 &&
                theClass.Members.OfType<Property>().Count(p => p.PropertyType == "Java.Lang.Object") == 1 &&
                theClass.Members.OfType<Property>().Count(p => p.SetMethod == null) == 2;
        }

        protected override string GetBoundEventTypeForListenerType(Type listenerType)
        {
            string eventType = null;

            Interface listenerInterface = listenerType as Interface;
            if (listenerInterface != null)
            {
                // IsEventListenerInterface already validated that the interface has one method,
                // and the method has 1 parameter which is the event object.
                eventType = ((Method)listenerInterface.Members[0]).Parameters[0].ParameterType;
            }

            return eventType;
        }

        protected override string GetNamespaceFromTypeFullName(string typeFullName)
        {
            string namespaceName = base.GetNamespaceFromTypeFullName(typeFullName);

            foreach (PluginInfo.NamespaceMappingInfo nsMapping in this.NamespaceMappings)
            {
                if (String.Equals(nsMapping.Package, namespaceName, StringComparison.OrdinalIgnoreCase))
                {
                    return nsMapping.Namespace;
                }
            }

            return namespaceName;
        }

        string FindAsyncReturnType(string classFullName, string methodName, string declaringTypeFullName)
        {
            string classNamespace = base.GetNamespaceFromTypeFullName(classFullName);
            string className = base.GetNameFromTypeFullName(classFullName);

            Class javaClass = this.FindJavaApiClass(classNamespace, className);
            if (javaClass != null)
            {
                Method javaMethod = javaClass.Methods.FirstOrDefault(
                    m => String.Equals(m.Name, methodName, StringComparison.OrdinalIgnoreCase));
                if (javaMethod != null)
                {
                    string returnType = javaMethod.ReturnType;
                    const string futurePrefix = "java.util.concurrent.Future<";
                    if (returnType != null && returnType.StartsWith(futurePrefix))
                    {
                        returnType = returnType.Substring(
                            futurePrefix.Length, returnType.Length - futurePrefix.Length - 1);
                        return returnType;
                    }
                }
            }

            throw new InvalidOperationException("Async return type not found for " + classFullName + "." + methodName);
        }

        Class FindJavaApiClass(string packageName, string className)
        {
            Package javaPackage = this.JavaApi.Packages.FirstOrDefault(
                p => String.Equals(p.Name, packageName, StringComparison.OrdinalIgnoreCase));
            if (javaPackage == null)
            {
                return null;
            }

            Class javaClass = javaPackage.Classes.FirstOrDefault(c => c.Name == className);
            return javaClass;
        }

        protected override string MapTypeName(string typeFullName)
        {
            switch (typeFullName)
            {
                case "java.lang.Void": return "void";
                case "java.lang.String": return "string";
                case "java.util.List": return "System.Collections.Generic.IList";
                case "java.util.Dictionary":
                case "Java.Util.Dictionary": return "System.Collections.Generic.IDictionary";
                case "java.util.UUID":
                case "Java.Util.UUID": return "System.Guid";
                case "java.lang.Boolean":
                case "Java.Lang.Boolean": return "bool?";
                case "java.lang.Integer":
                case "Java.Lang.Integer": return "int?";
                case "java.lang.Double":
                case "Java.Lang.Double": return "double?";
                case "android.net.Uri":
                case "Android.Net.Uri": return "System.Uri";
                case "java.util.Date":
                case "Java.Util.Date": return "System.DateTimeOffset?";
                default: return base.MapTypeName(typeFullName);
            }
        }

        protected override string CastArgument(
            string argument, string fromTypeName, string toTypeName, bool alias = false)
        {
            if (fromTypeName == "Java.Util.UUID" && toTypeName == "System.Guid")
            {
                return $"({argument} == null ? System.Guid.Empty : System.Guid.Parse({argument}.ToString()))";
            }
            else if (fromTypeName == "System.Guid" && toTypeName == "Java.Util.UUID")
            {
                return $"Java.Util.UUID.FromString({argument}.ToString())";
            }
            else if (fromTypeName == "Java.Util.UUID" && toTypeName == "System.Guid?")
            {
                return $"({argument} == null ? (System.Guid?)null : System.Guid.Parse({argument}.ToString()))";
            }
            else if (fromTypeName == "System.Guid?" && toTypeName == "Java.Util.UUID")
            {
                return $"({argument} == null ? null : Java.Util.UUID.FromString({argument}.ToString()))";
            }
            else if (fromTypeName == "int?" && toTypeName == "Java.Lang.Integer")
            {
                return $"({argument} == null ? null : Java.Lang.Integer.ValueOf({argument}.Value))";
            }
            else if (fromTypeName == "Java.Lang.Integer" && toTypeName == "int?")
            {
                return $"({argument} == null ? (int?)null : {argument}.IntValue())";
            }
            else if (fromTypeName == "int" && toTypeName == "Java.Lang.Integer")
            {
                return $"Java.Lang.Integer.ValueOf({argument}.Value)";
            }
            else if (fromTypeName == "Java.Lang.Integer" && toTypeName == "int")
            {
                return $"({argument} == null ? 0 : {argument}.IntValue())";
            }
            else if (fromTypeName == "double?" && toTypeName == "Java.Lang.Double")
            {
                return $"({argument} == null ? null : Java.Lang.Double.ValueOf({argument}.Value))";
            }
            else if (fromTypeName == "Java.Lang.Double" && toTypeName == "double?")
            {
                return $"({argument} == null ? (double?)null : {argument}.DoubleValue())";
            }
            else if (fromTypeName == "double" && toTypeName == "Java.Lang.Double")
            {
                return $"Java.Lang.Double.ValueOf({argument}.Value)";
            }
            else if (fromTypeName == "Java.Lang.Double" && toTypeName == "double")
            {
                return $"({argument} == null ? 0 : {argument}.DoubleValue())";
            }
            else if (fromTypeName == "bool?" && toTypeName == "Java.Lang.Boolean")
            {
                return $"({argument} == null ? null : Java.Lang.Boolean.ValueOf({argument}.Value))";
            }
            else if (fromTypeName == "Java.Lang.Boolean" && toTypeName == "bool?")
            {
                return $"({argument} == null ? (bool?)null : {argument}.BooleanValue())";
            }
            else if (fromTypeName == "bool" && toTypeName == "Java.Lang.Boolean")
            {
                return $"Java.Lang.Boolean.ValueOf({argument}.Value)";
            }
            else if (fromTypeName == "Java.Lang.Boolean" && toTypeName == "bool")
            {
                return $"({argument} == null ? false : {argument}.BooleanValue())";
            }
            else if (fromTypeName == "Android.Net.Uri" && toTypeName == "System.Uri")
            {
                return $"{argument} != null ? new System.Uri({argument}.ToString()) : null";
            }
            else if (fromTypeName == "System.Uri" && toTypeName == "Android.Net.Uri")
            {
                return $"{argument} != null ? Android.Net.Uri.Parse({argument}.AbsoluteUri) : null";
            }
            else if (fromTypeName == "Java.Util.Date" && toTypeName == "System.DateTimeOffset?")
            {
                return $"{argument} == null ? (System.DateTimeOffset?)null :" +
                    $"new System.DateTimeOffset(new System.DateTime(621355968000000000 + {argument}.Time))";
            }
            else if (fromTypeName == "System.DateTimeOffset?" && toTypeName == "Java.Util.Date")
            {
                return $"{argument} == null ? null : " +
                    $"new Java.Util.Date({argument}.Value.LocalDateTime.Ticks - 621355968000000000)";
            }
            else
            {
                return base.CastArgument(argument, fromTypeName, toTypeName, alias);
            }
        }
    }
}
