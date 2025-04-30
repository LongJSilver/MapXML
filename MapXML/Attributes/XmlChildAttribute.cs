using System;
using System.Runtime.CompilerServices;

namespace MapXML.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = true)]
    public class XMLChildAttribute : AbstractXMLMemberAttribute
    {
        public XMLChildAttribute([CallerMemberName] string xmlAttributeName = "", DeserializationPolicy policy = DeserializationPolicy.Create)
             : base(xmlAttributeName, XMLSourceType.Child, policy)
        {

        }
    }
}
