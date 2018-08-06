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
        private const string syntax = @"CREATE +INDEX +""*(?<name>\w+)""* +ON +""*(?<table>[\w\$]+)""* " +
                                      @"\((?:(?<columns>""*\w+""* *[""*\w+""*]*) *,* *)+\)";

        public IEnumerable<DatabaseIndex> Select(IRequest<DatabaseIndex> request)
        {
            var sqls = new List<string>();
            Database.Query("SELECT sql FROM sqlite_master WHERE type='index'", row => sqls.Add(row.GetString(0)));
            return sqls.Select(sql =>
            {
                var groups = Regex.Match(sql, syntax, RegexOptions.IgnoreCase).Groups;
                var tableName = groups["table"].Value;
                var mapping = TableMapping.All.FirstOrDefault(m => m.TableName.EqualsNoCase(tableName));
                if (mapping == null) throw new Exception($"Unknown SQLite table '{tableName}'");
                return new DatabaseIndex(mapping.Resource.Name)
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
                var tableMapping = TableMapping.Get(index.Resource.Type);
                if (index.Resource == null)
                    throw new Exception("Found no resource to register index on");
                var sql = $"CREATE INDEX {index.Name.Fnuttify()} ON {tableMapping.TableName} " +
                          $"({string.Join(", ", index.Columns.Select(c => $"{c.Name.Fnuttify()} {(c.Descending ? "DESC" : "ASC")}"))})";
                Database.Query(sql);
                count += 1;
            }
            return count;
        }

        public int Update(IRequest<DatabaseIndex> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var count = 0;
            foreach (var index in request.GetInputEntities())
            {
                var tableMapping = TableMapping.Get(index.Resource.Type);
                Database.Query($"DROP INDEX {index.Name.Fnuttify()} ON {tableMapping.TableName}");
                var sql = $"CREATE INDEX {index.Name.Fnuttify()} ON {tableMapping.TableName} " +
                          $"({string.Join(", ", index.Columns.Select(c => $"{c.Name.Fnuttify()} {(c.Descending ? "DESC" : "")}"))})";
                Database.Query(sql);
                count += 1;
            }
            return count;
        }

        public int Delete(IRequest<DatabaseIndex> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var count = 0;
            foreach (var index in request.GetInputEntities())
            {
                Database.Query($"DROP INDEX {index.Name.Fnuttify()}");
                count += 1;
            }
            return count;
        }
    }
}