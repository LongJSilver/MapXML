using System;
using System.Runtime.Serialization;


namespace MapXML
{
    public class UnhandledNodeException : SerializationException
    {
        internal UnhandledNodeException(string NodeName, int Level, string XMLPath
            ) : base(NodeName, Level, XMLPath, "The deserializer encountered a node without context. Either define context for this Node at the parent node or through the handler, or allow unhandled nodes in the deserialization options.")
        {
        }
    }
}