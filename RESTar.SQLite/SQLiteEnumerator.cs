using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SQLite;
using RESTar.Meta;
using RESTar.SQLite.Meta;

namespace RESTar.SQLite
{
    internal class SQLiteEnumerator<T> : IEnumerator<T> where T : SQLiteTable
    {
        private static readonly Constructor<T> Constructor = typeof(T).MakeStaticConstructor<T>();
        private SQLiteDataReader Reader { get; set; }
        private SQLiteConnection Connection { get; }
        private string SQL { get; }

        public void Dispose()
        {
            Reader.Dispose();
            Connection.Dispose();
        }

        public bool MoveNext() => Reader.Read();

        public void Reset()
        {
            Reader.Dispose();
            Init();
        }

        private void Init() => Reader = new SQLiteCommand(SQL, Connection).ExecuteReader();

        internal SQLiteEnumerator(string sql)
        {
            Connection = new SQLiteConnection(Settings.ConnectionString);
            Connection.Open();
            SQL = sql;
            Init();
        }

        private T MakeEntity()
        {
            var entity = Constructor();
            entity.RowId = Reader.GetInt64(0);
            foreach (var column in TableMapping<T>.TransactMappings)
            {
                var value = Reader[column.SQLColumn.Name];
                if (!(value is DBNull))
                    column.CLRProperty.Set?.Invoke(entity, value);
                else if (!column.CLRProperty.IsDeclared)
                    column.CLRProperty.Set?.Invoke(entity, null);
            }
            return entity;
        }

        object IEnumerator.Current => Current;
        public T Current => MakeEntity();
    }
}