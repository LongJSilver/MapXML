using System;

namespace MapXML
{
#pragma warning disable CA1051
    public class SerializationException : Exception
    {
        public readonly string NodeName;
        public readonly string Path;
        public readonly int Level;

        public readonly Exception[] PreviousExceptions;

        private static string CreateMessage(string NodeName, int Level, string XMLPath, string Message)
        {
            return $"<{XMLPath}> - {Message}";
        }

        internal SerializationException(string NodeName, int Level, string XMLPath, Exception inner, params Exception[] previousExceptions)
            : base(CreateMessage(NodeName, Level, XMLPath, inner?.Message ?? string.Empty), inner)
        {
            this.NodeName = NodeName;
            this.Level = Level;
            this.Path = XMLPath;
            this.PreviousExceptions = previousExceptions;
        }

        internal SerializationException(string NodeName, int Level, string XMLPath, string Message, Exception inner, params Exception[] previousExceptions)
            : base(CreateMessage(NodeName, Level, XMLPath, !string.IsNullOrEmpty(Message) ? Message : (inner.Message ?? string.Empty)), inner)
        {
            this.NodeName = NodeName;
            this.Level = Level;
            this.Path = XMLPath;
            this.PreviousExceptions = previousExceptions;
        }

        internal SerializationException(string NodeName, int Level, string XMLPath, string Message, params Exception[] previousExceptions) : base(CreateMessage(NodeName, Level, XMLPath, Message))
        {
            this.NodeName = NodeName;
            this.Level = Level;
            this.Path = XMLPath;
            this.PreviousExceptions = previousExceptions;
        }
    }
}
