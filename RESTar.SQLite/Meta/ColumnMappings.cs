using System.Collections.Generic;
using System.Linq;

namespace RESTar.SQLite.Meta
{
    /// <inheritdoc />
    /// <summary>
    /// A collection of ColumnMapping instances
    /// </summary>
    public class ColumnMappings : List<ColumnMapping>
    {
        /// <inheritdoc />
        public ColumnMappings(IEnumerable<ColumnMapping> collection) : base(collection) { }

        internal string ToSQL() => string.Join(", ", this.Select(c => c.SQLColumn.ToSQL()));
        internal void Push() => ForEach(mapping => mapping.Push());
    }

    internal static class ColumnMappingsExtensions
    {
        public static ColumnMappings ToColumnMappings(this IEnumerable<ColumnMapping> mappings) => new ColumnMappings(mappings);
    }
}