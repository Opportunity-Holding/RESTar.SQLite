using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Requests;
using RESTar.Resources;

namespace RESTar.SQLite
{
    /// <inheritdoc />
    /// <summary>
    /// Defines a resourcecontroller for inserting procedural (runtime) SQLite resources and modifying their
    /// column definitions
    /// </summary>
    /// <typeparam name="TController"></typeparam>
    /// <typeparam name="TBaseType"></typeparam>
    public abstract class SQLiteResourceController<TController, TBaseType> : ResourceController<TController, SQLiteProvider>
        where TController : SQLiteResourceController<TController, TBaseType>, new()
        where TBaseType : ElasticSQLiteTable
    {
        /// <inheritdoc />
        /// <summary>
        /// The ElasticSQLiteTableController used for retreiving and modifying the table definition
        /// of generated procedural SQLite resources
        /// </summary>
        public class TableDefinition : ElasticSQLiteTableController<TableDefinition, TBaseType> { }

        /// <inheritdoc />
        protected override dynamic Data { get; }

        private Type BaseType { get; }

        /// <summary>
        /// The table definition for this procedural SQLite resource
        /// </summary>
        [RESTarMember(order: 100)] public TableDefinition Definition { get; private set; }

        /// <inheritdoc />
        public override IEnumerable<TController> Select(IRequest<TController> request) => base
            .Select(request)
            .Where(item => item.Type.IsSubclassOf(BaseType))
            .Select(item =>
            {
                item.Definition = TableDefinition.Select().First(s => s.CLRTypeName == item.Name);
                return item;
            });

        /// <inheritdoc />
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

        /// <inheritdoc />
        protected SQLiteResourceController()
        {
            BaseType = typeof(TBaseType);
            if (!BaseType.IsSubclassOf(typeof(ElasticSQLiteTable)))
                throw new SQLiteException($"Cannot create procedural SQLite resource from base type '{BaseType}'. Must be a " +
                                          "subclass of RESTar.SQLite.ElasticSQLiteTable with at least one defined column property.");
            Data = new SQLiteProceduralResourceData {BaseTypeName = BaseType.AssemblyQualifiedName};
        }
    }

    internal class SQLiteProceduralResourceData
    {
        public string BaseTypeName { get; set; }
    }
}