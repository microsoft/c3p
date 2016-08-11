// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.C3P
{
    /// <summary>
    /// Validates that APIs for multiple platfoms match and merges the APIs together into
    /// a unified cross-platform API.
    /// </summary>
    class PluginApiMerger
    {
        public PluginInfo PluginInfo { get; set; }

        /// <summary>
        /// Map from platform to API.
        /// </summary>
        public Dictionary<string, ClrApi> PlatformApis { get; set; }

        /// <summary>
        /// The merged API produced by this class.
        /// </summary>
        public ClrApi MergedApi { get; set; }

        /// <summary>
        /// Map from platform to API assembly.
        /// </summary>
        Dictionary<string, Assembly> platformAssemblies;

        /// <summary>
        /// Map from type full name to (map from platform to type).
        /// </summary>
        Dictionary<string, Dictionary<string, Type>> platformTypesIndex;

        /// <summary>
        /// Map from type full name dot member name to (map from platform to list of member overloads).
        /// </summary>
        Dictionary<string, Dictionary<string, List<Member>>> platformMembersIndex;

        List<string> validationWarnings;
        List<string> validationErrors;

        public void Run()
        {
            if (this.PluginInfo == null)
            {
                throw new ArgumentNullException(nameof(PluginInfo));
            }

            if (this.PlatformApis == null || this.PlatformApis.Count == 0 ||
                this.PlatformApis.Values.Any(a => a == null || a.Assemblies == null || a.Assemblies.Count == 0))
            {
                throw new ArgumentNullException(nameof(PlatformApis));
            }

            if (this.PlatformApis.Values.Any(a => a.Assemblies.Count > 1))
            {
                throw new NotSupportedException("Merging does not support multiple assemblies.");
            }

            this.platformAssemblies =
                this.PlatformApis.ToDictionary(pa => pa.Key, pa => pa.Value.Assemblies.Single());

            this.platformTypesIndex = IndexTypes(this.platformAssemblies);
            this.platformMembersIndex = IndexMembers(this.platformTypesIndex);

            this.ValidateAssemblies();

            if (this.validationWarnings.Count > 0)
            {
                Log.Warning("");
                foreach (string message in this.validationWarnings)
                {
                    Log.Warning("{0}", message);
                }
            }

            if (this.validationErrors.Count > 0)
            {
                Log.Error("");
                foreach (string message in this.validationErrors)
                {
                    Log.Error("{0}", message);
                }

                Log.Error("");
                throw new InvalidOperationException("Cannot merge APIs due to validation errors.");
            }

            Assembly mergedAssembly = this.MergeAssemblies(this.platformAssemblies.Values);
            this.MergedApi = new ClrApi
            {
                Platform = String.Join(",", this.PlatformApis.Keys),
                Assemblies = new List<Assembly>(new[] { mergedAssembly }),
            };

            // Event classes are automatically marshalled by value.
            foreach (Class eventClass in mergedAssembly.Types.OfType<Class>().Where(
                c => c.ExtendsType == "System.EventArgs"))
            {
                PluginInfo.AssemblyClassInfo classInfo = this.PluginInfo.Assembly.Classes
                    .FirstOrDefault(c => c.Name == eventClass.Name || c.Name == eventClass.FullName);
                if (classInfo != null)
                {
                    classInfo.MarshalByValue = "true";
                }
                else
                {
                    this.PluginInfo.Assembly.Classes.Add(new PluginInfo.AssemblyClassInfo
                    {
                        Name = eventClass.Name,
                        MarshalByValue = "true",
                    });
                }
            }
        }

        static Dictionary<string, Dictionary<string, Type>> IndexTypes(
            Dictionary<string, Assembly> platformAssemblies)
        {
            Dictionary<string, Dictionary<string, Type>> platformTypesIndex =
                new Dictionary<string, Dictionary<string, Type>>();

            foreach (KeyValuePair<string, Assembly> platformAssembly in platformAssemblies)
            {
                string platform = platformAssembly.Key;

                foreach (Type type in platformAssembly.Value.Types)
                {
                    if (type.Name.StartsWith("I") && type.Name.EndsWith("Invoker"))
                    {
                        // Ignore the auto-generated MonoTouch interface Invoker classes.
                        continue;
                    }

                    IndexType(platform, type, platformTypesIndex);
                }
            }

            return platformTypesIndex;
        }

        static void IndexType(
            string platform,
            Type type,
            Dictionary<string, Dictionary<string, Type>> platformTypesIndex)
        {
            if (type.Namespace != null && type.Namespace.StartsWith("Java."))
            {
                return;
            }

            Dictionary<string, Type> typesMap;
            if (!platformTypesIndex.TryGetValue(type.FullName, out typesMap))
            {
                typesMap = new Dictionary<string, Type>();
                platformTypesIndex.Add(type.FullName, typesMap);
            }

            typesMap.Add(platform, type);

            foreach (Member member in type.Members)
            {
                Type nestedType = member as Type;
                if (nestedType != null)
                {
                    IndexType(platform, nestedType, platformTypesIndex);
                }
            }
        }

        static Dictionary<string, Dictionary<string, List<Member>>> IndexMembers(
            Dictionary<string, Dictionary<string, Type>> platformTypes)
        {
            Dictionary<string, Dictionary<string, List<Member>>> platformMembersIndex =
                new Dictionary<string, Dictionary<string, List<Member>>>();

            foreach (KeyValuePair<string, Dictionary<string, Type>> typesMap in platformTypes)
            {
                string typeFullName = typesMap.Key;

                foreach (KeyValuePair<string, Type> platformType in typesMap.Value)
                {
                    string platform = platformType.Key;
                    Type type = platformType.Value;

                    foreach (Member member in type.Members)
                    {
                        string memberFullName = (member is Type ?
                            ((Type)member).FullName : type.FullName + "." + member.Name);
                        Dictionary<string, List<Member>> membersMap;
                        if (!platformMembersIndex.TryGetValue(memberFullName, out membersMap))
                        {
                            membersMap = new Dictionary<string, List<Member>>();
                            platformMembersIndex.Add(memberFullName, membersMap);
                        }

                        List<Member> overloadList;
                        if (!membersMap.TryGetValue(platform, out overloadList))
                        {
                            overloadList = new List<Member>();
                            membersMap.Add(platform, overloadList);
                        }

                        overloadList.Add(member);
                    }
                }
            }

            return platformMembersIndex;
        }

        void ValidateAssemblies()
        {
            this.validationWarnings = new List<string>();
            this.validationErrors = new List<string>();

            Assembly identityAssembly = null;
            foreach (KeyValuePair<string, Assembly> platformAssembly in this.platformAssemblies)
            {
                if (identityAssembly == null)
                {
                    identityAssembly = platformAssembly.Value;
                }
                else if (!AssemblyNamesEqual(platformAssembly.Value, identityAssembly))
                {
                    throw new InvalidOperationException("Assembly names do not match: " +
                        String.Join(", ", this.platformAssemblies.Select(pa => $"{{{pa.Key}: {pa.Value}}}")));
                }
            }

            foreach (KeyValuePair<string, Dictionary<string, Type>> typeMap in this.platformTypesIndex)
            {
                ValidateTypes(typeMap.Key, typeMap.Value);
            }
        }

        static bool AssemblyNamesEqual(Assembly assembly1, Assembly assebmly2)
        {
            return assembly1.Name == assebmly2.Name &&
                assembly1.Version == assebmly2.Version;
        }

        void ValidateTypes(string typeFullName, Dictionary<string, Type> platformTypes)
        {
            System.Reflection.TypeAttributes? typeAttributes = null;
            string typeType = null;
            foreach (string platform in this.platformAssemblies.Keys)
            {
                Type platformType;
                if (!platformTypes.TryGetValue(platform, out platformType))
                {
                    this.validationWarnings.Add("Type " + typeFullName +
                        " is not present on platform: " + platform);
                    continue;
                }

                if (typeAttributes == null)
                {
                    typeAttributes = platformType.Attributes;
                }
                else if (platformType.Attributes != typeAttributes.Value)
                {
                    this.validationErrors.Add(
                        $"Type attributes do not match for type ${typeFullName}: " +
                        String.Join(", ", platformTypes.Select(
                            pa => $"{{{pa.Key}: {pa.Value.Attributes}}}")));
                }

                if (typeType == null)
                {
                    typeType = platformType.GetType().Name;
                }
                else if (platformType.GetType().Name != typeType)
                {
                    this.validationErrors.Add(
                        $"Type {typeFullName} is an inconsistent type of type: " +
                        String.Join(", ", platformTypes.Select(
                            pt => $"{{{pt.Key}: {pt.Value.GetType().Name}}}")));
                }
            }

            // TODO: Validate other type equivalency:
            //     isAbstract
            //     generic arguments
            //     base class
            //     interfaces

            foreach (KeyValuePair<string, Dictionary<string, List<Member>>> memberMap
                in this.platformMembersIndex)
            {
                if (memberMap.Key.StartsWith(typeFullName + "."))
                {
                    ValidateMembers(memberMap.Key, memberMap.Value);
                }
            }
        }

        void ValidateMembers(string memberFullName, Dictionary<string, List<Member>> platformMembers)
        {
            foreach (string platform in this.platformAssemblies.Keys)
            {
                List<Member> platformMemberList;
                if (!platformMembers.TryGetValue(platform, out platformMemberList))
                {
                    this.validationWarnings.Add("Member " + memberFullName +
                        " is not present on platform: " + platform);
                    continue;
                }
            }

            this.ValidateMemberPlatformItemsMatch(
                memberFullName,
                this.GetMemberTypes(platformMembers),
                "member types");
            this.ValidateMemberPlatformItemsMatch(
                memberFullName,
                this.GetMemberReturnTypes(platformMembers),
                "return types");
            this.ValidateMemberPlatformItemsMatch(
                memberFullName,
                this.GetMemberParameterTypes(platformMembers),
                "parameter types");

            // Custom attributes are allowed to be inconsistent.
            /*
            this.ValidateMemberPlatformItemsMatch(
                memberFullName,
                this.GetMemberCustomAttributes(platformMembers),
                "custom attributes");
            */

            // TODO: Validate other member equivalency:
            //     enum values
            //     generic arguments
        }

        Dictionary<string, string> GetMemberTypes(Dictionary<string, List<Member>> platformMembers)
        {
            return platformMembers
                .Select(pm => new KeyValuePair<string, string>(
                    pm.Key,
                    String.Join(", ", pm.Value.Select(m => m.GetType().Name).Distinct())))
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        Dictionary<string, string> GetMemberReturnTypes(Dictionary<string, List<Member>> platformMembers)
        {
            Func<Member, string> getReturnType = m =>
                m is Field ? ((Field)m).FieldType :
                m is Property ? ((Property)m).PropertyType :
                m is Method ? ((Method)m).ReturnType :
                String.Empty;
            return platformMembers
                .Select(pm => new KeyValuePair<string, string>(
                    pm.Key,
                    String.Join(", ", pm.Value.Select(m => getReturnType(m)).Distinct())))
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        Dictionary<string, string> GetMemberParameterTypes(Dictionary<string, List<Member>> platformMembers)
        {
            Func<Member, string> getParameterTypes = m =>
                m is Constructor ? "(" + String.Join(", ",
                    ((Constructor)m).Parameters.Select(p => p.ParameterType)) + ")" :
                m is Method ? "(" + String.Join(", ",
                    ((Method)m).Parameters.Select(p => p.ParameterType)) + ")" :
                String.Empty;
            return platformMembers
                .Select(pm => new KeyValuePair<string, string>(
                    pm.Key,
                    String.Join(", ", pm.Value.Select(m => getParameterTypes(m)))))
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        Dictionary<string, string> GetMemberCustomAttributes(Dictionary<string, List<Member>> platformMembers)
        {
            Func<Member, string> getCustomAttributes = m => m.CustomAttributes != null ?
                String.Join(", ", m.CustomAttributes.Select(a => a.ToString())) : String.Empty;
            return platformMembers
                .Select(pm => new KeyValuePair<string, string>(
                    pm.Key,
                    String.Join(", ", pm.Value.Select(m => getCustomAttributes(m)))))
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        void ValidateMemberPlatformItemsMatch(
            string memberFullName, Dictionary<string, string> platformItems, string itemDescription)
        {
            if (platformItems.Values.Distinct().Count() != 1)
            {
                this.validationErrors.Add(
                    $"The member {memberFullName} does not have consistent {itemDescription} across platforms: " +
                    String.Join(", ", platformItems.Select(pi => $"{{{pi.Key}: {pi.Value}}}")));
            }
        }

        Assembly MergeAssemblies(ICollection<Assembly> assemblies)
        {
            Assembly mergedAssembly = new Assembly
            {
                Name = assemblies.First().Name,
                Version = assemblies.First().Version,
                Types = new List<Type>(),
            };

            foreach (KeyValuePair<string, Dictionary<string, Type>> typeMap in this.platformTypesIndex)
            {
                string typeFullName = typeMap.Key;
                if (typeFullName.IndexOf('+') < 0)
                {
                    Type mergedType = MergeTypes(typeMap.Value.Values);
                    mergedAssembly.Types.Add(mergedType);
                }
            }

            return mergedAssembly;
        }

        Type MergeTypes(ICollection<Type> platformTypes)
        {
            Type mergedType;
            Type firstType = platformTypes.First();
            if (firstType is Class)
            {
                mergedType = new Class
                {
                    IsStatic = ((Class)firstType).IsStatic,
                    IsSealed = ((Class)firstType).IsSealed,
                    IsAbstract = ((Class)firstType).IsAbstract,
                    ExtendsType = ((Class)firstType).ExtendsType,
                };
                ((Class)mergedType).ImplementsTypes = (((Class)firstType).ImplementsTypes != null ?
                    new List<string>(((Class)firstType).ImplementsTypes) : null);
            }
            else if (firstType is Interface)
            {
                mergedType = new Interface();
                ((Interface)mergedType).ExtendsTypes = (((Interface)firstType).ExtendsTypes != null ?
                    new List<string>(((Interface)firstType).ExtendsTypes) : null);
            }
            else if (firstType is Struct)
            {
                mergedType = new Struct();
                ((Struct)mergedType).ImplementsTypes = (((Struct)firstType).ImplementsTypes != null ?
                    new List<string>(((Struct)firstType).ImplementsTypes) : null);
            }
            else if (firstType is Enum)
            {
                mergedType = new Enum
                {
                    UnderlyingType = ((Enum)firstType).UnderlyingType,
                };
            }
            else
            {
                throw new NotSupportedException("Unsupported type of type: " + firstType.GetType().Name);
            }

            mergedType.Name = firstType.Name;
            mergedType.Namespace = firstType.Namespace;
            mergedType.Attributes = firstType.Attributes;
            mergedType.IsGenericType = firstType.IsGenericType;
            mergedType.GenericArgumentNames = (firstType.GenericArgumentNames != null ?
                new List<string>(firstType.GenericArgumentNames) : null);

            string typeFullName = mergedType.FullName;
            mergedType.Members = new List<Member>();
            foreach (KeyValuePair<string, Dictionary<string, List<Member>>> memberMap
                in this.platformMembersIndex)
            {
                if (memberMap.Key.StartsWith(typeFullName + "."))
                {
                    List<Member> mergedMembers = this.MergeMembers(memberMap.Value.Values);
                    mergedType.Members.AddRange(mergedMembers);
                }
                else if (memberMap.Key.StartsWith(typeFullName + "+") &&
                    memberMap.Key.LastIndexOf('.') < memberMap.Key.LastIndexOf('+'))
                {
                    Type nestedType = this.MergeTypes(
                        memberMap.Value.Values.Select(m => (Type)m.Single()).ToArray());
                    mergedType.Members.Add(nestedType);
                }
            }

            return mergedType;
        }

        List<Member> MergeMembers(ICollection<List<Member>> platformMembers)
        {
            // TODO: Merge member overloads
            return platformMembers.OrderByDescending(m => m.Count).First();
        }
    }
}
