using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Starcounter;

namespace RESTar.SQLite
{
    /// <summary>
    /// Settings for the SQLite instance
    /// </summary>
    [Database]
    public class Settings
    {
        /// <summary>
        /// The path to the database
        /// </summary>
        public string DatabasePath { get; internal set; }

        /// <summary>
        /// The directory of the database
        /// </summary>
        public string DatabaseDirectory { get; internal set; }

        /// <summary>
        /// The database name
        /// </summary>
        public string DatabaseName { get; internal set; }

        /// <summary>
        /// The connection string to use when accessing the database
        /// </summary>
        [IgnoreDataMember] public string DatabaseConnectionString { get; internal set; }

        /// <summary>
        /// The SQLite database connection string to use for manual access to the SQLite
        /// database
        /// </summary>
        public static string ConnectionString => Instance.DatabaseConnectionString;

        private const string SQL = "SELECT t FROM RESTar.SQLite.Settings t";
        internal static IEnumerable<Settings> All => Db.SQL<Settings>(SQL);
        internal static Settings Instance => Db.SQL<Settings>(SQL).FirstOrDefault();
    }
}