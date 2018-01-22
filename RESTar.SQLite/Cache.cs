using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Deflection.Dynamic;
using RESTar.Internal;

namespace RESTar.SQLite
{
    internal static class Cache
    {
        private static readonly Dictionary<Type, Dictionary<string, DeclaredProperty>> Columns;
        private static readonly Dictionary<Type, string> TableNames;

        static Cache()
        {
            Columns = new Dictionary<Type, Dictionary<string, DeclaredProperty>>();
            TableNames = new Dictionary<Type, string>();
        }

        internal static string GetSQLiteTableName(this IResource resource) => GetSQLiteTableName(resource.Type);
        internal static string GetSQLiteTableName(this Type type) => TableNames[type];
        internal static Dictionary<string, DeclaredProperty> GetColumns(this IResource resource) => Columns[resource.Type];
        internal static Dictionary<string, DeclaredProperty> GetColumns(this Type type) => Columns[type];

        internal static void Add(IResource resource)
        {
            TableNames[resource.Type] = resource.Type.FullName?.Replace('.', '$');
            Columns[resource.Type] = resource.Type
                .GetDeclaredProperties()
                .Where(p => p.Value.HasAttribute<ColumnAttribute>())
                .ToDictionary(p => p.Key, p => p.Value, StringComparer.OrdinalIgnoreCase);
        }
    }
}