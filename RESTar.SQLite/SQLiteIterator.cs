using System.Collections;
using System.Collections.Generic;
using System.Data.SQLite;

namespace RESTar.SQLite
{
    internal class SQLiteIterator<T> : IEnumerable<T> where T : SQLiteTable
    {
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        private SQLiteEnumerator<T> Enumerator { get; }
        internal SQLiteIterator(SQLiteDataReader reader, SQLiteConnection connection) => Enumerator = new SQLiteEnumerator<T>(reader, connection);
        public IEnumerator<T> GetEnumerator() => Enumerator;
    }
}