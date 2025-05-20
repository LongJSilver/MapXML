using System;
using System.Collections.Generic;
#pragma warning disable CA1051 // Make an exception for fields in attribute subclasses
namespace MapXML.Attributes
{
    /// <summary>
    /// Returns true if the attribute should be Omitted, false if it should be serialized.
    /// </summary>
    /// <param name="AttributeName"></param>
    /// <param name="RawValue"></param>
    /// <param name="StringValue"></param>
    /// <param name="MemberType"></param>
    /// <returns></returns>
#pragma warning disable CA1711 // The "Delegate" suffix is appropriate here
    public delegate bool ShouldOmitDelegate(string AttributeName, object? RawValue, string? StringValue, Type MemberType);
#pragma warning restore CA1711 


    public abstract class XMLOmitAttribute : Attribute { }

#pragma warning disable CA1711 // The "Delegate" suffix is appropriate here
    public abstract class XMLOmitWithDelegate : XMLOmitAttribute
    {
        public abstract bool ShouldOmit(string nodename, object? value, string? sValue, Type t);
    }
#pragma warning restore CA1711 
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public class XMLOmitWhenNullAttribute : XMLOmitWithDelegate
    {
        public static bool ShouldOmitRule(string nodename, object? value, string? sValue, Type t)
            => value == null || String.IsNullOrEmpty(sValue);
        public override bool ShouldOmit(string nodename, object? value, string? sValue, Type t)
        => ShouldOmitRule(nodename, value, sValue, t);
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public class XMLOmitWhenDefaultAttribute : XMLOmitWithDelegate
    {
        private static Dictionary<Type, object> _defaultValueCache = new Dictionary<Type, object>();
        public static bool ShouldOmitRule(string nodename, object? value, string? sValue, Type t)
        {
            if (t.IsValueType && Nullable.GetUnderlyingType(t) == null) // Check if 't' is a non-nullable value type
            {
                object defValue;
                if (!_defaultValueCache.TryGetValue(t, out defValue))
                {
                    defValue = Activator.CreateInstance(t);
                    _defaultValueCache[t] = defValue;
                }
                return Equals(value, defValue); // Return true if 'v' is the default value
            }
            else
            {
                if (t.Equals(typeof(string)))
                {
                    return string.IsNullOrEmpty(sValue);
                }
                else
                    return value == null;
            }

        }
        public override bool ShouldOmit(string nodename, object? value, string? sValue, Type t)
        => ShouldOmitRule(nodename, value, sValue, t);
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public class XMLOmitWhenAttribute : XMLOmitWithDelegate
    {
        public readonly object Value;
        public XMLOmitWhenAttribute(object value)
        {
            this.Value = value;
        }

        public override bool ShouldOmit(string nodename, object? value, string? sValue, Type t)
        {
            if (value == null)
                return false;
            return (Object.Equals(this.Value, value));
        }
    }
}
