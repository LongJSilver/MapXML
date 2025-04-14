
using MapXML.Behaviors;
using MapXML.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MapXML
{
    internal class XmlStaticClassData
    {
        public readonly Type Type;
        public readonly IReadOnlyDictionary<string, XMLMemberBehavior> _attributeBehaviors_forDes;
        public readonly IReadOnlyDictionary<string, XMLMemberBehavior> _childBehaviors_forDes;
        public readonly IReadOnlyDictionary<string, XMLMemberBehavior> _attributeBehaviors_forSer;
        public readonly IReadOnlyDictionary<string, XMLMemberBehavior> _childBehaviors_forSer;
        public readonly XMLMemberBehavior? _textContentBehavior_forDes;
        public readonly XMLMemberBehavior? _textContentBehavior_forSer;

        private IEnumerable<XMLFunction> _functions;
        public IEnumerable<XMLMemberBehavior> AllAttributes_ForDes => (_childBehaviors_forDes.Values).Union(_attributeBehaviors_forDes.Values).Distinct();
        public IEnumerable<XMLMemberBehavior> AllAttributes_ForSer => (_childBehaviors_forSer.Values).Union(_attributeBehaviors_forSer.Values).Distinct();
        public XmlStaticClassData(Type t, IEnumerable<XMLMemberBehavior> behaviors, IEnumerable<XMLFunction> functions)
        {
            this.Type = t;
            var forAttributes_forDes = new CIDictionary<XMLMemberBehavior>();
            var forChildren_forDes = new CIDictionary<XMLMemberBehavior>();
            var forAttributes_forSer = new CIDictionary<XMLMemberBehavior>();
            var forChildren_forSer = new CIDictionary<XMLMemberBehavior>();

            this._attributeBehaviors_forDes = forAttributes_forDes;
            this._childBehaviors_forDes = forChildren_forDes;
            this._attributeBehaviors_forSer = forAttributes_forSer;
            this._childBehaviors_forSer = forChildren_forSer;
            XMLMemberBehavior? tc_forDes = null;
            XMLMemberBehavior? tc_forSer = null;
            behaviors.ForEach(beh =>
                {
                    if (beh.SourceType.HasFlag(XmlSourceType.Attribute))
                    {
                        if (beh.CanDeserialize)
                            forAttributes_forDes.Add(beh.NodeName, beh);
                        if (beh.CanSerialize)
                            forAttributes_forSer.Add(beh.NodeName, beh);
                    }
                    if (beh.SourceType.HasFlag(XmlSourceType.Child))
                    {
                        if (beh.CanDeserialize)
                            forChildren_forDes.Add(beh.NodeName, beh);
                        if (beh.CanSerialize)
                            forChildren_forSer.Add(beh.NodeName, beh);

                    }
                    if (beh.SourceType.HasFlag(XmlSourceType.TextContent))
                    {
                        if (beh.CanDeserialize)
                        {
                            if (tc_forDes != null)
                                throw new ArgumentException($"Type <{t.Name}> declared more than one TextContent behavior.");
                            tc_forDes = beh;
                        }
                        if (beh.CanSerialize)
                        {
                            if (tc_forSer != null)
                                throw new ArgumentException($"Type <{t.Name}> declared more than one TextContent behavior.");
                            tc_forSer = beh;
                        }
                    }
                }
            );
            this._textContentBehavior_forDes = tc_forDes;
            this._textContentBehavior_forSer = tc_forSer;
            this._functions = new HashSet<XMLFunction>(functions);
        }

        public bool HasLookupForType(Type t, ISet<string> attributes, out XMLFunction func)
        {
            func = _functions.Where(f => f.IsLookupFor(t)).OrderByDescending(f => f.GetParameterMatchScore(attributes)).FirstOrDefault();
            return func != null;
        }

        public bool HasTextContentLookupForType(Type t, out XMLFunction func)
        {
            func = _functions.FirstOrDefault(f => f.IsSingleAttributeLookupFor(t));
            return func != null;
        }

        public bool HasReverseLookupForType(Type targetClass, out XMLFunction lookupFunction)
        {
            lookupFunction = _functions.FirstOrDefault(f => f.IsReverseLookupFor(targetClass));
            return lookupFunction != null;
        }

        public bool HasConverterForType(Type t, out XMLFunction func)
        {
            func = _functions.FirstOrDefault(f => f.IsConverter && t.Equals(f.ReturnType));
            return func != null;
        }
        public bool HasReverseConverterForType(Type t, out XMLFunction func)
        {
            func = _functions.FirstOrDefault(f => f.IsReverseConverterFor(t));
            return func != null;
        }
        internal bool HasSerializationLookupForType(Type t, bool SingleAttribute, out XMLFunction lookupFunction)
        {
            if (SingleAttribute)
                lookupFunction = _functions.FirstOrDefault(f => f.IsSingleAttributeLookupFor(t));
            else
                lookupFunction = _functions.FirstOrDefault(f => f.IsLookupFor(t));
            return lookupFunction != null;
        }


    }
}
