using System.Collections;
using System.Collections.Generic;

namespace RESTar.SQLite
{
    internal class SQLiteEnumerable<T> : IEnumerable<T> where T : SQLiteTable
    {
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        private SQLiteEnumerator<T> Enumerator { get; }
        internal SQLiteEnumerable(string sql) => Enumerator = new SQLiteEnumerator<T>(sql);
        public IEnumerator<T> GetEnumerator() => Enumerator;
    }
}