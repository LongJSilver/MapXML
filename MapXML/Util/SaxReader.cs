using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace MapXML.Utils
{

    public class SaxReader
    {
        public interface ISaxReaderContext
        {
            void NodeStart(string name, Dictionary<string, string> attributes);
            void NodeEnd(string name);
            void TextContent(string text);
        }

        public delegate void NodeStart(string name, Dictionary<string, string> attributes);
        public delegate void NodeEnd(string name);
        public delegate void TextContent(string text);
        private readonly Stack<string> _currentPath;
        private readonly string[] _LatestPaths;
        private int _NextFreePath = 0;
        public string ReadCurrentNodeAsText()
        {
            this.reader.MoveToElement();
            this.shouldCallEndNow = true;
            return this.reader.ReadOuterXml();
        }
        private bool shouldCallEndNow;
        public event NodeStart? OnNodeStart;
        public event NodeEnd? OnNodeEnd;
        public event TextContent? OnText;

        private XmlTextReader reader;
        public SaxReader(Stream xml)
        {
            this.reader = new XmlTextReader(xml);
            _currentPath = new Stack<string>();
            _LatestPaths = new string[10];

        }
        public SaxReader(Stream xml, ISaxReaderContext cont) : this(xml)
        {
            this.OnNodeStart += cont.NodeStart;
            this.OnNodeEnd += cont.NodeEnd;
            this.OnText += cont.TextContent;
        }

        public void Read()
        {
            while ((this.reader.Read()))
            {
                switch (this.reader.NodeType)
                {
                    case XmlNodeType.Element:
                        //' The node is an element.
                        string CurrentNodeName = this.reader.Name;
                        _currentPath.Push(CurrentNodeName);
                        //////////////////
                        _LatestPaths[(_NextFreePath++) % _LatestPaths.Length] = CurrentPath;
                        //////////////////

                        Dictionary<string, string> attributes = new Dictionary<string, string>();
                        this.shouldCallEndNow = this.reader.IsEmptyElement;
                        //' Read the attributes.
                        while ((this.reader.MoveToNextAttribute()))
                        {
                            attributes.Add(this.reader.Name, this.reader.Value);
                        }

                        this.OnNodeStart?.Invoke(CurrentNodeName, attributes);
                        if (this.shouldCallEndNow)
                        {
                            this.OnNodeEnd?.Invoke(CurrentNodeName);
                            _currentPath.Pop();
                            this.shouldCallEndNow = false;
                        }
                        break;
                    case XmlNodeType.CDATA:
                    case XmlNodeType.Text:
                        _currentPath.Push("[TEXT_CONTENT]");
                        //'Display the text in each element.
                        this.OnText?.Invoke(this.reader.Value);
                        _currentPath.Pop();
                        break;
                    case XmlNodeType.EndElement:
                        //'Display the end of the element.
                        this.OnNodeEnd?.Invoke(this.reader.Name);
                        _currentPath.Pop();
                        break;
                }
            }
        }
        public string CurrentPath
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (var elem in _currentPath.Reverse())

                {
                    if (sb.Length > 0)
                        sb.Append(".");
                    sb.Append(elem);
                }
                return sb.ToString();
            }
        }
        public void Close()
        {
            this.reader.Close();
        }
    }

}
