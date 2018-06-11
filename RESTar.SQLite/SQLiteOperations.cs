using System.Collections.Generic;
using System.Linq;
using RESTar.Linq;
using RESTar.Requests;
using RESTar.SQLite.Meta;

namespace RESTar.SQLite
{
    internal static class SQLiteOperations<T> where T : SQLiteTable
    {
        public static IEnumerable<T> Select(IRequest<T> request)
        {
            var (sql, post) = request.Conditions.Split(IsSQLiteQueryable);
            return SQLite<T>.Select(sql.ToSQLiteWhereClause(), request.Method == Method.DELETE).Where(post);
        }

        public static int Insert(IRequest<T> request) => SQLite<T>.Insert(request.GetInputEntities());
        public static int Update(IRequest<T> request) => SQLite<T>.Update(request.GetInputEntities().ToList());

        public static int Delete(IRequest<T> request)
        {
            var input = request.GetInputEntities();
            return SQLite<T>.Delete(input.AsParallel().Select(e => e.RowId).ToList());
        }

        public static long Count(IRequest<T> request)
        {
            var (sql, post) = request.Conditions.Split(IsSQLiteQueryable);
            return post.Any()
                ? SQLite<T>
                    .Select(sql.ToSQLiteWhereClause())
                    .Where(post)
                    .Count()
                : SQLite<T>.Count(sql.ToSQLiteWhereClause());
        }

        private static bool IsSQLiteQueryable(ICondition condition)
        {
            return condition.Term.Count == 1 && TableMapping<T>.SQLColumnNames.Contains(condition.Term.First.Name);
        }
    }
}