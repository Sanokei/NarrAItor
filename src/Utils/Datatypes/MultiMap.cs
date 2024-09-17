using System;
using System.Collections.Generic;
using System.Linq;

namespace NarrAItor.Utils.Datatypes
{
    public class MultiMap<TKey, TValue> 
        where TKey : notnull 
        where TValue : notnull
    {
        private Dictionary<TValue, HashSet<TKey>> _Dictionary = [];

        public MultiMap()
        {
            _Dictionary = new Dictionary<TValue, HashSet<TKey>>(EqualityComparer<TValue>.Default);
        }
        public void Add(TKey key, TValue value)
        {
            if (!_Dictionary.TryGetValue(value, out HashSet<TKey>? keys))
            {
                keys = [];
                _Dictionary[value] = keys;
            }

            keys.Add(key);
        }

        // Rest of the methods remain the same

        public bool Remove(TKey key, TValue value)
        {
            if (_Dictionary.TryGetValue(value, out var keys))
            {
                return keys.Remove(key);
            }
            return false;
        }

        public bool ContainsKey(TKey key)
        {
            return _Dictionary.Values.Any(set => set.Contains(key));
        }

        public bool ContainsValue(TValue value)
        {
            return _Dictionary.ContainsKey(value);
        }

        public IEnumerable<TKey> GetKeys(TValue value)
        {
            return _Dictionary.TryGetValue(value, out var keys) ? keys : Enumerable.Empty<TKey>();
        }

        public TValue? GetValue(TKey key)
        {
            foreach (var pair in _Dictionary)
            {
                if (pair.Value.Contains(key))
                {
                    return pair.Key;
                }
            }
            return default;
        }

        public void Clear()
        {
            _Dictionary.Clear();
        }

        public int Count => _Dictionary.Values.Sum(set => set.Count);
    }
}