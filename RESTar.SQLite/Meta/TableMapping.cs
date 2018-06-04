using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RESTar.Linq;

namespace RESTar.SQLite.Meta
{
    /// <summary>
    /// A static class for accessing table mappings
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class TableMapping<T> where T : SQLiteTable
    {
        private static TableMapping mapping;

        /// <summary>
        /// Gets the table mapping for the given type
        /// </summary>
        public static TableMapping Get => mapping ?? (mapping = TableMapping.Get(typeof(T)));

        /// <summary>
        /// Gets the name from the table mapping for the given type
        /// </summary>
        public static string TableName => Get.TableName;

        /// <summary>
        /// Gets the column mappings from the table mapping for the given type
        /// </summary>
        public static ColumnMappings ColumnMappings => Get.ColumnMappings;

        /// <summary>
        /// Gets the column names from the table mapping for the given type
        /// </summary>
        public static HashSet<string> ColumnNames => mapping.ColumnNames;
    }

    /// <summary>
    /// Represents a mapping between a CLR class and an SQLite table
    /// </summary>
    public class TableMapping
    {
        #region Static

        static TableMapping() => TableMappingByType = new ConcurrentDictionary<Type, TableMapping>();
        private static IDictionary<Type, TableMapping> TableMappingByType { get; }
        private static void RemoveTable(TableMapping tableMapping) => TableMappingByType.Remove(tableMapping.CLRClass);

        /// <summary>
        /// Gets the table mapping for a given CLR type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static TableMapping Get(Type type) => TableMappingByType.SafeGet(type);

        #endregion

        /// <summary>
        /// The kind of this table mapping
        /// </summary>
        public TableMappingKind TableMappingKind { get; }

        /// <summary>
        /// The CLR class of the mapping
        /// </summary>
        public Type CLRClass { get; }

        /// <summary>
        /// The name of the mapped SQLite table
        /// </summary>
        public string TableName { get; }

        /// <summary>
        /// The column mappings of this table mapping
        /// </summary>
        public ColumnMappings ColumnMappings { get; private set; }

        /// <summary>
        /// The names of the mapped columns of this table mapping
        /// </summary>
        public HashSet<string> ColumnNames { get; private set; }

        private HashSet<string> GetMakeColumnNames() => new HashSet<string>(
            ColumnMappings.Select(c => c.SQLColumn.Name), StringComparer.OrdinalIgnoreCase);

        private void DropTable()
        {
            SQLiteDbController.Query($"DROP TABLE IF EXISTS {TableName}");
            RemoveTable(this);
        }

        private bool Exists => SQLiteDbController.Query($"PRAGMA table_info({TableName})") > 0;

        /// <summary>
        /// Gets the SQL columns of the mapped SQL table
        /// </summary>
        /// <returns></returns>
        public List<SQLColumn> GetSQLColumns()
        {
            var columns = new List<SQLColumn>();
            SQLiteDbController.Query($"PRAGMA table_info({TableName})", row => columns.Add(new SQLColumn(row.GetString(1), row.GetString(2))));
            return columns;
        }

        private void Update()
        {
            ColumnMappings = GetDeclaredColumnMappings();
            ColumnMappings.Push();
            var columnNames = GetMakeColumnNames();
            GetSQLColumns()
                .Where(column => !columnNames.Contains(column.Name))
                .ForEach(column => ColumnMappings.Add(new ColumnMapping
                (
                    tableMapping: this,
                    clrProperty: new CLRProperty(column.Name, column.Type.ToCLRTypeCode(true)),
                    sqlColumn: column
                )));
            ColumnNames = GetMakeColumnNames();
        }

        private ColumnMappings GetDeclaredColumnMappings() => CLRClass
            .GetDeclaredColumnProperties()
            .Values
            .Select(property => new ColumnMapping
            (
                tableMapping: this,
                clrProperty: property,
                sqlColumn: new SQLColumn
                (
                    name: property.MemberAttribute?.ColumnName ?? property.Name,
                    type: property.Type.ToSQLTypeString()
                )
            ))
            .ToColumnMappings();

        /// <summary>
        /// Creates a new table mapping, mapping a CLR class to an SQL table
        /// </summary>
        internal TableMapping(Type clrClass, TableMappingKind tableMappingKind)
        {
            TableMappingKind = tableMappingKind;
            CLRClass = clrClass;
            TableName = clrClass.GetCustomAttribute<SQLiteAttribute>()?.CustomTableName ?? clrClass.FullName?.Replace('.', '$')
                        ?? throw new SQLiteException("RESTar.SQLite encountered an unknown CLR class when creating table mappings");
            TableMappingByType[CLRClass] = this;
            if (!Exists) SQLiteDbController.Query($"CREATE TABLE {TableName} ({GetDeclaredColumnMappings().ToSQL()})");
            Update();
        }
    }
}