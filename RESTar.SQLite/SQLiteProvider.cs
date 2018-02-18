using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Operations;
using RESTar.Resources;
using Starcounter;
using static RESTar.Methods;

namespace RESTar.SQLite
{
    /// <inheritdoc />
    /// <summary>
    /// A resource provider for the SQLite database system. To use, include an instance of this class 
    /// in the call to RESTarConfig.Init(). To register SQLite resources, create subclasses of SQLiteTable
    /// and decorate them with the SQLiteAttribute together with the RESTarAttribute. Public instance 
    /// properties can be mapped to columns in SQLite by decorating the with the ColumnAttribute. All O/RM 
    /// mapping and query building is done by RESTar. Use the DatabaseIndex resource to register indexes 
    /// for SQLite resources (just like you would for Starcounter resources).
    /// </summary>
    public class SQLiteProvider : ResourceProvider<SQLiteTable>
    {
        /// <inheritdoc />
        public override bool IsValid(IEntityResource resource, out string reason)
        {
            var columnProperties = resource.Members.Values
                .Where(p => p.HasAttribute<ColumnAttribute>())
                .ToList();
            if (!columnProperties.Any())
            {
                reason = "SQLite resource types must contain at least one public instance property declared " +
                         "as column using the ColumnAttribute";
                return false;
            }

            if (!typeof(SQLiteTable).IsAssignableFrom(resource.Type))
            {
                reason = $"'{resource.Type.FullName}' does not subclass the '{typeof(SQLiteTable).FullName}' abstract " +
                         "class needed for all SQLite resource types.";
                return false;
            }

            if (resource.AvailableMethods.Contains(POST) && resource.Type.GetConstructor(Type.EmptyTypes) == null)
                reason = $"Expected parameterless constructor for type '{resource.Type.FullName}' to support POST";

            foreach (var column in columnProperties)
            {
                if (column.Name.ToLower() == "rowid")
                {
                    reason = "SQLite resources cannot contain column properties called 'RowId' or any case " +
                             "variants of it";
                    return false;
                }

                if (!column.Type.IsSQLiteCompatibleValueType(resource.Type, out var error))
                {
                    reason = error;
                    return false;
                }
            }

            reason = null;
            return true;
        }

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
        public override void ReceiveClaimed(ICollection<IEntityResource> claimedResources)
        {
            typeof(SQLiteTable)
                .GetConcreteSubclasses()
                .Except(claimedResources.Select(r => r.Type))
                .ForEach(r => throw new SQLiteException(
                    $"Found an invalid SQLiteTable resource declaration for type '{r.FullName}'. " +
                    "RESTar.SQLite.SQLiteTable subclasses must be declared as RESTar resources or " +
                    "wrapped by a ResourceWrapper"));
            claimedResources.ForEach(Cache.Add);
            SQLiteDb.SetupTables(claimedResources);
        }

        /// <inheritdoc />
        public override Type AttributeType => typeof(SQLiteAttribute);

        /// <inheritdoc />
        public override Selector<T> GetDefaultSelector<T>() => SQLiteOperations<T>.Select;

        /// <inheritdoc />
        public override Inserter<T> GetDefaultInserter<T>() => SQLiteOperations<T>.Insert;

        /// <inheritdoc />
        public override Updater<T> GetDefaultUpdater<T>() => SQLiteOperations<T>.Update;

        /// <inheritdoc />
        public override Deleter<T> GetDefaultDeleter<T>() => SQLiteOperations<T>.Delete;

        /// <inheritdoc />
        public override Counter<T> GetDefaultCounter<T>() => SQLiteOperations<T>.Count;

        /// <inheritdoc />
        public override Profiler<T> GetProfiler<T>() => null;
    }
}