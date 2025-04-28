using System;

namespace MapXML.Attributes
{
    [System.AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method)]
    public abstract class AbstractXMLMemberAttribute : Attribute
    {
        public readonly string NodeName;
        internal readonly DeserializationPolicy Policy;
        public readonly XMLSourceType SourceType;
        public int SerializationOrder { get; set; } = int.MaxValue;
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
