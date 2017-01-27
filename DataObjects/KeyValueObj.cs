using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataObjects
{
    public class KeyValueObj<K, V>
    {
        public KeyValueObj(K _key, V _val)
        {
            Key = _key;
            Value = _val;
        }

        public K Key { get; set; }
        public V Value { get; set; }
    }
}
