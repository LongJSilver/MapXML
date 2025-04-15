﻿using MapXML.Attributes;
using MapXML.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace MapXML.Behaviors
{
    internal abstract partial class XMLMemberBehavior
    {
        //-----------------------------------------------------------------//
        public string NodeName => _attribute?.NodeName ?? this.Member.Name;
        public abstract Type TypeToCreate { get; }
        public int SerializationOrder { get; set; } = int.MaxValue;
        public XmlSourceType SourceType { get; }
        internal DeserializationPolicy Policy { get; }
        private ConvertFromString? _conversionFromString_nullable;
        private ConvertToString? _conversionToString_nullable;

        internal ConvertFromString ConversionFromString => _conversionFromString_nullable
                   ?? throw new ArgumentException($"Unable to find a conversion from member type '{TypeToCreate}' to String");
        internal ConvertToString ConversionToString => _conversionToString_nullable
                   ?? throw new ArgumentException($"Unable to find a conversion from  string to member type '{TypeToCreate}'");


        protected readonly AbstractXMLMemberAttribute? _attribute;

        public readonly MemberInfo Member;
        public bool CanSerialize => _canSerialize && (CanSerializeAsAttribute || CanSerializeAsChild || CanSerializeAsTextContent);
        public bool CanDeserialize => _canDeserialize;
        private readonly bool _canSerialize;
        private readonly bool _canDeserialize;
        internal bool CanSerializeAsAttribute => InternalCanSerializeAsAttribute && SourceType.HasFlag(XmlSourceType.Attribute);
        internal bool CanSerializeAsTextContent => InternalCanSerializeAsTextContent && SourceType.HasFlag(XmlSourceType.TextContent);
        internal bool CanSerializeAsChild => InternalCanSerializeAsChild && SourceType.HasFlag(XmlSourceType.Child);

        internal abstract string GetAttributeToSerialize(IXMLInternalContext context, string NodeName, string AttributeName);
        internal abstract IEnumerable<object> GetChildrenToSerialize(IXMLInternalContext context, string NodeName);
        protected abstract bool InternalCanSerializeAsAttribute { get; }
        protected abstract bool InternalCanSerializeAsChild { get; }
        protected abstract bool InternalCanSerializeAsTextContent { get; }
        internal abstract string ObtainAttributeValue(IXMLInternalContext context);
        internal abstract object ObtainValue(IXMLInternalContext context);
        internal abstract void InjectValue(IXMLInternalContext context, object value);

        //-----------------------------------------------------------------//

#pragma warning disable CS8618 
        protected XMLMemberBehavior(MemberInfo member, XmlSourceType source, DeserializationPolicy policy)
        {
            this.Member = member;
            this.SourceType = source;
            this.Policy = policy;
            _canDeserialize = _canSerialize = true;
        }

        protected XMLMemberBehavior(MemberInfo member, AbstractXMLMemberAttribute? attribute)
            : this(member, attribute?.SourceType ?? XmlSourceType.Attribute, attribute?.Policy ?? DeserializationPolicy.Create)
        {
            this._attribute = attribute;
            if (attribute != null)
            {
                _canDeserialize = attribute.CanDeserialize;
                _canSerialize = attribute.CanSerialize;
            }

            this.SerializationOrder = attribute?.SerializationOrder ?? int.MaxValue;
            //-------//
        }

        public void Init()
        {
            CreateConversionFromString();
        }

        internal virtual void OnFinalized(object finalizedInstance, IXMLInternalContext context)
        {

        }
        protected virtual void CreateConversionFromString()
        {
            _conversionFromString_nullable = XMLNodeBehaviorProfile.CreateStandardConversionFromString(TypeToCreate);

            _conversionToString_nullable = XMLNodeBehaviorProfile.CreateStandardConversionToString(TypeToCreate);
        }

        protected object Convert(IXMLInternalContext context, string value)
        {
            if (TypeToCreate.IsEnum) return Enum.Parse(TypeToCreate, value);
            if (TypeToCreate.Equals(typeof(string))) return value;
            if (context.Convert(value, TypeToCreate, out object? result))
            {
                return result;
            }
            else
                return ConversionFromString(value, context.FormatProvider);
        }
        protected string ConvertBack(IXMLInternalContext context, object value)
        {
            if (value == null) return "";
            if (TypeToCreate.Equals(typeof(string))) return (string)value;
            if (context.ConvertBack(value, TypeToCreate, out string? result))
            {
                return result;
            }
            else
                return ConversionToString(value, context.FormatProvider);
        }

        internal virtual void ProcessAttribute(IXMLInternalContext context, string NodeName, string AttributeName, string AttributeValue)
        {
            object? result;
            if (Policy == DeserializationPolicy.Create)
            {
                result = Convert(context, AttributeValue);
            }
            else
            {
                bool ok = context.Lookup_FromAttribute(NodeName, AttributeName, AttributeValue, this.TypeToCreate, out result);
                if (!ok) throw new InvalidOperationException($"No lookup available for element <{NodeName}>");

            }
            InjectValue(context, result!);
        }
        internal virtual void ProcessTextContent(IXMLInternalContext context, string TextContentValue)
        {
            if (Policy == DeserializationPolicy.Create)
            {
                var conv = Convert(context, TextContentValue);
                InjectValue(context, conv);
            }
            else throw new InvalidOperationException();
        }

        internal virtual void ProcessValue(IXMLInternalContext context, string NodeName, string AttributeName, object value)
        {
            InjectValue(context, value);
        }

        internal virtual void ProcessChildNodeResult(IXMLInternalContext context, string ChildNodeName,
            IReadOnlyDictionary<string, string> childAttributes, object childNodeResult)
        {
            InjectValue(context, childNodeResult);
        }

        internal static XMLMemberBehavior Generate(MemberInfo member, AbstractXMLMemberAttribute? attribute)
        {
            if (member is MethodInfo method)
            {
                return new _forMethod(method, attribute);
            }
            else
            {
                Type t = member.FieldOrPropertyType();
                if (attribute is XmlMapAttribute xma && IsDictionaryType(t, out var KeyType, out var ValueType))
                {
                    return new _forDictionaryMember(member, xma, KeyType, ValueType);
                }
                else if (t.IsArray)
                {
                    return new _forArray(member, attribute);
                }
                else if (IsCollectionType(t))
                {
                    return new _forCollectionMember(member, attribute);
                }
                else if (IsEnumerableType(t))
                {
                    return new _forEnumerableMember(member, attribute);
                }
                else
                    return new _forMember(member, attribute);
            }
        }

        private static bool IsDictionaryType(Type t, [NotNullWhen(true)][MaybeNullWhen(false)] out Type? KeyType, [NotNullWhen(true)][MaybeNullWhen(false)] out Type? ValueType)
        {
            if (!t.IsGenericType)
            {
                ValueType = KeyType = null;

                return false;
            }
            if (t.GetGenericTypeDefinition().Equals(typeof(IDictionary<,>)))
            {
                KeyType = t.GetGenericArguments()[0];
                ValueType = t.GetGenericArguments()[1];
                return true;
            }
            Type DictionaryType = t.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));
            if (DictionaryType != null)
            {
                KeyType = DictionaryType.GetGenericArguments()[0];
                ValueType = DictionaryType.GetGenericArguments()[1];
                return true;
            }
            ValueType = KeyType = null;
            return false;
        }

        private static bool IsCollectionType(Type t)
        {
            if (!t.IsGenericType) return false;
            if (t.IsArray) return false;
            if (t.GetGenericTypeDefinition().Equals(typeof(ICollection<>))) return true;
            if (t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>))) return true;
            return false;
        }
        private static bool IsEnumerableType(Type t)
        {
            if (!t.IsGenericType) return false;
            if (t.IsArray) return true;
            if (t.GetGenericTypeDefinition().Equals(typeof(IEnumerable<>))) return true;
            if (t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))) return true;
            return false;
        }

        internal abstract string GetTextContentToSerialize(IXMLInternalContext context);

    }
}
