using System;
using RESTar.Resources;

namespace RESTar.SQLite
{
    /// <inheritdoc cref="SQLiteTable" />
    /// <inheritdoc cref="IProceduralEntityResource" />
    /// <summary>
    /// Creates and structures all the dynamic resources for this RESTar instance
    /// </summary>
    [SQLite]
    public class ProceduralResource : SQLiteTable, IProceduralEntityResource
    {
        /// <inheritdoc />
        /// <summary>
        /// The name of this resource
        /// </summary>
        public string Name { get; set; }

        /// <inheritdoc />
        /// <summary>
        /// The available methods for this resource
        /// </summary>
        public Method[] Methods
        {
            get => AvailableMethodsString.ToMethodsArray();
            set => AvailableMethodsString = value.ToMethodsString();
        }

        /// <inheritdoc />
        /// <summary>
        /// The description for this resource
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The name of the dynamic table (used internally)
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// The name of the base type to generate the CLR type from
        /// </summary>
        public string BaseTypeName { get; set; }

        /// <summary>
        /// A string representation of the available REST methods
        /// </summary>
        [RESTarMember(ignore: true)] public string AvailableMethodsString { get; set; }

        /// <inheritdoc />
        public Type Type => TypeBuilder.GetType(this);
    }
}