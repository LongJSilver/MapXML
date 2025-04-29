using System;

namespace MapXML
{
#pragma warning disable CA1051
    public class XMLSerializationException : Exception
    {
        public readonly string NodeName;
        public readonly string Path;
        public readonly int Level;

        public readonly Exception[] PreviousExceptions;

        private static string CreateMessage(string NodeName, int Level, string XMLPath, string Message)
        {
            return $"<{XMLPath}> - {Message}";
        }

        internal XMLSerializationException(string NodeName, int Level, string XMLPath, Exception inner, params Exception[] previousExceptions)
            : base(CreateMessage(NodeName, Level, XMLPath, inner?.Message ?? string.Empty), inner)
        {
            this.NodeName = NodeName;
            this.Level = Level;
            this.Path = XMLPath;
            this.PreviousExceptions = previousExceptions;
        }

        internal XMLSerializationException(string NodeName, int Level, string XMLPath, string Message, Exception inner, params Exception[] previousExceptions)
            : base(CreateMessage(NodeName, Level, XMLPath, !string.IsNullOrEmpty(Message) ? Message : (inner.Message ?? string.Empty)), inner)
        {
            this.NodeName = NodeName;
            this.Level = Level;
            this.Path = XMLPath;
            this.PreviousExceptions = previousExceptions;
        }

        internal XMLSerializationException(string NodeName, int Level, string XMLPath, string Message, params Exception[] previousExceptions) : base(CreateMessage(NodeName, Level, XMLPath, Message))
        {
            this.NodeName = NodeName;
            this.Level = Level;
            this.Path = XMLPath;
            this.PreviousExceptions = previousExceptions;
        }
    }

    public class XMLMixedContentException : XMLSerializationException
    {
        internal XMLMixedContentException(string NodeName, int Level, string XMLPath
            ) : base(NodeName, Level, XMLPath, "Mixed content is not allowed. Text content is allowed only once, regardless of location. There cannot be child nodes mixed in-between.")
        {
        }
    }
}
