using System;

namespace MapXML.Attributes
{

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class XMLNonSerializedAttribute : Attribute
    {
    }
}
