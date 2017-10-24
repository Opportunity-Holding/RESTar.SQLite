namespace RESTar.SQLite
{
    /// <summary>
    /// The base class for all SQLite table resource types
    /// </summary>
    public abstract class SQLiteTable
    {
        internal long RowId { get; set; }
    }
}