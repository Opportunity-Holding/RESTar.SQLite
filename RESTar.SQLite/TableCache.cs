using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RESTar.Meta;

namespace RESTar.SQLite
{
    internal static class TableCache
    {
        private static readonly Dictionary<Type, Dictionary<string, DeclaredProperty>> Columns;
        private static readonly Dictionary<Type, string> TableNames;

        static TableCache()
        {
            Columns = new Dictionary<Type, Dictionary<string, DeclaredProperty>>();
            TableNames = new Dictionary<Type, string>();
        }

        //internal static string GetSQLiteTableName(this IResource resource) => GetSQLiteTableName(resource.Type);
        //internal static string GetSQLiteTableName(this Type type) => TableNames[type];
        internal static Dictionary<string, DeclaredProperty> GetColumns(this IResource resource) => Columns[resource.Type];
        internal static Dictionary<string, DeclaredProperty> GetColumns(this Type type) => Columns[type];

        internal static void Add(IResource resource)
        {
            var tableName = resource.Type.FullName?.Replace('.', '$');
            if (resource.Type.GetCustomAttribute<SQLiteAttribute>() is SQLiteAttribute a && a.CustomTableName is string customName)
                tableName = customName;
            TableNames[resource.Type] = tableName;
            Columns[resource.Type] = resource.Type
                .GetDeclaredProperties()
                .Where(p => p.Value.HasAttribute<ColumnAttribute>())
                .ToDictionary(p => p.Key, p => p.Value, StringComparer.OrdinalIgnoreCase);
        }
    }
}