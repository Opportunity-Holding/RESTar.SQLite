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
        internal static IDictionary<string, CLRProperty> GetDeclaredColumnProperties(this Type type)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            return type.GetProperties(Public | Instance)
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
                                                  $"Valid property types: {string.Join(", ", EnumMember<CLRDataType>.Names)}");
                    if (property.HasAttribute(out SQLiteMemberAttribute attr) && attr.ColumnName.Equals("rowid", StringComparison.OrdinalIgnoreCase))
                        throw new SQLiteException($"SQLite type '{type}' contained a public auto-implemented instance property '{property.Name}' " +
                                                  "with a custom column name 'rowid'. This name is reserved by SQLite and cannot be used.");
                    if (!names.Add(property.Name))
                        throw new SQLiteException($"The type definition for class '{type}' contained multiple properties with the name " +
                                                  $"'{property.Name}' (case insensitive). SQL is case insensitive, so for mapping to work, all mapped " +
                                                  "properties must have unique case insensitive names.");
                    return true;
                })
                .ToDictionary(
                    keySelector: property => property.Name,
                    elementSelector: property => new CLRProperty(property),
                    comparer: StringComparer.OrdinalIgnoreCase
                );
        }


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

        internal static bool EqualsNoCase(this string s1, string s2) => string.Equals(s1, s2, StringComparison.OrdinalIgnoreCase);

        private static bool HasAttribute<TAttribute>(this MemberInfo type, out TAttribute attribute)
            where TAttribute : Attribute
        {
            attribute = type?.GetCustomAttributes<TAttribute>().FirstOrDefault();
            return attribute != null;
        }

        internal static IEnumerable<Type> GetConcreteSubclasses(this Type baseType) => AppDomain
            .CurrentDomain
            .GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes(), (assembly, type) => (assembly, type))
            .Where(t => t.type.IsSubclassOf(baseType))
            .Select(t => t.type)
            .Where(type => !type.IsAbstract);
    }
}