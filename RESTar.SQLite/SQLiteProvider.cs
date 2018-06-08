using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Text.RegularExpressions;
using RESTar.Linq;
using RESTar.Meta;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.SQLite.Meta;

namespace RESTar.SQLite
{
    /// <inheritdoc cref="EntityResourceProvider{T}" />
    /// <inheritdoc cref="IProceduralEntityResourceProvider" />
    /// <summary>
    /// A resource provider for the SQLite database system. To use, include an instance of this class 
    /// in the call to RESTarConfig.Init(). To register SQLite resources, create subclasses of SQLiteTable
    /// and decorate them with the SQLiteAttribute together with the RESTarAttribute. Public instance 
    /// properties can be mapped to columns in SQLite by decorating the with the ColumnAttribute. All O/RM 
    /// mapping and query building is done by RESTar. Use the DatabaseIndex resource to register indexes 
    /// for SQLite resources (just like you would for Starcounter resources).
    /// </summary>
    public class SQLiteProvider : EntityResourceProvider<SQLiteTable>, IProceduralEntityResourceProvider
    {
        private static bool IsInitiated { get; set; }

        private static void Init()
        {
            if (IsInitiated) return;
            typeof(SQLiteTable).GetConcreteSubclasses().ForEach(TableMapping.Create);
            IsInitiated = true;
        }


        /// <inheritdoc />
        protected override bool IsValid(IEntityResource resource, out string reason)
        {
            reason = null;
            return true;
        }

        /// <inheritdoc />
        public override void ModifyResourceAttribute(Type type, RESTarAttribute attribute)
        {
            if (type.IsSubclassOf(typeof(ElasticSQLiteTable)))
            {
                attribute.AllowDynamicConditions = true;
                attribute.FlagStaticMembers = true;
            }
        }

        /// <inheritdoc />
        public override IDatabaseIndexer DatabaseIndexer { get; }

        /// <inheritdoc />
        public SQLiteProvider(string databaseDirectory, string databaseName)
        {
            if (!Regex.IsMatch(databaseName, @"^[a-zA-Z0-9_]+$"))
                throw new SQLiteException($"SQLite database name '{databaseName}' contains invalid characters: " +
                                          "Only letters, numbers and underscores are valid in SQLite database names.");
            var databasePath = $"{databaseDirectory}\\{databaseName}.sqlite";
            if (!Directory.Exists(databaseDirectory))
                Directory.CreateDirectory(databaseDirectory);
            if (!File.Exists(databasePath))
                SQLiteConnection.CreateFile(databasePath);
            Starcounter.Db.TransactAsync(() =>
            {
                Settings.All.ForEach(Starcounter.Db.Delete);
                new Settings
                {
                    DatabasePath = databasePath,
                    DatabaseDirectory = databaseDirectory,
                    DatabaseName = databaseName,
                    DatabaseConnectionString = $"Data Source={databasePath};Version=3;"
                };
            });
            DatabaseIndexer = new SQLiteIndexer();
            Init();
        }

        /// <inheritdoc />
        public override void ReceiveClaimed(ICollection<IEntityResource> claimedResources)
        {
            foreach (var claimed in claimedResources)
            {
                var tableMapping = TableMapping.Get(claimed.Type) ?? throw new SQLiteException(
                                       $"A resource '{claimed}' was claimed by the SQLite resource provider, " +
                                       "but had no existing table mapping");
                tableMapping.Resource = claimed;
            }
        }

        /// <inheritdoc />
        protected override Type AttributeType => typeof(SQLiteAttribute);

        protected override IEnumerable<T> DefaultSelect<T>(IRequest<T> request) => SQLiteOperations<T>.Select(request);
        protected override int DefaultInsert<T>(IRequest<T> request) => SQLiteOperations<T>.Insert(request);
        protected override int DefaultUpdate<T>(IRequest<T> request) => SQLiteOperations<T>.Update(request);
        protected override int DefaultDelete<T>(IRequest<T> request) => SQLiteOperations<T>.Delete(request);
        protected override long DefaultCount<T>(IRequest<T> request) => SQLiteOperations<T>.Count(request);

        protected override IEnumerable<IProceduralEntityResource> SelectProceduralResources()
        {
            foreach (var resource in SQLite<ProceduralResource>.Select())
            {
                if (TableMapping.Get(resource.Type) == null)
                    TableMapping.Create(resource.Type);
                yield return resource;
            }
        }

        protected override IProceduralEntityResource InsertProceduralResource(string name, string description, Method[] methods, dynamic data)
        {
            var resource = new ProceduralResource
            {
                Name = name,
                Description = description,
                Methods = methods,
                BaseTypeName = data.BaseTypeName ?? throw new SQLiteException("No BaseTypeName defined in 'Data' in resource controller")
            };
            var resourceType = resource.Type;
            TableMapping.Create(resourceType);
            SQLite<ProceduralResource>.Insert(resource);
            return resource;
        }

        protected override void SetProceduralResourceMethods(IProceduralEntityResource resource, Method[] methods)
        {
            var _resource = (ProceduralResource) resource;
            _resource.Methods = methods;
            SQLite<ProceduralResource>.Update(_resource);
        }

        protected override void SetProceduralResourceDescription(IProceduralEntityResource resource, string newDescription)
        {
            var _resource = (ProceduralResource) resource;
            _resource.Description = newDescription;
            SQLite<ProceduralResource>.Update(_resource);
        }

        protected override bool DeleteProceduralResource(IProceduralEntityResource resource)
        {
            var _resource = (ProceduralResource) resource;
            TableMapping.Drop(_resource.Type);
            SQLite<ProceduralResource>.Delete(_resource);
            return true;
        }
    }
}