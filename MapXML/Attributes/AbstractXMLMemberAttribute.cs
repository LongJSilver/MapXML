using System;

namespace MapXML.Attributes
{
#pragma warning disable CA1051 // Make an exception for fields in attribute subclasses
    [Flags]
    public enum AggregationPolicy
    {
        NoAggregation = 0,
        AggregateAttributes = 1,
        AggregateChildren = 2,
        AggregateTextContent = 4,
        AggregateAll = AggregateAttributes | AggregateTextContent | AggregateChildren
    }
    public abstract class AbstractXMLMemberAttribute : Attribute
    {
        public readonly string NodeName;
        internal readonly DeserializationPolicy Policy;
        public readonly XMLSourceType SourceType;
        public int SerializationOrder { get; set; } = byte.MaxValue;
        public bool CanSerialize { get; set; } = true;
        public bool CanDeserialize { get; set; } = true;
        public bool AllowImplicit { get; set; } = false;

        /// <summary>
        /// Relevant for deserialization; when applied to child nodes it's only relevant when the policy is <see cref="DeserializationPolicy.Create"/>. <para/>
        /// When the value is different then <see cref="AggregationPolicy.NoAggregation"/>, it allows the source xml to specify the same entity for creation more than once;
        /// every time an entity is encountered again after creation the system will attempt to look it back up 
        /// and integrate the already parsed attributes and children with the ones specified in the new node. <para/>
        /// Useful when parsing more than one xml source that integrate data for entities defined previously.<para/>
        /// 
        /// The lookup will be performed using the same rules as if the policy was <see cref="DeserializationPolicy.Lookup"/>, 
        /// and the implementor must provide a valid lookup function within the same class or a parent class, or by use of the current handler.<para/>
        /// 
        /// After the lookup is successfully completed, the deserialization behavior will proceed as follows:
        /// <list type="bullet">
        ///     <item>For Attributes, the system will automatically ignore any attributes that already have a non-default value in the looked-up entity (ie. already defined previously)
        ///     unless the attribute is itself flagged with <see cref="AbstractXMLMemberAttribute.AggregateMultipleDefinitions"/></item>
        ///     <item>For child nodes the deserialization process is carried out normally, so it's the implementor's responsibility to check for duplication and 
        ///     in general to ensure that no conflicts arise from the integration of new data.</item>
        /// </list>
        /// Finally, the looked-up entity will pass through the 
        /// same finalization process as any other entity created during deserialization, so it will show up multiple times in whatever 
        /// downstream mechanism is used to collect results.<para/>
        /// </summary>
        public AggregationPolicy AggregateMultipleDefinitions { get; set; } = AggregationPolicy.NoAggregation;
        internal AbstractXMLMemberAttribute(string xmlAttributeName,
                                    XMLSourceType sourceType = XMLSourceType.Attribute,
                                    DeserializationPolicy type = DeserializationPolicy.Create)
        {
            this.NodeName = xmlAttributeName ?? throw new ArgumentNullException(nameof(xmlAttributeName));
            this.Policy = type;
            this.SourceType = sourceType;
        }
    }
}
