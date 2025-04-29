using MapXML.Attributes;
using MapXML.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using static MapXML.Attributes.XMLMapAttribute;

namespace MapXML.Behaviors
{
    internal sealed class BehaviorForDictionary : XMLMemberBehavior
    {
        private static readonly string ExceptionMessage_NoDirectSerialization
            = $"A {nameof(IDictionary)} member cannot be serialized directly, it should be flagged with '{nameof(XMLChildAttribute)}' and serialized as a set of children.";

        //-----------------//
        //results cache
        private static readonly Dictionary<string, (MemberInfo member, KeyFinderType type)> __KeyFinderCache
                          = new Dictionary<string, (MemberInfo member, KeyFinderType type)>();

        private enum KeyFinderType
        {
            FieldOrProperty,
            ZeroParameterFunction,
            OneParameterFunction,
            OneParameterDelegate
        }

        private static object FindKey(Type KeyType, object target, string KeyFinderName, object ValueToInsert)
        {
            /*
             * Proprietà/Campo di tipo stringa che contenga la chiave
             * Proprietà/Campo di tipo delegato che accetta come input il valore e restituisce la chiave
             * Funzione con zero parametri che restituisca la chiave
             * Funzione che accetta come parametro il valore da inserire e restituisce la chiave
             * */
            var targetType = target.GetType();
            string CacheKey = $"{targetType.FullName}###{KeyFinderName}";
            if (!__KeyFinderCache.TryGetValue(CacheKey, out var resultMember))
            {
                bool found = false;
                foreach (var member in targetType.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static))
                {
                    if (!member.Name.Equals(KeyFinderName, StringComparison.Ordinal))
                        continue;
                    if (member.IsFieldOrProperty())
                    {
                        var memberType = member.FieldOrPropertyType();
                        if (KeyType.IsAssignableFrom(memberType))
                        {
                            resultMember = (member, KeyFinderType.FieldOrProperty);
                            found = true;
                            break;
                        }
                    }
                    else if (member is MethodInfo method)
                    {
                        ParameterInfo[] par = method.GetParameters();
                        if (KeyType.IsAssignableFrom(method.ReturnType))
                        {
                            switch (par.Length)
                            {
                                case 0:
                                    resultMember = (member, KeyFinderType.ZeroParameterFunction);
                                    found = true;
                                    break;
                                case 1:
                                    if (ValueToInsert.GetType().IsAssignableFrom(par[0].ParameterType))
                                    {
                                        resultMember = (member, KeyFinderType.OneParameterFunction);
                                        found = true;
                                    }
                                    break;
                                default:
                                    throw new NotSupportedException("Multi parameter functions are not supported");
                            }
                        }
                    }
                    if (found) break;
                }
                if (!found)
                    throw new ArgumentException($"Cannot find Key Source named <{KeyFinderName}> in type <{targetType}>.");

                __KeyFinderCache[CacheKey] = resultMember;
            }

            //---------------//
            switch (resultMember.type)
            {
                case KeyFinderType.FieldOrProperty:
                    return resultMember.member.GetValue(target);
                case KeyFinderType.ZeroParameterFunction:
                    return (resultMember.member as MethodInfo)!.Invoke(target, EMPTY);
                case KeyFinderType.OneParameterFunction:
                    return (resultMember.member as MethodInfo)!.Invoke(target, new object[] { ValueToInsert });
                case KeyFinderType.OneParameterDelegate:
                default:
                    throw new NotSupportedException();
            }
        }

        private static object[] EMPTY = Array.Empty<object>();
        //-----------------//

        private XMLMapAttribute XMLMapAttribute => (this._attribute as XMLMapAttribute)!;
        private bool IsPrimitiveDictionary => !string.IsNullOrEmpty(XMLMapAttribute.ValueSourceName);
        private readonly Type _valueType;
        private readonly Type _keyType;
        public override Type TypeToCreate => _valueType;

        protected override bool InternalCanSerializeAsAttribute => false;
        protected override bool InternalCanSerializeAsChild => true;
        protected override bool InternalCanSerializeAsTextContent => false;

        private readonly ConvertFromString _KeyConversion;
        public BehaviorForDictionary(MemberInfo member, XMLMapAttribute attribute, Type KeyType, Type ValueType)
          : base(member, attribute)
        {
            var t = member.FieldOrPropertyType();
            _valueType = ValueType;
            _keyType = KeyType;

            _KeyConversion = XMLNodeBehaviorProfile.CreateStandardConversionFromString(_keyType)
               ?? throw new ArgumentException($"Unable to find a conversion from dictionary key type '{KeyType}' to String");
        }

        internal override void InjectValue(IXMLInternalContext context, object value)
        {
            throw new NotSupportedException();
        }
        /// <summary>
        /// This method is responsible for processing an XML attribute, extracting its value, determining the corresponding key for the dictionary,
        /// and injecting the key-value pair into the dictionary.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="NodeName"></param>
        /// <param name="AttributeName"></param>
        /// <param name="AttributeValue"></param>
        internal override void ProcessAttribute(IXMLInternalContext context, string NodeName, string AttributeName, string AttributeValue)
        {
            object? Key = null;
            object value = Convert(context, AttributeValue);

            switch (XMLMapAttribute.KeySourceType)
            {
                case XMLMapAttribute.KeySourceTypes.ObjectMember:
                    Key = FindKey(_keyType, value, XMLMapAttribute.KeySourceName, value);
                    break;
                case XMLMapAttribute.KeySourceTypes.ParentMember:
                    Key = FindKey(_keyType, context.GetCurrentInstance(), XMLMapAttribute.KeySourceName, value);
                    break;
                case XMLMapAttribute.KeySourceTypes.NodeAttribute:
                    Key = AttributeName;
                    break;
                default:
                    throw new NotImplementedException($"{nameof(KeySourceTypes)} value '{XMLMapAttribute.KeySourceType}' is not recognized");
            }
            Inject(context, Key, value);
        }

        private void Inject(IXMLInternalContext context, object Key, object Value)
        {
            if (Key is String && _keyType != typeof(string))
            {
                Key = _KeyConversion((string)Key, context.FormatProvider);
            }

            object dict = this.Member.GetValue(context.GetCurrentInstance());
            if (dict == null)
                throw new InvalidOperationException("Target dictionary is null");
            var _addMethod = dict.GetType().GetMethod("Add");

            _addMethod.Invoke(dict, new object[] { Key, Value });
        }

        internal override void ProcessChildNodeResult(IXMLInternalContext context, string ChildNodeName,
        IReadOnlyDictionary<string, string> childAttributes, object childNodeResult)
        {
            object Key;
            object value = childNodeResult;

            switch (XMLMapAttribute.KeySourceType)
            {
                case XMLMapAttribute.KeySourceTypes.ObjectMember:
                    Key = FindKey(_keyType, value, XMLMapAttribute.KeySourceName, value);
                    break;
                case XMLMapAttribute.KeySourceTypes.ParentMember:
                    Key = FindKey(_keyType, context.GetCurrentInstance(), XMLMapAttribute.KeySourceName, value);
                    break;
                case XMLMapAttribute.KeySourceTypes.NodeAttribute:
                    Key = childAttributes[XMLMapAttribute.KeySourceName];
                    break;
                default:
                    throw new ArgumentNullException($"Key source '{XMLMapAttribute.KeySourceType}' is unknown.");
            }
            Inject(context, Key, value);
        }

        internal override string? GetAttributeToSerialize(IXMLInternalContext context, string NodeName, string AttributeName)
        {
            throw new InvalidOperationException($"Attribute: {AttributeName} - {ExceptionMessage_NoDirectSerialization}");
        }

        internal override IEnumerable<object> GetChildrenToSerialize(IXMLInternalContext context, string NodeName)
        {
            IDictionary dict = (IDictionary)Member.GetValue(context.GetCurrentInstance());
            if (IsPrimitiveDictionary)
            {
                foreach (var key in dict.Keys)
                {
                    string sKey = (string)ConversionToString(key, context.FormatProvider);

                    yield return new PrimitiveDictionaryEntry(XMLMapAttribute.KeySourceName, sKey, XMLMapAttribute.ValueSourceName, ConvertBack(context, dict[key]));
                }
            }
            else
            {
                foreach (var item in dict.Values)
                {
                    yield return item;
                }
            }
        }

        internal override object ObtainValueForLookup(IXMLInternalContext context)
        {
            throw new InvalidOperationException(ExceptionMessage_NoDirectSerialization);
        }

        internal override string GetTextContentToSerialize(IXMLInternalContext context)
        {
            throw new InvalidOperationException(ExceptionMessage_NoDirectSerialization);
        }
    }
}
