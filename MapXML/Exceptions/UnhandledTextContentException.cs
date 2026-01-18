using System;
using System.Runtime.Serialization;


namespace MapXML
{
    public class UnhandledTextContentException : SerializationException
    {
        internal UnhandledTextContentException(string NodeName, int Level, string XMLPath
            ) : base(NodeName, Level, XMLPath, "The deserializer encountered Text content without context. Either define context for this text content at the parent node or through the handler, or allow unhandled text content in the deserialization options.")
        {
        }
    }
}