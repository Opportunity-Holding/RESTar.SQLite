using System.Collections.Generic;
using System.Linq;
using RESTar.SQLite.Meta;

namespace RESTar.SQLite
{
    public class ElasticSQLiteTableController<T> where T : ElasticSQLiteTable
    {
        private TableMapping TableMapping { get; set; }
        public string CLRTypeName { get; private set; }
        public string SQLTableName { get; private set; }
        public Dictionary<string, CLRDataType> Columns { get; private set; }

        protected ElasticSQLiteTableController() { }

        protected static IEnumerable<TDerived> Select<TDerived>() where TDerived : ElasticSQLiteTableController<T>, new() => TableMapping.All
            .Where(mapping => typeof(T).IsAssignableFrom(mapping.CLRClass))
            .Select(mapping => new TDerived
            {
                TableMapping = mapping,
                CLRTypeName = mapping.CLRClass.FullName,
                SQLTableName = mapping.TableName,
                Columns = mapping.ColumnMappings.ToDictionary(
                    keySelector: columnMapping => columnMapping.CLRProperty.Name,
                    elementSelector: columnMapping => columnMapping.CLRProperty.Type)
            });

        protected bool DropColumn(string columnName)
        {
            var columnMapping = TableMapping.ColumnMappings.FirstOrDefault(cm => cm.CLRProperty.Name.EqualsNoCase(columnName));
            if (columnMapping == null) return false;
            if (columnMapping.IsRowId || columnMapping.CLRProperty.IsDeclared)
                throw new SQLiteException($"Cannot drop column '{columnMapping.SQLColumn.Name}' from table '{TableMapping.TableName}'. " +
                                          "Column is not editable.");
            TableMapping.ColumnMappings.Remove(columnMapping);
            TableMapping.ReloadColumnNames();
            columnMapping.Drop();
            TableMapping.Update();
            return true;
        }

        protected bool Update()
        {
            var updated = false;
            var columnsToAdd = Columns.Keys
                .Except(TableMapping.SQLColumnNames)
                .Select(name => (name, type: Columns[name]));
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