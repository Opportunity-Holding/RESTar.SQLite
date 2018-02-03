namespace RESTar.SQLite
{
    /// <inheritdoc />
    /// <summary>
    /// Decorate a class definition with this attribute to register it with 
    /// the SQLite resource provider.
    /// </summary>
    public sealed class SQLiteAttribute : ResourceProviderAttribute
    {
        /// <summary>
        /// To manually bind against a certain SQLite table, set the CustomTableName 
        /// to that table's name.
        /// </summary>
        public string CustomTableName;
    }
}