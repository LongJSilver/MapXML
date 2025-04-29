using MapXML.Attributes;
using MapXML.Behaviors;
using MapXML.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MapXML
{
    internal sealed class XMLParameterMap
    {
        public readonly string AttributeName;
        public readonly ConvertFromString? ConversionFromString;
        public readonly ConvertToString? ConversionToString;
        public readonly Type TargetType;

        public XMLParameterMap(string attributeName, Type type)
        {
            AttributeName = attributeName;
            this.TargetType = type;
            this.ConversionFromString = XMLNodeBehaviorProfile.CreateStandardConversionFromString(type);
            this.ConversionToString = XMLNodeBehaviorProfile.CreateStandardConversionToString(type);
        }
        public XMLParameterMap(string attributeName, Type type, ConvertFromString? conversionFromString, ConvertToString? conversionToString) : this(attributeName, type)
        {
            this.ConversionFromString = conversionFromString ?? XMLNodeBehaviorProfile.CreateStandardConversionFromString(type);
            this.ConversionToString = conversionToString ?? XMLNodeBehaviorProfile.CreateStandardConversionToString(type);
        }
    }


    /// <summary>
    /// Fornisce le informazioni di mappatura che consentano di invocare un metodo di una classe 
    /// a partire dalle informazioni contenute in uno specifico nodo xml.
    /// </summary>
    internal sealed class XMLFunction
    {
        XMLParameterMap[] _parameterMapping;
        public readonly bool IsSingleParameter;
        public readonly bool IsConverter;

        public Type ReturnType => _method.ReturnType;
        private MethodInfo _method;
        public XMLFunction(MethodInfo method)
        {
            this._method = method;
            var param = method.GetParameters();
            _parameterMapping = new XMLParameterMap[param.Length];
            this.IsSingleParameter = param.Length == 1;
            IsConverter = param.Length == 2 && (!typeof(void).Equals(ReturnType)) && param[0].ParameterType.Equals(typeof(string))
                                                                                            && param[1].ParameterType.Equals(typeof(IFormatProvider));
            int mappedParameters = 0;
            for (int i = 0; i < param.Length; i++)
            {
                var p = param[i];
                if (p.IsOut)
                    throw new InvalidOperationException("Out parameters are not supported.");

                var attr = p.GetCustomAttribute<XMLParameterAttribute>();
                if (attr != null)
                {
                    ConvertFromString? conv = null;
                    if (attr.ConversionFunction != null)
                    {
                        MethodInfo m = method.DeclaringType.GetMethod(attr.ConversionFunction);
                        if (m != null && m.IsCompatibleWithDelegateType(typeof(ConvertFromString)))
                        {
                            conv = (ConvertFromString)method.CreateDelegate(typeof(ConvertFromString), null);
                        }
                    }

                    ConvertToString? convBack = null;
                    if (attr.ConversionBackFunction != null)
                    {
                        MethodInfo m = method.DeclaringType.GetMethod(attr.ConversionBackFunction);
                        if (m != null && m.IsCompatibleWithDelegateType(typeof(ConvertFromString)))
                        {
                            convBack = (ConvertToString)method.CreateDelegate(typeof(ConvertToString), null);
                        }
                    }

                    _parameterMapping[i] = new XMLParameterMap(attr.AttributeName, p.ParameterType, conv, convBack);
                    mappedParameters++;
                }
                else
                {
                    _parameterMapping[i] = new XMLParameterMap(p.Name, p.ParameterType, null, null);
                    mappedParameters++;
                }
            }
        }

        private static object ConvertParameterFromString(IXMLInternalContext context, object functionInstance, string paramValue, XMLParameterMap paramInfo)
        {
            if (paramInfo.ConversionFromString != null)
            {
                return paramInfo.ConversionFromString(paramValue, context.FormatProvider);
            }

            if (context.Convert(paramValue, paramInfo.TargetType, out object? result))
            {
                return result;
            }

            throw new InvalidOperationException($"Unable to convert param {paramInfo.AttributeName}");
        }

        private static string ConvertParameterToString(IXMLInternalContext context, object functionInstance, object paramValue, XMLParameterMap paramInfo)
        {
            if (paramInfo.ConversionToString != null)
            {
                return (string)paramInfo.ConversionToString(paramValue, context.FormatProvider);
            }

            if (context.ConvertBack(paramValue, paramInfo.TargetType, out string? result))
            {
                return result;
            }

            throw new InvalidOperationException($"Unable to convert param {paramInfo.AttributeName}");
        }
        public bool IsLookupFor(Type t) => t.IsAssignableFrom(_method.ReturnType);
        public bool IsSingleAttributeLookupFor(Type t)
        {
            return t.IsAssignableFrom(_method.ReturnType) && _parameterMapping.Length == 1;
        }

        /// <summary>
        /// True if this function is specifically created to convert <paramref name="targetClass"/> into string, hence it's a reverse lookup.
        /// They have <see cref="string"/> as return type and a single parameter of <paramref name="t"/> type.
        /// <para/> Note that since these functions do not accept a IFormatProvider parameter, they are NOT equivalent to a <see cref="ConvertToString"/> delegate
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public bool IsReverseLookupFor(Type t)
        {
            var param = _method.GetParameters();
            return param.Length == 1 && param[0].ParameterType.IsAssignableFrom(t) && ReturnType.Equals(typeof(string));
        }
        public bool IsReverseConverterFor(Type t)
        {
            var param = _method.GetParameters();
            return param.Length == 2
                && param[0].ParameterType.IsAssignableFrom(t)
                && param[1].ParameterType.IsAssignableFrom(typeof(IFormatProvider))
                && ReturnType.Equals(typeof(string));
        }
        public bool IsConverterFor(Type t)
        {
            if (!IsConverter) return false;
            return t.IsAssignableFrom(ReturnType);
        }
        internal object InvokeWithAttributes(IXMLInternalContext context, object functionInstance, IReadOnlyDictionary<string, string> attributes)
        {
            object?[] par = new object?[_parameterMapping.Length];
            for (int i = 0; i < _parameterMapping.Length; i++)
            {
                if (!attributes.TryGetValue(_parameterMapping[i].AttributeName, out string param))
                {
                    par[i] = null;
                }
                else
                {
                    par[i] = ConvertParameterFromString(context, functionInstance, param, _parameterMapping[i]);
                }
            }
            return _method.Invoke(_method.IsStatic ? null : functionInstance, par);
        }

        public IReadOnlyDictionary<string, string> GetLookupAttributes(IXMLInternalContext context, string TargetNodeName, object item)
        {
            XMLNodeBehaviorProfile profile = XMLNodeBehaviorProfile.GetDummyForLookupAttributes(context.Handler, context.Options, TargetNodeName, item);
            var attNames = new HashSet<String>(_parameterMapping.Select(p => p.AttributeName), StringComparer.InvariantCultureIgnoreCase);
            return profile.GetAttributesToSerialize(attNames.Contains);
        }
        public (String attName, String AttValue) GetLookupAttribute(IXMLInternalContext context, string TargetNodeName, object item)
        {
            XMLNodeBehaviorProfile profile = XMLNodeBehaviorProfile.GetDummyForLookupAttributes(context.Handler, context.Options, TargetNodeName, item);
            var attNames = new HashSet<String>(_parameterMapping.Select(p => p.AttributeName));
            var result = profile.GetAttributesToSerialize(attNames.Contains).First();
            return (result.Key, result.Value);
        }

        public object InvokeWithAttribute(IXMLInternalContext context, object functionInstance, string AttributeName, string AttributeValue)
        {
            if (_parameterMapping.Length != 1)
            {
                throw new InvalidOperationException("This function cannot be called for a single attribute.");
            }

            object value = ConvertParameterFromString(context, functionInstance, AttributeValue, _parameterMapping[0]);
            return _method.Invoke(_method.IsStatic ? null : functionInstance, new object[] { value });
        }

        public object InvokeWithTextContent(IXMLInternalContext context, object functionInstance, string TextContent)
        {
            if (_parameterMapping.Length != 1)
            {
                throw new InvalidOperationException("This function cannot be called with a single parameter.");
            }

            object value = ConvertParameterFromString(context, functionInstance, TextContent, _parameterMapping[0]);
            return _method.Invoke(_method.IsStatic ? null : functionInstance, new object[] { value });
        }

        public string InvokeReverseLookup(IXMLInternalContext context, object functionInstance, object value)
        {
            if (_parameterMapping.Length != 1)
            {
                throw new InvalidOperationException("This function cannot be called for a single attribute.");
            }

            return (string)_method.Invoke(_method.IsStatic ? null : functionInstance, new object[] { value });
        }

        public object Convert(object instance, string value, IFormatProvider? fprov)
        {
            return _method.Invoke(_method.IsStatic ? null : instance, new object?[] { value, fprov });
        }
        public string ConvertBack(object instance, object value, IFormatProvider? fprov)
        {
            return (string)_method.Invoke(_method.IsStatic ? null : instance, new object?[] { value, fprov });
        }

        internal float GetParameterMatchScore(ISet<string> attributes)
        {
            int count = 0;
            foreach (XMLParameterMap item in this._parameterMapping)
            {
                if (attributes.Contains(item.AttributeName))
                    count++;
            }
            return count / (_parameterMapping.Length * 1f);
        }
    }
}
