using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Text;
using RESTar.Linq;

namespace RESTar.SQLite
{
    public static class SQLite<T> where T : SQLiteTable
    {
        /// <summary>
        /// Selects entities in the SQLite database using the RESTar.SQLite O/RM mapping 
        /// facilities. Returns an IEnumerable of the provided resource type.
        /// </summary>
        /// <param name="where">The WHERE clause of the SQL query to execute. Will be preceded 
        /// by "SELECT * FROM {type} " in the actual query</param>
        /// <returns></returns>
        public static IEnumerable<T> Select(string where)
        {
            var sql = $"SELECT RowId,* FROM {typeof(T).GetSQLiteTableName().Fnuttify()} {where}";
            using (var connection = new SQLiteConnection(Settings.ConnectionString))
            {
                connection.Open();
                using (var reader = new SQLiteCommand(sql, connection).ExecuteReader())
                {
                    T MakeEntity()
                    {
                        var entity = Activator.CreateInstance<T>();
                        entity.RowId = reader.GetInt64(0);
                        typeof(T).GetColumns().ForEach(column =>
                        {
                            var value = reader[column.Key];
                            if (value is DBNull) return;
                            column.Value.SetValue(entity, value);
                        });
                        return entity;
                    }

                    while (reader.Read()) yield return MakeEntity();
                }
            }
        }

        /// <summary>
        /// Inserts an IEnumerable of SQLiteTable entities into the appropriate SQLite database
        /// table and returns the number of rows affected.
        /// </summary>
        public static int Insert(IEnumerable<T> entities)
        {
            var columns = typeof(T).GetColumns().Values;
            var sqlStub = $"INSERT INTO {typeof(T).GetSQLiteTableName().Fnuttify()} VALUES ";
            var stringBuilder = new StringBuilder(sqlStub);
            var iterations = 0;
            foreach (var entity in entities)
            {
                if (iterations > 0)
                    stringBuilder.Append(',');
                stringBuilder.Append('(');
                stringBuilder.Append(entity.ToSQLiteInsertInto(columns));
                stringBuilder.Append(')');
                iterations += 1;
            }
            if (iterations == 0) return 0;
            return SQLiteDb.Query(stringBuilder.ToString());
        }

        /// <summary>
        /// Updates the corresponding SQLite database table rows for a given IEnumerable 
        /// of updated entities and returns the number of rows affected.
        /// </summary>
        public static int Update(IEnumerable<T> updatedEntities)
        {
            var columns = typeof(T).GetColumns().Values;
            var updateTable = $"UPDATE {typeof(T).GetSQLiteTableName()} SET ";
            var stringBuilder = new StringBuilder();
            var iterations = 0;
            foreach (var entity in updatedEntities)
            {
                stringBuilder.Append(updateTable);
                var index = 0;
                foreach (var column in columns)
                {
                    if (index > 0) stringBuilder.Append(',');
                    stringBuilder.Append(column.Name);
                    stringBuilder.Append('=');
                    var valueLiteral = ((object) column.GetValue(entity)).MakeSQLValueLiteral();
                    stringBuilder.Append(valueLiteral);
                    index += 1;
                }
                stringBuilder.Append("WHERE RowId=");
                stringBuilder.Append(entity.RowId);
                stringBuilder.Append(';');
                iterations += 1;
            }
            if (iterations == 0) return 0;
            return SQLiteDb.Query(stringBuilder.ToString());
        }

        /// <summary>
        /// Deletes the corresponding SQLite database table rows for a given IEnumerable 
        /// of entities, and returns the number of database rows affected.
        /// </summary>
        /// <param name="entities"></param>
        /// <returns></returns>
        public static int Delete(IEnumerable<T> entities)
        {
            var sqlstub = $"DELETE FROM {typeof(T).GetSQLiteTableName()} WHERE RowId=";
            var stringBuilder = new StringBuilder(sqlstub);
            var iterations = 0;
            foreach (var entity in entities)
            {
                if (iterations > 0)
                    stringBuilder.Append(" OR RowId=");
                stringBuilder.Append(entity.RowId);
                iterations += 1;
            }
            if (iterations == 0) return 0;
            return SQLiteDb.Query(stringBuilder.ToString());
        }

        /// <summary>
        /// Counts all rows in the SQLite database where a certain where clause is true.
        /// </summary>
        /// <param name="where">The WHERE clause of the SQL query to execute. Will be preceded 
        /// by "SELECT COUNT(*) FROM {type} " in the actual query</param>
        /// <returns></returns>
        public static long Count(string where)
        {
            var sql = $"SELECT COUNT(*) FROM {typeof(T).GetSQLiteTableName().Fnuttify()} {where}";
            var count = 0L;
            using (var connection = new SQLiteConnection(Settings.ConnectionString))
            {
                connection.Open();
                using (var reader = new SQLiteCommand(sql, connection).ExecuteReader())
                    while (reader.Read()) count = reader.GetInt64(0);
            }
            return count;
        }
    }
}