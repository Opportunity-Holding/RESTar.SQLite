using System.ComponentModel.DataAnnotations;
using RESTar.Resources;

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
        [RESTarMember(order: int.MaxValue), Key]
        public long RowId { get; internal set; }

        internal void _OnSelect() => OnSelect();
        internal void _OnInsert() => OnInsert();
        internal void _OnUpdate() => OnUpdate();
        internal void _OnDelete() => OnDelete();
        
        /// <summary>
        /// Called for this entity after it has been created and populated with data from
        /// the SQLite table.
        /// </summary>
        protected virtual void OnSelect() { }

        /// <summary>
        /// Called for this entity before it is converted to a row in the SQLite table. No
        /// new dynamic members can be added here, since the INSERT statement is already
        /// compiled. Values can be changed.
        /// </summary>
        protected virtual void OnInsert() { }

        /// <summary>
        /// Called for this entity before it is used to push updates to a given row in
        /// the SQLite table.
        /// </summary>
        protected virtual void OnUpdate() { }

        /// <summary>
        /// Called for this entity before it is deleted from the SQLite table.
        /// </summary>
        protected virtual void OnDelete() { }
    }
}