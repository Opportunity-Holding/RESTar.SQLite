using System.Collections.Concurrent;
using System.Collections.Generic;
using Newtonsoft.Json;
using RESTar.Meta;
using RESTar.Resources;

namespace RESTar.SQLite
{
    /// <inheritdoc cref="SQLiteTable" />
    /// <inheritdoc cref="IDynamicMemberValueProvider" />
    /// <summary>
    /// Defines an elastic SQLite table
    /// </summary>
    public abstract class ElasticSQLiteTable : SQLiteTable, IDynamicMemberValueProvider
    {
        /// <summary>
        /// The dynamic members stored for this instance
        /// </summary>
        [RESTarMember(hide: true), JsonExtensionData(ReadData = true, WriteData = true)]
        public IDictionary<string, object> DynamicMembers { get; }

        /// <summary>
        /// Indexer used for access to dynamic members
        /// </summary>
        public object this[string memberName]
        {
            get => DynamicMembers.SafeGet(memberName);
            set => DynamicMembers[memberName] = value;
        }

        /// <summary>
        /// Creates a new instance of this ElasticSQLiteTable type
        /// </summary>
        protected ElasticSQLiteTable()
        {
            DynamicMembers = new ConcurrentDictionary<string, object>();
        }

        /// <inheritdoc />
        public bool TryGetValue(string memberName, out string actualMemberName, out object value)
        {
            try
            {
                var name = memberName.Capitalize();
                if (DynamicMembers.TryGetValue(name, out value))
                {
                    actualMemberName = name;
                    return true;
                }
                return DynamicMembers.TryFindInDictionary(name, out actualMemberName, out value);
            }
            catch
            {
                actualMemberName = null;
                value = null;
                return false;
            }
        }

        /// <inheritdoc />
        public bool TrySetValue(string memberName, object value)
        {
            try
            {
                DynamicMembers[memberName.Capitalize()] = value;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}