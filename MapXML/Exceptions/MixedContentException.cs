namespace MapXML
{
    public class MixedContentException : SerializationException
    {
        internal MixedContentException(string NodeName, int Level, string XMLPath
            ) : base(NodeName, Level, XMLPath, "Mixed content is not allowed. Text content is allowed only once, regardless of location. There cannot be child nodes mixed in-between.")
        {
        }
    }
}
