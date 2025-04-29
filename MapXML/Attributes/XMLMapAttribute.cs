namespace MapXML.Attributes
{
#pragma warning disable CA1051 // Make an exception for fields in attribute subclasses
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
