using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RESTar.Admin;
using RESTar.Linq;
using RESTar.Requests;
using RESTar.Resources;

namespace RESTar.SQLite
{
    internal class SQLiteIndexer : IDatabaseIndexer
    {
        private const string syntax =
            @"CREATE +INDEX +""*(?<name>\w+)""* +ON +""*(?<table>[\w\$]+)""* +\((?:(?<columns>""*\w+""* *[""*\w+""*]*) *,* *)+\)";

        public IEnumerable<DatabaseIndex> Select(IRequest<DatabaseIndex> request)
        {
            var sqls = new List<string>();
            SQLiteDbController.Query("SELECT sql FROM sqlite_master WHERE type='index'", row => sqls.Add(row.GetString(0)));
            return sqls.Select(sql =>
            {
                var groups = Regex.Match(sql, syntax, RegexOptions.IgnoreCase).Groups;
                return new DatabaseIndex(groups["table"].Value.GetResourceName())
                {
                    Name = groups["name"].Value,
                    Columns = groups["columns"].Captures.Cast<Capture>().Select(column =>
                    {
                        var (name, direction) = column.ToString().TSplit(' ');
                        return new ColumnInfo(name.Replace("\"", ""), direction.ToLower().Contains("desc"));
                    }).ToArray()
                };
            }).Where(request.Conditions);
        }

        public int Insert(IRequest<DatabaseIndex> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var count = 0;
            foreach (var index in request.GetInputEntities())
            {
                if (index.IResource == null)
                    throw new Exception("Found no resource to register index on");
                var sql = $"CREATE INDEX {index.Name.Fnuttify()} ON {index.IResource.GetSQLiteTableName().Fnuttify()} " +
                          $"({string.Join(", ", index.Columns.Select(c => $"{c.Name.Fnuttify()} {(c.Descending ? "DESC" : "ASC")}"))})";
                count += SQLiteDbController.Query(sql);
            }
            return count;
        }

        public int Update(IRequest<DatabaseIndex> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var count = 0;
            foreach (var index in request.GetInputEntities())
            {
                SQLiteDbController.Query($"DROP INDEX {index.Name.Fnuttify()} ON {index.IResource.GetSQLiteTableName().Fnuttify()}");
                count += SQLiteDbController.Query($"CREATE INDEX {index.Name.Fnuttify()} ON " +
                                        $"{index.IResource.GetSQLiteTableName().Fnuttify()} " +
                                        $"({string.Join(", ", index.Columns.Select(c => $"{c.Name.Fnuttify()} {(c.Descending ? "DESC" : "")}"))})");
            }
            return count;
        }

        public int Delete(IRequest<DatabaseIndex> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            return request.GetInputEntities().Sum(index => SQLiteDbController.Query($"DROP INDEX {index.Name.Fnuttify()} ON " +
                                                                     $"{index.IResource.GetSQLiteTableName().Fnuttify()}"));
        }
    }
}