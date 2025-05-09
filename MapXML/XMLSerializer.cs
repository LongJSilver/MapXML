﻿using MapXML.Attributes;
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
    internal sealed class PrimitiveDictionaryEntry
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

    public enum AttributeOmissionPolicy : byte
    {
        /// <summary>
        /// Only omit values annotated with <see cref="XMLOmitAttribute"/>
        /// </summary>
        AsDictatedByCodeAnnotations = 0,
        /// <summary>
        /// Always omit null attributes (when attribute type is nullable) and attributes with default values (when attribute is of ValueType)
        /// </summary>
        AlwaysWhenDefault,
        /// <summary>
        /// Always omit null attributes (when attribute type is nullable) but not ValueType attributes with default values
        /// </summary>
        AlwaysWhenNull
    }
    public interface ISerializationOptions : IXMLOptions
    {
        /// <summary>
        /// Whenever a lookup node with ONE single attribute 
        /// needs to be written, opt instead for writing the lookup value as text within the node.
        /// <para/>DEFAULT: false
        /// </summary>
        public bool PreferTextNodesForLookups { get; }

        /// <summary>
        /// The name of a root node to be included in the serialization process.
        /// If there is only one 'first level' node, the <see cref="AdditionalRootNode"/> is optional, 
        /// otherwise it is required and its absence will cause an exception to be thrown.
        /// </summary>
        public string? AdditionalRootNode { get; }

        /// <summary>
        /// Whenever an attribute is null or empty, it will be omitted from the serialization process.
        /// <para/>DEFAULT: false
        /// </summary>
        AttributeOmissionPolicy AttributeOmissionPolicy { get; }
        /// <summary>
        /// <para/>DEFAULT: UTF8
        /// </summary>
        Encoding Encoding { get; }
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
        /// 
        /// if set to TRUE, Whenever an attribute is null or empty it will be omitted from the serialization process.
        /// 
        /// <para/>DEFAULT: false
        /// </summary>
        public ISerializationOptionsBuilder OmitAttributes(AttributeOmissionPolicy policy);

        /// <summary>
        /// The name of a root node to be included in the serialization process.
        /// If there is only one 'first level' node, the <see cref="AdditionalRootNode"/> is optional, 
        /// otherwise it is required and its absence will cause an exception to be thrown.
        /// <para/>DEFAULT: null
        /// </summary>
        public ISerializationOptionsBuilder WithAdditionalRootNode(string s);
        public ISerializationOptionsBuilder WithEncoding(Encoding e);
        public ISerializationOptionsBuilder WithEncoding(string e);

        ISerializationOptions Build();
    }

    public class XMLSerializer : XMLSerializerBase
    {
        public static ISerializationOptionsBuilder OptionsBuilder() => new DefaultOptions();

        private sealed class DefaultOptions : AbstractOptionsBuilder<ISerializationOptionsBuilder>, ISerializationOptionsBuilder, ISerializationOptions
        {
#pragma warning disable CA1805 // Let the default values be explicit
            public bool PreferTextNodesForLookups { get; private set; } = false;
            public AttributeOmissionPolicy AttributeOmissionPolicy { get; private set; } = AttributeOmissionPolicy.AsDictatedByCodeAnnotations;
            public string? AdditionalRootNode { get; private set;}
            public Encoding Encoding { get; private set; } = Encoding.UTF8;
#pragma warning restore CS1805 

            public ISerializationOptions Build() => this;

            ISerializationOptionsBuilder ISerializationOptionsBuilder.PreferTextNodesForLookups(bool b)
            {
                PreferTextNodesForLookups = b;
                return this;
            }
            ISerializationOptionsBuilder ISerializationOptionsBuilder.OmitAttributes(AttributeOmissionPolicy policy)
            {
                AttributeOmissionPolicy = policy;
                return this;
            }

            ISerializationOptionsBuilder ISerializationOptionsBuilder.WithAdditionalRootNode(string s)
            {
                if (IsValidXMLNodeName(s))
                {
                    AdditionalRootNode = s;
                }
                else
                    throw new ArgumentException($"<{s}> is not a valid xml node name");
                return this;
            }

            public ISerializationOptionsBuilder WithEncoding(Encoding e)
            {
                this.Encoding = e;
                return this;
            }

            public ISerializationOptionsBuilder WithEncoding(string e)
            {
                this.Encoding = Encoding.GetEncoding(e);
                if (Encoding == null)
                    throw new ArgumentException($"Unable to find Encoding '{e}'");
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
            if (attributes.TryGetValue(FormatProviderAttributeName, out string formatName))
            {
                ContextStack.Culture = CultureInfo.GetCultureInfo(formatName);
            }
        }
        public override void Run()
        {
            CultureInfo cult;

            if (_firstLevelObjects.Count > 1 && IsValidXMLNodeName(Options.AdditionalRootNode!))
            {
                throw new NotSupportedException($"Cannot serialize multiple first level objects without a root node; please specify a VALID root node name through the {nameof(ISerializationOptions.AdditionalRootNode)} option.");
            }


            cult = this.Options.Culture ?? CultureInfo.CurrentCulture;
            Encoding enc = this.Options.Encoding;
            try
            {
                _sb.Clear();
                ClearStack();
                using (var stringWriter = new StringWriter_WithEncoding(_sb, cult, enc))
                using (var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings { Indent = true, Encoding = enc }))
                {
                    Push(XMLNodeBehaviorProfile.CreateTopNode(_handler, this.Options, Options.AdditionalRootNode, null, true));

                    ContextStack!.Culture = cult;

                    if (Options.AdditionalRootNode != null)
                    {
                        xmlWriter.WriteStartElement(Options.AdditionalRootNode);
                        xmlWriter.WriteAttributeString(FormatProviderAttributeName, ContextStack.Culture.Name);
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

            if (ContextStack.Culture != null)
            {
                attributes.Add(FormatProviderAttributeName, ContextStack.Culture.Name);
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

        private static void WriteLookupTextContentChild(XmlWriter xmlWriter, string nodeName, string TextContent)
        {
            xmlWriter.WriteStartElement(nodeName);
            xmlWriter.WriteString(TextContent);
            xmlWriter.WriteEndElement();
        }
        private static void WriteLookupChild(XmlWriter xmlWriter, string nodeName, IReadOnlyDictionary<string, string> attributes)
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
