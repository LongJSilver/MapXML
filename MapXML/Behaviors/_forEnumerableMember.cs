using MapXML.Attributes;
using MapXML.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MapXML.Behaviors
{
    internal class _forEnumerableMember : XMLMemberBehavior
    {
        private static readonly string ExceptionMessage_NoDirectSerialization =
            $"An {nameof(IEnumerable)} member cannot be serialized directly, it should be flagged with '{nameof(XMLChildAttribute)}' and serialized as a set of children.";

        private readonly Type _typeToCreate;
        public override Type TypeToCreate => _typeToCreate;
        protected override bool InternalCanSerializeAsAttribute => false;
        protected override bool InternalCanSerializeAsChild => true;
        protected override bool InternalCanSerializeAsTextContent => false;

        public _forEnumerableMember(MemberInfo member, AbstractXMLMemberAttribute? attribute)
          : base(member, attribute)
        {
            var args = new[] { typeof(int) };
            Type memberType = member.FieldOrPropertyType();

            _typeToCreate = memberType.GetGenericArguments()[0];
        }

        internal override void InjectValue(IXMLInternalContext context, object value)
        {
            throw new InvalidOperationException($"Cannot inject a value into an {nameof(IEnumerable)} member");
        }

        internal override string? GetAttributeToSerialize(IXMLInternalContext context, string NodeName, string AttributeName)
        {
            throw new InvalidOperationException($"Attribute: {AttributeName} - {ExceptionMessage_NoDirectSerialization}");
        }

        internal override IEnumerable<object> GetChildrenToSerialize(IXMLInternalContext context, string NodeName)
        {
            IEnumerable val = (IEnumerable)Member.GetValue(context.GetCurrentInstance());
            foreach (var item in val)
            {
                yield return item;
            }
        }

        internal override object ObtainValueForLookup(IXMLInternalContext context)
        {
            throw new InvalidOperationException(ExceptionMessage_NoDirectSerialization);
        }

        internal override string GetTextContentToSerialize(IXMLInternalContext context)
        {
            throw new InvalidOperationException(ExceptionMessage_NoDirectSerialization);
        }

    }
}
