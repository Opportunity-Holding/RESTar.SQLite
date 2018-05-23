using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using RESTar.Resources;

namespace RESTar.SQLite
{
    /// <summary>
    /// An SQLite table that can have its schema changed during runtime
    /// </summary>
    public abstract class ElasticSQLiteTable : SQLiteTable, IDictionary<string, object>
    {
        /// <inheritdoc />
        protected ElasticSQLiteTable()
        {
            _dynamicMembers = new ConcurrentDictionary<string, object>();
        }

        public abstract IEnumerable<(TypeCode dataType, string name)> GetDefaultMembers();

        #region Dictionary

        private IDictionary<string, object> _dynamicMembers { get; }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => _dynamicMembers.GetEnumerator();

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _dynamicMembers.GetEnumerator();

        /// <inheritdoc />
        public void Add(KeyValuePair<string, object> item) => _dynamicMembers.Add(item);

        /// <inheritdoc />
        public void Clear() => _dynamicMembers.Clear();

        /// <inheritdoc />
        public bool Contains(KeyValuePair<string, object> item) => _dynamicMembers.Contains(item);

        /// <inheritdoc />
        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) => _dynamicMembers.CopyTo(array, arrayIndex);

        /// <inheritdoc />
        public bool Remove(KeyValuePair<string, object> item) => _dynamicMembers.Remove(item);

        /// <inheritdoc />
        public int Count => _dynamicMembers.Count;

        /// <inheritdoc />
        public bool IsReadOnly => _dynamicMembers.IsReadOnly;

        /// <inheritdoc />
        public bool ContainsKey(string key) => _dynamicMembers.ContainsKey(key);

        /// <inheritdoc />
        public void Add(string key, object value) => _dynamicMembers.Add(key, value);

        /// <inheritdoc />
        public bool Remove(string key) => _dynamicMembers.Remove(key);

        /// <inheritdoc />
        public bool TryGetValue(string key, out object value) => _dynamicMembers.TryGetValue(key, out value);

        /// <inheritdoc />
        public object this[string key]
        {
            get => _dynamicMembers[key];
            set => _dynamicMembers[key] = value;
        }

        /// <inheritdoc />
        public ICollection<string> Keys => _dynamicMembers.Keys;

        /// <inheritdoc />
        public ICollection<object> Values => _dynamicMembers.Values;

        #endregion
    }

    /// <summary>
    /// The base class for all SQLite table resource types
    /// </summary>
    public abstract class SQLiteTable
    {
        /// <summary>
        /// The unique SQLite row ID for this row
        /// </summary>
        [RESTarMember(order: int.MaxValue), Key]
        public long RowId { get; internal set; }
    }
}