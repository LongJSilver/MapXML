using System;

namespace MapXML
{
    public class LookupFailureException : Exception
    {
        public LookupFailureException()
        {
            InnerExceptions = new Exception[0];
        }
        public readonly Exception[] InnerExceptions;
        public LookupFailureException(params Exception[] innerExceptions) : base("The lookup process was unsuccessful", innerExceptions[0])
        {
            InnerExceptions = innerExceptions;
        }
    }

}
