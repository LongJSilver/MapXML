using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace MapXML.Utils
{
    /// <summary>
    /// A dictionary that uses a list as the internal value type. This allows for multiple values to be associated with a single key.
    /// The dictionary takes care of allocating the internal lists, and a few utility 'Add' methods were included for ease of use.
    /// </summary>
    /// <typeparam name="Key"></typeparam>
    /// <typeparam name="Value"></typeparam>
    public class ListIndexer<Key, Value> : Dictionary<Key, IList<Value>>
    {
        public ListIndexer() : base()
        {
        }

        public ListIndexer(IEnumerable<Value> toAdd, Func<Value, Key> GetKey) : this()
        {
            AddRange(toAdd, GetKey);
        }

        public void AddRange(IEnumerable<Value> toAdd, Func<Value,Key> GetKey )
        {
            foreach (var val in toAdd)
            {
                Add(GetKey(val), val);
            }
        }

        public void Add(Key k, Value v)
        {
            IList<Value> valori = this[k];
            valori.Add(v);
        }

        public void Remove(Key k, Value v)
        {
            IList<Value> valori = this[k];
            valori.Remove(v);
        }

        public new IList<Value> this[Key key]
        {
            get
            {
                IList<Value> target;
                if (base.ContainsKey(key)) target = base[key];
                else { base[key] = target = new List<Value>(); }
                return target;
            }
        }
    }

}
