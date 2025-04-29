
using MapXML.Behaviors;
using MapXML.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MapXML
{
    internal sealed class XMLStaticClassData
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
        public XMLStaticClassData(Type t, IEnumerable<XMLMemberBehavior> behaviors, IEnumerable<XMLFunction> functions)
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
                    if (beh.SourceType.HasFlag(XMLSourceType.Attribute))
                    {
                        if (beh.CanDeserialize)
                            forAttributes_forDes.Add(beh.NodeName, beh);
                        if (beh.CanSerializeAsAttribute)
                            forAttributes_forSer.Add(beh.NodeName, beh);
                    }
                    if (beh.SourceType.HasFlag(XMLSourceType.Child))
                    {
                        if (beh.CanDeserialize)
                            forChildren_forDes.Add(beh.NodeName, beh);
                        if (beh.CanSerializeAsChild)
                            forChildren_forSer.Add(beh.NodeName, beh);

                    }
                    if (beh.SourceType.HasFlag(XMLSourceType.TextContent))
                    {
                        if (beh.CanDeserialize)
                        {
                            if (tc_forDes != null)
                                throw new ArgumentException($"Type <{t.Name}> declared more than one TextContent behavior.");
                            tc_forDes = beh;
                        }
                        if (beh.CanSerializeAsTextContent)
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

        /// <summary>
        /// Looks for and returns the best lookup function to match the list of attribute names passed as parameter.
        /// </summary>
        /// <param name="t">Type this function qualifies as lookup for</param>
        /// <param name="attributes">The set of attributes this function should accept</param>
        /// <param name="func">the best function found, or null if none were defined</param>
        /// <returns>True when a function was found, false otherwise</returns>
        public bool HasLookupForType(Type t, ISet<string> attributes, [MaybeNullWhen(false)][NotNullWhen(true)] out XMLFunction? func)
        {
            func = _functions.Where(f => f.IsLookupFor(t)).OrderByDescending(f => f.GetParameterMatchScore(attributes)).FirstOrDefault();
            return func != null;
        }
        /// <summary>
        /// True if a function exists that is specifically created to convert <paramref name="targetClass"/> into string, hence it's a reverse lookup
        /// <para/> Note that since these function do not accept a IFormatProvider parameter, they are NOT equivalent to a <see cref="ConvertToString"/> delegate
        /// </summary>
        /// <param name="targetClass"></param>
        /// <param name="lookupFunction">Null when false, not-Null when true</param>
        /// <returns>True when a function was found, false otherwise</returns>
        public bool HasReverseLookupForType(Type targetClass, [MaybeNullWhen(false)][NotNullWhen(true)] out XMLFunction? lookupFunction)
        {
            lookupFunction = _functions.FirstOrDefault(f => f.IsReverseLookupFor(targetClass));
            return lookupFunction != null;
        }

        /// <summary>
        /// Returns True if at least one function exists that can act as lookup for Type <paramref name="t"/>, and returns that function.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="SingleAttribute">Only look for lookup functions that accept a single parameter</param>
        /// <param name="lookupFunction">Null when false, not-Null when true</param>
        /// <returns>True when a function was found, false otherwise</returns>
        internal bool HasLookupForType(Type t, bool SingleAttribute, [MaybeNullWhen(false)][NotNullWhen(true)] out XMLFunction? lookupFunction)
        {
            if (SingleAttribute)
                lookupFunction = _functions.FirstOrDefault(f => f.IsSingleAttributeLookupFor(t));
            else
                lookupFunction = _functions.FirstOrDefault(f => f.IsLookupFor(t));
            return lookupFunction != null;
        }


        /// <summary>
        /// Checks if there is a converter function available for the specified type.
        /// A converter function is one that transforms a string value into the specified type.
        /// </summary>
        /// <param name="t">The type the required function should qualify as a converter to.</param>
        /// <param name="func">
        /// When the method returns, contains the converter function if one exists; otherwise, null.
        /// </param>
        /// <returns>True when a function was found, false otherwise</returns>
        public bool HasConverterForType(Type t, [MaybeNullWhen(false)][NotNullWhen(true)] out XMLFunction? func)
        {
            func = _functions.FirstOrDefault(f => f.IsConverterFor(t));
            return func != null;
        }
        /// <summary>
        /// Checks if there is a reverse converter function available for the specified type.
        /// A reverse converter function is one that transforms a value of the specified type into another representation.
        /// </summary>
        /// <param name="t">The target type to check for a reverse converter function.</param>
        /// <param name="func">
        /// When the method returns, contains the reverse converter function if one exists; otherwise, null.
        /// </param>
        /// <returns>True when a function was found, false otherwise</returns>
        public bool HasReverseConverterForType(Type t, [MaybeNullWhen(false)][NotNullWhen(true)] out XMLFunction? func)
        {
            func = _functions.FirstOrDefault(f => f.IsReverseConverterFor(t));
            return func != null;
        }


    }
}
