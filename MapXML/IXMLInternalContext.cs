using MapXML.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MapXML
{
    public delegate object ConvertFromString(string s, IFormatProvider? fp);
    public delegate string ConvertToString(object s, IFormatProvider? fp);

    public enum DeserializationPolicy
    {
        /// <summary>
        /// Used when the xml content represents the object itself
        /// </summary>
        Create,
        /// <summary>
        /// Used when the xml content is used to retrieve an existing object. <para/>
        /// In some edge cases it can be used to provide custom code to create the object in a way 
        /// that wouldn't be compatible with the automatic creation process employed by the library
        /// </summary>
        Lookup
    }

    [Flags]
    public enum XMLSourceType
    {
        Attribute = 1,
        Child = 2,
        TextContent = 4,
        //---- 
        ChildOrAttribute = Child | Attribute,
        AttributeOrText = Attribute | TextContent,
        ChildOrText = Child | TextContent,
        All = Attribute | Child | TextContent
    }


    public interface IXMLState
    {
        /// <summary>
        /// The depth level based on the current position within the XML Hierarchy
        /// </summary>
        int XMLLevel { get; }
        /// <summary>
        /// The current result level, where 0 is the first level in the hierarchy associated to an actual result.
        /// This may be different from <see cref="XMLLevel"/> in such cases where the caller chose to ignore the root node, 
        /// and also whether or not the caller provided an owner for the root node.
        /// </summary>
        int LogicalLevel { get; }
        object? CurrentInstance { get; }

        /// <summary>
        /// 0 => returns the current instance
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        object? GetParent(int level = 1);
        IXMLOptions Options { get; }
    }

    /// <summary>
    /// Provides functions, helpers and informations relative to the current xml node to various internal object
    /// </summary>
    internal interface IXMLInternalContext
    {
        /// <summary>
        /// Gets the static class data associated with the XML context.
        /// </summary>
        XMLStaticClassData? StaticClassData { get; }

        IXMLOptions Options { get; }

        /// <summary>
        /// Gets the custom data associated with the XML context.
        /// </summary>
        IDictionary<string, object> CustomData { get; }

        /// <summary>
        /// Gets the current instance being processed.
        /// </summary>
        object? CurrentInstance { get; }

        /// <summary>
        /// Gets the format provider for the XML context.
        /// </summary>
        IFormatProvider? FormatProvider { get; }

        /// <summary>
        /// Converts a string value to the specified target type.
        /// </summary>
        /// <param name="valueString">The string value to convert.</param>
        /// <param name="TargetType">The target type to convert to.</param>
        /// <param name="result">The converted result.</param>
        /// <returns>True if the conversion was successful; otherwise, false.</returns>
        bool Convert(string valueString, Type TargetType, [MaybeNullWhen(false)][NotNullWhen(true)] out object? result);

        /// <summary>
        /// Converts a value back to a string representation.
        /// </summary>
        /// <param name="value">The value to convert back.</param>
        /// <param name="TargetType">The target type of the value.</param>
        /// <param name="result">The string representation of the value.</param>
        /// <returns>True if the conversion was successful; otherwise, false.</returns>
        bool ConvertBack(object value, Type TargetType, [MaybeNullWhen(false)][NotNullWhen(true)] out string? result);

        /// <summary>
        /// Gets the deserializer context associated with the XML context.
        /// </summary>
        IXMLSerializationHandler? Handler { get; }

        /// <summary>
        /// Looks up an attribute in the XML context.
        /// </summary>
        /// <param name="nodeName">The name of the node.</param>
        /// <param name="attributeName">The name of the attribute.</param>
        /// <param name="attributeValue">The value of the attribute.</param>
        /// <param name="targetClass">The target class type.</param>
        /// <param name="result">The result of the lookup.</param>
        /// <returns>True if the lookup was successful; otherwise, false.</returns>
        bool Lookup_FromAttribute(string nodeName, string attributeName, string attributeValue, Type targetClass, [MaybeNullWhen(false)][NotNullWhen(true)] out object? result);
    }

    public struct ElementMappingInfo
    {
        public DeserializationPolicy Policy { get; internal set; }
        public Type? TargetType { get; internal set; }
        public AggregationPolicy AggregateMultipleDefinitions { get; internal set; }

        public ElementMappingInfo(DeserializationPolicy policy, Type? typeToCreate, AggregationPolicy aggregateMultipleDefinitions = AggregationPolicy.NoAggregation)
        {
            this.Policy = policy;
            this.TargetType = typeToCreate;
            this.AggregateMultipleDefinitions = aggregateMultipleDefinitions;
        }
    }
    public interface IXMLSerializationHandler
    {
        bool InfoForNode(IXMLState state, string nodeName, IReadOnlyDictionary<string, string> attributes, out ElementMappingInfo info);
        //***************************************************************************//

        //Domain: object creation and initiazation
        bool OverrideCreation(IXMLState state, Type t, [MaybeNullWhen(false)][NotNullWhen(true)] out object? result);
        void InjectDependencies(IXMLState state, object newObject);

        //*****************//
        //Domain: Lookup for existing objects

        bool Lookup_FromTextContent(IXMLState state, string nodeName, string TextContent, Type targetClass, [MaybeNullWhen(false)][NotNullWhen(true)] out object? result);
        bool Lookup_FromAttribute(IXMLState state, string nodeName, string attributeName, string attributeValue, Type targetClass, [MaybeNullWhen(false)][NotNullWhen(true)] out object? result);
        bool Lookup_FromAttributes(IXMLState state, string nodeName, IReadOnlyDictionary<string, string> attributes, Type targetClass, [MaybeNullWhen(false)][NotNullWhen(true)] out object? result);

        //*****************//
        //Domain: Conversion to and from string
        bool HasConversionFromString(Type targetType, [MaybeNullWhen(false)][NotNullWhen(true)] out ConvertFromString? deleg);
        bool HasConversionToString(Type targetType, [MaybeNullWhen(false)][NotNullWhen(true)] out ConvertToString? deleg);
        //***************************************************************************//

        void Finalized(IXMLState state, string nodeName, object result);

        //***************************************************************************//
        //Domain: Lookup for existing objects

        bool GetLookupAttributes(IXMLState state, string parentNode, string targetNode, object item, [MaybeNullWhen(false)][NotNullWhen(true)] out IReadOnlyDictionary<string, string>? attributes);
        bool GetLookupAttribute(IXMLState state, string nodeName, string attributeName, Type targetClass, object value, [MaybeNullWhen(false)][NotNullWhen(true)] out string? result);
        bool GetLookupTextContent(IXMLState state, string nodeName, Type targetClass, object value, [MaybeNullWhen(false)][NotNullWhen(true)] out string? result);

    }
}
