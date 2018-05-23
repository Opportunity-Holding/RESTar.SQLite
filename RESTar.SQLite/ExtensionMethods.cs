using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RESTar.Meta;
using RESTar.Requests;
using static RESTar.Requests.Operators;

namespace RESTar.SQLite
{
    internal static class ExtensionMethods
    {
        internal static string GetColumnDef(this DeclaredProperty column) =>
            $"{column.ActualName.ToLower().Fnuttify()} {column.Type.ToSQLType()}";

        internal static string GetResourceName(this string tableName) => Resource.ByTypeName(tableName.Replace('$', '.')).Name;

        internal static string Fnuttify(this string sqlKey) => $"\"{sqlKey}\"";

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

        private static bool IsNullable(this Type type, out Type baseType)
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

        internal static Schema GetSchema(this IResource resource)
        {
            var columns = new List<Column>();
            SQLiteDbController.Query($"PRAGMA table_info({resource.GetSQLiteTableName()})", row =>
            {
                var columnName = row.GetString(1);
                var columnType = row.GetString(2);
                columns.Add(new Column(columnName, columnType.GetTypeCode()));

                //if (!uncheckedColumns.TryGetValue(columnName, out var correspondingColumn))
                //    return;
                //var foundType = correspondingColumn.Type.ToSQLType();
                //if (!string.Equals(foundType, columnType, StringComparison.OrdinalIgnoreCase))
                //{
                //    throw new SQLiteException($"The underlying database schema for SQLite resource '{resource.Name}' has " +
                //                              $"changed. Cannot convert column of SQLite type '{columnType}' to '{foundType}' " +
                //                              $"in SQLite database table '{resource.GetSQLiteTableName()}'.");
                //}
                //uncheckedColumns.Remove(columnName);
            });
            return null;
        }

        internal static TypeCode GetTypeCode(this string sqlTypeString)
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
                default: throw new ArgumentOutOfRangeException();
            }
        }

        //internal static string ToSQLType(this Type type)
        //{
        //    switch (Type.GetTypeCode(type))
        //    {
        //        case TypeCode.Int16: return "SMALLINT";
        //        case TypeCode.Int32: return "INT";
        //        case TypeCode.Int64: return "BIGINT";
        //        case TypeCode.Single: return "SINGLE";
        //        case TypeCode.Double: return "DOUBLE";
        //        case TypeCode.Decimal: return "DECIMAL";
        //        case TypeCode.Byte: return "TINYINT";
        //        case TypeCode.String: return "TEXT";
        //        case TypeCode.Boolean: return "BOOLEAN";
        //        case TypeCode.DateTime: return "DATETIME";
        //        case var _ when type.IsNullable(out var t): return t.ToSQLType();
        //        default: return null;
        //    }
        //}

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

        internal static string ToSQLiteInsertValues<T>(this T entity) where T : SQLiteTable => string.Join(",",
            typeof(T).GetColumns().Values.Select(c => MakeSQLValueLiteral((object) c.GetValue(entity))));

        internal static string ToSQLiteUpdateSet<T>(this T entity) where T : SQLiteTable => string.Join(",",
            typeof(T).GetColumns().Values.Select(c => $"{c.Name}={MakeSQLValueLiteral((object) c.GetValue(entity))}"));

        internal static bool HasAttribute<TAttribute>(this Type type, out TAttribute attribute)
            where TAttribute : Attribute
        {
            attribute = type?.GetCustomAttributes<TAttribute>().FirstOrDefault();
            return attribute != null;
        }

        internal static bool HasAttribute<TAttribute>(this MemberInfo type, out TAttribute attribute)
            where TAttribute : Attribute
        {
            attribute = type?.GetCustomAttributes<TAttribute>().FirstOrDefault();
            return attribute != null;
        }

        internal static IList<Type> GetConcreteSubclasses(this Type baseType) => baseType.GetSubclasses()
            .Where(type => !type.IsAbstract)
            .ToList();

        internal static IEnumerable<Type> GetSubclasses(this Type baseType) =>
            from assembly in AppDomain.CurrentDomain.GetAssemblies()
            from type in assembly.GetTypes()
            where type.IsSubclassOf(baseType)
            select type;
    }
}