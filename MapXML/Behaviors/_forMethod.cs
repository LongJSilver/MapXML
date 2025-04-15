using MapXML.Attributes;
using MapXML.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MapXML.Behaviors
{
    internal class _forMethod : XMLMemberBehavior
    {
        private MethodInfo _method => (this.Member as MethodInfo) ?? throw new ArgumentException("Member is either null or not a method");

        public _forMethod(MethodInfo method, AbstractXMLMemberAttribute? attribute)
            : base(method, attribute)
        {
            var param = method.GetParameters();
            if (param.Length != 1)
                throw new InvalidOperationException($"Cannot inject node or attribute <{NodeName}> to method <{Member.Name}> of Type <{Member.DeclaringType}>: Parameter count is != 1.");
            _typeToCreate = param[0].ParameterType;
        }

        private readonly Type _typeToCreate;
        public override Type TypeToCreate => _typeToCreate;

        internal override void InjectValue(IXMLInternalContext context, object value)
        {
            var param = _method.GetParameters();
            if (param.Length != 1)
                throw new InvalidOperationException($"Cannot inject value to method <{Member.Name}> of Type <{Member.DeclaringType}>: Parameter count is != 1.");
            _method.Invoke(context.GetCurrentInstance(), new object[] { value });
        }

        internal override string GetAttributeToSerialize(IXMLInternalContext context, string NodeName, string AttributeName)
        {
            throw new InvalidOperationException();
        }

        internal override IEnumerable<object> GetChildrenToSerialize(IXMLInternalContext context, string NodeName)
        {
            throw new InvalidOperationException();
        }

        internal override string GetTextContentToSerialize(IXMLInternalContext context)
        {
            throw new NotSupportedException();
        }
        internal override string ObtainAttributeValue(IXMLInternalContext context)
        {
            throw new InvalidOperationException();
        }
        internal override object ObtainValue(IXMLInternalContext context)
        {
            throw new InvalidOperationException();
        }

        protected override bool InternalCanSerializeAsAttribute => false;
        protected override bool InternalCanSerializeAsChild => false;
        protected override bool InternalCanSerializeAsTextContent => false;
    }
}
