using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Playground.Base
{
    public class IndexTable<K, V>
    {
        Dictionary<K, List<V>> table = null;

        public IndexTable()
        {
            this.table = new Dictionary<K, List<V>>();
        }

        public List<K> GetMapKeys()
        {
            return table.Keys.ToList();
        }

        public List<V> GetMap(K key, bool autoMap = true)
        {
            List<V> map = null;
            if (!table.TryGetValue(key, out map))
            {
                map = new List<V>();
                if (autoMap) table[key] = map;
            }
            return map;
        }

        public void AddMapForKey(K key, V value)
        {
            var map = GetMap(key);
            map.Add(value);
        }

        public void RemoveMapForKey(K key, V value)
        {
            var map = GetMap(key);
            map.Remove(value);
        }
    }

    public class IndexManager<I, V>
    {
        Dictionary<I, IndexTable<object, V>> table = null;

        public IndexManager()
        {
            this.table = new Dictionary<I, IndexTable<object, V>>();
        }

        public IndexTable<object, V> GetMap(I index, bool autoMap = true)
        {
            IndexTable<object, V> map = null;
            if (!table.TryGetValue(index, out map))
            {
                map = new IndexTable<object, V>();
                if (autoMap) table[index] = map;
            }
            return map;
        }

        public void AddMapForKey(I index, object key, V value)
        {
            var map = GetMap(index);
            map.AddMapForKey(key, value);
        }

        public void RemoveMapForKey(I index, object key, V value)
        {
            var map = GetMap(index);
            map.RemoveMapForKey(key, value);
        }

        public void RemoveIndex(I index)
        {
            table.Remove(index);
        }
    }
}
