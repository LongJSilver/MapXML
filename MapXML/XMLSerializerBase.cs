using MapXML.Behaviors;
using System.Collections.Generic;

namespace MapXML
{
    public abstract class XMLSerializerBase
    {
        public const string FORMAT_PROVIDER_ATTRIBUTE = "xml.format";
        private XMLNodeBehaviorProfile? _contextStack;
        internal XMLNodeBehaviorProfile ContextStack => _contextStack!;
        /// <summary>
        /// IF True, allowes the Serialized/Deserialized process to process Fields and Properties 
        /// which have no explicit XmlMemberAttribute defined. These fields and properties
        /// will be serialized as attributes of the parent node, with the same name as the field itself
        /// </summary>
        public bool AllowImplicitFields { get; set; } = true;

        public string CurrentNodeName => _path.Peek().CurrentNodeName;
        public int CurrentLevel => _path.Peek().CurrentLevel;
        public string CurrentPath => _path.Peek().Path;


        public abstract void Run();

        internal void ClearStack()
        {
            _contextStack = null;
        }
        internal void Pop()
        {
            _contextStack = _contextStack?.Parent;
            _path.Pop();
        }
        private struct PathElement
        {
            public readonly string CurrentNodeName;
            public readonly int CurrentLevel;
            public int CurrentChildrenIndex;
            public readonly string Path;

            public PathElement(string CurrentNodeName, int currentLevel, int currentSibling, string path)
            {
                this.CurrentNodeName = CurrentNodeName;
                this.CurrentLevel = currentLevel;
                this.CurrentChildrenIndex = currentSibling;
                this.Path = path;
            }
        }
        private readonly Stack<PathElement> _path;

        protected XMLSerializerBase()
        {
            _path = new Stack<PathElement>();
            _path.Push(new PathElement("", -1, 0, "/"));
        }

        internal void Push(XMLNodeBehaviorProfile c)
        {
            if (c != null)
                c.Parent = _contextStack;
            _contextStack = c;
            if (c?.NodeName != null)
            {
                c.Level = CurrentLevel;
                Push(c.NodeName);
            }
        }

        internal void Push(string NodeName)
        {
            PathElement currentElement = _path.Pop();
            currentElement.CurrentChildrenIndex++;
            _path.Push(currentElement);
            string newPath = $"{currentElement.Path}[{currentElement.CurrentChildrenIndex}]{NodeName}/";
            _path.Push(new PathElement(NodeName, currentElement.CurrentLevel + 1, 0, newPath));
        }


    }
}
