using System.Linq;
using RESTar.Deflection.Dynamic;
using RESTar.Linq;
using RESTar.Operations;

namespace RESTar.SQLite
{
    internal static class SQLiteOperations<T> where T : SQLiteTable
    {
        internal static readonly Selector<T> Select;
        public static readonly Inserter<T> Insert;
        public static readonly Updater<T> Update;
        public static readonly Deleter<T> Delete;
        public static readonly Counter<T> Count;

        static SQLiteOperations()
        {
            Select = request =>
            {
                var (dbConditions, postConditions) = request.Conditions.Split(c =>
                    c.Term.Count == 1 &&
                    c.Term.First is StaticProperty stat &&
                    stat.HasAttribute<ColumnAttribute>()
                );
                return SQLite<T>
                    .Select(dbConditions.ToSQLiteWhereClause())
                    .Where(postConditions);
            };
            Insert = (e, r) => SQLite<T>.Insert(e);
            Update = (e, r) => SQLite<T>.Update(e);
            Delete = (e, r) => SQLite<T>.Delete(e);
            Count = request =>
            {
                var (dbConditions, postConditions) = request.Conditions.Split(c =>
                    c.Term.Count == 1 &&
                    c.Term.First is StaticProperty stat &&
                    stat.HasAttribute<ColumnAttribute>()
                );
                return postConditions.Any()
                    ? Select(request).Count()
                    : SQLite<T>.Count(dbConditions.ToSQLiteWhereClause());
            };
        }
    }
}