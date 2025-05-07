using MapXML.Attributes;
using MapXML.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace MapXML.Behaviors
{
    internal sealed class BehaviorForGenericMember : XMLMemberBehavior
    {
        internal BehaviorForGenericMember(MemberInfo m, AbstractXMLMemberAttribute? attribute, IEnumerable<ShouldOmitDelegate>? serializationFilters = null)
            : base(m, attribute)
        {
            _serializationFilters = new List<ShouldOmitDelegate>();
            if (serializationFilters != null)
                serializationFilters
                    .Where(f => f != null)
                    .ForEach(f => _serializationFilters.Add(f));
            _typeToCreate = m.FieldOrPropertyType();
        }

        private readonly Type _typeToCreate;
        public override Type TypeToCreate => _typeToCreate;

        protected override bool InternalCanSerializeAsAttribute => true;
        protected override bool InternalCanSerializeAsChild => true;
        protected override bool InternalCanSerializeAsTextContent => true;

        internal override void InjectValue(IXMLInternalContext context, object value)
        {
            Member.SetValue(context.GetCurrentInstance(), value);
        }

        internal override string? GetAttributeToSerialize(IXMLInternalContext context, string NodeName, string AttributeName)
        {
            return ObtainAttributeValue(context);
        }

        internal override IEnumerable<object> GetChildrenToSerialize(IXMLInternalContext context, string NodeName)
        {
            object? val = ObtainValueForLookup(context);
            if (val == null) return Enumerable.Empty<object>();
            return new object[] { val };
        }

        private string? ObtainAttributeValue(IXMLInternalContext context)
        {
            object? value = Member.GetValue(context.GetCurrentInstance());
            if (value == null) return null;
            string sValue = ConvertBack(context, value);


            if (context.Options is ISerializationOptions serOpt && serOpt.AttributeOmissionPolicy != AttributeOmissionPolicy.AsDictatedByCodeAnnotations)
            {
                // Check if the attribute should be omitted based on the global policy
                if (serOpt.AttributeOmissionPolicy == AttributeOmissionPolicy.AlwaysWhenDefault)
                {
                    if (XMLOmitWhenDefaultAttribute.ShouldOmitRule(this.NodeName, value, sValue, _typeToCreate)) return null;

                }
                else if (serOpt.AttributeOmissionPolicy == AttributeOmissionPolicy.AlwaysWhenNull)
                {
                    if (XMLOmitWhenNullAttribute.ShouldOmitRule(this.NodeName, value, sValue, _typeToCreate)) return null;
                }
            }

            foreach (var filter in _serializationFilters)
            {
                if (!filter(this.NodeName, value, sValue, _typeToCreate))
                {
                    return null;
                }
            }
            return sValue;
        }

        internal override object? ObtainValueForLookup(IXMLInternalContext context)
        {
            return Member.GetValue(context.GetCurrentInstance());
        }

        internal override string? GetTextContentToSerialize(IXMLInternalContext context)
         => ObtainAttributeValue(context);

        private readonly List<ShouldOmitDelegate> _serializationFilters;
    }
}
