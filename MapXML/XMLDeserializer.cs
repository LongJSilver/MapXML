﻿using MapXML.Behaviors;
using MapXML.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml.Linq;

namespace MapXML
{
    public interface IDeserializationOptions : IXMLOptions
    {
        /// <summary>
        /// Whether to skip the root node of the XML document.
        /// <para/> DEFAULT: false
        /// </summary>
        bool IgnoreRootNode { get; }
    }
    public interface IDeserializationOptionsBuilder : IXMLOptionsBuilder<IDeserializationOptionsBuilder>
    {
        IDeserializationOptionsBuilder IgnoreRootNode(bool b);
        IDeserializationOptions Build();
    }
    public class XMLDeserializer : XMLSerializerBase
    {
        /// <summary>
        /// An instance of the default options for the deserializer, with the exception of the <see cref="IDeserializationOptions.IgnoreRootNode"/> value which is set to true
        /// </summary>
        public static IDeserializationOptions DefaultOptions_IgnoreRootNode { get; } = OptionsBuilder().IgnoreRootNode(true).Build();
        public static IDeserializationOptionsBuilder OptionsBuilder() => new DefaultOptions();
        private class DefaultOptions : AbstractOptionsBuilder<IDeserializationOptionsBuilder>, IDeserializationOptions, IDeserializationOptionsBuilder
        {
            public bool IgnoreRootNode { get; private set; } = false;


            IDeserializationOptions IDeserializationOptionsBuilder.Build() => this;

            IDeserializationOptionsBuilder IDeserializationOptionsBuilder.IgnoreRootNode(bool b)
            {
                IgnoreRootNode = b;
                return this;
            }
        }

        private readonly Stream _source;
        private readonly SaxReader _reader;
        private readonly object? _firstNodeOwner;

        public new IDeserializationOptions Options => (IDeserializationOptions)base.Options;

        public XMLDeserializer(string XmlString, IDeserializationOptions? Options = null)
            : this(XmlString, Handler: null, RootNodeOwner: null, Options)
        { }
        public XMLDeserializer(string XmlString, IXMLSerializationHandler? Handler, IDeserializationOptions? Options = null)
            : this(XmlString, Handler: null, RootNodeOwner: null, Options)
        { }
        public XMLDeserializer(string XmlString, IXMLSerializationHandler? Handler, object? RootNodeOwner = null, IDeserializationOptions? Options = null)
            : this(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(XmlString)), Handler, RootNodeOwner: RootNodeOwner, Options)
        { }
        public XMLDeserializer(Stream source, IDeserializationOptions? Options = null)
            : this(source, Handler: null, RootNodeOwner: null, Options)
        { }
        public XMLDeserializer(Stream source, IXMLSerializationHandler? Handler, IDeserializationOptions? Options = null)
            : this(source, Handler, RootNodeOwner: null, Options)
        { }
        public XMLDeserializer(Stream source, IXMLSerializationHandler? Handler, object? RootNodeOwner = null, IDeserializationOptions? Options = null)
            : base(Options ?? new DefaultOptions())
        {
            _firstNodeOwner = RootNodeOwner;
            Push(XMLNodeBehaviorProfile.CreateTopNode(Handler, this.Options, null, null, false));
            this._source = source ?? throw new ArgumentNullException(nameof(source));
            _reader = new SaxReader(_source);
            _reader.OnNodeEnd += this.NodeEnd;
            _reader.OnNodeStart += this.NodeStart;
            _reader.OnText += this.TextContent;
        }

        void NodeStart(string nodeName, Dictionary<string, string> attributes)
        {
            object? currentObject = null;

            IXMLSerializationHandler? newHandler = null; //No mechanism right now to introduce a new handler
            bool shouldAbsorbAttributes = false;
            Type? targetType = null;
            DeserializationPolicy nodePolicy = DeserializationPolicy.Create;

            if (CurrentLevel == 0)
            {
                /**  
                * we are at the root node, and we must deal with 4 possible corner cases:
                * 1 - we where told to IGNORE the root node, and we were given NO owner for it 
                * 2 - we where told to IGNORE the root node, but we were given an OWNER for it 
                * 3 - we where NOT told to ignore the root node, but we were given an OWNER for it 
                * 4 - we where NOT told to ignore the root node, and we were given NO owner for it 
                */

                if (this.Options.IgnoreRootNode && _firstNodeOwner == null)
                {
                    // we where told to IGNORE the root node, and we were given NO owner for it,
                    // so we just push the name to the stack and return
                    Push(nodeName);
                    return;

                }
                else if (this.Options.IgnoreRootNode && _firstNodeOwner != null)
                {
                    // we where told to IGNORE the root node, but we were given an OWNER for it.
                    // So we push a Behavior with the owner as current instance and skip the attribute absorption
                    currentObject = _firstNodeOwner;
                    shouldAbsorbAttributes = false;
                    targetType = _firstNodeOwner.GetType();
                }
                else if (!this.Options.IgnoreRootNode && _firstNodeOwner != null)
                {
                    // we where NOT told to ignore the root node, but we were given an OWNER for it
                    // so we skip the creation phase, we use the given object as CurrentInstance and go
                    // ahead with the attribute absorption phase
                    currentObject = _firstNodeOwner;
                    shouldAbsorbAttributes = true;
                    targetType = _firstNodeOwner.GetType();
                }
                else
                {
                    // we where NOT told to ignore the root node, and we were given NO owner for it,
                    // so we go ahead and treat this like any other node
                }
            }

            if (currentObject == null)
            {
                if (!ContextStack.IsCreation)
                {
                    Throw($"Only 'Create' nodes can have children nodes");
#if NETSTANDARD2_0
                    // just to let the compiler know we can't get past this point
                    // Since the [DoesNotReturn] attribute is not available in .NET Standard 2.0
                    return;
#endif
                }
                if (!ContextStack.InfoForNode(nodeName, attributes, out nodePolicy, out targetType))
                {
                    Throw($"No context was found for element <{nodeName}>");
#if NETSTANDARD2_0
                    // just to let the compiler know we can't get past this point
                    // Since the [DoesNotReturn] attribute is not available in .NET Standard 2.0
                    return;
#endif

                }
                shouldAbsorbAttributes = nodePolicy == DeserializationPolicy.Create;
                if (nodePolicy == DeserializationPolicy.Create)
                {
                    if (currentObject == null && !(ContextStack.Handler?.OverrideCreation(ContextStack, targetType, out currentObject) ?? false)
                        )
                    {
                        if (targetType.Equals(typeof(string)))
                        {
                            currentObject = string.Empty;
                        }
                        else
                        {
                            //----------------- creazione  ----//
                            ConstructorInfo c = targetType.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
                            if (c != null)
                            {
                                currentObject = c.Invoke(new object[0]);
                            }
                            else
                                currentObject = FormatterServices.GetUninitializedObject(targetType);
                        }
                        //-----------------           ----//
                    }
                    ContextStack.InjectDependencies(currentObject);
                }
                else
                {
                    try
                    {
                        if (!ContextStack.Lookup_FromAttributes(nodeName, attributes, targetType, out currentObject))
                            currentObject = new PlaceHolderForLateLookup();
                    }
                    catch (Exception e)
                    {
                        //we swallow this exception for now but we store it in the placeholder
                        //this process will hopefully be finalized later
                        //otherwise this exception will be thrown then
                        currentObject = new PlaceHolderForLateLookup(new XMLSerializationException(this.CurrentNodeName, this.CurrentLevel, this.CurrentPath, e));
                    }

                    //If Lookup with attributes failed, let's suspend the lookup process for the moment,
                    //using a placeholder instead of the final instance
                    //in the hope that this node might contain some text content that would let us perform a successful lookup
                }
            }

            Push(XMLNodeBehaviorProfile.CreateDeserializationNode(newHandler, this.Options,
                nodePolicy == DeserializationPolicy.Create, nodeName, targetType!, attributes, currentObject));

            if (shouldAbsorbAttributes && ContextStack.CanProcessAttributes)
            {
                foreach (KeyValuePair<string, string> item in attributes)
                {
                    string AttributeName = item.Key;
                    string AttributeValue = item.Value;
                    if (ContextStack.InfoForAttribute(nodeName, AttributeName, out var attPolicy, out var attTargetType))
                    {

                        if (attPolicy == DeserializationPolicy.Create)
                            ContextStack.ProcessAttribute(nodeName, AttributeName, AttributeValue);
                        else if (attPolicy == DeserializationPolicy.Lookup)
                        {
                            if (ContextStack.Lookup_FromAttribute(nodeName, AttributeName, AttributeValue, attTargetType, out object? lookedUpValue))
                                ContextStack.ProcessValue(nodeName, AttributeName, lookedUpValue);
                            else
                            {
                                Throw($"Lookup failed for attribute named <{AttributeName}>");
#if NETSTANDARD2_0
                                // just to let the compiler know we can't get past this point
                                // Since the [DoesNotReturn] attribute is not available in .NET Standard 2.0
                                return;
#endif
                            }
                        }
                        else
                            throw new NotSupportedException($"Unknown {nameof(DeserializationPolicy)} : <{attPolicy}>");
                    }
                }
            }


            if (attributes.TryGetValue(FORMAT_PROVIDER_ATTRIBUTE, out var formatName))
                ContextStack.Format = CultureInfo.GetCultureInfo(formatName);
        }


        void NodeEnd(string name)
        {
            //---------------------------------//
            try
            {
                ContextStack?.Finalized(name);
            }
            catch (Exception e)
            {
                Throw($"Error on node end: <{name}>", e);
#if NETSTANDARD2_0
                // just to let the compiler know we can't get past this point
                // Since the [DoesNotReturn] attribute is not available in .NET Standard 2.0
                return;
#endif
            }
            Pop();
        }
        void TextContent(string text)
        {
            try
            {
                if (ContextStack.EncounteredTextContent)
                    throw new XMLMixedContentException(CurrentNodeName, CurrentLevel, CurrentPath);
                ContextStack.StoreTextContent(text);
            }
            catch (Exception e)
            {
                Throw("Error while parsing inner text content", e);
#if NETSTANDARD2_0
                // just to let the compiler know we can't get past this point
                // Since the [DoesNotReturn] attribute is not available in .NET Standard 2.0
                return;
#endif
            }

        }

        public override void Run()
        {
            _reader.Read();
        }
    }

    internal class PlaceHolderForLateLookup
    {
        public XMLSerializationException? PreviousException;

        public PlaceHolderForLateLookup()
        {
        }

        public PlaceHolderForLateLookup(XMLSerializationException e)
        {
            this.PreviousException = e;
        }
    }
}


