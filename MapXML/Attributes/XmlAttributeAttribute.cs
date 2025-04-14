using System.Runtime.CompilerServices;

namespace MapXML.Attributes
{
    public class XmlAttributeAttribute : AbstractXMLMemberAttribute
    {
        public XmlAttributeAttribute([CallerMemberName] string xmlAttributeName = "", DeserializationPolicy policy = DeserializationPolicy.Create)
            : base(xmlAttributeName, XmlSourceType.Attribute, policy)
        {

        }
    }
}
