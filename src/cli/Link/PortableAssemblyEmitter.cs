// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.IO;

namespace Microsoft.C3P
{
    /// <summary>
    /// Generates a portable metadata-only assembly containing the cross-platform APIs.
    /// </summary>
    /// <remarks>
    /// The generated portable assembly is used only at development time in a Xamarin application.
    /// When the application is built, the correct platform-specific assembly is included instead
    /// (known as the "bait-and-switch" technique).</remarks>
    class PortableAssemblyEmitter
    {
        public Assembly Assembly { get; set; }

        public string AssemblyFilePath { get; set; }

        public void Run()
        {
            if (this.Assembly == null)
            {
                throw new ArgumentNullException(nameof(Assembly));
            }
            else if (String.IsNullOrEmpty(this.AssemblyFilePath))
            {
                throw new ArgumentNullException(nameof(AssemblyFilePath));
            }

            PortableAssemblyEmitter.EmitAssembly(this.Assembly, this.AssemblyFilePath);
        }

        static void EmitAssembly(Assembly assembly, string assemblyFilePath)
        {
            AssemblyName assemblyName = new AssemblyName
            {
                Name = assembly.Name,
                Version = assembly.Version,
            };
            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
                assemblyName, AssemblyBuilderAccess.Save, Path.GetDirectoryName(assemblyFilePath));
            ModuleBuilder moduleBuilder =
                assemblyBuilder.DefineDynamicModule(Path.GetFileName(assemblyFilePath), false);

            EmitTypes(assembly.Types, moduleBuilder);

            assemblyBuilder.Save(
                Path.GetFileName(assemblyFilePath), PortableExecutableKinds.ILOnly, ImageFileMachine.I386);
        }

        static void EmitTypes(
            IList<Type> types,
            ModuleBuilder moduleBuilder)
        {
            Dictionary<string, TypeBuilder> typeBuilders = new Dictionary<string, TypeBuilder>();

            foreach (Type type in types)
            {
                EmitType(type, typeBuilders, moduleBuilder);
            }

            foreach (Type type in types)
            {
                EmitTypeMembers(type, typeBuilders);
            }
        }

        static void EmitType(
            Type type,
            Dictionary<string, TypeBuilder> typeBuilders,
            ModuleBuilder moduleBuilder,
            TypeBuilder parentTypeBuilder = null)
        {
            System.Type baseType = null;
            System.Type[] interfaceTypes = null;

            if (type is Class)
            {
                baseType = ResolveType(((Class)type).ExtendsType, typeBuilders);
                interfaceTypes = ((Class)type).ImplementsTypes?.Select(
                    t => ResolveType(t, typeBuilders))?.Where(t => t != null)?.ToArray() ?? System.Type.EmptyTypes;
            }
            else if (type is Interface)
            {
                interfaceTypes = ((Interface)type).ExtendsTypes?.Select(
                    t => ResolveType(t, typeBuilders))?.Where(t => t != null)?.ToArray() ?? System.Type.EmptyTypes;
            }
            else if (type is Struct)
            {
                interfaceTypes = ((Struct)type).ImplementsTypes?.Select(
                    t => ResolveType(t, typeBuilders))?.Where(t => t != null)?.ToArray() ?? System.Type.EmptyTypes;
            }
            else if (type is Enum)
            {
                baseType = typeof(System.Enum);
            }
            else
            {
                throw new NotSupportedException("Unsupported type of type: " + type.GetType().Name);
            }

            TypeBuilder typeBuilder = null;
            if (parentTypeBuilder == null)
            {
                typeBuilder = moduleBuilder.DefineType(type.FullName, type.Attributes, baseType, interfaceTypes);
            }
            else
            {
                typeBuilder = parentTypeBuilder.DefineNestedType(
                    type.Name.Substring(type.Name.IndexOf('+') + 1), type.Attributes, baseType, interfaceTypes);
            }

            typeBuilders.Add(type.FullName, typeBuilder);

            if (typeBuilder != null)
            {
                foreach (Type nestedType in type.Members.OfType<Type>())
                {
                    EmitType(nestedType, typeBuilders, moduleBuilder, typeBuilder);
                }
            }
        }

        static void EmitTypeMembers(Type type, Dictionary<string, TypeBuilder> typeBuilders)
        {
            TypeBuilder typeBuilder;
            if (typeBuilders.TryGetValue(type.FullName, out typeBuilder))
            {
                if (type is Enum)
                {
                    System.Type underlyingType = ResolveType(((Enum)type).UnderlyingType, typeBuilders);
                    typeBuilder.DefineField(
                        "value__", underlyingType, FieldAttributes.Private | FieldAttributes.SpecialName);
                }

                foreach (Member member in type.Members)
                {
                    if (member is Type)
                    {
                        EmitTypeMembers((Type)member, typeBuilders);
                    }
                    else
                    {
                        EmitMember(type, member, typeBuilders, typeBuilder);
                    }
                }

                ////Log.Normal("Creating type: " + type.FullName);
                typeBuilder.CreateType();
            }
        }

        static void EmitMember(
            Type declaringType,
            Member member,
            Dictionary<string, TypeBuilder> typeBuilders,
            TypeBuilder typeBuilder)
        {
            Constructor constructor = member as Constructor;
            Property property = member as Property;
            Method method = member as Method;
            Field field = member as Field;
            Event eventMember = member as Event;
            EnumValue enumMember = member as EnumValue;

            if (constructor != null)
            {
                ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(
                    constructor.Attributes,
                    constructor.CallingConvention,
                    constructor.Parameters?.Select(p => ResolveType(p.ParameterType, typeBuilders))?.ToArray());
                IList<Parameter> parameters = constructor.Parameters;
                if (parameters != null)
                {
                    for (int i = 0; i < parameters.Count; i++)
                    {
                        constructorBuilder.DefineParameter(i + 1, parameters[i].Attributes, parameters[i].Name);
                    }
                }

                ILGenerator il = constructorBuilder.GetILGenerator();

                if (typeBuilder.IsClass)
                {
                    ConstructorInfo baseConstructor = typeBuilder.BaseType.GetConstructor(System.Type.EmptyTypes);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Call, baseConstructor);
                }

                il.Emit(OpCodes.Ret);
            }
            else if (method != null)
            {
                MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                    method.Name,
                    method.Attributes,
                    method.CallingConvention,
                    ResolveType(method.ReturnType, typeBuilders),
                    method.Parameters?.Select(p => ResolveType(p.ParameterType, typeBuilders))?.ToArray());
                IList<Parameter> parameters = method.Parameters;
                if (parameters != null)
                {
                    for (int i = 0; i < parameters.Count; i++)
                    {
                        methodBuilder.DefineParameter(i + 1, parameters[i].Attributes, parameters[i].Name);
                    }
                }

                ILGenerator il = methodBuilder.GetILGenerator();
                il.DeclareLocal(ResolveType(method.ReturnType, typeBuilders));
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ret);
            }
            else if (property != null)
            {
                PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(
                  property.Name,
                  property.Attributes,
                  ResolveType(property.PropertyType, typeBuilders),
                  property.IndexParameters?.Select(p => ResolveType(p.ParameterType, typeBuilders))?.ToArray());

                Method getMethod = property.GetMethod;
                if (getMethod != null)
                {
                    MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                        "get_" + property.Name,
                        getMethod.Attributes,
                        ResolveType(getMethod.ReturnType, typeBuilders),
                        getMethod.Parameters?.Select(p => ResolveType(p.ParameterType, typeBuilders))?.ToArray());

                    ILGenerator il = methodBuilder.GetILGenerator();
                    il.DeclareLocal(ResolveType(getMethod.ReturnType, typeBuilders));
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ret);

                    propertyBuilder.SetGetMethod(methodBuilder);
                }

                Method setMethod = property.SetMethod;
                if (setMethod != null)
                {
                    MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                        "set_" + property.Name,
                        setMethod.Attributes,
                        null,
                        setMethod.Parameters?.Select(p => ResolveType(p.ParameterType, typeBuilders))?.ToArray());

                    ILGenerator il = methodBuilder.GetILGenerator();
                    il.Emit(OpCodes.Ret);
                    propertyBuilder.SetSetMethod(methodBuilder);
                }
            }
            else if (field != null)
            {
                typeBuilder.DefineField(
                    field.Name,
                    ResolveType(field.FieldType, typeBuilders),
                    field.Attributes);
            }
            else if (eventMember != null)
            {
                MethodBuilder addMethodBuilder = typeBuilder.DefineMethod(
                    "add_" + eventMember.Name,
                    MethodAttributes.Public | MethodAttributes.SpecialName |
                        (eventMember.IsStatic ? MethodAttributes.Static : 0),
                    null,
                    new[] { ResolveType(eventMember.EventHandlerType, typeBuilders) });
                ILGenerator il = addMethodBuilder.GetILGenerator();
                il.DeclareLocal(typeof(void));
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ret);

                MethodBuilder removeMethodBuilder = typeBuilder.DefineMethod(
                    "remove_" + eventMember.Name,
                    MethodAttributes.Public | MethodAttributes.SpecialName |
                        (eventMember.IsStatic ? MethodAttributes.Static : 0),
                    null,
                    new[] { ResolveType(eventMember.EventHandlerType, typeBuilders) });
                il = removeMethodBuilder.GetILGenerator();
                il.DeclareLocal(typeof(void));
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ret);

                EventBuilder eventBuilder = typeBuilder.DefineEvent(
                    eventMember.Name,
                    eventMember.Attributes,
                    ResolveType(eventMember.EventHandlerType, typeBuilders));
                eventBuilder.SetAddOnMethod(addMethodBuilder);
                eventBuilder.SetRemoveOnMethod(removeMethodBuilder);
            }
            else if (enumMember != null)
            {
                FieldBuilder fieldBuilder = typeBuilder.DefineField(
                    enumMember.Name,
                    typeBuilder,
                    FieldAttributes.Public | FieldAttributes.Literal | FieldAttributes.Static);

                System.Type underlyingType = ResolveType(((Enum)declaringType).UnderlyingType, typeBuilders);
                fieldBuilder.SetConstant(Convert.ChangeType(enumMember.Value, underlyingType));
            }
            else
            {
                throw new NotSupportedException("Unsupported member type: " + member.GetType().Name);
            }
        }

        static System.Type ResolveType(string typeFullName, Dictionary<string, TypeBuilder> typeBuilders)
        {
            System.Type resolvedType = null;
            if (typeFullName == "Java.Lang.Object")
            {
                return typeof(object);
            }
            else if (typeFullName == "Android.Runtime.IJavaObject" ||
                     typeFullName == "Java.Interop.IJavaObjectEx")
            {
                // Classes bound to Java APIs implement these interfaces.
                // It's safe to discard them in the portable assembly.
                return null;
            }
            else if (typeFullName == "Java.Util.IEventListener" ||
                typeFullName == "Java.IO.ISerializable")
            {
                // IEventListener and ISerializable are just tagging interfaces with no methods,
                // so they are save to discard.
                return null;
            }
            else if (typeFullName == "Java.Util.EventObject")
            {
                // EventObject is a base class for Java events. It has a getSource() method.
                // TODO: Consider generating a GetSource() method when discarding this base class.
                return typeof(System.Object);
            }
            else if (typeFullName.EndsWith("]"))
            {
                ////Log.Normal("Resolving array type " + typeFullName);
                if (typeFullName.IndexOfAny(new char[] { '*', ',' }) >= 0 ||
                    typeFullName.IndexOf(']') < typeFullName.Length - 1)
                {
                    throw new NotSupportedException("Multi-dimensional and jagged arrays are not supported.");
                }

                string elementTypeFullName = typeFullName.Substring(0, typeFullName.IndexOf('['));
                System.Type elementType = ResolveType(elementTypeFullName, typeBuilders);
                resolvedType = elementType.MakeArrayType();
            }
            else if (typeFullName.EndsWith(">"))
            {
                ////Log.Normal("Resolving generic type " + typeFullName);

                int ltIndex = typeFullName.IndexOf('<');
                string genericArguments = typeFullName.Substring(ltIndex + 1, typeFullName.Length - ltIndex - 2);
                IEnumerable<string> genericArgumentTypeFullNames = genericArguments.Split(',').Select(t => t.Trim());
                System.Type[] genericArgumentTypes = genericArgumentTypeFullNames.Select(
                    t => ResolveType(t, typeBuilders)).ToArray();

                string genericTypeDefinitionFullName = typeFullName.Substring(0, ltIndex) +
                    '`' + genericArgumentTypes.Length;
                System.Type genericDefinition = ResolveType(genericTypeDefinitionFullName, typeBuilders);

                resolvedType = genericDefinition.MakeGenericType(genericArgumentTypes);
            }
            else if (typeBuilders.ContainsKey(typeFullName))
            {
                ////Log.Normal("Resolving type " + typeFullName + " from emitted module.");
                TypeBuilder resolvedTypeBuilder;
                typeBuilders.TryGetValue(typeFullName, out resolvedTypeBuilder);
                resolvedType = resolvedTypeBuilder;
            }
            else if (!(typeFullName.StartsWith("Android.") || typeFullName.StartsWith("Java.")))
            {
                ////Log.Normal("Resolving type " + typeFullName + " from BCL.");
                resolvedType = System.Type.GetType(typeFullName, false);
                if (resolvedType == null)
                {
                    // Also check in the "System" assembly in addition to mscorlib.
                    resolvedType = typeof(System.Uri).Assembly.GetType(typeFullName, false);
                }
            }

            if (resolvedType == null)
            {
                throw new InvalidOperationException("Failed to resolve type: " + typeFullName);
            }

            return resolvedType;
        }
    }
}
