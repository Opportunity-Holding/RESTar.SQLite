using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SQLite;
using RESTar.Deflection;
using RESTar.Deflection.Dynamic;

namespace RESTar.SQLite
{
    internal class SQLiteEnumerator<T> : IEnumerator<T> where T : SQLiteTable
    {
        private static readonly Constructor<T> Constructor = typeof(T).MakeStaticConstructor<T>();
        private Dictionary<string, DeclaredProperty> Columns { get; }
        private SQLiteDataReader Reader { get; }
        private SQLiteConnection Connection { get; }

        public void Dispose()
        {
            Reader.Dispose();
            Connection.Dispose();
        }

        public bool MoveNext() => Reader.Read();
        public void Reset() { }
        object IEnumerator.Current => Current;

        internal SQLiteEnumerator(SQLiteDataReader reader, SQLiteConnection connection)
        {
            Reader = reader;
            Connection = connection;
            Columns = typeof(T).GetColumns();
        }

        private T currentCache;

        private T MakeEntity()
        {
            var entity = Constructor();
            entity.RowId = Reader.GetInt64(0);
            foreach (var column in Columns)
            {
                var value = Reader[column.Key];
                if (value is DBNull) break;
                column.Value.SetValue(entity, value);
            }
            return entity;
        }

        public T Current => currentCache ?? (currentCache = MakeEntity());
    }
}