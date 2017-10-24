using System;

namespace RESTar.SQLite
{
    /// <inheritdoc />
    public class SQLiteException : Exception
    {
        internal SQLiteException(string message) : base(message)
        {
        }
    }
}