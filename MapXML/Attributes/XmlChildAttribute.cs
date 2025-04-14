using System.Runtime.CompilerServices;

namespace MapXML.Attributes
{
    public class XmlChildAttribute : AbstractXMLMemberAttribute
    {
        public XmlChildAttribute([CallerMemberName] string xmlAttributeName = "", DeserializationPolicy policy = DeserializationPolicy.Create)
             : base(xmlAttributeName, XmlSourceType.Child, policy)
        {

        }
    }
}
