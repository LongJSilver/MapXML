using System;
using System.Collections.Generic;
using System.Text;

namespace MapXML.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class XMLAllowImplicitAttribute : Attribute
    {

    }
}
