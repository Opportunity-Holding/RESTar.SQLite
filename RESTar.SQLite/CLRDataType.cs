namespace RESTar.SQLite
{
    /// <summary>
    /// Allowed CLR data types for mapping with SQLite tables
    /// </summary>
    public enum CLRDataType
    {
        /// <summary />
        Unsupported = 0,

        /// <summary />
        Int16,

        /// <summary />
        Int32,

        /// <summary />
        Int64,

        /// <summary />
        Single,

        /// <summary />
        Double,

        /// <summary />
        Decimal,

        /// <summary />
        Byte,

        /// <summary />
        String,

        /// <summary />
        Boolean,

        /// <summary />
        DateTime,
    }
}