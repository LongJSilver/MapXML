using System;
using System.Runtime.CompilerServices;

namespace MapXML.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = true)]
    public class XMLAttributeAttribute : AbstractXMLMemberAttribute
    {
        public XMLAttributeAttribute([CallerMemberName] string xmlAttributeName = "", DeserializationPolicy policy = DeserializationPolicy.Create)
            : base(xmlAttributeName, XMLSourceType.Attribute, policy)
        {

        }
    }
}
