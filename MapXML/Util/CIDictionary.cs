using System;
using System.Collections.Generic;

namespace MapXML.Utils
{
    /// <summary>
    /// A case insensitive dictionary that uses the OrdinalIgnoreCase comparer.
    /// </summary>
    /// <typeparam name="VAL"></typeparam>
    public class CIDictionary<VAL> : Dictionary<string, VAL>
    {
        public new VAL? this[String key]
        {
            get
            {
                if (key == null)
                    return default(VAL);


                if (base.ContainsKey(key))
                {
                    return base[key];
                }
                else
                {
                    return default(VAL);
                }
            }
            set
            {
                base[key] = value ?? throw new ArgumentNullException(nameof(value));
            }
        }


        public CIDictionary() : base(StringComparer.OrdinalIgnoreCase) { }
        public CIDictionary(IDictionary<string, VAL> oldValues) : base(oldValues, StringComparer.OrdinalIgnoreCase) { }
        public CIDictionary(IReadOnlyDictionary<string, VAL> oldValues) : base(oldValues.Count, StringComparer.OrdinalIgnoreCase)
        {
            foreach (var item in oldValues)
            {
                Add(item.Key, item.Value);
            }
        }
    }
}
