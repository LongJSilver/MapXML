using MapXML.Attributes;
using MapXML.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MapXML.Behaviors
{
    internal class _forMember : XMLMemberBehavior
        {
            internal _forMember(MemberInfo m, XmlSourceType sourceType = XmlSourceType.Attribute, DeserializationPolicy type = DeserializationPolicy.Create)
                : base(m, sourceType, type)
            {
                _typeToCreate = m.FieldOrPropertyType();
            }

            internal _forMember(MemberInfo m, AbstractXMLMemberAttribute attribute)
                : base(m, attribute)
            {
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

            internal override string GetAttributeToSerialize(IXMLInternalContext context, string NodeName, string AttributeName)
            {
                return ObtainAttributeValue(context);
            }

            internal override IEnumerable<object> GetChildrenToSerialize(IXMLInternalContext context, string NodeName)
            {
                return new object[] { ObtainValue(context) };
            }

            internal override string ObtainAttributeValue(IXMLInternalContext context)
            {
                object value = ObtainValue(context);
                return ConvertBack(context, value);
            }
            internal override object ObtainValue(IXMLInternalContext context)
            {
                return Member.GetValue(context.GetCurrentInstance());
            }

            internal override string GetTextContentToSerialize(IXMLInternalContext context)
             => ObtainAttributeValue(context);
    }
}
