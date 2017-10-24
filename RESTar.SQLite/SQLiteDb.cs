using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using RESTar.Deflection.Dynamic;
using RESTar.Internal;
using RESTar.Linq;

namespace RESTar.SQLite
{
    internal static class SQLiteDb
    {
        #region Operations

        private static void CreateTableIfNotExists(IResource resource) => Query
        (
            sql: $"CREATE TABLE IF NOT EXISTS {resource.GetSQLiteTableName()} " +
                 $"({string.Join(",", resource.GetColumns().Values.Select(c => c.GetColumnDef()))})",
            action: command => command.ExecuteNonQuery()
        );

        private static void AddColumn(IResource resource, StaticProperty toAdd) => Query
        (
            sql: $"ALTER TABLE {resource.GetSQLiteTableName()} ADD COLUMN {toAdd.GetColumnDef()}",
            action: command => command.ExecuteNonQuery()
        );

        private static void UpdateTableSchema(IResource resource)
        {
            var uncheckedColumns = resource.GetColumns();

            Query($"PRAGMA table_info({resource.GetSQLiteTableName()})", row =>
            {
                var columnName = row.GetString(1);
                var columnType = row.GetString(2);
                if (!uncheckedColumns.TryGetValue(columnName, out var correspondingColumn))
                    return;
                var foundType = correspondingColumn.Type.ToSQLType();
                if (foundType != columnType)
                {
                    throw new SQLiteException($"The underlying database schema for SQLite resource '{resource.Name}' has " +
                                              $"changed. Cannot convert column of SQLite type '{columnType}' to '{foundType}' " +
                                              $"in SQLite database table '{resource.GetSQLiteTableName()}'.");
                }
                uncheckedColumns.Remove(columnName);
            });
            uncheckedColumns.Values.ForEach(column => AddColumn(resource, column));
        }

        #endregion

        internal static void SetupTables(IEnumerable<IResource> resources) => resources.ForEach(resource =>
        {
            CreateTableIfNotExists(resource);
            UpdateTableSchema(resource);
        });

        internal static int Query(string sql)
        {
            var res = 0;
            Query(sql, command => res = command.ExecuteNonQuery());
            return res;
        }

        private static void Query(string sql, Action<SQLiteCommand> action)
        {
            using (var connection = new SQLiteConnection(Settings.Instance.DatabaseConnectionString))
            {
                connection.Open();
                action(new SQLiteCommand(sql, connection));
            }
        }

        internal static void Query(string sql, Action<SQLiteDataReader> rowAction)
        {
            using (var connection = new SQLiteConnection(Settings.Instance.DatabaseConnectionString))
            {
                connection.Open();
                using (var reader = new SQLiteCommand(sql, connection).ExecuteReader())
                    while (reader.Read()) rowAction(reader);
            }
        }

        internal static IEnumerable<T> Query<T>(string sql, IDictionary<string, StaticProperty> columns) where T : SQLiteTable
        {
            using (var connection = new SQLiteConnection(Settings.Instance.DatabaseConnectionString))
            {
                connection.Open();
                using (var reader = new SQLiteCommand(sql, connection).ExecuteReader())
                    while (reader.Read()) yield return MakeEntity<T>(reader, columns);
            }
        }

        private static T MakeEntity<T>(IDataRecord reader, IDictionary<string, StaticProperty> columns) where T : SQLiteTable
        {
            var entity = Activator.CreateInstance<T>();
            entity.RowId = reader.GetInt64(0);
            foreach (var column in columns)
            {
                var value = reader[column.Key];
                if (!(value is DBNull))
                    column.Value.SetValue(entity, value);
            }
            return entity;
        }
    }
}