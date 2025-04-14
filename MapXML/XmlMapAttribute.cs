using MapXML.Attributes;

namespace MapXML
{
    public class XmlMapAttribute : AbstractXMLMemberAttribute
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
        public XmlMapAttribute(string xmlAttributeName, XmlSourceType type, KeySourceTypes KeySourceType, string KeySourceName, string ValueSourceName = "")
            : this(xmlAttributeName, DeserializationPolicy.Create, type, KeySourceType, KeySourceName, ValueSourceName)
        {
        }
        public XmlMapAttribute(string xmlAttributeName, DeserializationPolicy policy, XmlSourceType type, KeySourceTypes KeySourceType, string KeySourceName, string valueSourceName = "")
            : base(xmlAttributeName, type, policy)
        {
            this.KeySourceName = KeySourceName;
            this.KeySourceType = KeySourceType;
            this.ValueSourceName = valueSourceName;
        }
    }
}
