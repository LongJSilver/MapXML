using System;

namespace MapXML.Attributes
{
    /// <summary>
    /// This attribute is used to map XML Content into a IDictionary<> typed member, specifically as the dictionary values.
    /// The calller must define the source of the dictionary Key for each value.
    /// </summary>
#pragma warning disable CA1051 // Make an exception for fields in attribute subclasses
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class XMLMapAttribute : AbstractXMLMemberAttribute
    {
        public enum KeySourceTypes
        {
            ObjectMember,
            ParentMember,
            NodeAttribute
        }

        public readonly KeySourceTypes KeySourceType;
        public readonly string KeySourceName;
        public readonly string ValueSourceName;
        public XMLMapAttribute(string xmlAttributeName, XMLSourceType type, KeySourceTypes KeySourceType, string KeySourceName, string ValueSourceName = "")
            : this(xmlAttributeName, DeserializationPolicy.Create, type, KeySourceType, KeySourceName, ValueSourceName)
        {
        }
        public XMLMapAttribute(string xmlAttributeName, DeserializationPolicy policy, XMLSourceType type, KeySourceTypes KeySourceType, string KeySourceName, string valueSourceName = "")
            : base(xmlAttributeName, type, policy)
        {
            this.KeySourceName = KeySourceName;
            this.KeySourceType = KeySourceType;
            this.ValueSourceName = valueSourceName;
        }
    }
}
