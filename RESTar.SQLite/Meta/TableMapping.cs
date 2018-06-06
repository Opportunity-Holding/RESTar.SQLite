using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RESTar.Linq;
using RESTar.Meta;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Resources.Operations;
using RESTar.Resources.Templates;

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

        public static IEnumerable<ColumnMapping> TransactMappings => Get.TransactMappings;

        /// <summary>
        /// Gets the column names from the table mapping for the given type
        /// </summary>
        internal static HashSet<string> SQLColumnNames => mapping.SQLColumnNames;
    }

    /// <inheritdoc />
    /// <summary>
    /// Represents a mapping between a CLR class and an SQLite table
    /// </summary>
    [RESTar(Method.GET)]
    public class TableMapping : ISelector<TableMapping>
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

        internal static IEnumerable<TableMapping> All => TableMappingByType.Values;

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
        /// The RESTar resource instance, if any, corresponding to this mapping
        /// </summary>
        public IEntityResource Resource { get; internal set; }

        public IEnumerable<ColumnMapping> TransactMappings { get; private set; }

        /// <summary>
        /// The names of the mapped columns of this table mapping
        /// </summary>
        internal HashSet<string> SQLColumnNames { get; private set; }

        /// <summary>
        /// Does this table mapping have a corresponding SQL table?
        /// </summary>
        public bool Exists
        {
            get
            {
                var results = 0;
                SQLiteDbController.Query($"PRAGMA table_info({TableName})", rowAction: row => results += 1);
                return results > 0;
            }
        }

        /// <inheritdoc />
        public IEnumerable<TableMapping> Select(IRequest<TableMapping> request)
        {
            return TableMappingByType.Values.Where(request.Conditions);
        }

        [RESTar]
        public class Options : OptionsTerminal
        {
            protected override IEnumerable<Option> GetOptions() => new[]
                {new Option("update", "Updates all table mappings", _ => TableMappingByType.Values.ForEach(m => m.Update()))};
        }

        private HashSet<string> MakeColumnNames() => new HashSet<string>(
            ColumnMappings.Select(c => c.SQLColumn.Name), StringComparer.OrdinalIgnoreCase);

        private void DropTable()
        {
            SQLiteDbController.Query($"DROP TABLE IF EXISTS {TableName}");
            RemoveTable(this);
        }

        /// <summary>
        /// Gets the SQL columns of the mapped SQL table
        /// </summary>
        /// <returns></returns>
        public List<SQLColumn> GetSQLColumns()
        {
            var columns = new List<SQLColumn>();
            SQLiteDbController.Query($"PRAGMA table_info({TableName})",
                row => columns.Add(new SQLColumn(row.GetString(1), row.GetString(2).ParseSQLDataType())));
            return columns;
        }

        internal void ReloadColumnNames() => SQLColumnNames = MakeColumnNames();

        internal void Update()
        {
            ColumnMappings = GetDeclaredColumnMappings();
            ColumnMappings.Push();
            var columnNames = MakeColumnNames();
            GetSQLColumns()
                .Where(column => !columnNames.Contains(column.Name))
                .ForEach(column => ColumnMappings.Add(new ColumnMapping
                (
                    tableMapping: this,
                    clrProperty: new CLRProperty(column.Name, column.Type.ToCLRTypeCode()),
                    sqlColumn: column
                )));
            ReloadColumnNames();
            TransactMappings = ColumnMappings.Where(mapping => !mapping.CLRProperty.IsIgnored).ToArray();
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
                    type: property.Type.ToSQLDataType()
                )
            ))
            .ToColumnMappings();

        internal string GetCreateTableSQL() => $"CREATE TABLE {TableName} ({(ColumnMappings ?? GetDeclaredColumnMappings()).ToSQL()});";

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
            if (!Exists) SQLiteDbController.Query(GetCreateTableSQL());
            Update();
        }
    }
}