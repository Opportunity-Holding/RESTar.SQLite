using System.Linq;
using System.Text;
using RESTar.Deflection.Dynamic;
using RESTar.Linq;
using RESTar.Operations;

namespace RESTar.SQLite
{
    internal static class SQLiteOperations<T> where T : SQLiteTable
    {
        internal static Selector<T> Select => request =>
        {
            var (dbConditions, postConditions) = request.Conditions.Split(c =>
                c.Term.Count == 1 &&
                c.Term.First is StaticProperty stat &&
                stat.HasAttribute<ColumnAttribute>()
            );
            var where = dbConditions.ToSQLiteWhereClause();
            return SQLiteDb.Query<T>(
                sql: $"SELECT RowId,* FROM {request.Resource.GetSQLiteTableName()} {where}",
                columns: request.Resource.GetColumns()
            ).Where(postConditions);
        };

        public static Inserter<T> Insert => (entities, request) =>
        {
            var columns = request.Resource.GetColumns().Values;
            var sqlStub = $"INSERT INTO {request.Resource.GetSQLiteTableName()} VALUES ";
            var stringBuilder = new StringBuilder(sqlStub);
            var iterations = 0;
            foreach (var entity in entities)
            {
                if (iterations > 0)
                    stringBuilder.Append(',');
                stringBuilder.Append('(');
                stringBuilder.Append(entity.ToSQLiteInsertInto(columns));
                stringBuilder.Append(')');
                iterations += 1;
            }
            if (iterations == 0) return 0;
            return SQLiteDb.Query(stringBuilder.ToString());
        };

        public static Updater<T> Update => (entities, request) =>
        {
            var columns = request.Resource.GetColumns().Values;
            var updateTable = $"UPDATE {request.Resource.GetSQLiteTableName()} SET ";
            var stringBuilder = new StringBuilder();
            var iterations = 0;
            foreach (var entity in entities)
            {
                stringBuilder.Append(updateTable);
                var index = 0;
                foreach (var column in columns)
                {
                    if (index > 0) stringBuilder.Append(',');
                    stringBuilder.Append(column.Name);
                    stringBuilder.Append('=');
                    var valueLiteral = ((object) column.GetValue(entity)).MakeSQLValueLiteral();
                    stringBuilder.Append(valueLiteral);
                    index += 1;
                }
                stringBuilder.Append("WHERE RowId=");
                stringBuilder.Append(entity.RowId);
                stringBuilder.Append(';');
                iterations += 1;
            }
            if (iterations == 0) return 0;
            return SQLiteDb.Query(stringBuilder.ToString());
        };

        public static Deleter<T> Delete => (entities, request) =>
        {
            var sqlstub = $"DELETE FROM {request.Resource.GetSQLiteTableName()} WHERE RowId=";
            var stringBuilder = new StringBuilder(sqlstub);
            var iterations = 0;
            foreach (var entity in entities)
            {
                if (iterations > 0)
                    stringBuilder.Append(" OR RowId=");
                stringBuilder.Append(entity.RowId);
                iterations += 1;
            }
            if (iterations == 0) return 0;
            return SQLiteDb.Query(stringBuilder.ToString());
        };

        public static Counter<T> Count => request =>
        {
            var (dbConditions, postConditions) = request.Conditions.Split(c =>
                c.Term.Count == 1 &&
                c.Term.First is StaticProperty stat &&
                stat.HasAttribute<ColumnAttribute>()
            );
            if (postConditions.Any())
                return Select(request).Count();
            var where = dbConditions.ToSQLiteWhereClause();
            var count = 0L;
            SQLiteDb.Query(
                sql: $"SELECT COUNT(*) FROM {request.Resource.GetSQLiteTableName()} {where}",
                rowAction: row => count = row.GetInt64(0)
            );
            return count;
        };
    }
}