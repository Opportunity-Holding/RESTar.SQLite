namespace RESTar.SQLite
{
    /// <summary>
    /// The base class for all SQLite table resource types
    /// </summary>
    public abstract class SQLiteTable
    {
        /// <summary>
        /// The unique SQLite row ID for this row
        /// </summary>
        [RESTarMember(hide: true)] public long RowId { get; set; }
    }
}