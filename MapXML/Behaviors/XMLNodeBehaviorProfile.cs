using MapXML.Attributes;
using MapXML.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace MapXML.Behaviors
{
    internal partial class XMLNodeBehaviorProfile : IXMLInternalContext, IXMLState
    {
        object? IXMLState.GetParent(int i)
        {
            if (i == 0) return CurrentInstance;
            else return (Parent as IXMLState)?.GetParent(i - 1);
        }

        public XMLNodeBehaviorProfile? Parent;


        public bool AllowImplicitFields { get; }
        public readonly string NodeName;
        public readonly bool IsSerializing;
        public readonly Type? TargetType;

        public int Level { get; internal set; }
        public bool EncounteredTextContent { get; private set; }
        public bool IsCreation { get; private set; }
        public readonly IReadOnlyDictionary<string, string> Attributes;
        private int ChildrenCount = 0;
        public readonly IDictionary<string, object> _customData;
        private XmlStaticClassData? _staticClassData;
        private readonly IXMLSerializationHandler? _handler;
        internal CultureInfo? Format { get; set; }

        //--------------------------------------//

        public IDictionary<string, object> CustomData => _customData;

        XmlStaticClassData? IXMLInternalContext.StaticClassData => _staticClassData;

        public object? CurrentInstance { get; internal set; }

        IFormatProvider? IXMLInternalContext.FormatProvider => GetFormat();

        public IXMLSerializationHandler? Handler => GetHandler();
        //--------------------------------------//
        private IFormatProvider? GetFormat()
        {
            XMLNodeBehaviorProfile prof = this;
            while (prof.Format == null && prof.Parent != null)
                prof = prof.Parent;
            return prof?.Format;
        }
        private IXMLSerializationHandler? GetHandler()
        {
            XMLNodeBehaviorProfile prof = this;
            while (prof._handler == null && prof.Parent != null)
                prof = prof.Parent;
            return prof?._handler;
        }


        private XMLNodeBehaviorProfile(
            IXMLSerializationHandler? handler,
                 string Node, IReadOnlyDictionary<string, string>? attributes,
              object? currentObject, Type? targetType,
                 bool IsSerializing, bool allowImplicitFields)
        {
            _handler = handler;

            this.NodeName = Node;
            this.Attributes = attributes ?? new Dictionary<string, string>();

            CurrentInstance = currentObject;
            this.TargetType = targetType;

            this.IsSerializing = IsSerializing;
            this.AllowImplicitFields = allowImplicitFields;
            //------------------//
            _customData = new Dictionary<string, object>();
            if (CurrentInstance != null && !CurrentInstance.Equals(typeof(string)))
            {
                FindStaticData(this.AllowImplicitFields);
            }
        }

        internal static XMLNodeBehaviorProfile CreateTopNode(IXMLSerializationHandler? handler, string? NodeName, object? owner, bool isSerializing, bool allowImplicitFields)
        {
            return new XMLNodeBehaviorProfile(handler, NodeName ?? string.Empty, null, owner, owner?.GetType(), isSerializing, allowImplicitFields)
            {
                IsCreation = true
            };
        }

        internal static XMLNodeBehaviorProfile CreateSerializationNode(IXMLSerializationHandler? handler, bool allowImplicitFields,
            string name, Type targetType, object item, string? formatName = null)
        {
            var result = new XMLNodeBehaviorProfile(handler, name, null, item, targetType, true, allowImplicitFields);
            if (formatName != null)
                result.Format = CultureInfo.GetCultureInfo(formatName);
            return result;
        }

        internal static XMLNodeBehaviorProfile GetDummyForLookupAttributes(
            IXMLSerializationHandler? handler,
            bool allowImplicitFields,
            string targetNodeName, object item)
        {
            return new XMLNodeBehaviorProfile(handler, targetNodeName, null, item, item.GetType(), true, allowImplicitFields);
        }


        internal static XMLNodeBehaviorProfile CreateDeserializationNode(IXMLSerializationHandler? handler, bool creation,
            bool allowImplicitFields, string nodeName, Type targetType, Dictionary<string, string> attributes, object owner)
        {

            return new XMLNodeBehaviorProfile(handler, nodeName, attributes, owner, targetType, false, allowImplicitFields)
            {
                IsCreation = creation
            };
        }

        private static Dictionary<Type, XmlStaticClassData> __typeCache =
                   new Dictionary<Type, XmlStaticClassData>();

        public string? GetTextContentToSerialize()
        {
            if (CanSerializeAsTextContent)
            {
                ConvertBack(this.GetCurrentInstance(), TargetType!, out string? result);
                return result;
            }
            else
            {
                if (_staticClassData?._textContentBehavior_forSer != null)
                {
                    var beh = _staticClassData._textContentBehavior_forSer;
                    {
                        if (beh.Policy == DeserializationPolicy.Create)
                        {
                            return beh.GetTextContentToSerialize(this);
                        }
                        else
                        {
                            object ValueToLookup = beh.ObtainValue(this);
                            if (ValueToLookup != null && this.LookupAttributeFor(this.NodeName, null,
                                  beh.TypeToCreate, ValueToLookup, out string AttributeValue)
                                )
                                return AttributeValue;
                            else return null;
                        }
                    }
                }
                else return null;
            }
        }
        public List<(String nodeName, object child, Type targetType, DeserializationPolicy policy)> GetAllChildrenToSerialize()
        {
            List<(String nodeName, object child, Type targetType, DeserializationPolicy policy)> result = new List<(String nodeName, object child, Type targetType, DeserializationPolicy policy)>();
            if (IsNamedTextContentNode)
            {
                return result;
            }
            HashSet<XMLMemberBehavior> _serialized = new HashSet<XMLMemberBehavior>();

            if (_staticClassData != null)
            {
                foreach (var beh in _staticClassData._childBehaviors_forSer.Values.OrderBy(m => m.SerializationOrder))
                {
                    if (_serialized.Contains(beh) || beh.CanSerializeAsAttribute) continue;
                    _serialized.Add(beh);

                    string NodeName = beh.NodeName;
                    foreach (var child in beh.GetChildrenToSerialize(this, NodeName))
                    {
                        result.Add((NodeName, child, beh.TypeToCreate, beh.Policy));
                    }
                }
            }
            return result;
        }
        public Dictionary<String, String> GetAttributesToSerialize(Predicate<string>? ShouldGetThis = null)
        {
            Dictionary<String, String> result = new Dictionary<string, string>();
            if (IsNamedTextContentNode)
            {
                return result;
            }
            if (ShouldGetThis == null) ShouldGetThis = _ => true;

            HashSet<XMLMemberBehavior> _serialized = new HashSet<XMLMemberBehavior>();

            if (CurrentInstance is PrimitiveDictionaryEntry pde)
            {
                result.Add(pde.KeyName, pde.Key);
                result.Add(pde.ValueName, pde.Value);
            }
            else
            {

                if (_staticClassData != null)
                {
                    foreach (var beh in _staticClassData._attributeBehaviors_forSer.Values)
                    {
                        if (_serialized.Contains(beh)) continue;
                        _serialized.Add(beh);

                        string AttributeName = beh.NodeName;
                        if (!ShouldGetThis(AttributeName)) continue;
                        if (beh.Policy == DeserializationPolicy.Create)
                        {
                            string AttributeValue = beh.GetAttributeToSerialize(this, NodeName, AttributeName);
                            if (AttributeValue != null) result.Add(AttributeName, AttributeValue);
                        }
                        else
                        {
                            object ValueToLookup = beh.ObtainValue(this);
                            if (ValueToLookup != null && this.LookupAttributeFor(this.NodeName, AttributeName,
                                  beh.TypeToCreate, ValueToLookup, out string AttributeValue)
                                )
                                result.Add(AttributeName, AttributeValue);
                        }
                    }
                }
            }
            return result;
        }

        private void FindStaticData(bool allowImplicitFields)
        {
            Type t = this.GetCurrentInstance().GetType();
            if (!__typeCache.TryGetValue(t, out var data))
            {
                List<XMLMemberBehavior> _behaviors = new List<XMLMemberBehavior>();
                List<XMLFunction> _functions = new List<XMLFunction>();

                ICollection<(MemberInfo member, AbstractXMLMemberAttribute attr)> members = new Collection<(MemberInfo, AbstractXMLMemberAttribute)>();
                int order = 0;
                foreach (var m in t.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
                {
                    if (m is EventInfo || (m is FieldInfo f && (f.FieldType.BaseType?.Equals(typeof(MulticastDelegate)) ?? false)))
                    {
                        continue;
                    }
                    bool isPropOrField = m is PropertyInfo || m is FieldInfo;
                    bool isMethod = m is MethodInfo;

                    if (!isPropOrField && !isMethod) continue;
                    if (m.IsDefined(typeof(NonSerializedAttribute))) continue;
                    if (m.IsDefined(typeof(XMLNonSerializedAttribute))) continue;

                    int added = 0;
                    foreach (var att in m.GetCustomAttributes(true))
                    {
                        if (att is AbstractXMLMemberAttribute xmlMemberAttribute)
                        {
                            added++;
                            members.Add((m, xmlMemberAttribute));
                        }
                    }

                    if (added == 0 && allowImplicitFields && (m is PropertyInfo || (m is FieldInfo fi && fi.IsPublic)))
                    {
                        members.Add((m, null));
                    }
                    order++;
                }

                foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.Static))
                {
                    if (!m.IsDefined(typeof(XMLFunctionAttribute), true))
                    {
                        continue;
                    }
                    XMLFunctionAttribute att = m.GetCustomAttribute<XMLFunctionAttribute>();
                    XMLFunction f = new XMLFunction(m);
                    _functions.Add(f);
                }

                foreach (var item in members)
                {
                    XMLMemberBehavior beh;
                    beh = XMLMemberBehavior.Generate(item.member, item.attr);
                    beh.Init();
                    _behaviors.Add(beh);
                }
                __typeCache[t] = data = new XmlStaticClassData(t, _behaviors, _functions);
            }
            _staticClassData = data;
        }

        private static Dictionary<Type, ConvertFromString> __standardConversions =
                   new Dictionary<Type, ConvertFromString>();

        private static Dictionary<Type, ConvertToString> __reverseConversions =
                   new Dictionary<Type, ConvertToString>();

        private static string ToString(String s, IFormatProvider fp) => s.ToString(fp);
        internal static ConvertFromString? CreateStandardConversionFromString(Type targetType)
        {
            if (__standardConversions.TryGetValue(targetType, out var result))
            {
                return result;
            }


            MethodInfo IntermediateMethod;
            if (targetType.Equals(typeof(string)))
                IntermediateMethod = typeof(XMLNodeBehaviorProfile).GetMethod("ToString", new Type[] { typeof(string), typeof(IFormatProvider) });
            else

                IntermediateMethod = (from mt in targetType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                                      where
                                      mt.Name.Equals(nameof(int.Parse))
                                      && targetType.IsAssignableFrom(mt.ReturnType)
                                      && mt.GetParameters().Length == 2
                                      && mt.GetParameters()[0].ParameterType.Equals(typeof(String))
                                      && mt.GetParameters()[1].ParameterType.Equals(typeof(IFormatProvider))
                                      select mt).FirstOrDefault();
            if (IntermediateMethod != null)
            {
                object Func(string s, IFormatProvider ifp)
                {
                    return IntermediateMethod.Invoke(null, new object[] { s, ifp });
                }
                result = Func;
            }

            if (result == default)
            {
                IntermediateMethod = (from mt in targetType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                                      where
                                      mt.Name.Equals(nameof(int.Parse))
                                      && targetType.IsAssignableFrom(mt.ReturnType)
                                      && mt.GetParameters().Length == 1
                                      && mt.GetParameters()[0].ParameterType.Equals(typeof(String))
                                      select mt).FirstOrDefault();
                if (IntermediateMethod != null)
                {
                    object Func(string s, IFormatProvider ifp)
                    {
                        return IntermediateMethod.Invoke(null, new object[] { s });
                    }
                    result = Func;
                }
            }

            if (result == default)
            {
                var constr = targetType.GetConstructor(new Type[] { typeof(string) });

                if (constr != null)
                {
                    object Func(string s, IFormatProvider ifp)
                    {
                        return constr.Invoke(null, new object[] { s });
                    }
                    result = Func;
                }
            }

            if (result == default)
            {
                Type nonNullableTargetType = targetType;
                if (targetType.IsGenericType && targetType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
                {
                    nonNullableTargetType = targetType.GenericTypeArguments[0];
                }

                if (nonNullableTargetType.IsEnum)
                {
                    object Func(string s, IFormatProvider ifp)
                    {
                        return (Enum)Enum.Parse(nonNullableTargetType, s);
                    }
                    result = Func;
                }

            }

            if (result == default)
            {
                IntermediateMethod = (from mt in typeof(Convert).GetMethods(BindingFlags.Public | BindingFlags.Static)
                                      where targetType.IsAssignableFrom(mt.ReturnType)
                                      && mt.GetParameters().Length == 2
                                      && mt.GetParameters()[0].ParameterType.Equals(typeof(String))
                                      && mt.GetParameters()[1].ParameterType.Equals(typeof(IFormatProvider))
                                      select mt).FirstOrDefault();

                if (IntermediateMethod != null)
                {
                    object fromStringWrapper(string s, IFormatProvider ifp)
                    {
                        return IntermediateMethod.Invoke(null, new object[] { s, ifp });
                    }

                    result = fromStringWrapper;
                }
            }

            if (result != default)
                __standardConversions[targetType] = result;
            return result;
        }

        internal static ConvertToString? CreateStandardConversionToString(Type targetType)
        {
            if (__reverseConversions.TryGetValue(targetType, out ConvertToString result))
            {
                return result;
            }
            MethodInfo IntermediateMethod;
            if (targetType.Equals(typeof(string)))
                IntermediateMethod = typeof(string).GetMethod(nameof(object.ToString), new Type[] { typeof(IFormatProvider) });
            else
                IntermediateMethod = (from mt in targetType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                                      where
                                      mt.Name.Equals(nameof(int.ToString))
                                      && typeof(String).IsAssignableFrom(mt.ReturnType)
                                      && mt.GetParameters().Length == 1
                                      && mt.GetParameters()[0].ParameterType.Equals(typeof(IFormatProvider))
                                      select mt).FirstOrDefault();

            if (IntermediateMethod != null)
            {
                string ToStringWrapper(object s, IFormatProvider ifp)
                {
                    return (string)IntermediateMethod.Invoke(s, new object[] { ifp });
                }

                result = ToStringWrapper;
            }

            if (result != default)
                __reverseConversions[targetType] = result;

            return result;
        }
        public void InjectDependencies(object newObject)
        {
            GetHandler()?.InjectDependencies(this, newObject);
        }
        public bool InfoForNode(string nodeName, IReadOnlyDictionary<string, string> attributes, out DeserializationPolicy policy,
            [MaybeNullWhen(false)][NotNullWhen(true)] out Type? resultType)
        {
            resultType = default;
            policy = default;

            if (_staticClassData != null && _staticClassData._childBehaviors_forDes.TryGetValue(nodeName, out var beh))
            {
                resultType = beh.TypeToCreate;
                policy = beh.Policy;
                return true;
            }

            /*
             Nota: qui invochiamo GetHandler() perché non siamo interessati a chiedere ai nostri Parent informazioni
             su un nodo che abbiamo incontrato noi. Se questa informazione non è nei nostri staticClassData la chiediamo
             al context come ultima spiaggia e basta.
             */
            if (GetHandler()?.InfoForNode(this, nodeName, attributes, out policy, out resultType) ?? false)
                return true;

            return false;
        }

        public bool InfoForAttribute(string nodeName, string attributeName, out DeserializationPolicy policy, [MaybeNullWhen(false)][NotNullWhen(true)] out Type? result)
        {
            result = default;
            policy = default;

            if (_staticClassData != null && _staticClassData._attributeBehaviors_forDes.TryGetValue(attributeName, out var beh))
            {
                result = beh.TypeToCreate;
                policy = beh.Policy;
                return true;
            }
            return false;
        }

        public bool InfoForTextContent(string nodeName, out Type result)
        {
            result = default;
            //policy = default;

            if (_staticClassData?._textContentBehavior_forDes != null)
            {
                result = _staticClassData._textContentBehavior_forDes.TypeToCreate;
                //policy = _staticClassData._textContentBehavior.Policy;
                return true;
            }
            return false;
        }

        //---------------//
        public bool Lookup_FromTextContent(string nodeName, string TextContent, Type targetClass, [MaybeNullWhen(false)][NotNullWhen(true)] out object? result)
        {
            result = null;

            if (_staticClassData != null && _staticClassData.HasTextContentLookupForType(targetClass, out XMLFunction lookupFunction))
            {
                result = lookupFunction.InvokeWithTextContent(this, this.GetCurrentInstance(), TextContent);
                return true;
            }

            /* NOTA BENE:
                interroghiamo solo il context locale invece di usare GetHandler() perché in caso di fallimento giriamo la palla al parent,
                il quale dovrà PRIMA controllare i SUOI staticClassData e poi eventualmente interrogare il suo context.
                una chiamata a GetHandler() finirebbe per risalire al primo context disponibile bypassando il controllo dei vari staticClassData che potrebbero trovarsi in mezzo
            */
            if (_handler == null || !_handler.Lookup_FromTextContent(this, nodeName, TextContent, targetClass, out result))
            {
                return Parent?.Lookup_FromTextContent(nodeName, TextContent, targetClass, out result) ?? false;
            }
            else return true;

        }
        public bool Lookup_FromAttributes(string nodeName, IReadOnlyDictionary<string, string> attributes, Type targetClass, [MaybeNullWhen(false)][NotNullWhen(true)] out object? result)
        {
            result = null;

            if (_staticClassData != null && _staticClassData.HasLookupForType(targetClass, new HashSet<string>(attributes.Keys), out XMLFunction lookupFunction))
            {
                result = lookupFunction.InvokeWithAttributes(this, this.GetCurrentInstance(), attributes);
                return true;
            }

            /* NOTA BENE:
                interroghiamo solo il context locale invece di usare GetHandler() perché in caso di fallimento giriamo la palla al parent,
                il quale dovrà PRIMA controllare i SUOI staticClassData e poi eventualmente interrogare il suo context.
                una chiamata a GetHandler() finirebbe per risalire al primo context disponibile bypassando il controllo dei vari staticClassData che potrebbero trovarsi in mezzo
            */
            if (_handler == null || !_handler.Lookup_FromAttributes(this, nodeName, attributes, targetClass, out result))
            {
                return Parent?.Lookup_FromAttributes(nodeName, attributes, targetClass, out result) ?? false;
            }
            else return true;
        }
        /// <summary>
        /// Stiamo cercando di serializzare un nodo che "punta" ad un altro oggetto tramite lookup.
        /// Dato un oggetto (<paramref name="item"/>) dobbiamo restituire la lista di attributi di quell'oggetto che occorreranno
        /// per effettuare il lookup in fase di deserializzazione
        /// </summary>
        /// <param name="targetNodeName">Nome del nodo che stiamo creando</param>
        /// <param name="item">L'oggetto "puntato" da questo nodo</param>
        /// <param name="targetClass">La classe del campo che contiene questo oggetto (potrebbe essere una super classe di quella di runtime)</param>
        /// <param name="attributes">(out) Dizionario di attributi da restituire</param>
        /// <returns></returns>
        public bool GetLookupAttributes(string targetNodeName, object item, Type targetClass,
          [MaybeNullWhen(false)][NotNullWhen(true)] out IReadOnlyDictionary<string, string>? attributes)
        {
            attributes = null;

            if (_staticClassData != null && _staticClassData.HasSerializationLookupForType(targetClass, false, out var lookupFunction))
            {
                attributes = lookupFunction.GetLookupAttributes(context: this, TargetNodeName: targetNodeName, item: item);
                return true;
            }

            /* NOTA BENE:
                interroghiamo solo il context locale invece di usare GetHandler() perché in caso di fallimento giriamo la palla al parent,
                il quale dovrà PRIMA controllare i SUOI staticClassData e poi eventualmente interrogare il suo context.
                una chiamata a GetHandler() finirebbe per risalire al primo context disponibile bypassando il controllo dei vari staticClassData che potrebbero trovarsi in mezzo
            */
            if (_handler == null || !_handler.GetLookupAttributes(this, parentNode: this.NodeName, targetNode: targetNodeName, item: item, out attributes))
            {
                return Parent?.GetLookupAttributes(targetNodeName, item, targetClass, out attributes) ?? false;
            }
            else return true;
        }

        //---------------//
        public bool Lookup_FromAttribute(string nodeName, string attributeName, string attributeValue, Type targetClass, [MaybeNullWhen(false)][NotNullWhen(true)] out object? result)
        {
            result = null;

            if (_staticClassData != null && _staticClassData.HasLookupForType(targetClass,
                new HashSet<string>() { attributeName },
                out var lookupFunction))
            {
                result = lookupFunction.InvokeWithAttribute(this, this.GetCurrentInstance(), attributeName, attributeValue);
                return true;
            }

            /* NOTA BENE:
                interroghiamo solo il context locale invece di usare GetHandler() perché in caso di fallimento giriamo la palla al parent,
                il quale dovrà PRIMA controllare i SUOI staticClassData e poi eventualmente interrogare il suo context.
                una chiamata a GetHandler() finirebbe per risalire al primo context disponibile bypassando il controllo dei vari staticClassData che potrebbero trovarsi in mezzo
            */
            if (_handler == null || !_handler.Lookup_FromAttribute(this, nodeName, attributeName, attributeValue, targetClass, out result))
            {
                return Parent?.Lookup_FromAttribute(nodeName, attributeName, attributeValue, targetClass, out result) ?? false;
            }
            else return true;
        }
        public bool LookupAttributeFor(string nodeName, string attributeName, Type targetClass, object value, [MaybeNullWhen(false)][NotNullWhen(true)] out string? result)
        {
            result = null;

            if (_staticClassData != null)
            {
                if (_staticClassData.HasReverseLookupForType(targetClass, out var lookupFunction))
                {
                    result = lookupFunction.InvokeReverseLookup(this, this.GetCurrentInstance(), value);
                    return true;
                }
                else if (_staticClassData.HasSerializationLookupForType(targetClass, true, out lookupFunction))
                {
                    var (attName, AttValue) = lookupFunction.GetLookupAttribute(context: this, TargetNodeName: nodeName, item: value);
                    result = AttValue;
                    return true;
                }
            }

            /* NOTA BENE:
                interroghiamo solo il context locale invece di usare GetHandler() perché in caso di fallimento giriamo la palla al parent,
                il quale dovrà PRIMA controllare i SUOI staticClassData e poi eventualmente interrogare il suo context.
                una chiamata a GetHandler() finirebbe per risalire al primo context disponibile bypassando il controllo dei vari staticClassData che potrebbero trovarsi in mezzo
            */
            if (_handler == null || !_handler.GetLookupAttribute(this, nodeName, attributeName, targetClass, value, out result))
            {
                return Parent?.LookupAttributeFor(nodeName, attributeName, targetClass, value, out result) ?? false;
            }
            else return true;
        }
        public void Finalized(string nodeName)
        {
            if (!string.IsNullOrEmpty(_TextContent))
            {
                if (this.GetCurrentInstance() is PlaceHolderForLateLookup phll)
                {
                    object result = null;
                    bool ok = false;
                    try
                    {
                        ok = Lookup_FromTextContent(this.NodeName, _TextContent, TargetType, out result);
                    }
                    catch (Exception e)
                    {
                        throw new LookupFailureException(e, phll.PreviousException);
                    }

                    CurrentInstance = result;

                    if (CurrentInstance == null)
                        throw new LookupFailureException(phll.PreviousException);
                }
                else
                {
                    ProcessTextContent(_TextContent);
                }
            }

            if (IsCreation)
            {
                if (CurrentInstance != null)
                {
                    if (CurrentInstance is IDeserializationCallback dc)
                        dc.OnDeserialization(CurrentInstance);
                    //--------------------------//
                    if (_staticClassData != null)
                        foreach (XMLMemberBehavior item in _staticClassData.AllAttributes_ForDes)
                        {
                            item.OnFinalized(CurrentInstance, this);
                        }
                    //--------------------------//
                }
            }
            this.Parent?.FinalizedChild(nodeName, Attributes, this.GetCurrentInstance());


            //-----//
            XMLNodeBehaviorProfile c = this;
            GetHandler()?.Finalized(this, nodeName, this.CurrentInstance);
            //----//
        }
        public void FinalizedChild(string childNodeName, IReadOnlyDictionary<string, string> childAttributes, object result)
        {
            ChildrenCount++;
            if (_staticClassData != null)
            {
                if (_staticClassData._childBehaviors_forDes.TryGetValue(childNodeName, out var beh))
                {
                    beh.ProcessChildNodeResult(this, childNodeName, childAttributes, result);
                }
            }
        }
        //---------------//

        internal bool ProcessAttribute(string NodeName, string AttributeName, String AttributeValue)
        {
            XMLMemberBehavior beh;
            if (!_staticClassData._attributeBehaviors_forDes.TryGetValue(AttributeName, out beh))
                return false;

            beh.ProcessAttribute(this, NodeName, AttributeName, AttributeValue);
            return true;
        }


        private string _TextContent = null;
        internal void StoreTextContent(string text)
        {
            _TextContent = text;
            EncounteredTextContent = true;
        }

        private bool ProcessTextContent(String value)
        {
            if (this.IsNamedTextContentNode)
            {
                if (TargetType.Equals(typeof(string)))
                {
                    CurrentInstance = value;
                    return true;
                }
                else if (Convert(value, TargetType, out var result))
                {
                    CurrentInstance = result;
                    return true;
                }
            }

            if (_staticClassData?._textContentBehavior_forDes == null)
                return false;

            _staticClassData._textContentBehavior_forDes.ProcessTextContent(this, value);
            return true;
        }
        internal bool ProcessValue(string NodeName, string AttributeName, object value)
        {
            XMLMemberBehavior beh;
            if (!_staticClassData._attributeBehaviors_forDes.TryGetValue(AttributeName, out beh))
                return false;

            beh.ProcessValue(this, NodeName, AttributeName, value);
            return true;
        }

        private bool HasConverterFunctionForType(Type t, out (XMLFunction function, object instance)? Converter)
        {
            Converter = null;
            if (_staticClassData?.HasConverterForType(t, out XMLFunction? function) ?? false)
            {
                Converter = (function, this.GetCurrentInstance());
            }

            return Converter != null;
        }
        public bool HasReverseConverterForType(Type t, out (XMLFunction function, object instance)? Converter)
        {
            Converter = null;
            if (_staticClassData?.HasReverseConverterForType(t, out XMLFunction? function) ?? false)
            {
                Converter = (function, this.GetCurrentInstance());
            }
            return Converter != null;
        }

        public bool Convert(string valueString, Type ToType, [MaybeNullWhen(false)][NotNullWhen(true)] out object? result)
        {
            //first we check if anyone in the stack has a conversion function we can use
            (XMLFunction function, object instance)? f = null;
            XMLNodeBehaviorProfile? current = this;
            while (current != null && !current.HasConverterFunctionForType(ToType, out f))
            {
                current = current.Parent;
            }

            if (f != null)
            {
                //found one
                result = f.Value.function.Convert(f.Value.instance, valueString, GetFormat());
                return true;
            }

            // No luck, let's check if we have a handler up the stack and if IT can offer a conversion for our target type
            if ((GetHandler()?.HasConversionFromString(ToType, out ConvertFromString? converter) ?? false)
                || __standardConversions.TryGetValue(ToType, out converter)
                )
            {

                result = converter(valueString, GetFormat());
                return true;
            }

            //nope, we give up

            result = null;
            return false;
        }

        public bool ConvertBack(object value, Type FromType, [MaybeNullWhen(false)][NotNullWhen(true)] out string? result)
        {
            //first we check if anyone in the stack has a conversion function we can use
            (XMLFunction function, object instance)? f = null;
            XMLNodeBehaviorProfile? current = this;
            while (current != null && !current.HasReverseConverterForType(FromType, out f))
            {
                current = current.Parent;
            }

            if (f != null)
            {
                //found one
                result = f.Value.function.ConvertBack(f.Value.instance, value, GetFormat());
                return true;
            }

            // No luck, let's check if we have a handler up the stack and if IT can offer a conversion for our target type
            if ((GetHandler()?.HasConversionToString(FromType, out ConvertToString? converter) ?? false)
                || __reverseConversions.TryGetValue(FromType, out converter))
            {
                result = converter(value, Format);
                return true;
            }

            //nope, we give up

            result = null;
            return false;
        }

        public bool CanProcessAttributes => (_staticClassData != null && _staticClassData._attributeBehaviors_forDes.Count > 0);

        public bool CanSerializeAsTextContent
        {
            get
            {
                if (!IsSerializing) return false;
                if (TargetType == null) return false;

                //we check in reverse order to (possibly) save time, because the sources we check last
                //are usually more likely to yield a positive result
                //and here we only care for *ANY* conversion function, regardless of priority
                if (__reverseConversions.ContainsKey(TargetType)) return true;
                if (GetHandler()?.HasConversionToString(TargetType, out _) ?? false) return true;

                (XMLFunction function, object instance)? f = null;
                XMLNodeBehaviorProfile? current = this;
                while (current != null && !current.HasReverseConverterForType(TargetType, out f))
                {
                    current = current.Parent;
                }
                return f != null;
            }
        }
        public bool IsNamedTextContentNode
        {
            get
            {
                if (IsSerializing)
                {
                    return CanSerializeAsTextContent;
                }
                else
                    return Attributes.Count == 0 && ChildrenCount == 0;
            }
        }

    }
}
