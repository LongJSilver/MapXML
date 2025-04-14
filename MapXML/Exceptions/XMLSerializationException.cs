using System;

namespace MapXML
{
    public class XMLSerializationException : Exception
    {
        public readonly string NodeName;
        public readonly string Path;
        public readonly int Level;

        public readonly Exception[] PreviousExceptions;

        private static string CreateMessage(string NodeName, int Level, string XmlPath, string Message)
        {
            return $"<{XmlPath}> - {Message}";
        }

        internal XMLSerializationException(string NodeName, int Level, string XmlPath, Exception inner, params Exception[] previousExceptions)
            : base(CreateMessage(NodeName, Level, XmlPath, inner?.Message ?? string.Empty), inner)
        {
            this.NodeName = NodeName;
            this.Level = Level;
            this.Path = XmlPath;
            this.PreviousExceptions = previousExceptions;
        }

        internal XMLSerializationException(string NodeName, int Level, string XmlPath, string Message, params Exception[] previousExceptions) : base(CreateMessage(NodeName, Level, XmlPath, Message))
        {
            this.NodeName = NodeName;
            this.Level = Level;
            this.Path = XmlPath;
            this.PreviousExceptions = previousExceptions;
        }
    }

    public class XMLMixedContentException : XMLSerializationException
    {
        internal XMLMixedContentException(string NodeName, int Level, string XmlPath
            ) : base(NodeName, Level, XmlPath, "Mixed content is not allowed. Text content is allowed only once, regardless of location. There cannot be child nodes mixed in-between.")
        {
        }
    }
}
