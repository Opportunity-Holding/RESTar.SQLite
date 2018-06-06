using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Admin;
using RESTar.Requests;
using static System.StringComparison;
using static RESTar.SQLite.SQLiteDbController;

namespace RESTar.SQLite.Meta
{
    /// <summary>
    /// Represents a column in a SQL table
    /// </summary>
    public class SQLColumn
    {
        private ColumnMapping Mapping { get; set; }

        /// <summary>
        /// The name of the column
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The type of the column, as defined in SQL
        /// </summary>
        public SQLDataType Type { get; }

        /// <summary>
        /// Does this instance represent the RowId SQLite column?
        /// </summary>
        public bool IsRowId { get; }

        /// <summary>
        /// Creates a new SQLColumn instance
        /// </summary>
        public SQLColumn(string name, SQLDataType type)
        {
            Name = name;
            IsRowId = name.EqualsNoCase("rowid");
            if (type == SQLDataType.Unsupported)
                throw new InvalidOperationException($"An SQL column '{Name}' was created with an unsupported data type");
            Type = type;
        }

        internal void SetMapping(ColumnMapping mapping) => Mapping = mapping;

        internal void Push()
        {
            if (Mapping == null)
                throw new InvalidOperationException($"Cannot push the unmapped SQL column '{Name}' to the database");
            foreach (var column in Mapping.TableMapping.GetSQLColumns())
            {
                if (column.Equals(this)) return;
                if (string.Equals(Name, column.Name, OrdinalIgnoreCase))
                    throw new SQLiteException($"Cannot push column '{Name}' to SQLite table '{Mapping.TableMapping.TableName}'. " +
                                              $"The table already contained a column definition '({column.ToSQL()})'.");
            }
            Query($"ALTER TABLE {Mapping.TableMapping.TableName} ADD COLUMN {ToSQL()}");
        }

        internal void Drop()
        {
            if (Mapping == null)
                throw new InvalidOperationException($"Cannot drop the unmapped SQL column '{Name}' from the database");
            var columnNames = new HashSet<string>(Mapping.TableMapping.SQLColumnNames);
            columnNames.Remove("rowid");
            var columnsSQL = string.Join(", ", columnNames);
            var tempName = $"__{Mapping.TableMapping.TableName}__RESTAR_TEMP";
            var query = "PRAGMA foreign_keys=off;" +
                        "BEGIN TRANSACTION;" +
                        $"ALTER TABLE {Mapping.TableMapping.TableName} RENAME TO {tempName};" +
                        $"{Mapping.TableMapping.GetCreateTableSQL()}" +
                        $"INSERT INTO {Mapping.TableMapping.TableName} ({columnsSQL})" +
                        $"  SELECT {columnsSQL}" +
                        $"  FROM {tempName};" +
                        $"DROP TABLE {tempName};" +
                        "COMMIT;" +
                        "PRAGMA foreign_keys=on;";
            var indexRequest = Context.Root.CreateRequest<DatabaseIndex>();
            indexRequest.Conditions.Add(new Condition<DatabaseIndex>
            (
                key: nameof(DatabaseIndex.ResourceName),
                op: Operators.EQUALS,
                value: Mapping.TableMapping.Resource.Name
            ));
            var indexes = indexRequest
                .EvaluateToEntities()
                .Where(index => !index.Columns.Any(column => column.Name.EqualsNoCase(Name)))
                .ToList();

            Query(query);

            indexRequest.Method = Method.POST;
            indexRequest.Selector = () => indexes;
            indexRequest.Evaluate().ThrowIfError();
        }

        internal string ToSQL() => $"{Name.Fnuttify()} {Type}";

        /// <inheritdoc />
        public override string ToString() => ToSQL();

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is SQLColumn col
                                                   && string.Equals(Name, col.Name, OrdinalIgnoreCase)
                                                   && Type == col.Type;

        /// <inheritdoc />
        public override int GetHashCode() => (Name.ToUpperInvariant(), Type).GetHashCode();
    }
}