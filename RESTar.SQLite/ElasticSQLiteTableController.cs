using System.Collections.Generic;
using System.Linq;
using RESTar.Linq;
using RESTar.Requests;
using RESTar.Resources.Operations;
using RESTar.SQLite.Meta;

namespace RESTar.SQLite
{
    public class ElasticSQLiteTableController<TController, TTable> : ISelector<TController>, IUpdater<TController>
        where TTable : ElasticSQLiteTable
        where TController : ElasticSQLiteTableController<TController, TTable>, new()
    {
        private TableMapping TableMapping { get; set; }
        public string CLRTypeName { get; private set; }
        public string SQLTableName { get; private set; }
        public Dictionary<string, CLRDataType> Columns { get; private set; }
        public string[] DroppedColumns { get; set; }

        public virtual IEnumerable<TController> Select(IRequest<TController> request) => Select().Where(request.Conditions);
        public virtual int Update(IRequest<TController> request) => request.GetInputEntities().ToList().Count(entity => entity.Update());

        protected ElasticSQLiteTableController() { }

        public static IEnumerable<TController> Select() => TableMapping.All
            .Where(mapping => typeof(TTable).IsAssignableFrom(mapping.CLRClass))
            .Select(mapping => new TController
            {
                TableMapping = mapping,
                CLRTypeName = mapping.CLRClass.FullName,
                SQLTableName = mapping.TableName,
                Columns = mapping.ColumnMappings.ToDictionary(
                    keySelector: columnMapping => columnMapping.CLRProperty.Name,
                    elementSelector: columnMapping => columnMapping.CLRProperty.Type),
                DroppedColumns = new string[0]
            });

        protected bool DropColumns(params string[] columnNames)
        {
            var toDrop = columnNames
                .Select(columnName =>
                {
                    var mapping = TableMapping.ColumnMappings.FirstOrDefault(cm => cm.CLRProperty.Name.EqualsNoCase(columnName));
                    if (mapping == null) return null;
                    if (mapping.IsRowId || mapping.CLRProperty.IsDeclared)
                        throw new SQLiteException($"Cannot drop column '{mapping.SQLColumn.Name}' from table '{TableMapping.TableName}'. " +
                                                  "Column is not editable.");
                    return mapping;
                })
                .Where(mapping => mapping != null)
                .ToList();
            if (!toDrop.Any()) return false;
            TableMapping.DropColumns(toDrop);
            return true;
        }

        public bool Update()
        {
            var updated = false;
            var columnsToAdd = Columns.Keys
                .Except(TableMapping.SQLColumnNames)
                .Select(name => (name, type: Columns[name]));
            DropColumns(DroppedColumns);
            foreach (var (name, type) in columnsToAdd.Where(c => c.type != CLRDataType.Unsupported))
            {
                TableMapping.ColumnMappings.Add(new ColumnMapping
                (
                    tableMapping: TableMapping,
                    clrProperty: new CLRProperty(name, type),
                    sqlColumn: new SQLColumn(name, type.ToSQLDataType())
                ));
                updated = true;
            }
            TableMapping.ColumnMappings.Push();
            TableMapping.Update();
            return updated;
        }
    }
}