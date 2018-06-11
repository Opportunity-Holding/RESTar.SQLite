using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RESTar.Meta;

namespace RESTar.SQLite
{
    public class DynamicMemberCollection : IDictionary<string, object>, IDynamicMemberValueProvider
    {
        private readonly IDictionary<string, KeyValuePair<string, object>> dict;
        public DynamicMemberCollection() => dict = new ConcurrentDictionary<string, KeyValuePair<string, object>>(StringComparer.OrdinalIgnoreCase);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => dict.Values.GetEnumerator();
        public bool ContainsKey(string key) => dict.ContainsKey(key);
        public void Add(string key, object value) => dict.Add(key, new KeyValuePair<string, object>(key, value));
        public bool Remove(string key) => dict.Remove(key);
        public bool TryGetValue(string key, out object value) => TryGetValue(key, out value, out _);
        public ICollection<string> Keys => dict.Keys;
        public ICollection<object> Values => dict.Values.Select(item => item.Value).ToList();
        public void Add(KeyValuePair<string, object> item) => dict.Add(item.Key, item);
        public void Clear() => dict.Clear();
        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) => throw new NotImplementedException();
        public bool Remove(KeyValuePair<string, object> item) => dict.Remove(new KeyValuePair<string, KeyValuePair<string, object>>(item.Key, item));
        public int Count => dict.Count;
        public bool IsReadOnly => dict.IsReadOnly;

        public object this[string key]
        {
            get => dict[key].Value;
            set => TrySetValue(key, value);
        }

        public object SafeGet(string memberName)
        {
            dict.TryGetValue(memberName, out var pair);
            return pair.Value;
        }

        public bool TryGetValue(string memberName, out object value, out string actualMemberName)
        {
            if (dict.TryGetValue(memberName, out var pair))
            {
                value = pair.Value;
                actualMemberName = pair.Key;
                return true;
            }
            value = actualMemberName = null;
            return false;
        }

        public bool TrySetValue(string memberName, object value)
        {
            dict[memberName] = new KeyValuePair<string, object>(memberName, value);
            return true;
        }

        public bool Contains(KeyValuePair<string, object> item) =>
            dict.Contains(new KeyValuePair<string, KeyValuePair<string, object>>(item.Key, item));
    }
}