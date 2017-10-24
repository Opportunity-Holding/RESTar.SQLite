using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Starcounter;

namespace RESTarSQLiteExample
{
    /// <summary>
    /// This class provides static methods for database queries in the DRTB system.
    /// </summary>
    internal static class DB
    {
        private static readonly MethodInfo SQL = typeof(Db).GetMethods()
            .First(m => m.Name == "SQL" && m.IsGenericMethod);

        #region Get methods

        public static ICollection<T> All<T>() where T : class
        {
            return Db.SQL<T>($"SELECT t FROM {typeof(T).FullName} t").ToList();
        }

        private static string Fnuttify(this string sqlKey) => $"\"{sqlKey.Replace(".", "\".\"")}\"";

        public static bool Exists<T>() where T : class
        {
            return Db.SQL<T>($"SELECT t FROM {typeof(T).FullName} t").First != null;
        }

        #endregion
    }
}