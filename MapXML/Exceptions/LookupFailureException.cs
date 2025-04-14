using System;

namespace MapXML
{
    public class LookupFailureException : Exception
    {
        public LookupFailureException()
        {
        }
        public readonly Exception[] InnerExceptions;
        public LookupFailureException(params Exception[] innerExceptions) : base("The lookup process was unsuccessful", innerExceptions[0])
        {
        }
    }
 
}
