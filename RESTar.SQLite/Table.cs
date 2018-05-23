using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Resources.Operations;
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

    [Database, RESTar]
    public class Table
    {
        public string Name { get; set; }
        public TableKind Kind { get; set; }
        public bool Editable => Kind != TableKind.Static;
        public Column[] Columns => Db.SQL<Column>(Column.ByTable, this).ToArray();

        public Table(Type type, SQLiteAttribute attribute)
        {
            Name = attribute.CustomTableName ?? type.FullName?.Replace('.', '$')
                            ?? throw new SQLiteException($"Unable to create table for unknown type with GUID '{type.GUID}'");
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
    }

    [Database]
    public class Column
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

        public Column(Table table, string name, TypeCode dataType)
        {
            Table = table;
            Name = name;
            switch (DataType)
            {
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                case TypeCode.Byte:
                case TypeCode.String:
                case TypeCode.Boolean:
                case TypeCode.DateTime:
                    DataType = dataType;
                    break;
                case var other:
                    throw new ArgumentException($"Invalid data type '{other}' for SQLite column. Allowed values: Int16, " +
                                                "Int32, Int64, Single, Double, Decimal, Byte, String, Boolean, DateTime");
            }
        }
    }
}