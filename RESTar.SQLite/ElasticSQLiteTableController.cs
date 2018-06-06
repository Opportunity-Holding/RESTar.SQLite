using System.Collections.Generic;
using System.Linq;
using RESTar.SQLite.Meta;

namespace RESTar.SQLite
{
    public class ElasticSQLiteTableController<T> where T : ElasticSQLiteTable
    {
        private TableMapping Mapping { get; set; }
        public string CLRTypeName { get; private set; }
        public string SQLTableName { get; private set; }
        public Dictionary<string, CLRDataType> Columns { get; private set; }

        protected ElasticSQLiteTableController() { }

        protected static IEnumerable<TDerived> Select<TDerived>() where TDerived : ElasticSQLiteTableController<T>, new() => TableMapping.All
            .Where(mapping => typeof(T).IsAssignableFrom(mapping.CLRClass))
            .Select(mapping => new TDerived
            {
                Mapping = mapping,
                CLRTypeName = mapping.CLRClass.FullName,
                SQLTableName = mapping.TableName,
                Columns = mapping.ColumnMappings.ToDictionary(
                    keySelector: columnMapping => columnMapping.CLRProperty.Name,
                    elementSelector: columnMapping => columnMapping.CLRProperty.Type)
            });

        protected bool DropColumn(string columnName)
        {
            var columnMapping = Mapping.ColumnMappings.FirstOrDefault(cm => cm.CLRProperty.Name.EqualsNoCase(columnName));
            if (columnMapping == null) return false;
            if (columnMapping.IsRowId || columnMapping.CLRProperty.IsDeclared)
                throw new SQLiteException($"Cannot drop column '{columnMapping.SQLColumn.Name}' from table '{Mapping.TableName}'. " +
                                          "Column is not editable.");
            Mapping.ColumnMappings.Remove(columnMapping);
            Mapping.ReloadColumnNames();
            columnMapping.Drop();
            Mapping.Update();
            return true;
        }

        protected bool Update()
        {
            var updated = false;
            var columnsToAdd = Columns.Keys
                .Except(Mapping.SQLColumnNames)
                .Select(name => (name, type: Columns[name]));
            foreach (var (name, type) in columnsToAdd.Where(c => c.type != CLRDataType.Unsupported))
            {
                var clrProperty = new CLRProperty(name, type);
                var sqlColumn = new SQLColumn(name, type.ToSQLDataType());
                Mapping.ColumnMappings.Add(new ColumnMapping(Mapping, clrProperty, sqlColumn));
                updated = true;
            }
            Mapping.ColumnMappings.Push();
            Mapping.Update();
            return updated;
        }
    }
}