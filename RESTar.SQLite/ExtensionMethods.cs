using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using RESTar.Meta;
using RESTar.SQLite.Meta;
using static System.Reflection.BindingFlags;
using static RESTar.Method;

namespace RESTar.SQLite
{
    internal static class ExtensionMethods
    {
        internal static string GetResourceName(this string tableName) => Resource.ByTypeName(tableName.Replace('$', '.')).Name;

        internal static IDictionary<string, CLRProperty> GetDeclaredColumnProperties(this Type type) => type
            .GetProperties(Public | Instance)
            .Where(property => !property.HasAttribute(out SQLiteMemberAttribute attribute) || !attribute.Ignored)
            .Where(property =>
            {
                var getter = property.GetGetMethod();
                var setter = property.GetGetMethod();
                if (getter == null && setter == null) return false;
                if (!(getter ?? setter).HasAttribute<CompilerGeneratedAttribute>(out _))
                    return false;
                if (getter == null)
                    throw new SQLiteException($"SQLite type '{type}' contained a public auto-implemented instance property '{property.Name}' " +
                                              "with a non-defined or non-public get accessor. This property cannot be used with SQLite. To ignore this " +
                                              "property, decorate it with the 'SQLiteMemberAttribute' and set 'ignore' to true");
                if (setter == null)
                    throw new SQLiteException($"SQLite type '{type}' contained a public auto-implemented instance property '{property.Name}' " +
                                              "with a non-public set accessor. This property cannot be used with SQLite. To ignore this " +
                                              "property, decorate it with the 'SQLiteMemberAttribute' and set 'ignore' to true");
                if (!property.PropertyType.IsSQLiteCompatibleValueType())
                    throw new SQLiteException($"SQLite type '{type}' contained a public auto-implemented instance property '{property.Name}' " +
                                              $"with a non-compatible type '{property.PropertyType.Name}'. This property cannot be used with SQLite. " +
                                              "To ignore this property, decorate it with the 'SQLiteMemberAttribute' and set 'ignore' to true. " +
                                              $"Valid property types: {SQLiteDbController.AllowedCLRDataTypes}");
                if (property.HasAttribute(out SQLiteMemberAttribute attr) && attr.ColumnName.Equals("rowid", StringComparison.OrdinalIgnoreCase))
                    throw new SQLiteException($"SQLite type '{type}' contained a public auto-implemented instance property '{property.Name}' " +
                                              "with a custom column name 'rowid'. This name is reserved by SQLite and cannot be used.");

                return true;
            })
            .ToDictionary(
                keySelector: property => property.Name.ToUpperInvariant(),
                elementSelector: property => new CLRProperty(property)
            );


        private static bool IsNullable(this Type type, out Type baseType) => (baseType = Nullable.GetUnderlyingType(type)) != null;

        internal static (string, string) TSplit(this string str, char splitCharacter)
        {
            var split = str.Split(splitCharacter);
            return (split[0], split.ElementAtOrDefault(1));
        }

        internal static string ToMethodsString(this IEnumerable<Method> ie) => string.Join(", ", ie);

        internal static Method[] ToMethodsArray(this string methodsString)
        {
            if (methodsString == null) return null;
            if (methodsString.Trim() == "*")
                return new[] {GET, POST, PATCH, PUT, DELETE, REPORT, HEAD};
            return methodsString.Split(',')
                .Where(s => s != "")
                .Select(s => (Method) Enum.Parse(typeof(Method), s))
                .ToArray();
        }

        internal static string Capitalize(this string input)
        {
            var array = input.ToCharArray();
            array[0] = char.ToUpper(array[0]);
            return new string(array);
        }

        internal static bool EqualsNoCase(this string s1, string s2) => string.Equals(s1, s2, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the value of a key from an IDictionary, without case sensitivity, or null if the dictionary does 
        /// not contain the key. The actual key is returned in the actualKey out parameter.
        /// </summary>
        internal static bool TryFindInDictionary<T>(this IDictionary<string, T> dict, string key, out string actualKey,
            out T result)
        {
            result = default;
            actualKey = null;
            var matches = dict.Where(pair => pair.Key.EqualsNoCase(key)).ToList();
            switch (matches.Count)
            {
                case 0: return false;
                case 1:
                    actualKey = matches[0].Key;
                    result = matches[0].Value;
                    return true;
                default:
                    if (!dict.TryGetValue(key, out result)) return false;
                    actualKey = key;
                    return true;
            }
        }

        internal static TypeCode ResolveCLRTypeCode(this Type type)
        {
            if (type.IsNullable(out var t)) type = t;
            var typeCode = Type.GetTypeCode(type);
            switch (typeCode)
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
                case TypeCode.DateTime: return typeCode;
                case var other:
                    throw new ArgumentException(
                        $"Invalid CLR data type \'{other}\' for SQLite column. " +
                        $"Allowed values: {SQLiteDbController.AllowedCLRDataTypes}");
            }
        }

        internal static string ToSQLTypeString(this Type type)
        {
            if (type.IsNullable(out var t))
                type = t;
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

        internal static string ToSQLTypeString(this TypeCode type)
        {
            switch (type)
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