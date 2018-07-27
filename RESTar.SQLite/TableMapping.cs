using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RESTar.Admin;
using RESTar.Linq;
using RESTar.Meta;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Resources.Operations;
using RESTar.Resources.Templates;
using RESTar.SQLite.Meta;

namespace RESTar.SQLite
{
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

        #region Public

        /// <summary>
        /// The CLR class of the mapping
        /// </summary>
        [RESTarMember(ignore: true)] public Type CLRClass { get; }

        /// <summary>
        /// The name of the CLR class of the mapping
        /// </summary>
        public string ClassName => CLRClass.FullName?.Replace('+', '.');

        /// <summary>
        /// The name of the mapped SQLite table
        /// </summary>
        public string TableName { get; }

        /// <summary>
        /// The kind of this table mapping
        /// </summary>
        public TableMappingKind TableMappingKind { get; }

        /// <summary>
        /// Is this table mapping declared, as opposed to procedural?
        /// </summary>
        public bool IsDeclared { get; }

        /// <summary>
        /// The column mappings of this table mapping
        /// </summary>
        public ColumnMappings ColumnMappings { get; private set; }

        /// <summary>
        /// Does this table mapping have a corresponding SQL table?
        /// </summary>
        public bool Exists
        {
            get
            {
                var results = 0;
                Db.Query($"PRAGMA table_info({TableName})", rowAction: row => results += 1);
                return results > 0;
            }
        }

        #endregion

        /// <summary>
        /// The RESTar resource instance, if any, corresponding to this mapping
        /// </summary>
        internal IEntityResource Resource { get; set; }

        internal IEnumerable<ColumnMapping> TransactMappings { get; private set; }

        /// <summary>
        /// The names of the mapped columns of this table mapping
        /// </summary>
        internal HashSet<string> SQLColumnNames { get; private set; }

        internal (string name, string columns, string[] param, ColumnMapping[] mappings) InsertSpec { get; private set; }

        #region RESTar

        /// <inheritdoc />
        public IEnumerable<TableMapping> Select(IRequest<TableMapping> request)
        {
            return TableMappingByType.Values.Where(request.Conditions);
        }

        /// <inheritdoc />
        /// <summary>
        /// Options for table mappings
        /// </summary>
        [RESTar]
        public class Options : OptionsTerminal
        {
            /// <inheritdoc />
            protected override IEnumerable<Option> GetOptions()
            {
                return new[] {new Option("Update", "Updates all table mappings", _ => TableMappingByType.Values.ForEach(m => m.Update()))};
            }
        }

        #endregion

        /// <summary>
        /// Creates a new table mapping, mapping a CLR class to an SQL table
        /// </summary>
        private TableMapping(Type clrClass)
        {
            Validate(clrClass);
            TableMappingKind = clrClass.IsSubclassOf(typeof(ElasticSQLiteTable)) ? TableMappingKind.Elastic : TableMappingKind.Static;
            IsDeclared = !clrClass.Assembly.Equals(TypeBuilder.Assembly);
            CLRClass = clrClass;
            TableName = clrClass.GetCustomAttribute<SQLiteAttribute>()?.CustomTableName ?? clrClass.FullName?.Replace('+', '.').Replace('.', '$')
                        ?? throw new SQLiteException("RESTar.SQLite encountered an unknown CLR class when creating table mappings");
            TableMappingByType[CLRClass] = this;
            if (!Exists) Db.Query(GetCreateTableSQL());
            Update();
        }

        #region Helpers

        private static void Validate(Type type)
        {
            if (type.FullName == null)
                throw new SQLiteException($"RESTar.SQLite encountered an unknown type: '{type.GUID}'");
            if (type.Namespace == null)
                throw new SQLiteException($"RESTar.SQLite encountered a type '{type}' with no specified namespace.");
            if ((type.FullName.StartsWith("RESTar.", StringComparison.OrdinalIgnoreCase) ||
                 type.Namespace.StartsWith("RESTar.", StringComparison.OrdinalIgnoreCase))
                && !type.Assembly.Equals(typeof(TableMapping).Assembly)
                && !type.Assembly.Equals(TypeBuilder.Assembly))
                throw new SQLiteException($"RESTar.SQLite encountered a type '{type}' with an invalid name or namespace. Must not " +
                                          "start with 'RESTar'");
            if (type.IsGenericType)
                throw new SQLiteException($"Invalid SQLite table mapping for CLR class '{type}'. Cannot map a " +
                                          "generic CLR class.");

            if (type.GetConstructor(Type.EmptyTypes) == null)
                throw new SQLiteException($"Expected parameterless constructor for SQLite type '{type}'.");
            var columnProperties = type.GetDeclaredColumnProperties();
            if (columnProperties.Values.All(p => p.Name == "RowId"))
                throw new SQLiteException(
                    $"No public auto-implemented instance properties found in type '{type}'. SQLite does not support empty tables, " +
                    "so each SQLiteTable must define at least one public auto-implemented instance property.");
        }

        private HashSet<string> MakeColumnNames()
        {
            var allColumns = new HashSet<string>(ColumnMappings.Select(c => c.SQLColumn.Name), StringComparer.OrdinalIgnoreCase);
            var notRowId = ColumnMappings.Where(m => !m.IsRowId).ToArray();
            var columns = string.Join(", ", notRowId.Select(c => c.SQLColumn.Name));
            var mappings = notRowId;
            var param = notRowId.Select(c => $"@{c.SQLColumn.Name}").ToArray();
            InsertSpec = (TableName, columns, param, mappings);
            return allColumns;
        }

        private void DropTable()
        {
            Db.Query($"DROP TABLE IF EXISTS {TableName}");
            RemoveTable(this);
        }

        internal void DropColumns(List<ColumnMapping> mappings)
        {
            mappings.ForEach(mapping => ColumnMappings.Remove(mapping));
            ReloadColumnNames();
            var tempColumnNames = new HashSet<string>(SQLColumnNames);
            tempColumnNames.Remove("rowid");
            var columnsSQL = string.Join(", ", tempColumnNames);
            var tempName = $"__{TableName}__RESTAR_TEMP";
            var query = "PRAGMA foreign_keys=off;BEGIN TRANSACTION;" +
                        $"ALTER TABLE {TableName} RENAME TO {tempName};" +
                        $"{GetCreateTableSQL()}" +
                        $"INSERT INTO {TableName} ({columnsSQL})" +
                        $"  SELECT {columnsSQL} FROM {tempName};" +
                        $"DROP TABLE {tempName};" +
                        "COMMIT;PRAGMA foreign_keys=on;";
            var indexRequest = Context.Root.CreateRequest<DatabaseIndex>();
            indexRequest.Conditions.Add(new Condition<DatabaseIndex>
            (
                key: nameof(DatabaseIndex.ResourceName),
                op: Operators.EQUALS,
                value: Resource.Name
            ));
            var tableIndexesToKeep = indexRequest
                .EvaluateToEntities()
                .Where(index => !index.Columns.Any(column => mappings.Any(mapping => column.Name.EqualsNoCase(mapping.SQLColumn.Name))))
                .ToList();
            Db.Query(query);
            indexRequest.Method = Method.POST;
            indexRequest.Selector = () => tableIndexesToKeep;
            indexRequest.Evaluate().ThrowIfError();
            Update();
        }

        /// <summary>
        /// Gets the SQL columns of the mapped SQL table
        /// </summary>
        /// <returns></returns>
        public List<SQLColumn> GetSQLColumns()
        {
            var columns = new List<SQLColumn>();
            Db.Query($"PRAGMA table_info({TableName})", row => columns.Add(new SQLColumn(row.GetString(1), row.GetString(2).ParseSQLDataType())));
            return columns;
        }

        private void ReloadColumnNames() => SQLColumnNames = MakeColumnNames();

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

        private void Drop() => Db.Query($"DROP TABLE {TableName};");

        private string GetCreateTableSQL() => $"CREATE TABLE {TableName} ({(ColumnMappings ?? GetDeclaredColumnMappings()).ToSQL()});";

        private ColumnMappings GetDeclaredColumnMappings() => CLRClass
            .GetDeclaredColumnProperties()
            .Values
            .Select(property => new ColumnMapping
            (
                tableMapping: this,
                clrProperty: property,
                sqlColumn: new SQLColumn(property.MemberAttribute?.ColumnName ?? property.Name, property.Type.ToSQLDataType())
            ))
            .ToColumnMappings();

        internal static void Create(Type clrClass) => new TableMapping(clrClass);

        internal static bool Drop(Type clrClass)
        {
            switch (Get(clrClass))
            {
                case null: return false;
                case var declared when declared.IsDeclared: return false;
                case var procedural:
                    procedural.Drop();
                    TableMappingByType.Remove(clrClass);
                    return true;
            }
        }

        #endregion
    }
}