using System.Runtime.CompilerServices;

namespace MapXML.Attributes
{
    public class XMLTextContentAttribute : AbstractXMLMemberAttribute
    {
        public XMLTextContentAttribute([CallerMemberName] string xmlAttributeName = "", DeserializationPolicy policy = DeserializationPolicy.Create)
             : base(xmlAttributeName, XMLSourceType.TextContent, policy)
        {

        }
    }
}
