using System;
using System.Reflection;
using RESTar.Meta;

namespace RESTar.SQLite.Meta
{
    /// <summary>
    /// An object representing a property of a CLR class
    /// </summary>
    public class CLRProperty
    {
        private ColumnMapping Mapping { get; set; }

        /// <summary>
        /// The name of the CLR property
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The type of the CLR property, defined as a TypeCode
        /// </summary>
        public TypeCode Type { get; }

        /// <summary>
        /// Is this CLR property declared, or was it defined at runtime?
        /// </summary>
        public bool IsDeclared { get; }

        /// <summary>
        /// The getter for the property value
        /// </summary>
        public Getter Get { get; }

        /// <summary>
        /// The setter for the property value
        /// </summary>
        public Setter Set { get; }

        /// <summary>
        /// The optional SQLiteMemberAttribute associated with this CLR property
        /// </summary>
        public SQLiteMemberAttribute MemberAttribute { get; }

        internal void SetMapping(ColumnMapping mapping)
        {
            Mapping = mapping;
        }

        /// <summary>
        /// From CLR
        /// </summary>
        /// <param name="propertyInfo"></param>
        public CLRProperty(PropertyInfo propertyInfo)
        {
            Name = propertyInfo.Name;
            Type = propertyInfo.PropertyType.ResolveCLRTypeCode();
            Get = propertyInfo.MakeDynamicGetter();
            Set = propertyInfo.MakeDynamicSetter();
            MemberAttribute = propertyInfo.GetCustomAttribute<SQLiteMemberAttribute>();
            IsDeclared = true;
        }

        /// <summary>
        /// From SQL
        /// </summary>
        public CLRProperty(string name, TypeCode typeCode)
        {
            Name = name;
            Type = typeCode;
            IsDeclared = false;
            if (Type == TypeCode.Empty) return;
            Get = obj =>
            {
                if (obj is IDynamicMemberValueProvider dm && dm.TryGetValue(Name, out var actualKey, out var value))
                {
                    Name = actualKey;
                    return value;
                }
                return null;
            };
            Set = (obj, value) => (obj as IDynamicMemberValueProvider)?.TrySetValue(Name, value);
        }
    }
}