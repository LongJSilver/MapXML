using System.Runtime.CompilerServices;

namespace MapXML.Attributes
{
    public class XmlTextContentAttribute : AbstractXMLMemberAttribute
    {
        public XmlTextContentAttribute([CallerMemberName] string xmlAttributeName = "", DeserializationPolicy policy = DeserializationPolicy.Create)
             : base(xmlAttributeName, XmlSourceType.TextContent, policy)
        {

        }
    }
}
