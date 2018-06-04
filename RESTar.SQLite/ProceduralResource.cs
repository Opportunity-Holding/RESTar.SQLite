using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
        private static IDictionary<string, Type> TypeCache { get; }
        static ProceduralResource() => TypeCache = new ConcurrentDictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

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
        /// The name of this resource
        /// </summary>
        public string Name { get; internal set; }

        /// <inheritdoc />
        /// <summary>
        /// The description for this resource
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The name of the dynamic table (used internally)
        /// </summary>
        public string TableName { get; internal set; }

        /// <summary>
        /// The name of the base type to generate the CLR type from
        /// </summary>
        public string BaseTypeName { get; internal set; }

        /// <summary>
        /// A string representation of the available REST methods
        /// </summary>
        [RESTarMember(ignore: true)] public string AvailableMethodsString { get; internal set; }

        /// <inheritdoc />
        public Type Type { get; internal set; }

        internal ProceduralResource(string name, string tableName, Type baseType, IEnumerable<Method> availableMethods, string description = null)
        {
            Name = name;
            TableName = tableName;
            Description = description;
            BaseTypeName = baseType.FullName;
            Methods = availableMethods.ToArray();
        }
    }
}