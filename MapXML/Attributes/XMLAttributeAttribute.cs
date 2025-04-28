using System.Runtime.CompilerServices;

namespace MapXML.Attributes
{
    public class XMLAttributeAttribute : AbstractXMLMemberAttribute
    {
        public XMLAttributeAttribute([CallerMemberName] string xmlAttributeName = "", DeserializationPolicy policy = DeserializationPolicy.Create)
            : base(xmlAttributeName, XMLSourceType.Attribute, policy)
        {

        }
    }
}
