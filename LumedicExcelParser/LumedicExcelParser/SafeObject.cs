using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace Playground.Base
{
    
    public class SafeObject<K, V> : IEquatable<SafeObject<K, V>> , IDictionary<K,V>
    {
        IDictionary<K, V> innerDic = null;


        public static SafeObject<K, V> Create() { return new SafeObject<K, V>(); }
        public static SafeObject<K, V> Create(IDictionary<K, V> source) { return new SafeObject<K, V>(source); }


        public SafeObject()
        {
            innerDic = new Dictionary<K, V>();
        }

        public SafeObject(IDictionary<K, V> source)
        {
            innerDic = source;
        }

        public V this[K key]
        {
            get
            {
                V value = default(V);
                innerDic.TryGetValue(key, out value);
                return value;
            }
            set
            {
                innerDic[key] = value;
            }
        }

        public int Count
        {
            get
            {
                return innerDic.Count;
            }
        }


        public ICollection<K> Keys
        {
            get
            {
                return innerDic.Keys;
            }
        }

        public ICollection<V> Values
        {
            get
            {
                return innerDic.Values;
            }
        }

        public bool IsReadOnly => false;

        public void Add(K key, V value)
        {
            innerDic.Add(key, value);
        }

        public void Clear()
        {
            innerDic.Clear();
        }

        public bool ContainsKey(K key)
        {
            return innerDic.ContainsKey(key);
        }


        public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
        {
            return innerDic.GetEnumerator();
        }


        public bool Remove(K key)
        {
            return innerDic.Remove(key);
        }

        public bool TryGetValue(K key, out V value)
        {
            return innerDic.TryGetValue(key, out value);
        }
        
        public override int GetHashCode()
        {
            int hash = 0;
            foreach (var key in Keys)
            {
                hash ^= key.GetHashCode();
                hash ^= this[key].GetHashCode();
            }
            return hash;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SafeObject<K, V>);
        }

        public bool Equals(SafeObject<K, V> other)
        {
            if (ReferenceEquals(this, other))
                return true;

            if (ReferenceEquals(null, other))
                return false;

            if (!this.GetType().Equals(other.GetType()))
                return false;

            if (this.Keys.Except(other.Keys).Count() > 0)
                return false;

            bool match = true;

            foreach (var item in this)
            {
                if(!other[item.Key].Equals(item.Value))
                {
                    match = false;
                    break;
                }
                else
                {

                }
            }
            
            return match;
        }

        public void Add(KeyValuePair<K, V> item)
        {
            (innerDic as IDictionary<K, V>).Add(item);
        }

        public bool Contains(KeyValuePair<K, V> item)
        {
            return (innerDic as IDictionary<K, V>).Contains(item);
        }

        public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
        {
            (innerDic as IDictionary<K, V>).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<K, V> item)
        {
            return (innerDic as IDictionary<K, V>).Remove(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
