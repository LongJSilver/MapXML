using MapXML.Attributes;
using MapXML.Behaviors;
using MapXML.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;

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
    public class XMLDeserializer : XMLSerializerBase, IDisposable
    {
        /// <summary>
        /// An instance of the default options for the deserializer, with the exception of the <see cref="IDeserializationOptions.IgnoreRootNode"/> value which is set to true
        /// </summary>
        public static IDeserializationOptions DefaultOptions_IgnoreRootNode { get; } = OptionsBuilder().IgnoreRootNode(true).Build();

        public static IDeserializationOptionsBuilder OptionsBuilder(IXMLOptions? copyFrom = null) => new DefaultOptions(copyFrom);
        private sealed class DefaultOptions : AbstractOptionsBuilder<IDeserializationOptionsBuilder>, IDeserializationOptions, IDeserializationOptionsBuilder
        {
            public DefaultOptions(IXMLOptions? CopyFrom = null) : base(CopyFrom)
            {
                if (CopyFrom is IDeserializationOptions so)
                {
                    this.IgnoreRootNode = so.IgnoreRootNode;
                }
            }

            /// <summary>
            /// Whether to skip the root node of the XML document.
            /// <para/> DEFAULT: false
            /// </summary>

#pragma warning disable CA1805 // It should be extra clear what the default value is
            public bool IgnoreRootNode { get; private set; } = false;
#pragma warning restore CA1805


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

        public XMLDeserializer(string XMLString, IDeserializationOptions? Options = null)
            : this(XMLString, Handler: null, RootNodeOwner: null, Options)
        { }
        public XMLDeserializer(string XMLString, IXMLSerializationHandler? Handler, IDeserializationOptions? Options = null)
            : this(XMLString, Handler: null, RootNodeOwner: null, Options)
        { }
        public XMLDeserializer(string XMLString, IXMLSerializationHandler? Handler, object? RootNodeOwner = null, IDeserializationOptions? Options = null)
            : this(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(XMLString)), Handler, RootNodeOwner: RootNodeOwner, Options)
        { }
        public XMLDeserializer(Stream source, IDeserializationOptions? Options = null)
            : this(source, Handler: null, RootNodeOwner: null, Options)
        { }
        public XMLDeserializer(Stream source, IXMLSerializationHandler? Handler, IDeserializationOptions? Options = null)
            : this(source, Handler, RootNodeOwner: null, Options)
        { }
        public XMLDeserializer(Stream source, IXMLSerializationHandler? Handler, object? RootNodeOwner = null, IDeserializationOptions? Options = null)
            : base(Options ?? new DefaultOptions(), Handler)
        {
            _firstNodeOwner = RootNodeOwner;
            Push(XMLNodeBehaviorProfile.CreateTopNode(Handler, this.Options, null, null, false));
            this._source = source ?? throw new ArgumentNullException(nameof(source));
            _reader = new SaxReader(_source);
            _reader.OnNodeEnd += this.NodeEnd;
            _reader.OnNodeStart += this.NodeStart;
            _reader.OnText += this.TextContent;
        }
        /// <summary>
        /// A level of -1 means business as usual.
        /// NodeStart:
        ///     - if level == 0 then we are at the first level children of a node that should ignore children,
        ///        so we increment and skip
        ///     - if level > 0 then we are down the hierarchy of a node that we should ignore,
        ///        so we increment and skip
        /// NodeEnd:
        ///     - if level == 0 then we are closing a node whose children we have been ignoring,
        ///         so we decrease and finalize as usual
        ///     - if level > 0 then we are down the hierarchy of a node that we should ignore entirely,
        ///         so we decrease and skip
        /// TextContent:
        ///     - if level == 0 then we are encountering the text content of a node which is ignoring children,
        ///         we process text content as appropriate.        ///     
        ///     - if level > 0 then we are down the hierarchy of a node that we should ignore entirely,
        ///         so we skip
        /// </summary>
        private int _ignoreLevel = -1;
        void NodeStart(string nodeName, Dictionary<string, string> attributes)
        {
            if (_ignoreLevel >= 0)
            {
                _ignoreLevel++;
                return;
            }
            object? currentObject = null;

            bool shouldAbsorbAttributes = false;
            ElementMappingInfo info = default;

            if (CurrentLevel == 0)
            {
                /**  
                * we are entering the root node, and we must deal with 4 possible corner cases:
                * 1 - we where told to IGNORE the root node, and we were given NO owner for it 
                * 2 - we where told to IGNORE the root node, but we were given an OWNER for it 
                * 3 - we where NOT told to ignore the root node, but we were given an OWNER for it 
                * 4 - we where NOT told to ignore the root node, and we were given NO owner for it 
                */

                if (this.Options.IgnoreRootNode && _firstNodeOwner == null)
                {
                    // we where told to IGNORE the root node, and we were given NO owner for it,
                    // so we just push the name to the stack and return
                    LogicalLevelOffset = -1; // the root node will not yield any results
                    Push(XMLNodeBehaviorProfile.CreateTopNode(Handler, this.Options, nodeName, _firstNodeOwner, false));
                    return;
                }
                else if (this.Options.IgnoreRootNode && _firstNodeOwner != null)
                {
                    // we where told to IGNORE the root node, but we were given an OWNER for it.
                    // So we push a Behavior with the owner as current instance and skip the attribute absorption
                    currentObject = _firstNodeOwner;
                    shouldAbsorbAttributes = false;
                    info.TargetType = _firstNodeOwner.GetType();
                    LogicalLevelOffset = 0; // the root node will be associated to the item given to us by the caller 
                }
                else if (!this.Options.IgnoreRootNode && _firstNodeOwner != null)
                {
                    // we where NOT told to ignore the root node, and we were given an OWNER for it
                    // so we skip the creation phase, we use the given object as CurrentInstance and go
                    // ahead with the attribute absorption phase
                    shouldAbsorbAttributes = true;
                    currentObject = _firstNodeOwner;
                    info.TargetType = currentObject.GetType();
                    LogicalLevelOffset = 0; // the root node will be associated to the item given to us by the caller 
                }
                else
                {
                    // we where NOT told to ignore the root node, and we were given NO owner for it,
                    // so we go ahead and treat this like any other node
                    LogicalLevelOffset = 0; // the root node will be associated to whatever item we deserialize first
                }

            }
            bool IsAggregation = false;

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
                if (!ContextStack.InfoForNode(nodeName, attributes, out info))
                {
                    Throw($"No context was found for element <{nodeName}>");
#if NETSTANDARD2_0
                    // just to let the compiler know we can't get past this point
                    // Since the [DoesNotReturn] attribute is not available in .NET Standard 2.0
                    return;
#endif

                }
                if (info.Policy == DeserializationPolicy.Create)
                {

                    if (info.AggregateMultipleDefinitions != AggregationPolicy.NoAggregation)
                    {
                        if (ContextStack.Lookup_FromAttributes(nodeName, attributes, info.TargetType!, out currentObject)
                            && currentObject != null)
                        {

                            IsAggregation = true;
                            shouldAbsorbAttributes = info.AggregateMultipleDefinitions.HasFlag(AggregationPolicy.AggregateAttributes);
                        }
                    }

                    //try finding an existing instance if we are allowed to
                    if (!IsAggregation)
                    {
                        //no existing instance was found, so we create a new one
                        if (!(ContextStack.Handler?.OverrideCreation(ContextStack, info.TargetType!, out currentObject) ?? false)
                            )
                        {
                            if (info.TargetType!.Equals(typeof(string)))
                            {
                                currentObject = string.Empty;
                            }
                            else
                            {
                                //----------------- creazione  ----//
                                ConstructorInfo c = info.TargetType.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
                                if (c != null)
                                {
                                    currentObject = c.Invoke(Array.Empty<object>());
                                }
                                else
                                    currentObject = FormatterServices.GetUninitializedObject(info.TargetType);
                            }
                            //-----------------           ----//
                        }
                        shouldAbsorbAttributes = true;
                        ContextStack.InjectDependencies(currentObject);
                    }
                }
                else
                {
                    shouldAbsorbAttributes = false;
                    try
                    {
                        if (!ContextStack.Lookup_FromAttributes(nodeName, attributes, info.TargetType!, out currentObject))
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

            Push(XMLNodeBehaviorProfile.CreateDeserializationNode(this.Handler, this.Options,
                info.Policy == DeserializationPolicy.Create, IsAggregation ? info.AggregateMultipleDefinitions : AggregationPolicy.NoAggregation, nodeName, info.TargetType!, attributes, currentObject!));

            if (shouldAbsorbAttributes && ContextStack.CanProcessAttributes)
            {
                foreach (KeyValuePair<string, string> item in attributes)
                {
                    string AttributeName = item.Key;
                    string AttributeValue = item.Value;
                    if (ContextStack.InfoForAttribute(nodeName, AttributeName, out var attInfo))
                    {
                        if (attInfo.Policy == DeserializationPolicy.Create)
                            ContextStack.ProcessAttribute(nodeName, AttributeName, AttributeValue);
                        else if (attInfo.Policy == DeserializationPolicy.Lookup)
                        {
                            if (ContextStack.Lookup_FromAttribute(nodeName, AttributeName, AttributeValue, attInfo.TargetType!, out object? lookedUpValue))
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
                            throw new NotSupportedException($"Unknown {nameof(DeserializationPolicy)} : <{attInfo.Policy}>");
                    }
                }
            }


            if (attributes.TryGetValue(FormatProviderAttributeName, out var formatName))
                ContextStack.Culture = CultureInfo.GetCultureInfo(formatName);


            if (IsAggregation && !info.AggregateMultipleDefinitions.HasFlag(AggregationPolicy.AggregateChildren))
            {
                _ignoreLevel++;
            }
        }


        void NodeEnd(string name)
        {
            switch (_ignoreLevel)
            {
                case < 0:
                    break; //business as usual
                case 0:
                    _ignoreLevel--;
                    break;
                case > 0:
                    _ignoreLevel--;
                    return;
            }
        
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
            if (_ignoreLevel > 0) return;

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

        public void Dispose()
        {
            _reader?.Dispose();
            _source?.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    internal sealed class PlaceHolderForLateLookup
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


