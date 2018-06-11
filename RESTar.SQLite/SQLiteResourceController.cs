using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Requests;
using RESTar.Resources;

namespace RESTar.SQLite
{
    public abstract class SQLiteResourceController<TController, TBaseType> : ResourceController<TController, SQLiteProvider>
        where TController : SQLiteResourceController<TController, TBaseType>, new()
        where TBaseType : ElasticSQLiteTable
    {
        public class TableDefinition : ElasticSQLiteTableController<TableDefinition, TBaseType> { }

        protected override dynamic Data { get; }
        private Type BaseType { get; }

        [RESTarMember(order: 3)] public TableDefinition Definition { get; private set; }

        public override IEnumerable<TController> Select(IRequest<TController> request) => base
            .Select(request)
            .Where(item => item.Type.IsSubclassOf(BaseType))
            .Select(item =>
            {
                item.Definition = TableDefinition.Select().First(s => s.CLRTypeName == item.Name);
                return item;
            });

        public override int Update(IRequest<TController> request)
        {
            var i = 0;
            foreach (var resource in request.GetInputEntities().ToList())
            {
                resource.Update();
                resource.Definition.Update();
                i += 1;
            }
            return i;
        }

        protected SQLiteResourceController()
        {
            BaseType = typeof(TBaseType);
            if (!BaseType.IsSubclassOf(typeof(ElasticSQLiteTable)))
                throw new SQLiteException($"Cannot create procedural SQLite resource from base type '{BaseType}'. Must be a " +
                                          "subclass of RESTar.SQLite.ElasticSQLiteTable with at least one defined column property.");
            Data = new SQLiteProceduralResourceData { BaseTypeName = BaseType.AssemblyQualifiedName };
        }
    }

    internal class SQLiteProceduralResourceData
    {
        public string BaseTypeName { get; set; }
    }
}