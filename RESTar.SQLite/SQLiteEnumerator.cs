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
            Columns = typeof(T).GetColumns();
            Connection = new SQLiteConnection(Settings.ConnectionString);
            Connection.Open();
            SQL = sql;
            Init();
        }

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

        object IEnumerator.Current => Current;
        public T Current => MakeEntity();
    }
}