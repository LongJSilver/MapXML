using MapXML.Behaviors;
using MapXML.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml;

namespace MapXML
{
    internal class PrimitiveDictionaryEntry
    {
        public readonly string KeyName;
        public readonly string Key;
        public readonly string ValueName;
        public readonly string Value;

        public PrimitiveDictionaryEntry(string keyName, string key, string valueName, string value)
        {
            this.KeyName = keyName;
            this.Key = key;
            this.ValueName = valueName;
            this.Value = value;
        }
    }
    public class XMLSerializer : XMLSerializerBase
    {
        IXMLSerializationHandler? _handler;
        public String? RootNode { get; }
        public String? FormatString { get; }
        private readonly StringBuilder _sb = new StringBuilder();
        public String Result => _sb.ToString();


        private List<(string name, object item, string? FormatName, int externalOrder, int addOrder)> _firstLevelObjects =
            new List<(string name, object item, string? FormatName, int externalOrder, int addOrder)>();

        public XMLSerializer(IXMLSerializationHandler? context, string? UseFormat = null, string? OptionalRootNode = null)
        {
            this._handler = context;
            this.FormatString = UseFormat;
            RootNode = OptionalRootNode;
        }

        public XMLSerializer(IXMLSerializationHandler? context, string nodeName, object subject, string? UseFormat = null, string? OptionalRootNode = null)
            : this(context, UseFormat, OptionalRootNode)
        {
            AddItem(nodeName, subject, formatName: UseFormat);
        }

        public void AddItem(string NodeName, object item, int order = 0, string? formatName = null)
        {
            _firstLevelObjects.Add((NodeName, item, formatName, order, _firstLevelObjects.Count));
        }

        internal void CheckForFormatAttribute(IDictionary<String, String> attributes)
        {
            if (attributes.TryGetValue(FORMAT_PROVIDER_ATTRIBUTE, out string formatName))
            {
                ContextStack.Format = CultureInfo.GetCultureInfo(formatName);
            }
        }

        public override void Run()
        {
            CultureInfo cult;
            if (this.FormatString != null)
            {
                cult = CultureInfo.GetCultureInfo(FormatString);
            }
            else
            {
                cult = (CultureInfo.CurrentCulture);
            }
            _sb.Clear();
            ClearStack();
            using (var stringWriter = new StringWriter_WithEncoding(_sb, cult, Encoding.UTF8))
            using (var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 }))
            {
                Push(XMLNodeBehaviorProfile.CreateTopNode(_handler, RootNode, null, true, this.AllowImplicitFields));

                ContextStack!.Format = cult;

                if (RootNode != null)
                {
                    xmlWriter.WriteStartElement(RootNode);
                    xmlWriter.WriteAttributeString(FORMAT_PROVIDER_ATTRIBUTE, ContextStack.Format.Name);
                    Push(RootNode);
                }

                IEnumerable<(string name, object item, string? formatName)> toSerialize =
                    _firstLevelObjects
                        .OrderBy(s => s.externalOrder)
                        .ThenBy(s => s.addOrder)
                        .Select(s => (s.name, s.item, s.FormatName));

                foreach (var firstLevelItem in toSerialize)
                {
                    Push(XMLNodeBehaviorProfile.CreateSerializationNode((IXMLSerializationHandler?)null, this.AllowImplicitFields,
                        firstLevelItem.name, firstLevelItem.item.GetType(),
                        firstLevelItem.item, firstLevelItem.formatName));
                    WriteCurrentNodeCreate(xmlWriter);
                    Pop();
                }

                if (RootNode != null)
                {
                    xmlWriter.WriteEndElement();
                }
            }
        }


        private void WriteCurrentNodeCreate(XmlWriter xmlWriter)
        {
            Dictionary<string, string> attributes = ContextStack.GetAttributesToSerialize();
            xmlWriter.WriteStartElement(this.ContextStack.NodeName);

            if (ContextStack.Format != null)
            {
                attributes.Add(FORMAT_PROVIDER_ATTRIBUTE, ContextStack.Format.Name);
            }

            foreach (var attribute in attributes)
            {
                xmlWriter.WriteAttributeString(attribute.Key, attribute.Value);
            }

            string? textContent = ContextStack.GetTextContentToSerialize();
            List<(string nodeName, object child, Type targetType, DeserializationPolicy policy)> children = ContextStack.GetAllChildrenToSerialize();

            bool hasChildren = children.Count != 0;
            bool hasTextContent = !String.IsNullOrEmpty(textContent);
            bool hasNestedContent = hasChildren || hasTextContent;

            if (hasNestedContent)
            {
                if (hasChildren)
                {
                    foreach ((string nodeName, object child, Type targetType, DeserializationPolicy policy) in children)
                    {
                        if (policy == DeserializationPolicy.Create)
                        {
                            Push(XMLNodeBehaviorProfile.CreateSerializationNode(null, this.AllowImplicitFields, nodeName, targetType, child));
                            WriteCurrentNodeCreate(xmlWriter);
                            Pop();
                        }
                        else
                        {
                            if (false)
                            {
                                string TextContent = "";
                                WriteLookupTextContentChild(xmlWriter, nodeName, TextContent);
                            }
                            else
                            {

                                if (!this.ContextStack.GetLookupAttributes(nodeName, child, targetType, out IReadOnlyDictionary<string, string>? lookUpAttributes))
                                {
                                    throw new ArgumentException($"Cannot find lookup attributes for {nodeName} within {ContextStack.NodeName}");
                                }

                                WriteLookupChild(xmlWriter, nodeName, lookUpAttributes);
                            }
                        }
                    }
                }

                if (hasTextContent)
                {
                    xmlWriter.WriteString(textContent);
                }

                xmlWriter.WriteEndElement();
            }
            else
            {
                xmlWriter.WriteEndElement();
            }
        }

        private void WriteLookupTextContentChild(XmlWriter xmlWriter, string nodeName, string TextContent)
        {
            xmlWriter.WriteStartElement(nodeName);
            xmlWriter.WriteString(TextContent);
            xmlWriter.WriteEndElement();
        }
        private void WriteLookupChild(XmlWriter xmlWriter, string nodeName, IReadOnlyDictionary<string, string> attributes)
        {
            xmlWriter.WriteStartElement(nodeName);

            foreach (var attribute in attributes)
            {
                xmlWriter.WriteAttributeString(attribute.Key, attribute.Value);
            }

            xmlWriter.WriteEndElement();
        }
    }


}
