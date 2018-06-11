namespace RESTar.SQLite
{
    /// <summary>
    /// The different kinds of RESTar.SQLite table mappings
    /// </summary>
    public enum TableMappingKind
    {
        /// <summary>
        /// A static declared CLR class bound to an SQLite table
        /// </summary>
        Static,

        /// <summary>
        /// An elastic declared CLR class (may contain dynamic members), bound to an SQLite
        /// table with an explicit schema of allowed members.
        /// </summary>
        Elastic
    }
}