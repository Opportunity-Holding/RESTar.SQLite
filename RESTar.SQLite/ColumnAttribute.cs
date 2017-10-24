using System;

namespace RESTar.SQLite
{
    /// <inheritdoc />
    /// <summary>
    /// Decorate public instance properties with this attribute to 
    /// bind them to SQLite table columns
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ColumnAttribute : Attribute
    {
    }
}