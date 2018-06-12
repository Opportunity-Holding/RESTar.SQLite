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
            return SQLite<T>.Select(
                where: sql.ToSQLiteWhereClause(),
                onlyRowId: request.Method == Method.DELETE && !request.Conditions.Any()
            ).Where(post);
        }

        public static int Insert(IRequest<T> request)
        {
            var count = 0;
            Starcounter.Db.TransactAsync(() => count = SQLite<T>.Insert(request.GetInputEntities()));
            return count;
        }

        public static int Update(IRequest<T> request)
        {
            var count = 0;
            Starcounter.Db.TransactAsync(() => count = SQLite<T>.Update(request.GetInputEntities().ToList()));
            return count;
        }

        public static int Delete(IRequest<T> request)
        {
            var count = 0;
            Starcounter.Db.TransactAsync(() => count = SQLite<T>.Delete(request.GetInputEntities().ToList()));
            return count;
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