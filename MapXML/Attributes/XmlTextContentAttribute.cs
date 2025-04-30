using System;
using System.Runtime.CompilerServices;

namespace MapXML.Attributes
{
    [System.AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method)]
    public class XMLTextContentAttribute : AbstractXMLMemberAttribute
    {
        public XMLTextContentAttribute([CallerMemberName] string xmlAttributeName = "", DeserializationPolicy policy = DeserializationPolicy.Create)
             : base(xmlAttributeName, XMLSourceType.TextContent, policy)
        {

        }
    }
}
