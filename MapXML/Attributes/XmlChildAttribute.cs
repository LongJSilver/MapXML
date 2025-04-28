using System.Runtime.CompilerServices;

namespace MapXML.Attributes
{
    public class XMLChildAttribute : AbstractXMLMemberAttribute
    {
        public XMLChildAttribute([CallerMemberName] string xmlAttributeName = "", DeserializationPolicy policy = DeserializationPolicy.Create)
             : base(xmlAttributeName, XMLSourceType.Child, policy)
        {

        }
    }
}
