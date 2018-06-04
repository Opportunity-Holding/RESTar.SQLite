using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Requests;
using RESTar.SQLite.Meta;
using static RESTar.Requests.Operators;

namespace RESTar.SQLite
{
    internal static class SQLMethods
    {
        internal static string Fnuttify(this string sqlKey) => $"\"{sqlKey}\"";

        internal static bool IsSQLiteCompatibleValueType(this Type type) => ToSQLType(type) != null;

        private static string ToSQLType(this Type type)
        {
            if (type.IsNullable(out var baseType))
                type = baseType;
            switch (Type.GetTypeCode(type))
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
                default: return null;
            }
        }

        internal static TypeCode ToCLRTypeCode(this string sqlTypeString, bool ignoreUnsupported)
        {
            switch (sqlTypeString.ToUpperInvariant())
            {
                case "SMALLINT": return TypeCode.Int16;
                case "INT": return TypeCode.Int32;
                case "BIGINT": return TypeCode.Int64;
                case "SINGLE": return TypeCode.Single;
                case "DOUBLE": return TypeCode.Double;
                case "DECIMAL": return TypeCode.Decimal;
                case "TINYINT": return TypeCode.Byte;
                case "TEXT": return TypeCode.String;
                case "BOOLEAN": return TypeCode.Boolean;
                case "DATETIME": return TypeCode.DateTime;
                case var other:
                    if (ignoreUnsupported) return TypeCode.Empty;
                    throw new ArgumentException(
                        $"Invalid SQL data type for column. SQL type '{other}' is not supported by RESTar.SQLite. " +
                        $"Allowed values: {SQLiteDbController.AllowedSQLDataTypes}");
            }
        }

        private static string MakeSQLValueLiteral(this object o)
        {
            switch (o)
            {
                case null: return "NULL";
                case true: return "1";
                case false: return "0";

                case char _:
                case string _: return $"\'{o}\'";
                case DateTime _: return $"DATETIME(\'{o:O}\')";
                default: return $"{o}";
            }
        }

        private static string GetSQLOperator(Operators op)
        {
            switch (op)
            {
                case EQUALS: return "=";
                case NOT_EQUALS: return "<>";
                case LESS_THAN: return "<";
                case GREATER_THAN: return ">";
                case LESS_THAN_OR_EQUALS: return "<=";
                case GREATER_THAN_OR_EQUALS: return ">=";
                default: throw new ArgumentOutOfRangeException(nameof(op), op, null);
            }
        }

        internal static string ToSQLiteWhereClause<T>(this IEnumerable<Condition<T>> conditions) where T : class
        {
            var values = string.Join(" AND ", conditions.Where(c => !c.Skip).Select(c =>
            {
                var op = GetSQLOperator(c.Operator);
                var key = c.Term.First.ActualName;
                var valueLiteral = MakeSQLValueLiteral((object) c.Value);
                if (valueLiteral == "NULL")
                {
                    switch (c.Operator)
                    {
                        case EQUALS:
                            op = "IS";
                            break;
                        case NOT_EQUALS:
                            op = "IS NOT";
                            break;
                        default: throw new SQLiteException($"Operator '{op}' is not valid for comparison with NULL");
                    }
                }
                return $"{key.Fnuttify()} {op} {valueLiteral}";
            }));
            return string.IsNullOrWhiteSpace(values) ? null : "WHERE " + values;
        }

        internal static string ToSQLiteInsertValues<T>(this T entity) where T : SQLiteTable
        {
            return string.Join(",", TableMapping<T>.ColumnMappings.Select(c => MakeSQLValueLiteral((object) c.CLRProperty.Get(entity))));
        }

        internal static string ToSQLiteUpdateSet<T>(this T entity) where T : SQLiteTable
        {
            return string.Join(",", TableMapping<T>.ColumnMappings.Select(c =>
                $"{c.SQLColumn.Name}={MakeSQLValueLiteral((object) c.CLRProperty.Get(entity))}"));
        }
    }
}