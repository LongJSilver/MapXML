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

        internal XMLNodeBehaviorProfile? Parent;

        /****** STATIC ******/
        private XmlStaticClassData? _staticClassData;

        private static Dictionary<Type, XmlStaticClassData> __typeCache =
                   new Dictionary<Type, XmlStaticClassData>();

        XmlStaticClassData? IXMLInternalContext.StaticClassData => _staticClassData;
        public IXMLSerializationHandler? Handler => GetHandler();
        public IDictionary<string, object> CustomData => _customData;

        public IXMLOptions Options { get; }
        internal readonly string NodeName;
        internal readonly bool IsSerializing;
        internal readonly Type? TargetType;

        /****** Identity ******/
        private readonly IXMLSerializationHandler? _handler;
        internal bool IsCreation { get; private set; }
        internal CultureInfo? Format { get; set; }
        IFormatProvider? IXMLInternalContext.FormatProvider => GetFormat();
        public int Level { get; set; }
        internal readonly IReadOnlyDictionary<string, string> Attributes;
        /****** STATE ******/
        internal bool EncounteredTextContent => _TextContent != null;
        private string? _TextContent = null;

        internal readonly IDictionary<string, object> _customData;
        private int ChildrenCount = 0;
        public object? CurrentInstance { get; set; }
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
            IXMLSerializationHandler? handler, IXMLOptions options,
                 string Node, IReadOnlyDictionary<string, string>? attributes,
              object? currentObject, Type? targetType,
                 bool IsSerializing)
        {
            _handler = handler;
            this.Options = options;

            this.NodeName = Node;
            this.Attributes = attributes ?? new Dictionary<string, string>();

            CurrentInstance = currentObject;
            this.TargetType = targetType;

            this.IsSerializing = IsSerializing;
            //------------------//
            _customData = new Dictionary<string, object>();
            if (CurrentInstance != null && !CurrentInstance.Equals(typeof(string)))
            {
                FindStaticData(this.Options.AllowImplicitFields);
            }
        }

        internal static XMLNodeBehaviorProfile CreateTopNode(IXMLSerializationHandler? handler, IXMLOptions options, string? NodeName, object? owner, bool isSerializing)
        {
            return new XMLNodeBehaviorProfile(handler, options, NodeName ?? string.Empty, null, owner, owner?.GetType(), isSerializing)
            {
                IsCreation = true
            };
        }

        internal static XMLNodeBehaviorProfile CreateSerializationNode(IXMLSerializationHandler? handler, IXMLOptions opt,
            string name, Type targetType, object item, string? formatName = null)
        {
            var result = new XMLNodeBehaviorProfile(handler, opt, name, null, item, targetType, true);
            if (formatName != null)
                result.Format = CultureInfo.GetCultureInfo(formatName);
            return result;
        }

        internal static XMLNodeBehaviorProfile GetDummyForLookupAttributes(
            IXMLSerializationHandler? handler, IXMLOptions options,
            string targetNodeName, object item)
        {
            return new XMLNodeBehaviorProfile(handler, options, targetNodeName, null, item, item.GetType(), true);
        }


        internal static XMLNodeBehaviorProfile CreateDeserializationNode(IXMLSerializationHandler? handler, IXMLOptions options, bool creation,
            string nodeName, Type targetType, Dictionary<string, string> attributes, object owner)
        {

            return new XMLNodeBehaviorProfile(handler, options, nodeName, attributes, owner, targetType, false)
            {
                IsCreation = creation
            };
        }
        internal string? GetTextContentToSerialize()
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
                            object ValueToLookup = beh.ObtainValueForLookup(this);
                            if (ValueToLookup != null && this.LookupTextContentFor(this.NodeName, beh.TypeToCreate, ValueToLookup, out string? AttributeValue)
                                )
                                return AttributeValue;
                            else return null;
                        }
                    }
                }
                else return null;
            }
        }
        internal List<(String nodeName, object child, Type targetType, DeserializationPolicy policy)> GetAllChildrenToSerialize()
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
        internal Dictionary<String, String> GetAttributesToSerialize(Predicate<string>? ShouldGetThis = null)
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
                            object ValueToLookup = beh.ObtainValueForLookup(this);
                            if (ValueToLookup != null && this.LookupAttributeFor(this.NodeName, AttributeName,
                                  beh.TypeToCreate, ValueToLookup, out string? AttributeValue)
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

                ICollection<(MemberInfo member, AbstractXMLMemberAttribute? attr)> members = new Collection<(MemberInfo, AbstractXMLMemberAttribute?)>();
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

                foreach ((MemberInfo member, AbstractXMLMemberAttribute? attr) in members)
                {
                    XMLMemberBehavior beh;
                    beh = XMLMemberBehavior.Generate(member, attr);
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
                object Func(string s, IFormatProvider? ifp)
                {
                    return IntermediateMethod.Invoke(null, new object?[] { s, ifp });
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
                    object Func(string s, IFormatProvider? ifp)
                    {
                        return IntermediateMethod.Invoke(null, new object?[] { s });
                    }
                    result = Func;
                }
            }

            if (result == default)
            {
                var constr = targetType.GetConstructor(new Type[] { typeof(string) });

                if (constr != null)
                {
                    object Func(string s, IFormatProvider? ifp)
                    {
                        return constr.Invoke(null, new object?[] { s });
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
                    object Func(string s, IFormatProvider? ifp)
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
                    object fromStringWrapper(string s, IFormatProvider? ifp)
                    {
                        return IntermediateMethod.Invoke(null, new object?[] { s, ifp });
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
                string ToStringWrapper(object s, IFormatProvider? ifp)
                {
                    return (string)IntermediateMethod.Invoke(s, new object?[] { ifp });
                }

                result = ToStringWrapper;
            }

            if (result != default)
                __reverseConversions[targetType] = result;

            return result;
        }
        internal void InjectDependencies(object newObject)
        {
            GetHandler()?.InjectDependencies(this, newObject);
        }
        internal bool InfoForNode(string nodeName, IReadOnlyDictionary<string, string> attributes, out DeserializationPolicy policy,
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

        internal bool InfoForAttribute(string nodeName, string attributeName, out DeserializationPolicy policy, [MaybeNullWhen(false)][NotNullWhen(true)] out Type? result)
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

        internal bool InfoForTextContent(string nodeName, out Type? result)
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
        internal bool Lookup_FromTextContent(string nodeName, string TextContent, Type targetClass, [MaybeNullWhen(false)][NotNullWhen(true)] out object? result)
        {
            result = null;

            if (_staticClassData != null && _staticClassData.HasLookupForType(targetClass, SingleAttribute: true, out XMLFunction? lookupFunction))
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
        internal bool Lookup_FromAttributes(string nodeName, IReadOnlyDictionary<string, string> attributes, Type targetClass, [MaybeNullWhen(false)][NotNullWhen(true)] out object? result)
        {
            result = null;

            if (_staticClassData != null && _staticClassData.HasLookupForType(targetClass, new HashSet<string>(attributes.Keys), out XMLFunction? lookupFunction))
            {
                result = lookupFunction.InvokeWithAttributes(this, this.GetCurrentInstance(), attributes);
                return result != null;
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
            else return result != null;
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
        internal bool GetLookupAttributes(string targetNodeName, object item, Type targetClass,
          [MaybeNullWhen(false)][NotNullWhen(true)] out IReadOnlyDictionary<string, string>? attributes)
        {
            attributes = null;

            if (_staticClassData != null && _staticClassData.HasLookupForType(targetClass, false, out var lookupFunction))
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
        /// <summary>
        /// During the serialization phase, this method is called when an object member needs to be serialized as a lookup, 
        /// ie the object itself has been already serialized earlier and we only need to store a value that points to it. 
        /// More specifically this function is called when the object needs to be serialized as a SINGLE attribute value.
        /// 
        /// <para/> To achieve this we first look for a 'Reverse lookup' function defined specifically by the caller for the object's type;
        /// <para/> then we try and find a 'Lookup' function with a single parameter and try to deduce from its metadata what value we should store;
        /// <para/> As a third option we try and ask the handler if it can do the job for us.
        /// <para/> Finally we try and ask our parent up the stack to run this process from scratch recursively.
        /// </summary>
        /// <param name="nodeName"></param>
        /// <param name="attributeName"></param>
        /// <param name="targetClass"></param>
        /// <param name="value"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        internal bool LookupAttributeFor(string nodeName, string attributeName, Type targetClass, object value, [MaybeNullWhen(false)][NotNullWhen(true)] out string? result)
        {
            result = null;

            if (_staticClassData != null)
            {
                if (_staticClassData.HasReverseLookupForType(targetClass, out var lookupFunction))
                {
                    result = lookupFunction.InvokeReverseLookup(this, this.GetCurrentInstance(), value);
                    return true;
                }
                else if (_staticClassData.HasLookupForType(targetClass, true, out lookupFunction))
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

        internal bool LookupTextContentFor(string nodeName, Type targetClass, object value, [MaybeNullWhen(false)][NotNullWhen(true)] out string? result)
        {
            result = null;

            if (_staticClassData != null)
            {
                if (_staticClassData.HasReverseLookupForType(targetClass, out var lookupFunction))
                {
                    result = lookupFunction.InvokeReverseLookup(this, this.GetCurrentInstance(), value);
                    return true;
                }
                else if (_staticClassData.HasLookupForType(targetClass, SingleAttribute: true, out lookupFunction))
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
            if (_handler == null || !_handler.GetLookupTextContent(this, nodeName, targetClass, value, out result))
            {
                return Parent?.LookupTextContentFor(nodeName, targetClass, value, out result) ?? false;
            }
            else return true;
        }
        internal void Finalized(string nodeName)
        {
            if (!string.IsNullOrEmpty(_TextContent))
            {
                if (this.GetCurrentInstance() is PlaceHolderForLateLookup phll)
                {
                    object? result = null;
                    bool ok = false;
                    try
                    {
                        ok = Lookup_FromTextContent(this.NodeName, _TextContent!, TargetType!, out result);
                    }
                    catch (Exception e)
                    {
#pragma warning disable 8604 
                        throw new LookupFailureException(e, phll.PreviousException);
#pragma warning restore 8604 
                    }

                    CurrentInstance = result;

                    if (CurrentInstance == null)
#pragma warning disable 8604 
                        throw new LookupFailureException(phll.PreviousException);
#pragma warning restore 8604 
                }
                else
                {
                    ProcessTextContent(_TextContent!);
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
            else
            {
                if (CurrentInstance == null || CurrentInstance is PlaceHolderForLateLookup)
                {
                    //Lookup failed 
                    throw new LookupFailureException();
                }

            }
            this.Parent?.FinalizedChild(nodeName, Attributes, this.GetCurrentInstance());

            //-----//
            XMLNodeBehaviorProfile c = this;
            if (CurrentInstance != null)
                GetHandler()?.Finalized(this, nodeName, this.CurrentInstance);
            //----//
        }
        internal void FinalizedChild(string childNodeName, IReadOnlyDictionary<string, string> childAttributes, object result)
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
            if (_staticClassData == null || !_staticClassData._attributeBehaviors_forDes.TryGetValue(AttributeName, out beh))
                return false;

            beh.ProcessAttribute(this, NodeName, AttributeName, AttributeValue);
            return true;
        }


        internal void StoreTextContent(string text)
        {
            _TextContent = text;
        }

        private bool ProcessTextContent(String value)
        {
            if (this.IsNamedTextContentNode)
            {
                if (TargetType!.Equals(typeof(string)))
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
            if (_staticClassData == null || !_staticClassData._attributeBehaviors_forDes.TryGetValue(AttributeName, out beh))
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
        internal bool HasReverseConverterForType(Type t, out (XMLFunction function, object instance)? Converter)
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

        internal bool CanProcessAttributes => (_staticClassData != null && _staticClassData._attributeBehaviors_forDes.Count > 0);

        /// <summary>
        /// Check if the current node represents an instance that can be entirely serialized as a text content node.
        /// </summary>
        internal bool CanSerializeAsTextContent
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
        internal bool IsNamedTextContentNode
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
