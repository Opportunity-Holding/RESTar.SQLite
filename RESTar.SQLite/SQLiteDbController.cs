using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using RESTar.SQLite.Meta;
using static RESTar.SQLite.TableMappingKind;

namespace RESTar.SQLite
{
    public enum CLRDataType
    {
        Unsupported = 0,
        Int16,
        Int32,
        Int64,
        Single,
        Double,
        Decimal,
        Byte,
        String,
        Boolean,
        DateTime,
    }

    public enum SQLDataType
    {
        Unsupported = 0,
        SMALLINT,
        INT,
        BIGINT,
        SINGLE,
        DOUBLE,
        DECIMAL,
        TINYINT,
        TEXT,
        BOOLEAN,
        DATETIME
    }

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
            SetupDeclaredTypes();
            IsInitiated = true;
        }

        private static void Validate(Type type)
        {
            if (type.GetConstructor(Type.EmptyTypes) == null)
                throw new SQLiteException($"Expected parameterless constructor for SQLite type '{type}'.");
            if (type.FullName == null)
                throw new SQLiteException($"SQLite encountered an unknown type: '{type.GUID}'");
            var columnProperties = type.GetDeclaredColumnProperties();
            if (columnProperties.Values.All(p => p.Name == "RowId"))
                throw new SQLiteException(
                    $"No public auto-implemented instance properties found in type '{type}'. SQLite does not support empty tables, " +
                    "so each SQLiteTable must define at least one public auto-implemented instance property.");
        }

        /// <summary>
        /// Finds all static declared SQLiteTable CLR classes and maps them to corresponding SQLite tables
        /// </summary>
        private static void SetupDeclaredTypes()
        {
            foreach (var type in typeof(SQLiteTable).GetConcreteSubclasses())
            {
                Validate(type);
                new TableMapping
                (
                    clrClass: type,
                    tableMappingKind: type.IsSubclassOf(typeof(ElasticSQLiteTable)) ? ElasticDeclared : StaticDeclared
                );
            }
        }

        internal static int Query(string sql)
        {
            var res = 0;
            Query(sql, command => res = command.ExecuteNonQuery());
            return res;
        }

        internal static void Query(string sql, Action<SQLiteCommand> action)
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