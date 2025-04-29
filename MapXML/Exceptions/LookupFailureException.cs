using System;

namespace MapXML
{
#pragma warning disable CA1051
    public class LookupFailureException : Exception
    {
        public LookupFailureException()
        {
            InnerExceptions = Array.Empty<Exception>();
        }
        public readonly Exception[] InnerExceptions;
        public LookupFailureException(params Exception[] innerExceptions) : base("The lookup process was unsuccessful", innerExceptions[0])
        {
            InnerExceptions = innerExceptions;
        }
    }

}
