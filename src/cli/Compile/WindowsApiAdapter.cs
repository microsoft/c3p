// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.C3P
{
    /// <summary>
    /// Generates adapter code to convert Windows WinRT APIs into standard types and patterns
    /// that are the same across all platforms.
    /// </summary>
    class WindowsApiAdapter : ApiAdapter
    {
        Dictionary<string, List<Type>> innerTypeMap = new Dictionary<string, List<Type>>();

        protected override List<PluginInfo.NamespaceMappingInfo> NamespaceMappings
        {
            get
            {
                return this.PluginInfo.WindowsPlatform?.IncludeNamespaces;
            }
        }

        protected override List<PluginInfo.EnumInfo> EnumMappings
        {
            get
            {
                return null;
            }
        }

        protected override string PluginAlias
        {
            get
            {
                return "plugin::";
            }
        }

        public override void GenerateAdapterCodeForApi(Assembly api)
        {
            base.GenerateAdapterCodeForApi(api);
            Utils.ExtractResource("WindowsPluginContext.cs", this.OutputDirectoryPath);
        }

        protected override void IndexType(Type type)
        {
            base.IndexType(type);

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

        protected override bool IsAsyncMethod(Method method)
        {
            return method.ReturnType.StartsWith("Windows.Foundation.IAsync");
        }

        protected override void WriteAsyncMethod(
            Method method,
            string declaringTypeFullName,
            string boundTypeFullName,
            bool isInterfaceMember,
            CodeWriter code)
        {
            string context = this.GetImplicitContext(method, code);
            int skip = context != null ? 1 : 0;

            string asyncReturnType;
            string asyncTaskReturnType;
            if (method.ReturnType.StartsWith("Windows.Foundation.IAsyncAction"))
            {
                asyncReturnType = "void";
                asyncTaskReturnType = "System.Threading.Tasks.Task";
            }
            else
            {
                asyncReturnType = method.ReturnType.Substring(method.ReturnType.IndexOf('<') + 1);
                asyncReturnType = asyncReturnType.Substring(0, asyncReturnType.Length - 1);
                asyncTaskReturnType = "System.Threading.Tasks.Task<" +
                    GetMemberTypeName(asyncReturnType, declaringTypeFullName, method.Name) + ">";
            }

            bool isAbstract = isInterfaceMember || method.IsAbstract;
            string parameters = String.Join(", ", method.Parameters.Skip(skip).Select(
                (p, i) => GetParameterTypeName(p.ParameterType, declaringTypeFullName, method.Name, i) +
                    " " + p.Name));
            code.Code($"{(isInterfaceMember ? "" : "public ")}{(method.IsStatic ? "static " : "")}" +
                $"{(isAbstract && !isInterfaceMember ? "abstract " : "async ")}{asyncTaskReturnType} " +
                $"{method.Name}({parameters}){(isAbstract ? ";" : "")}");

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
                            p.ParameterType,
                            true)));
                string forward = (method.IsStatic ? this.PluginAlias + boundTypeFullName : "Forward");
                if (asyncReturnType == "void")
                {
                    code.Code($"\tawait System.WindowsRuntimeSystemExtensions.AsTask(" +
                        $"{forward}.{method.Name}({arguments}));");
                }
                else
                {
                    code.Code($"\tvar result = await " +
                        "System.WindowsRuntimeSystemExtensions.AsTask(" +
                        $"{forward}.{method.Name}({arguments}));");
                    code.Code("\treturn " + this.CastArgument(
                        $"result",
                        asyncReturnType,
                        this.GetMemberTypeName(asyncReturnType, declaringTypeFullName, method.Name)) + ";");
                }
                code.Code("}");
            }
        }

        protected override void WriteEvent(
            Event eventMember,
            string declaringTypeFullName,
            string boundTypeFullName,
            bool isInterfaceMember,
            CodeWriter code)
        {
            if (!isInterfaceMember)
            {
                string boundEventTypeFullName = this.GetBoundEventTypeForListenerType(eventMember.EventHandlerType);
                string eventTypeName = GetTypeName(boundEventTypeFullName, declaringTypeFullName);
                string listenerMapType = "System.Collections.Generic.Dictionary<" +
                    $"System.EventHandler<{eventTypeName}>, " +
                    $"System.EventHandler<{this.PluginAlias}{boundEventTypeFullName}>>";
                code.Code($"{(eventMember.IsStatic ? "static " : "")}" +
                    $"{listenerMapType} __{eventMember.Name}ListenerMap =");
                code.Code($"\tnew {listenerMapType}();");
                code.Code();
            }

            base.WriteEvent(eventMember, declaringTypeFullName, boundTypeFullName, isInterfaceMember, code);
        }

        protected override void WriteEventAdder(
            string eventName, Member eventMember, string declaringTypeFullName, string forward, CodeWriter code)
        {
            Event eventEvent = (Event)eventMember;
            string sender = eventEvent.IsStatic ? "typeof(" + declaringTypeFullName + ")" : "this";
            string boundEventTypeFullName = this.GetBoundEventTypeForListenerType(eventEvent.EventHandlerType);

            code.Code("add");
            code.Code("{");
            code.Code($"\tif (__{eventName}ListenerMap.ContainsKey(value)) return;");
            code.Code($"\tSystem.EventHandler<{this.PluginAlias}{boundEventTypeFullName}> handler = ");
            code.Code($"\t\t(sender, e) => value({sender}, e);");
            code.Code($"\t{forward}.{eventName} += handler;");
            code.Code($"\t__{eventName}ListenerMap[value] = handler;");
            code.Code("}");
        }

        protected override void WriteEventRemover(
            string eventName, Member eventMember, string declaringTypeFullName, string forward, CodeWriter code)
        {
            Event eventEvent = (Event)eventMember;
            string boundEventTypeFullName = this.GetBoundEventTypeForListenerType(eventEvent.EventHandlerType);

            code.Code("remove");
            code.Code("{");
            code.Code($"\tSystem.EventHandler<{this.PluginAlias}{boundEventTypeFullName}> handler;");
            code.Code($"\tif (__{eventName}ListenerMap.TryGetValue(value, out handler))");
            code.Code("\t{");
            code.Code($"\t\t{forward}.{eventName} -= handler;");
            code.Code($"\t\t__{eventName}ListenerMap.Remove(value);");
            code.Code("\t}");
            code.Code("}");
        }

        protected override bool IsIgnoredType(Type type)
        {
            int separatorIndex = type.FullName.LastIndexOf('_');
            if (separatorIndex > type.FullName.LastIndexOf('.'))
            {
                // Inner types inferred via underscores will be handled specially.
                return true;
            }

            return base.IsIgnoredType(type);
        }

        protected override bool IsIgnoredMember(Type declaringType, Member member)
        {
            if (base.IsIgnoredMember(declaringType, member))
            {
                return true;
            }

            Method method = member as Method;
            if (method != null && method.Name == "Dispose")
            {
                // The Dispose() method is generated automatically when the adapted object implements IDisposable.
                return true;
            }

            return false;
        }

        protected override string GetNamespaceFromTypeFullName(string typeFullName)
        {
            return base.GetNamespaceFromTypeFullName(typeFullName);
        }

        protected override string MapTypeName(string typeFullName)
        {
            switch (typeFullName)
            {
                case "Windows.Foundation.IAsyncAction":
                case "Windows.Foundation.IAsyncOperation": return "System.Threading.Tasks.Task";
                default: return base.MapTypeName(typeFullName);
            }
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

            return typeName;
        }
    }
}
