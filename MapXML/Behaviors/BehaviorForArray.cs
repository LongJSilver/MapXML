using MapXML.Attributes;
using MapXML.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace MapXML.Behaviors
{
    internal sealed class BehaviorForArray : XMLMemberBehavior
    {
        private static readonly string ExceptionMessage_NoDirectSerialization
            = $"An {nameof(Array)} member cannot be serialized directly, it should be flagged with '{nameof(XMLChildAttribute)}' and serialized as a set of children.";

        private readonly Type _typeToCreate;
        public override Type TypeToCreate => _typeToCreate;
        private const String DATA_KEY = "_forArray-TEMP_COLLECTION";

        protected override bool InternalCanSerializeAsAttribute => false;
        protected override bool InternalCanSerializeAsChild => true;
        protected override bool InternalCanSerializeAsTextContent => false;

        public BehaviorForArray(MemberInfo member, AbstractXMLMemberAttribute? attribute) : base(member, attribute)
        {
            _typeToCreate = member.FieldOrPropertyType().GetElementType();
        }

        internal override void InjectValue(IXMLInternalContext context, object value)
        {
            IList collection = GetTemporaryCollection(context.CustomData);
            collection.Add(value);
        }

        private IList GetTemporaryCollection(IDictionary<string, object> contextData)
        {
            if (!contextData.TryGetValue(DATA_KEY, out object? collection))
            {
                Type CollectionType = typeof(Collection<>).MakeGenericType(new Type[] { (TypeToCreate) });
                collection = Activator.CreateInstance(CollectionType);
                contextData[DATA_KEY] = collection;
            }
            return ((IList)collection);
        }

        internal override void OnFinalized(object finalizedInstance, IXMLInternalContext context)
        {
            IList collection = GetTemporaryCollection(context.CustomData);
            Array result = Array.CreateInstance(TypeToCreate, collection.Count);
            collection.CopyTo(result, 0);
            Member.SetValue(finalizedInstance, result);
        }

        internal override string GetTextContentToSerialize(IXMLInternalContext context)
        {
            throw new InvalidOperationException(ExceptionMessage_NoDirectSerialization);
        }
        internal override string? GetAttributeToSerialize(IXMLInternalContext context, string NodeName, string AttributeName)
        {
            throw new InvalidOperationException($"Attribute: {AttributeName} - {ExceptionMessage_NoDirectSerialization}");
        }

        internal override IEnumerable<object> GetChildrenToSerialize(IXMLInternalContext context, string NodeName)
        {
            Array value = (Array)Member.GetValue(context.GetCurrentInstance());
            for (int i = 0; i < value.Length; i++)
            {
                yield return value.GetValue(i);
            }
        }
        internal override object ObtainValueForLookup(IXMLInternalContext context)
        {
            throw new InvalidOperationException(ExceptionMessage_NoDirectSerialization);
        }

        internal override bool AttributeAlreadyHasValue(IXMLInternalContext context)
        {
            throw new InvalidOperationException($"An array cannot be deserialized as attribute.");
        }
    }
}
