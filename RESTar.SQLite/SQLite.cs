using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using RESTar.SQLite.Meta;

namespace RESTar.SQLite
{
    /// <summary>
    /// Helper class for accessing RESTar.SQLite tables
    /// </summary>
    /// <typeparam name="T">The SQLiteTable class to bind SQL operations to</typeparam>
    public static class SQLite<T> where T : SQLiteTable
    {
        private const string RowIdParameter = "@rowId";

        /// <summary>
        /// Selects entities in the SQLite database using the RESTar.SQLite O/RM mapping 
        /// facilities. Returns an IEnumerable of the provided resource type.
        /// </summary>
        /// <param name="where">The WHERE clause of the SQL query to execute. Will be preceded 
        /// by "SELECT * FROM {type} " in the actual query</param>
        /// <param name="onlyRowId">Populates only RowIds for the resulting entities</param>
        /// <returns></returns>
        public static IEnumerable<T> Select(string where = null, bool onlyRowId = false) => new EntityEnumerable<T>
        (
            sql: $"SELECT RowId,* FROM {TableMapping<T>.TableName} {where}",
            onlyRowId: onlyRowId
        );

        /// <summary>
        /// Inserts an IEnumerable of SQLiteTable entities into the appropriate SQLite database
        /// table and returns the number of rows affected.
        /// </summary>
        public static int Insert(IEnumerable<T> entities)
        {
            if (entities == null) return 0;
            var (name, columns, param, mappings) = TableMapping<T>.InsertSpec;
            return Database.Transact(command =>
            {
                var count = 0;
                command.CommandText = $"INSERT INTO {name} ({columns}) VALUES ({string.Join(", ", param)})";
                for (var i = 0; i < mappings.Length; i++)
                    command.Parameters.Add(param[i], mappings[i].SQLColumn.DbType.GetValueOrDefault());
                foreach (var entity in entities)
                {
                    entity._OnInsert();
                    for (var i = 0; i < mappings.Length; i++)
                    {
                        object propertyValue = mappings[i].CLRProperty.Get?.Invoke(entity);
                        command.Parameters[param[i]].Value = propertyValue;
                    }
                    count += command.ExecuteNonQuery();
                }
                return count;
            });
        }

        /// <summary>
        /// Updates the corresponding SQLite database table rows for a given IEnumerable 
        /// of updated entities and returns the number of rows affected.
        /// </summary>
        public static int Update(IEnumerable<T> updatedEntities)
        {
            if (updatedEntities == null) return 0;
            var (name, set, param, mappings) = TableMapping<T>.UpdateSpec;
            return Database.Transact(command =>
            {
                var count = 0;
                command.CommandText = $"UPDATE {name} SET {set} WHERE RowId = {RowIdParameter}";
                command.Parameters.Add(RowIdParameter, DbType.Int64);
                for (var i = 0; i < mappings.Length; i++)
                    command.Parameters.Add(param[i], mappings[i].SQLColumn.DbType.GetValueOrDefault());
                foreach (var entity in updatedEntities)
                {
                    entity._OnUpdate();
                    command.Parameters[RowIdParameter].Value = entity.RowId;
                    for (var i = 0; i < mappings.Length; i++)
                    {
                        object propertyValue = mappings[i].CLRProperty.Get?.Invoke(entity);
                        command.Parameters[param[i]].Value = propertyValue;
                    }
                    count += command.ExecuteNonQuery();
                }
                return count;
            });
        }

        /// <summary>
        /// Deletes the corresponding SQLite database table rows for a given IEnumerable 
        /// of entities, and returns the number of database rows affected.
        /// </summary>
        /// <param name="entities"></param>
        /// <returns></returns>
        public static int Delete(IEnumerable<T> entities)
        {
            if (entities == null) return 0;
            return Database.Transact(command =>
            {
                var count = 0;
                command.CommandText = $"DELETE FROM {TableMapping<T>.TableName} WHERE RowId = {RowIdParameter}";
                command.Parameters.Add(RowIdParameter, DbType.Int64);
                foreach (var entity in entities)
                {
                    entity._OnDelete();
                    command.Parameters[RowIdParameter].Value = entity.RowId;
                    count += command.ExecuteNonQuery();
                }
                return count;
            });
        }

        /// <summary>
        /// Counts all rows in the SQLite database where a certain where clause is true.
        /// </summary>
        /// <param name="where">The WHERE clause of the SQL query to execute. Will be preceded 
        /// by "SELECT COUNT(*) FROM {type} " in the actual query</param>
        /// <returns></returns>
        public static long Count(string where = null)
        {
            var sql = $"SELECT COUNT(RowId) FROM {TableMapping<T>.TableName} {where}";
            using (var connection = new SQLiteConnection(Settings.ConnectionString).OpenAndReturn())
            using (var command = new SQLiteCommand(sql, connection))
                return (long) (command.ExecuteScalar() ?? 0L);
        }
    }
}