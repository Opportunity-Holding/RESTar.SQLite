using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RESTar.Linq;
using RESTar.Meta;
using RESTar.Resources;
using RESTar.Resources.Operations;
using RESTar.SQLite.Meta;
using Starcounter;

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
        static SQLiteProvider() => SQLiteDbController.Init();

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
            Db.TransactAsync(() =>
            {
                Settings.All.ForEach(Db.Delete);
                new Settings
                {
                    DatabasePath = databasePath,
                    DatabaseDirectory = databaseDirectory,
                    DatabaseName = databaseName,
                    DatabaseConnectionString = $"Data Source={databasePath};Version=3;"
                };
            });
            DatabaseIndexer = new SQLiteIndexer();
        }

        /// <inheritdoc />
        public override void ReceiveClaimed(ICollection<IEntityResource> claimedResources) => claimedResources
            .Where(c => TableMapping.Get(c.Type) == null)
            .ForEach(c => throw new SQLiteException($"A resource '{c}' was claimed by the SQLite resource provider, but had " +
                                                    "no existing table mapping"));

        /// <inheritdoc />
        protected override Type AttributeType => typeof(SQLiteAttribute);

        /// <inheritdoc />
        protected override Selector<T> GetDefaultSelector<T>() => SQLiteOperations<T>.Select;

        /// <inheritdoc />
        protected override Inserter<T> GetDefaultInserter<T>() => SQLiteOperations<T>.Insert;

        /// <inheritdoc />
        protected override Updater<T> GetDefaultUpdater<T>() => SQLiteOperations<T>.Update;

        /// <inheritdoc />
        protected override Deleter<T> GetDefaultDeleter<T>() => SQLiteOperations<T>.Delete;

        /// <inheritdoc />
        protected override Counter<T> GetDefaultCounter<T>() => SQLiteOperations<T>.Count;

        /// <inheritdoc />
        protected override Profiler<T> GetProfiler<T>() => null;
    }
}