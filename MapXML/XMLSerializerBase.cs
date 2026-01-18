using MapXML.Behaviors;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Xml;

namespace MapXML
{
    public interface IXMLOptions
    {
        /// <summary>
        /// IF True, allows the Serializer/Deserializer to process Fields and Properties 
        /// which have no explicit XMLMemberAttribute defined. These fields and properties
        /// will be treated as attributes of the parent node, with the same name as the field itself
        /// </summary>
        public bool AllowImplicitFields { get; }

        CultureInfo? Culture { get; }
    }
    public interface IXMLOptionsBuilder<T> where T : IXMLOptionsBuilder<T>
    {
        /// <summary>
        /// IF True, allows the Serializer/Deserializer to process Fields and Properties 
        /// which have no explicit XMLMemberAttribute defined. These fields and properties
        /// will be treated as attributes of the parent node, with the same name as the field itself
        /// </summary>
        public T AllowImplicitFields(bool b);
        public T WithCulture(string s);
        public T WithCulture(CultureInfo s);
        public T WithDefaultCulture();
    }

    public abstract class XMLSerializerBase
    {
        public static bool IsValidXMLNodeName(string? nodeName)
        {
            if (string.IsNullOrWhiteSpace(nodeName)) return false;
            try
            {
                XmlConvert.VerifyName(nodeName!);
                return true; // Valid XML node name
            }
            catch (XmlException)
            {
                return false; // Invalid XML node name
            }
        }

        protected abstract class AbstractOptionsBuilder<T> : IXMLOptionsBuilder<T>, IXMLOptions where T : IXMLOptionsBuilder<T>
        {
            protected AbstractOptionsBuilder(IXMLOptions? CopyFrom = null)
            {
                if (CopyFrom != null)
                {
                    AllowImplicitFields = CopyFrom.AllowImplicitFields;
                    Culture = CopyFrom.Culture;
                }
            }

            public CultureInfo? Culture { get; private set; }
            public bool AllowImplicitFields { get; private set; }
            T IXMLOptionsBuilder<T>.AllowImplicitFields(bool b)
            {
                AllowImplicitFields = b;
                return (T)(object)this;
            }

            T IXMLOptionsBuilder<T>.WithCulture(string s)
            {
                Culture = CultureInfo.GetCultureInfo(s);
                return (T)(object)this;
            }

            T IXMLOptionsBuilder<T>.WithCulture(CultureInfo s)
            {
                Culture = s;
                return (T)(object)this;
            }

            T IXMLOptionsBuilder<T>.WithDefaultCulture()
            {
                Culture = null;
                return (T)(object)this;
            }
        }

        public const string FormatProviderAttributeName = "xml.format";

        private XMLNodeBehaviorProfile? _contextStack;
        internal XMLNodeBehaviorProfile ContextStack => _contextStack!;
        protected IXMLOptions Options { get; }
        public string CurrentNodeName => _path.Count == 0 ? "" : _path.Peek().CurrentNodeName;
        public int CurrentLevel => _path.Count - 1;
        protected int LogicalLevelOffset { get; set; }
        public string CurrentPath => _path.Count == 0 ? "" : _path.Peek().Path;
        public IXMLSerializationHandler? Handler { get; private set; }

        public abstract void Run();

        internal void ClearStack()
        {
            _contextStack = null;
            _path.Clear();
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
        protected XMLSerializerBase(IXMLOptions options, IXMLSerializationHandler? handler)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            _path = new Stack<PathElement>();
            this.Handler = handler;
        }

        internal void Push(XMLNodeBehaviorProfile c)
        {
            if (c != null)
                c.Parent = _contextStack;

            _contextStack = c;
            c.XMLLevel = CurrentLevel;
            c.LogicalLevel = CurrentLevel + LogicalLevelOffset;
            Push(c.NodeName ?? "");
        }

        internal void Push(string NodeName)
        {
            if (_path.Count == 0)
            {
                _path.Push(new PathElement(NodeName, 1, 0, NodeName));
            }
            else
            {
                PathElement currentElement = _path.Pop();
                currentElement.CurrentChildrenIndex++;
                _path.Push(currentElement);
                string newPath = $"{currentElement.Path}/[{currentElement.CurrentChildrenIndex}:{NodeName}]";
                _path.Push(new PathElement(NodeName, currentElement.CurrentLevel + 1, 0, newPath));
            }
        }

        [DoesNotReturn]
        protected void Throw(string message, params Exception[] previousExc)
        {
            throw new SerializationException(this.CurrentNodeName, this.CurrentLevel, this.CurrentPath,
                              message, previousExc);
        }
        [DoesNotReturn]
        protected void Throw(string message, Exception innerExc, params Exception[] previousExc)
        {
            throw new SerializationException(this.CurrentNodeName, this.CurrentLevel, this.CurrentPath, message,
                              inner: innerExc, previousExc);
        }

    }
}
