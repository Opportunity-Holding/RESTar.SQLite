using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using RESTar.Linq;
using RESTar.Meta;
using static System.Reflection.BindingFlags;

namespace RESTar.SQLite
{
    /// <summary>
    /// The integration point between RESTar.SQLite and System.Data.SQLite
    /// </summary>
    internal static class SQLiteDbController
    {
        private static IDictionary<Type, string> TableBindings { get; }

        static SQLiteDbController()
        {
            TableBindings = new ConcurrentDictionary<Type, string>();
        }

        private static bool IsInitiated { get; set; }

        internal static void Init()
        {
            if (IsInitiated) return;
            typeof(object)
                .GetConcreteSubclasses()
                .ForEach(type =>
                {
                    if (!type.HasAttribute(out SQLiteAttribute attribute))
                        return;
                });


            IsInitiated = true;
        }

        private static void CreateTableIfNotExists(Type type) => Query
        (
            sql: $"CREATE TABLE IF NOT EXISTS {type.GetSQLiteTableName()} " +
                 $"({string.Join(",", type.GetColumns().Values.Select(c => c.GetColumnDef()))})",
            action: command => command.ExecuteNonQuery()
        );

        private static void CreateTableIfNotExists(IResource resource) => Query
        (
            sql: $"CREATE TABLE IF NOT EXISTS {resource.GetSQLiteTableName()} " +
                 $"({string.Join(",", resource.GetColumns().Values.Select(c => c.GetColumnDef()))})",
            action: command => command.ExecuteNonQuery()
        );

        private static void AddColumn(IResource resource, DeclaredProperty toAdd) => Query
        (
            sql: $"ALTER TABLE {resource.GetSQLiteTableName()} ADD COLUMN {toAdd.GetColumnDef()}",
            action: command => command.ExecuteNonQuery()
        );

        private static void UpdateTableSchema(IResource resource)
        {
            var uncheckedColumns = new Dictionary<string, DeclaredProperty>(resource.GetColumns(), StringComparer.OrdinalIgnoreCase);
            Query($"PRAGMA table_info({resource.GetSQLiteTableName()})", row =>
            {
                var columnName = row.GetString(1);
                var columnType = row.GetString(2);
                if (!uncheckedColumns.TryGetValue(columnName, out var correspondingColumn))
                    return;
                var foundType = correspondingColumn.Type.ToSQLType();
                if (!string.Equals(foundType, columnType, StringComparison.OrdinalIgnoreCase))
                {
                    throw new SQLiteException($"The underlying database schema for SQLite resource '{resource.Name}' has " +
                                              $"changed. Cannot convert column of SQLite type '{columnType}' to '{foundType}' " +
                                              $"in SQLite database table '{resource.GetSQLiteTableName()}'.");
                }
                uncheckedColumns.Remove(columnName);
            });
            uncheckedColumns.Values.ForEach(column => AddColumn(resource, column));
        }

        internal static void EnsureTablesForResources(IEnumerable<IResource> resources) => resources.ForEach(resource =>
        {
            CreateTableIfNotExists(resource);
            UpdateTableSchema(resource);
            TableCache.Add(resource);
        });

        internal static int Query(string sql)
        {
            var res = 0;
            Query(sql, command => res = command.ExecuteNonQuery());
            return res;
        }

        private static void Query(string sql, Action<SQLiteCommand> action)
        {
            using (var connection = new SQLiteConnection(Settings.ConnectionString))
            {
                connection.Open();
                action(new SQLiteCommand(sql, connection) {CommandType = CommandType.Text});
            }
        }

        internal static void Query(string sql, Action<SQLiteDataReader> rowAction)
        {
            using (var connection = new SQLiteConnection(Settings.ConnectionString))
            {
                connection.Open();
                var command = new SQLiteCommand(sql, connection) {CommandType = CommandType.Text};
                using (var reader = command.ExecuteReader())
                    while (reader.Read())
                        rowAction(reader);
            }
        }

        internal static void Transact(Action<SQLiteCommand> commandAction)
        {
            using (var connection = new SQLiteConnection(Settings.ConnectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(connection) {CommandType = CommandType.Text})
                {
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            commandAction(command);
                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                        }
                    }
                }
            }
        }
    }
}