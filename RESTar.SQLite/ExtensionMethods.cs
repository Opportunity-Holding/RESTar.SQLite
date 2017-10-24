using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RESTar.Deflection.Dynamic;
using RESTar.Internal;
using static RESTar.Operators;

namespace RESTar.SQLite
{
    internal static class ExtensionMethods
    {
        internal static Dictionary<string, StaticProperty> GetColumns(this IResource resource) => resource
            .GetStaticProperties()
            .Where(p => p.Value.HasAttribute<ColumnAttribute>())
            .ToDictionary(p => p.Key, p => p.Value);

        internal static string GetColumnDef(this StaticProperty column) =>
            $"{column.Name.ToLower().Fnuttify()} {column.Type.ToSQLType()}";

        internal static string GetSQLiteTableName(this IResource resource) => resource.Type.FullName?.Replace('.', '$');

        internal static string GetResourceName(this string tableName) => Resource.ByTypeName(tableName.Replace('$', '.')).Name;

        internal static string Fnuttify(this string sqlKey) => $"\"{sqlKey.Replace(".", "\".\"")}\"";

        internal static bool IsSQLiteCompatibleValueType(this Type type, Type resourceType, out string error)
        {
            if (type.ToSQLType() == null)
            {
                error = $"Could not create SQLite database column for a property of type '{type.FullName}' in resource type " +
                        $"'{resourceType?.FullName}'. Unsupported type";
                return false;
            }
            error = null;
            return true;
        }

        internal static bool IsNullable(this Type type, out Type baseType)
        {
            baseType = null;
            if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Nullable<>))
                return false;
            baseType = type.GenericTypeArguments[0];
            return true;
        }

        internal static (string, string) TSplit(this string str, char splitCharacter)
        {
            var split = str.Split(splitCharacter);

            return (split[0], split.ElementAtOrDefault(1));
        }

        internal static string ToSQLType(this Type type)
        {
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
                case TypeCode.Boolean: return "BOOL";
                case TypeCode.DateTime: return "DATETIME";
                case var _ when type.IsNullable(out var t): return t.ToSQLType();
                default: return null;
            }
        }

        internal static string MakeSQLValueLiteral(this object o)
        {
            switch (o)
            {
                case null: return "NULL";
                case DateTime _: return $"\'{o:O}\'";
                case char _:
                case bool _:
                case string _: return $"\'{o}\'";
                default: return $"{o}";
            }
        }

        internal static string ToSQLiteWhereClause<T>(this IEnumerable<Condition<T>> conditions) where T : class
        {
            var values = string.Join(" AND ", conditions.Where(c => !c.Skip).Select(c =>
            {
                var op = c.Operator.SQL;
                var valueLiteral = MakeSQLValueLiteral((object) c.Value);
                if (valueLiteral == "NULL")
                {
                    switch (c.Operator.OpCode)
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
                return $"{c.Key.Fnuttify()} {op} {valueLiteral}";
            }));
            return string.IsNullOrWhiteSpace(values) ? null : "WHERE " + values;
        }

        internal static string ToSQLiteInsertInto<T>(this T entity, IEnumerable<StaticProperty> columns) where T : class
        {
            return string.Join(",", columns.Select(c => MakeSQLValueLiteral((object) c.GetValue(entity))));
        }

        internal static bool HasAttribute<TAttribute>(this MemberInfo type, out TAttribute attribute)
            where TAttribute : Attribute
        {
            attribute = type?.GetCustomAttributes<TAttribute>().FirstOrDefault();
            return attribute != null;
        }
    }
}