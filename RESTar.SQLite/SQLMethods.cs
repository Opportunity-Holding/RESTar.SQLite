using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using RESTar.Requests;
using static RESTar.Requests.Operators;

namespace RESTar.SQLite
{
    internal static class SQLMethods
    {
        internal static string Fnuttify(this string sqlKey) => $"\"{sqlKey}\"";

        internal static bool IsSQLiteCompatibleValueType(this Type type) => type.ResolveCLRTypeCode() != CLRDataType.Unsupported;

        internal static CLRDataType ResolveCLRTypeCode(this Type type)
        {
            if (type.IsNullable(out var t)) type = t;
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Int16: return CLRDataType.Int16;
                case TypeCode.Int32: return CLRDataType.Int32;
                case TypeCode.Int64: return CLRDataType.Int64;
                case TypeCode.Single: return CLRDataType.Single;
                case TypeCode.Double: return CLRDataType.Double;
                case TypeCode.Decimal: return CLRDataType.Decimal;
                case TypeCode.Byte: return CLRDataType.Byte;
                case TypeCode.String: return CLRDataType.String;
                case TypeCode.Boolean: return CLRDataType.Boolean;
                case TypeCode.DateTime: return CLRDataType.DateTime;
                default: return CLRDataType.Unsupported;
            }
        }

        internal static SQLDataType ParseSQLDataType(this string typeString)
        {
            switch (typeString.ToUpperInvariant())
            {
                case "SMALLINT": return SQLDataType.SMALLINT;
                case "INT": return SQLDataType.INT;
                case "BIGINT": return SQLDataType.BIGINT;
                case "SINGLE": return SQLDataType.SINGLE;
                case "DOUBLE": return SQLDataType.DOUBLE;
                case "DECIMAL": return SQLDataType.DECIMAL;
                case "TINYINT": return SQLDataType.TINYINT;
                case "TEXT": return SQLDataType.TEXT;
                case "BOOLEAN": return SQLDataType.BOOLEAN;
                case "DATETIME": return SQLDataType.DATETIME;
                default: return SQLDataType.Unsupported;
            }
        }

        internal static SQLDataType ToSQLDataType(this CLRDataType clrDataType)
        {
            switch (clrDataType)
            {
                case CLRDataType.Int16: return SQLDataType.SMALLINT;
                case CLRDataType.Int32: return SQLDataType.INT;
                case CLRDataType.Int64: return SQLDataType.BIGINT;
                case CLRDataType.Single: return SQLDataType.SINGLE;
                case CLRDataType.Double: return SQLDataType.DOUBLE;
                case CLRDataType.Decimal: return SQLDataType.DECIMAL;
                case CLRDataType.Byte: return SQLDataType.TINYINT;
                case CLRDataType.String: return SQLDataType.TEXT;
                case CLRDataType.Boolean: return SQLDataType.BOOLEAN;
                case CLRDataType.DateTime: return SQLDataType.DATETIME;
                default: return SQLDataType.Unsupported;
            }
        }

        internal static DbType? ToDbTypeCode(this SQLDataType sqlDataType)
        {
            switch (sqlDataType)
            {
                case SQLDataType.SMALLINT: return DbType.Int16;
                case SQLDataType.INT: return DbType.Int32;
                case SQLDataType.BIGINT: return DbType.Int64;
                case SQLDataType.SINGLE: return DbType.Single;
                case SQLDataType.DOUBLE: return DbType.Double;
                case SQLDataType.DECIMAL: return DbType.Decimal;
                case SQLDataType.TINYINT: return DbType.Byte;
                case SQLDataType.TEXT: return DbType.String;
                case SQLDataType.BOOLEAN: return DbType.Boolean;
                case SQLDataType.DATETIME: return DbType.DateTime;
                default: return null;
            }
        }


        internal static CLRDataType ToCLRTypeCode(this SQLDataType sqlDataType)
        {
            switch (sqlDataType)
            {
                case SQLDataType.SMALLINT: return CLRDataType.Int16;
                case SQLDataType.INT: return CLRDataType.Int32;
                case SQLDataType.BIGINT: return CLRDataType.Int64;
                case SQLDataType.SINGLE: return CLRDataType.Single;
                case SQLDataType.DOUBLE: return CLRDataType.Double;
                case SQLDataType.DECIMAL: return CLRDataType.Decimal;
                case SQLDataType.TINYINT: return CLRDataType.Byte;
                case SQLDataType.TEXT: return CLRDataType.String;
                case SQLDataType.BOOLEAN: return CLRDataType.Boolean;
                case SQLDataType.DATETIME: return CLRDataType.DateTime;
                default: return CLRDataType.Unsupported;
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
    }
}