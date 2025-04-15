using MapXML.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MapXML
{
    public class DefaultHandler : IXMLSerializationHandler
    {
        private readonly Dictionary<String, (Type targetType, DeserializationPolicy policy)> _quickAssociations =
            new Dictionary<string, (Type targetType, DeserializationPolicy policy)>();

        private readonly Dictionary<Type, ConvertToString> _Converters_ToString = new Dictionary<Type, ConvertToString>();
        private readonly Dictionary<Type, ConvertFromString> _Converters_FromString = new Dictionary<Type, ConvertFromString>();

        private List<object> _allResults = new List<object>();

        private ListIndexer<String, object> _resultsByNode =
            new ListIndexer<string, object>();
        public int ResultCount => _allResults.Count;

        public IReadOnlyList<object> Results => _allResults;

        public void Associate(String nodeName, Type targetType, DeserializationPolicy policy = DeserializationPolicy.Create)
        {
            _quickAssociations[nodeName] = (targetType, policy);
        }

        public void Associate<V>(String nodeName, DeserializationPolicy policy = DeserializationPolicy.Create)
        {
            Associate(nodeName, typeof(V), policy);
        }

        public void RegisterTypeConverter(Type targetType, ConvertToString converterDelegate)
        {
            _Converters_ToString[targetType ?? throw new ArgumentNullException(nameof(targetType))]
                = converterDelegate ?? throw new ArgumentNullException(nameof(converterDelegate));
        }
        public void RegisterTypeConverter(Type targetType, ConvertFromString converterDelegate)
        {
            _Converters_FromString[targetType ?? throw new ArgumentNullException(nameof(targetType))]
                = converterDelegate ?? throw new ArgumentNullException(nameof(converterDelegate));
        }
        /// <summary>
        /// A new List is created at each call. Consider caching the results.
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <returns></returns>
        public IReadOnlyList<V> GetResults<V>()
            => _allResults.Where(item => typeof(V).IsAssignableFrom(item.GetType())).OfType<V>().ToList();

        /// <summary>
        /// A new List is created at each call. Consider caching the results.
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <returns></returns>
        public IEnumerable<V> GetResults<V>(string nodeName)
            => _resultsByNode[nodeName].OfType<V>().ToList();

        public virtual void Finalized(IXMLState state, string nodeName, object result)
        {
            if (_quickAssociations.ContainsKey(nodeName))
            {
                _allResults.Add(result);
                _resultsByNode.Add(nodeName, result);
            }
        }

        public virtual bool GetLookupAttributes(IXMLState state, string parentNode, string targetNode, object item,
            [MaybeNullWhen(false)][NotNullWhen(true)] out IReadOnlyDictionary<string, string>? attributes)
        {
            attributes = null;
            return false;
        }

        public virtual bool InfoForNode(IXMLState state, string nodeName, IReadOnlyDictionary<string, string> attributes, out DeserializationPolicy policy,
           [MaybeNullWhen(false)][NotNullWhen(true)] out Type? result)
        {
            if (_quickAssociations.TryGetValue(nodeName, out var data))
            {
                policy = data.policy;
                result = data.targetType;
                return true;
            }
            else
            {
                policy = DeserializationPolicy.Create;
                result = null;
                return false;
            }
        }

        public virtual bool Lookup_FromTextContent(IXMLState state, string nodeName, string TextContent, Type targetClass, [MaybeNullWhen(false)][NotNullWhen(true)] out object? result)
        {
            result = null;
            return false;
        }
        public virtual bool Lookup_FromAttribute(IXMLState state, string nodeName, string attributeName, string attributeValue, Type targetClass, [MaybeNullWhen(false)][NotNullWhen(true)] out object result)
        {
            result = null;
            return false;
        }

        public virtual bool Lookup_FromAttributes(IXMLState state, string nodeName, IReadOnlyDictionary<string, string> attributes, Type targetClass, [MaybeNullWhen(false)][NotNullWhen(true)] out object? result)
        {
            result = null;
            return false;
        }


        public virtual bool GetLookupAttribute(IXMLState state, string nodeName, string attributeName, Type targetClass, object value, [MaybeNullWhen(false)][NotNullWhen(true)] out string result)
        {
            result = null;
            return false;
        }

        public virtual bool GetLookupTextContent(IXMLState state, string nodeName, Type targetClass, object value, [MaybeNullWhen(false)][NotNullWhen(true)] out string result)
        {
            result = null;
            return false;
        }

        public void AddCreationOveride<V>(Func<object> creator)
        {
            _creators[typeof(V)] = creator;
        }

        private Dictionary<Type, Func<object>> _creators = new Dictionary<Type, Func<object>>();

        public virtual bool OverrideCreation(IXMLState state, Type t, [MaybeNullWhen(false)][NotNullWhen(true)] out object result)
        {
            if (_creators.TryGetValue(t, out var creator))
            {
                result = creator();
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        public virtual void InjectDependencies(IXMLState state, object newObject)
        {
        }

        public virtual bool HasConversionFromString(Type targetType, out ConvertFromString deleg)
        {
            return _Converters_FromString.TryGetValue(targetType, out deleg);
        }

        public virtual bool HasConversionToString(Type targetType, out ConvertToString deleg)
        {
            return _Converters_ToString.TryGetValue(targetType, out deleg);
        }

    }
}
