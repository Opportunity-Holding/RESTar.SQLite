using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SQLite;
using RESTar.Meta;
using RESTar.SQLite.Meta;

namespace RESTar.SQLite
{
    internal class EntityEnumerator<T> : IEnumerator<T> where T : SQLiteTable
    {
        private static readonly Constructor<T> Constructor = typeof(T).MakeStaticConstructor<T>();
        private SQLiteDataReader Reader { get; set; }
        private SQLiteConnection Connection { get; }
        private SQLiteCommand Command { get; set; }
        private string SQL { get; }
        private bool OnlyRowId { get; }

        public void Dispose()
        {
            Command.Dispose();
            Reader.Dispose();
            Connection.Dispose();
        }

        public bool MoveNext() => Reader.Read();

        public void Reset()
        {
            Command.Dispose();
            Reader.Dispose();
            Init();
        }

        private void Init()
        {
            Command = new SQLiteCommand(SQL, Connection);
            Reader = Command.ExecuteReader();
        }

        internal EntityEnumerator(string sql, bool onlyRowId)
        {
            OnlyRowId = onlyRowId;
            Connection = new SQLiteConnection(Settings.ConnectionString);
            Connection.Open();
            SQL = sql;
            Init();
        }

        object IEnumerator.Current => Current;
        public T Current => MakeEntity();

        private T MakeEntity()
        {
            var entity = Constructor();
            entity.RowId = Reader.GetInt64(0);
            if (!OnlyRowId)
            {
                foreach (var column in TableMapping<T>.TransactMappings)
                {
                    var value = Reader[column.SQLColumn.Name];
                    if (!(value is DBNull))
                        column.CLRProperty.Set?.Invoke(entity, value);
                    else if (!column.CLRProperty.IsDeclared)
                        column.CLRProperty.Set?.Invoke(entity, null);
                }
            }
            entity._OnSelect();
            return entity;
        }
    }
}