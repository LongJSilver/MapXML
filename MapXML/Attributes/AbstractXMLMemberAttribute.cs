using System;

namespace MapXML.Attributes
{
#pragma warning disable CA1051 // Make an exception for fields in attribute subclasses
    public abstract class AbstractXMLMemberAttribute : Attribute
    {
        public readonly string NodeName;
        internal readonly DeserializationPolicy Policy;
        public readonly XMLSourceType SourceType;
        public int SerializationOrder { get; set; } = byte.MaxValue;
        public bool CanSerialize { get; set; } = true;
        public bool CanDeserialize { get; set; } = true;
        internal AbstractXMLMemberAttribute(string xmlAttributeName,
                                    XMLSourceType sourceType = XMLSourceType.Attribute,
                                    DeserializationPolicy type = DeserializationPolicy.Create)
        {
            this.NodeName = xmlAttributeName ?? throw new ArgumentNullException(nameof(xmlAttributeName));
            this.Policy = type;
            this.SourceType = sourceType;
        }
    }
}
