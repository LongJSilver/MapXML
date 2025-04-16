﻿using MapXML.Behaviors;
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


    public interface ISerializationOptions : IXMLOptions
    {
        /// <summary>
        /// Whenever a lookup node with ONE single attribute 
        /// needs to be written, opt instead for writing the lookup value as text within the node.
        /// </summary>
        public bool PreferTextNodesForLookups { get; }

        /// <summary>
        /// The name of a root node to be included in the serialization process.
        /// If there is only one 'first level' node, the <see cref="AdditionalRootNode"/> is optional, 
        /// otherwise it is required and its absence will cause an exception to be thrown.
        /// </summary>
        public string? AdditionalRootNode { get; }
    }

    public interface ISerializationOptionsBuilder : IXMLOptionsBuilder<ISerializationOptionsBuilder>
    {
        /// <summary>
        /// 
        /// Whenever a lookup node with ONE single attribute 
        /// needs to be written, opt instead for writing the lookup value as text within the node.
        /// <para/>DEFAULT: false
        /// </summary>
        public ISerializationOptionsBuilder PreferTextNodesForLookups(bool b);

        /// <summary>
        /// The name of a root node to be included in the serialization process.
        /// If there is only one 'first level' node, the <see cref="AdditionalRootNode"/> is optional, 
        /// otherwise it is required and its absence will cause an exception to be thrown.
        /// <para/>DEFAULT: null
        /// </summary>
        public ISerializationOptionsBuilder WithAdditionalRootNode(string s);

        ISerializationOptions Build();
    }

    public class XMLSerializer : XMLSerializerBase
    {
        public static ISerializationOptionsBuilder OptionsBuilder() => new DefaultOptions();

        private class DefaultOptions : AbstractOptionsBuilder<ISerializationOptionsBuilder>, ISerializationOptionsBuilder, ISerializationOptions
        {
            public bool PreferTextNodesForLookups { get; private set; } = false;
            public string? AdditionalRootNode { get; private set; } = null;

            public ISerializationOptions Build() => this;

            ISerializationOptionsBuilder ISerializationOptionsBuilder.PreferTextNodesForLookups(bool b)
            {
                PreferTextNodesForLookups = b;
                return this;
            }

            ISerializationOptionsBuilder ISerializationOptionsBuilder.WithAdditionalRootNode(string s)
            {
                if (IsValidXmlNodeName(s))
                {
                    AdditionalRootNode = s;
                }
                else
                    throw new ArgumentException($"<{s}> is not a valid xml node name");
                return this;
            }
        }


        IXMLSerializationHandler? _handler;
        private readonly StringBuilder _sb = new StringBuilder();
        public String Result => _sb.ToString();


        private List<(string name, object item, CultureInfo? culture, int externalOrder, int addOrder)> _firstLevelObjects =
            new List<(string name, object item, CultureInfo? culture, int externalOrder, int addOrder)>();

        public new ISerializationOptions Options => (ISerializationOptions)base.Options;
        public XMLSerializer(ISerializationOptions? options = null)
            : this(null, options ?? new DefaultOptions())
        { }

        public XMLSerializer(IXMLSerializationHandler? handler, ISerializationOptions? options = null)
            : base(options ?? new DefaultOptions())
        {
            this._handler = handler;
        }

        public void AddItem(string NodeName, object item, int order = 0, string? formatName = null)
        {
            CultureInfo? ci = this.Options.Culture;
            if (formatName != null)
            {
                ci = CultureInfo.GetCultureInfo(formatName);
            }
            _firstLevelObjects.Add((NodeName, item, ci, order, _firstLevelObjects.Count));
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

            if (_firstLevelObjects.Count > 1 && IsValidXmlNodeName(Options.AdditionalRootNode!))
            {
                throw new NotSupportedException($"Cannot serialize multiple first level objects without a root node; please specify a VALID root node name through the {nameof(ISerializationOptions.AdditionalRootNode)} option.");
            }


            cult = this.Options.Culture ?? CultureInfo.CurrentCulture;
            try
            {


            _sb.Clear();
            ClearStack();
            using (var stringWriter = new StringWriter_WithEncoding(_sb, cult, Encoding.UTF8))
            using (var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 }))
            {
                Push(XMLNodeBehaviorProfile.CreateTopNode(_handler, this.Options, Options.AdditionalRootNode, null, true));

                ContextStack!.Format = cult;

                if (Options.AdditionalRootNode != null)
                {
                    xmlWriter.WriteStartElement(Options.AdditionalRootNode);
                    xmlWriter.WriteAttributeString(FORMAT_PROVIDER_ATTRIBUTE, ContextStack.Format.Name);
                    Push(Options.AdditionalRootNode);
                }

                IEnumerable<(string name, object item, string? formatName)> toSerialize =
                    _firstLevelObjects
                        .OrderBy(s => s.externalOrder)
                        .ThenBy(s => s.addOrder)
                        .Select(s => (s.name, s.item, s.culture?.Name));

                foreach (var firstLevelItem in toSerialize)
                {
                        Push(XMLNodeBehaviorProfile.CreateSerializationNode(null, this.Options,
                        firstLevelItem.name, firstLevelItem.item.GetType(),
                        firstLevelItem.item, firstLevelItem.formatName));
                    WriteCurrentNodeCreate(xmlWriter);
                    Pop();
                }

                if (Options.AdditionalRootNode != null)
                {
                    xmlWriter.WriteEndElement();
                }
            }
        }
            catch (Exception e)
            {
                Throw("Serialization failed", e);
#if NETSTANDARD2_0
                // just to let the compiler know we can't get past this point
                // Since the [DoesNotReturn] attribute is not available in .NET Standard 2.0
                return;
#endif
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
                            Push(XMLNodeBehaviorProfile.CreateSerializationNode(null, this.Options, nodeName, targetType, child));
                            WriteCurrentNodeCreate(xmlWriter);
                            Pop();
                        }
                        else
                        {
                            if (!this.ContextStack.GetLookupAttributes(nodeName, child, targetType, out IReadOnlyDictionary<string, string>? lookUpAttributes))
                            {
                                throw new ArgumentException($"Cannot find lookup attributes for {nodeName} within {ContextStack.NodeName}");
                            }
                            if (this.Options.PreferTextNodesForLookups && lookUpAttributes.Count == 1)
                            {
                                WriteLookupTextContentChild(xmlWriter, nodeName, lookUpAttributes.First().Value);
                            }
                            else
                            {
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
