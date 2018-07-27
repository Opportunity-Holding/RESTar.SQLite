using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using RESTar.Linq;
using RESTar.SQLite.Meta;

namespace RESTar.SQLite
{
    /// <summary>
    /// Helper class for accessing RESTar.SQLite tables
    /// </summary>
    /// <typeparam name="T">The SQLiteTable class to bind SQL operations to</typeparam>
    public static class SQLite<T> where T : SQLiteTable
    {
        /// <summary>
        /// Selects entities in the SQLite database using the RESTar.SQLite O/RM mapping 
        /// facilities. Returns an IEnumerable of the provided resource type.
        /// </summary>
        /// <param name="where">The WHERE clause of the SQL query to execute. Will be preceded 
        /// by "SELECT * FROM {type} " in the actual query</param>
        /// <param name="onlyRowId">Populates only RowIds for the resulting entities</param>
        /// <returns></returns>
        public static IEnumerable<T> Select(string where = null, bool onlyRowId = false)
        {
            var sql = $"SELECT RowId,* FROM {TableMapping<T>.TableName} {where}";
            return new EntityEnumerable<T>(sql, onlyRowId);
        }

        /// <summary>
        /// Inserts an IEnumerable of SQLiteTable entities into the appropriate SQLite database
        /// table and returns the number of rows affected.
        /// </summary>
        public static int Insert(IEnumerable<T> entities)
        {
            if (entities == null) return 0;
            var count = 0;
            var (name, columns, param, mappings) = TableMapping<T>.InsertSpec;
            using (var connection = new SQLiteConnection(Settings.ConnectionString).OpenAndReturn())
            using (var command = connection.CreateCommand())
            using (var transaction = connection.BeginTransaction())
            {
                command.CommandText = $"INSERT INTO {name} ({columns}) VALUES ({string.Join(", ", param)})";
                for (var i = 0; i < mappings.Length; i++)
                    command.Parameters.Add(param[i], mappings[i].SQLColumn.DbType.GetValueOrDefault());
                try
                {
                    foreach (var entity in entities)
                    {
                        entity._OnInsert();
                        for (var i = 0; i < mappings.Length; i++)
                            command.Parameters[param[i]].Value = mappings[i].CLRProperty.Get?.Invoke(entity); // ?.MakeSQLValueLiteral();
                        count += command.ExecuteNonQuery();
                    }
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                }
            }
            return count;
        }

        /// <summary>
        /// Updates the corresponding SQLite database table rows for a given IEnumerable 
        /// of updated entities and returns the number of rows affected.
        /// </summary>
        public static int Update(IEnumerable<T> updatedEntities)
        {
            if (updatedEntities == null) return 0;
            var count = 0;
            var sqlStub = $"UPDATE {TableMapping<T>.TableName} SET ";
            Db.Transact(command => updatedEntities.ForEach(updatedEntity =>
            {
                updatedEntity._OnUpdate();
                command.CommandText = $"{sqlStub} {updatedEntity.ToSQLiteUpdateSet()} WHERE RowId={updatedEntity.RowId}";
                count += command.ExecuteNonQuery();
            }));
            return count;
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
            var sqlstub = $"DELETE FROM {TableMapping<T>.TableName} WHERE RowId=";
            var count = 0;
            Db.Transact(command => entities.ForEach(entity =>
            {
                entity._OnDelete();
                command.CommandText = sqlstub + entity.RowId;
                count += command.ExecuteNonQuery();
            }));
            return count;
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
            using (var connection = new SQLiteConnection(Settings.ConnectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(sql, connection) {CommandType = CommandType.Text})
                    return (long) command.ExecuteScalar();
            }
        }
    }
}