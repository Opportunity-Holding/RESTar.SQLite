using System.ComponentModel.DataAnnotations;

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
        [RESTarMember(order: int.MaxValue), Key] public long RowId { get; internal set; }
    }
}