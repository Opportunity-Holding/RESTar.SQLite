using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RESTar.Meta;

namespace RESTar.SQLite
{
    /// <inheritdoc cref="IDictionary{TKey,TValue}" />
    /// <inheritdoc cref="IDynamicMemberValueProvider" />
    /// <summary>
    /// Defines the dynamic members of an elastic SQLite table
    /// </summary>
    public class DynamicMemberCollection : IDictionary<string, object>, IDynamicMemberValueProvider
    {
        private readonly IDictionary<string, KeyValuePair<string, object>> dict;

        /// <inheritdoc />
        public DynamicMemberCollection() => dict = new ConcurrentDictionary<string, KeyValuePair<string, object>>(StringComparer.OrdinalIgnoreCase);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => dict.Values.GetEnumerator();

        /// <inheritdoc />
        public bool ContainsKey(string key) => dict.ContainsKey(key);

        /// <inheritdoc />
        public void Add(string key, object value) => dict.Add(key, new KeyValuePair<string, object>(key, value));

        /// <inheritdoc />
        public bool Remove(string key) => dict.Remove(key);

        /// <inheritdoc />
        public bool TryGetValue(string key, out object value) => TryGetValue(key, out value, out _);

        /// <inheritdoc />
        public ICollection<string> Keys => dict.Keys;

        /// <inheritdoc />
        public ICollection<object> Values => dict.Values.Select(item => item.Value).ToList();

        /// <inheritdoc />
        public void Add(KeyValuePair<string, object> item) => dict.Add(item.Key, item);

        /// <inheritdoc />
        public void Clear() => dict.Clear();

        /// <inheritdoc />
        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            if (arrayIndex + Count > array.Length - 1) throw new ArgumentException(nameof(arrayIndex));
            foreach (var kvp in dict)
            {
                array[arrayIndex] = new KeyValuePair<string, object>(kvp.Key, kvp.Value.Value);
                arrayIndex += 1;
            }
        }

        /// <inheritdoc />
        public bool Remove(KeyValuePair<string, object> item) => dict.Remove(new KeyValuePair<string, KeyValuePair<string, object>>(item.Key, item));

        /// <inheritdoc />
        public int Count => dict.Count;

        /// <inheritdoc />
        public bool IsReadOnly => dict.IsReadOnly;

        /// <inheritdoc />
        public object this[string key]
        {
            get => dict[key].Value;
            set => TrySetValue(key, value);
        }

        /// <summary>
        /// Returns the value with the given member name, or null if there is no such value
        /// </summary>
        public object SafeGet(string memberName)
        {
            dict.TryGetValue(memberName, out var pair);
            return pair.Value;
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public bool TrySetValue(string memberName, object value)
        {
            dict[memberName] = new KeyValuePair<string, object>(memberName, value);
            return true;
        }

        /// <inheritdoc />
        public bool Contains(KeyValuePair<string, object> item) =>
            dict.Contains(new KeyValuePair<string, KeyValuePair<string, object>>(item.Key, item));
    }
}