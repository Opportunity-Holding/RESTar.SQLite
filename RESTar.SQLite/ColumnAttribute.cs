using System;

namespace RESTar.SQLite
{
    /// <inheritdoc />
    /// <summary>
    /// Configure how this member is bound to an SQLite table column. Can only be
    /// used on public instance auto properties.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SQLiteMemberAttribute : Attribute
    {
        /// <summary>
        /// Is this property ignored by RESTar.SQLite? Does not imply that the property
        /// is ignored by RESTar, merely that it is not mapped to an SQLite table column.
        /// </summary>
        public bool Ignored { get; }

        /// <summary>
        /// The name of the column to map this property with. If null, the property name
        /// is used.
        /// </summary>
        public string ColumnName { get; }

        /// <inheritdoc />
        /// <summary>
        /// Creates a new instance of the <see cref="SQLiteMemberAttribute"/> class.
        /// </summary>
        /// <param name="ignore">Should this property be ignored by RESTar.SQLite? Does not imply that the property
        /// is ignored by RESTar, merely that it is not mapped to an SQLite table column.</param>
        /// <param name="columnName">The name of the column to map this property with. If null, the property name
        /// is used.</param>
        public SQLiteMemberAttribute(bool ignore = false, string columnName = null)
        {
            Ignored = ignore;
            ColumnName = columnName;
        }
    }
}