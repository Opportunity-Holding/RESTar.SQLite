using System.Linq;
using RESTar.Linq;
using RESTar.Requests;
using RESTar.Resources.Operations;
using RESTar.SQLite.Meta;

namespace RESTar.SQLite
{
    internal static class SQLiteOperations<T> where T : SQLiteTable
    {
        internal static readonly Selector<T> Select;
        public static readonly Inserter<T> Insert;
        public static readonly Updater<T> Update;
        public static readonly Deleter<T> Delete;
        public static readonly Counter<T> Count;

        private static bool IsSQLiteQueryable(ICondition condition)
        {
            return condition.Term.Count == 1 && TableMapping<T>.ColumnNames.Contains(condition.Term.First.Name);
        }

        static SQLiteOperations()
        {
            Select = request =>
            {
                var (sql, post) = request.Conditions.Split(IsSQLiteQueryable);
                return SQLite<T>.Select(sql.ToSQLiteWhereClause()).Where(post);
            };
            Insert = r => SQLite<T>.Insert(r.GetInputEntities());
            Update = r => SQLite<T>.Update(r.GetInputEntities());
            Delete = r => SQLite<T>.Delete(r.GetInputEntities());
            Count = request =>
            {
                var (sql, post) = request.Conditions.Split(IsSQLiteQueryable);
                return post.Any()
                    ? SQLite<T>
                        .Select(sql.ToSQLiteWhereClause())
                        .Where(post)
                        .Count()
                    : SQLite<T>.Count(sql.ToSQLiteWhereClause());
            };
        }
    }
}