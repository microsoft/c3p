// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

namespace Microsoft.C3P
{
    [DebuggerDisplay("{ToString()}")]
    public class Assembly
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlIgnore]
        public Version Version { get; set; }

        [XmlAttribute("version")]
        public string VersionString
        {
            get
            {
                return this.Version.ToString();
            }
            set
            {
                this.Version = Version.Parse(value);
            }
        }

        [XmlAttribute("codebase")]
        public string CodeBase { get; set; }

        [XmlElement("interface", typeof(Interface))]
        [XmlElement("class", typeof(Class))]
        [XmlElement("struct", typeof(Struct))]
        [XmlElement("enum", typeof(Enum))]
        public List<Type> Types { get; set; }

        public override string ToString()
        {
            return $"{this.Name}, Version={this.Version}, CodeBase={this.CodeBase}";
        }
    }

    [DebuggerDisplay("{ToString()}")]
    public abstract class Member
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("visibility")]
        public string Visibility { get; set; }

        [XmlElement("attribute")]
        public List<Attribute> CustomAttributes { get; set; }
    }

    public abstract class Type : Member
    {
        [XmlAttribute("namespace")]
        public string Namespace { get; set; }

        [XmlIgnore]
        public string FullName =>
            ((!String.IsNullOrEmpty(this.Namespace) ? this.Namespace + "." : "") + this.Name);

        [XmlIgnore]
        public bool IsNested => this.Name?.IndexOf('+') > 0;

        [XmlAttribute("attributes")]
        public TypeAttributes Attributes { get; set; }

        [XmlAttribute("generic")]
        public bool IsGenericType { get; set; }

        [XmlElement("generic-arg")]
        public List<string> GenericArgumentNames { get; set; }

        [XmlElement("field", typeof(Field))]
        [XmlElement("value", typeof(EnumValue))]
        [XmlElement("constructor", typeof(Constructor))]
        [XmlElement("property", typeof(Property))]
        [XmlElement("method", typeof(Method))]
        [XmlElement("event", typeof(Event))]
        [XmlElement("interface", typeof(Interface))]
        [XmlElement("class", typeof(Class))]
        [XmlElement("struct", typeof(Struct))]
        [XmlElement("enum", typeof(Enum))]
        public List<Member> Members { get; set; }
    }

    [DebuggerDisplay("{ToString()}")]
    public class Interface : Type
    {
        [XmlElement("extends")]
        public List<string> ExtendsTypes { get; set; }

        [XmlIgnore]
        public IEnumerable<Property> Properties { get { return this.Members.OfType<Property>(); } }

        [XmlIgnore]
        public IEnumerable<Method> Methods { get { return this.Members.OfType<Method>(); } }

        public override string ToString()
        {
            return $"interface {this.FullName}";
        }
    }

    [DebuggerDisplay("{ToString()}")]
    public class Class : Type
    {
        [XmlAttribute("static")]
        public bool IsStatic { get; set; }

        [XmlAttribute("sealed")]
        public bool IsSealed { get; set; }

        [XmlAttribute("abstract")]
        public bool IsAbstract { get; set; }

        [XmlElement("extends")]
        public string ExtendsType { get; set; }

        [XmlElement("implements")]
        public List<string> ImplementsTypes { get; set; }

        [XmlIgnore]
        public IEnumerable<Constructor> Constructor { get { return this.Members.OfType<Constructor>(); } }

        [XmlIgnore]
        public IEnumerable<Property> Properties { get { return this.Members.OfType<Property>(); } }

        [XmlIgnore]
        public IEnumerable<Method> Methods { get { return this.Members.OfType<Method>(); } }

        public override string ToString()
        {
            return $"class {this.FullName}";
        }
    }

    [DebuggerDisplay("{ToString()}")]
    public class Struct : Type
    {
        [XmlElement("implements")]
        public List<string> ImplementsTypes { get; set; }

        public override string ToString()
        {
            return $"struct {this.FullName}";
        }
    }

    [DebuggerDisplay("{ToString()}")]
    public class Enum : Type
    {
        [XmlAttribute("underlying-type")]
        public string UnderlyingType { get; set; }

        public override string ToString()
        {
            return $"enum {this.FullName}";
        }
    }

    [DebuggerDisplay("{ToString()}")]
    public class Delegate : Type
    {
        [XmlAttribute("return")]
        public string ReturnType { get; set; }

        [XmlElement("parameter")]
        public List<Parameter> Parameters { get; set; }

        public override string ToString()
        {
            string parameters = String.Empty;
            if (this.Parameters != null)
            {
                parameters = String.Join(", ", this.Parameters.Select(p => p.ToString()));
            }

            return $"delegate {this.ReturnType} {this.Name}({parameters})";
        }
    }

    [DebuggerDisplay("{ToString()}")]
    public class Field : Member
    {
        [XmlAttribute("static")]
        public bool IsStatic { get; set; }

        [XmlAttribute("type")]
        public string FieldType { get; set; }

        [XmlAttribute("attributes")]
        public FieldAttributes Attributes { get; set; }

        [XmlAttribute("value")]
        public string Value { get; set; }

        public override string ToString()
        {
            return $"{(this.IsStatic ? "static " : "")}{this.FieldType} {this.Name}";
        }
    }

    [DebuggerDisplay("{ToString()}")]
    public class EnumValue : Member
    {
        [XmlAttribute("value")]
        public long Value { get; set; }

        public override string ToString()
        {
            return $"{this.Name} = {this.Value}";
        }
    }

    [DebuggerDisplay("{ToString()}")]
    public class Property : Member
    {
        [XmlAttribute("static")]
        public bool IsStatic { get; set; }

        [XmlAttribute("type")]
        public string PropertyType { get; set; }

        [XmlElement("parameter")]
        public List<Parameter> IndexParameters { get; set; }

        [XmlElement("getter")]
        public Method GetMethod { get; set; }

        [XmlElement("setter")]
        public Method SetMethod { get; set; }

        [XmlAttribute("attributes")]
        public PropertyAttributes Attributes { get; set; }

        public override string ToString()
        {
            string indexParameters = String.Empty;
            if (this.IndexParameters != null && this.IndexParameters.Count > 0)
            {
                indexParameters = '[' + String.Join(", ", this.IndexParameters.Select(p => p.ToString())) + ']';
            }

            return $"{(this.IsStatic ? "static " : "")}{this.PropertyType} {this.Name}{indexParameters} " +
                $"{{ {(this.GetMethod != null ? "get; " : "")}{(this.SetMethod != null ? "set; " : "")}}}";
        }
    }

    [DebuggerDisplay("{ToString()}")]
    public class Event : Member
    {
        [XmlAttribute("static")]
        public bool IsStatic { get; set; }

        [XmlAttribute("handler-type")]
        public string EventHandlerType { get; set; }

        [XmlAttribute("attributes")]
        public EventAttributes Attributes { get; set; }

        public override string ToString()
        {
            return $"{(this.IsStatic ? "static " : "")}event {this.EventHandlerType} {this.Name}";
        }
    }

    [DebuggerDisplay("{ToString()}")]
    public class Constructor : Member
    {
        [XmlAttribute("attributes")]
        public MethodAttributes Attributes { get; set; }

        [XmlAttribute("calling-convention")]
        public CallingConventions CallingConvention { get; set; }

        [XmlElement("parameter")]
        public List<Parameter> Parameters { get; set; }

        public override string ToString()
        {
            string parameters = String.Empty;
            if (this.Parameters != null)
            {
                parameters = String.Join(", ", this.Parameters.Select(p => p.ToString()));
            }

            return $"{this.Name}({parameters})";
        }
    }

    [DebuggerDisplay("{ToString()}")]
    public class Method : Member
    {
        [XmlAttribute("static")]
        public bool IsStatic { get; set; }

        [XmlAttribute("abstract")]
        public bool IsAbstract { get; set; }

        [XmlAttribute("return")]
        public string ReturnType { get; set; }

        [XmlAttribute("attributes")]
        public MethodAttributes Attributes { get; set; }

        [XmlAttribute("calling-convention")]
        public CallingConventions CallingConvention { get; set; }

        [XmlElement("parameter")]
        public List<Parameter> Parameters { get; set; }

        public override string ToString()
        {
            string parameters = String.Empty;
            if (this.Parameters != null)
            {
                parameters = String.Join(", ", this.Parameters.Select(p => p.ToString()));
            }

            return $"{(this.IsStatic ? "static " : "")}{this.ReturnType} {this.Name}({parameters})";
        }
    }

    [DebuggerDisplay("{ToString()}")]
    public class Parameter
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("type")]
        public string ParameterType { get; set; }

        [XmlAttribute("has-default")]
        public bool HasDefaultValue { get; set; }

        [XmlIgnore]
        public object DefaultValue
        {
            get
            {
                string value = this.DefaultValueString;
                if (value == null || String.IsNullOrEmpty(this.ParameterType))
                {
                    return null;
                }

                System.Type parameterType = System.Type.GetType(this.ParameterType, false);
                if (parameterType == null)
                {
                    return null;
                }

                return Convert.ChangeType(value, parameterType);
            }
            set
            {
                this.DefaultValueString = value?.ToString();
            }
        }

        [XmlAttribute("default")]
        public string DefaultValueString { get; set; }

        [XmlAttribute("attributes")]
        public ParameterAttributes Attributes { get; set; }

        public override string ToString()
        {
            return $"{this.ParameterType} {this.Name}" +
                (this.HasDefaultValue ? " = " + this.DefaultValue : String.Empty);
        }
    }

    [DebuggerDisplay("{ToString()}")]
    public class Attribute
    {
        [XmlAttribute("type")]
        public string AttributeType { get; set; }

        [XmlElement("argument")]
        public List<AttributeArgument> Arguments { get; set; }

        public override string ToString()
        {
            return "[" + this.AttributeType + (this.Arguments != null ?
                "(" + String.Join(", ", this.Arguments.Select(a => a.ToString())) + ")" : String.Empty) + "]";
        }
    }

    [DebuggerDisplay("{ToString()}")]
    public class AttributeArgument
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("type")]
        public string ArgumentType { get; set; }

        [XmlAttribute("value")]
        public string Value { get; set; }

        public override string ToString()
        {
            return (this.Name != null ? this.Name + " = " : String.Empty) +
                "\"" + (this.Value ?? String.Empty) + "\"";
        }
    }

    /// <summary>
    /// Represents a Java package (only used when modeling a Java API).
    /// </summary>
    [DebuggerDisplay("{ToString()}")]
    public class Package
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlElement("interface")]
        public List<Interface> Interfaces { get; set; }

        [XmlElement("class")]
        public List<Class> Classes { get; set; }

        public override string ToString()
        {
            return "package " + this.Name;
        }
    }
}
