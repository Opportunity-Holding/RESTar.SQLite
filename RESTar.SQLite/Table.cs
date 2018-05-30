
using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Linq;
using RESTar.Meta;
using RESTar.Resources;
using Starcounter;
using static System.Reflection.BindingFlags;

namespace RESTar.SQLite
{
    public enum TableKind
    {
        Static,
        Elastic,
        Dynamic
    }

    public class ColumnInfo
    {
        [RESTarMember(ignore: true)] internal Column Column { get; set; }

        public bool CanBeDropped => Column.Table.Editable;

        public bool IsDropped
        {
            get => Column == null;
            set
            {
                if (!CanBeDropped) return;
                Column?.Delete();
                Column = null;
            }
        }

        internal TypeCode? _CLRType;
        internal string _SQLType;

        public TypeCode CLRType
        {
            get => Column.DataType;
            set => _CLRType = value;
        }

        public string SQLType
        {
            get => Column.SQLType;
            set => _SQLType = value;
        }

        public ColumnInfo() { }

        public ColumnInfo(Column column) => Column = column;
    }

    public class Columns : Dictionary<string, ColumnInfo>
    {
        public Table Table { get; }

        public new ColumnInfo this[string key]
        {
            get
            {
                TryGetValue(key, out var columnInfo);
                return columnInfo;
            }
            set
            {
                if (!Table.Editable) return;
                if (value.Column?.Name is string existing)
                    throw new ArgumentException($"Column '{existing}' already assigned to a table");
                var typeCode = value._CLRType ?? value._SQLType?.ToCLRTypeCode()
                               ?? throw new ArgumentException("Missing CLRType or SQLType value for new column");

                value.Columns = this;
            }
        }

        public Columns(Table table, IEnumerable<Column> columnCollection)
        {
            Table = table;
            columnCollection.ForEach(column => this[column.Name] = new ColumnInfo(column));
        }
    }

    [Database, RESTar]
    public class Table
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public TableKind Kind { get; set; }
        public bool Editable => Kind != TableKind.Static;

        [RESTarMember(ignore: true)] public IEntityResource EntityResource => Meta.EntityResource.SafeGet(Type);

        public Columns Columns => new Columns(this, GetColumns());

        public Table(Type type, SQLiteAttribute attribute)
        {
            Name = attribute.CustomTableName ?? type.FullName?.Replace('.', '$')
                   ?? throw new SQLiteException($"Unable to create table for unknown type with GUID '{type.GUID}'");
            Type = type.FullName;
            switch (type)
            {
                case var elastic when typeof(ElasticSQLiteTable).IsAssignableFrom(type):

                    Kind = TableKind.Elastic;

                    var method = elastic.GetMethod(nameof(ElasticSQLiteTable.GetDefaultMembers), Instance | Public);
                    var @delegate = method?.CreateDelegate(typeof(Func<(TypeCode dataType, string name)>), null);
                    if (!(@delegate is Func<(TypeCode dataType, string name)> getDefaultMembers))
                        throw new SQLiteException($"Could not find '{nameof(ElasticSQLiteTable.GetDefaultMembers)}' " +
                                                  $"method in elastic table definition '{type}'");
                    var members = getDefaultMembers();


                    break;
                case var @static when typeof(SQLiteTable).IsAssignableFrom(type):

                    Kind = TableKind.Static;

                    break;
            }
        }

        private IEnumerable<Column> GetColumns() => Db.SQL<Column>(Column.ByTable, this);

        /// <summary>
        /// Pushes this table configuration to SQLite
        /// </summary>
        internal void Push()
        {
            SQLiteDbController.Query
            (
                sql: $"CREATE TABLE IF NOT EXISTS {Name} ({string.Join(",", GetColumns().Select(c => c.ColumnDefinition))})",
                action: command => command.ExecuteNonQuery()
            );
        }
    }

    [Database]
    public class Column : IEntity
    {
        internal const string All = "SELECT t FROM \"RESTar\".\"SQLite\".\"Column\" t";
        internal const string ByTable = All + " WHERE t.\"Table\" =?";

        public string Name { get; }
        public TypeCode DataType { get; }
        public Table Table { get; }

        public string SQLType
        {
            get
            {
                switch (DataType)
                {
                    case TypeCode.Int16: return "SMALLINT";
                    case TypeCode.Int32: return "INT";
                    case TypeCode.Int64: return "BIGINT";
                    case TypeCode.Single: return "SINGLE";
                    case TypeCode.Double: return "DOUBLE";
                    case TypeCode.Decimal: return "DECIMAL";
                    case TypeCode.Byte: return "TINYINT";
                    case TypeCode.String: return "TEXT";
                    case TypeCode.Boolean: return "BOOLEAN";
                    case TypeCode.DateTime: return "DATETIME";
                    default: throw new ArgumentOutOfRangeException();
                }
            }
        }

        internal string ColumnDefinition => $"{Name.Fnuttify()} {SQLType}";

        public Column(Table table, Member property) : this(table, property.ActualName, Type.GetTypeCode(property.Type)) { }

        public Column(Table table, string name, TypeCode dataType)
        {
            Table = table;
            Name = name;
        }

        public void OnDelete()
        {
            SQLiteDbController.Query
            (
            );
        }
    }
}
