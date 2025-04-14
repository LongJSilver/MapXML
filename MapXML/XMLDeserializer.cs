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
    public class XMLDeserializer : XMLSerializerBase
    {
        public bool IgnoreRootNode { get; set; } = false;
        private readonly Stream _source;
        private readonly SaxReader _reader;

        public XMLDeserializer(IXMLSerializationHandler? Handler, Stream source, object? owner = null, bool AllowImplicitFields = true)
        {
            this.AllowImplicitFields = AllowImplicitFields;
            if (Handler != null)
                Push(XMLNodeBehaviorProfile.CreateTopNode(Handler, null, owner, false, this.AllowImplicitFields));
            this._source = source ?? throw new ArgumentNullException(nameof(source));
            _reader = new SaxReader(_source);
            _reader.OnNodeEnd += this.NodeEnd;
            _reader.OnNodeStart += this.NodeStart;
            _reader.OnText += this.TextContent;
        }

        void NodeStart(string nodeName, Dictionary<string, string> attributes)
        {
            if (CurrentLevel > 0 || !IgnoreRootNode)
            {
                if (!ContextStack.IsCreation)
                {
                    throw new XMLSerializationException(this.CurrentNodeName, this.CurrentLevel, this.CurrentPath, $"Only 'Create' nodes can have children nodes");
                }
                object? currentObject = null;
                bool shouldAbsorbAttributes;
                IXMLSerializationHandler? newHandler = null; //No mechanism right now to introduce a new handler
                if (!ContextStack.InfoForNode(nodeName, attributes, out DeserializationPolicy nodePolicy, out Type? targetType))
                    throw new XMLSerializationException(this.CurrentNodeName, this.CurrentLevel, this.CurrentPath, $"No context was found for element <{nodeName}>");

                if (nodePolicy == DeserializationPolicy.Create)
                {
                    shouldAbsorbAttributes = currentObject == null;
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
                    shouldAbsorbAttributes = false;
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


                Push(XMLNodeBehaviorProfile.CreateDeserializationNode(newHandler,
                    nodePolicy == DeserializationPolicy.Create, this.AllowImplicitFields,
                    nodeName, targetType, attributes, currentObject));
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
                                    throw new XMLSerializationException(this.CurrentNodeName, this.CurrentLevel, this.CurrentPath,
                                        $"Lookup failed for attribute named <{AttributeName}>");
                            }
                            else
                                throw new NotSupportedException($"Unknown {nameof(DeserializationPolicy)} : <{attPolicy}>");
                        }
                    }
                }
            }
            else
            {
                Push(nodeName);
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
                throw new XMLSerializationException(CurrentNodeName, CurrentLevel, CurrentPath, e);
            }
            Pop();
        }
        void TextContent(string text)
        {
            if (ContextStack.EncounteredTextContent)
                throw new XMLMixedContentException(CurrentNodeName, CurrentLevel, CurrentPath);
            try
            {
                ContextStack.StoreTextContent(text);
            }
            catch (Exception e)
            {
                throw new XMLSerializationException(CurrentNodeName, CurrentLevel, CurrentPath, e);
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


