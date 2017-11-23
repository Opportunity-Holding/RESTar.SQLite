using System;
using System.Collections.Generic;
using System.Linq;
using Dynamit;
using Newtonsoft.Json.Linq;
using RESTar;
using RESTar.Resources;
using RESTar.SQLite;
using Starcounter;

// ReSharper disable UnassignedGetOnlyAutoProperty
// ReSharper disable UnusedMember.Global

#pragma warning disable 1591

namespace RESTarSQLiteExample
{
    public class Program
    {
        public static void Main()
        {
            var sqliteProvider = new SQLiteProvider
            (
                databaseDirectory: @"C:\MyDb",
                databaseName: "MyDatabaseName"
            );
            RESTarConfig.Init
            (
                port: 8010,
                requireApiKey: true,
                configFilePath: @"C:\MyConfig.config",
                resourceProviders: new[] {sqliteProvider}
            );

            new Request<MySQLiteProduct>().POST(() => new[]
            {
                new MySQLiteProduct
                {
                    ProductId = "OLD_G1239",
                    InStock = 7,
                    NetPriceUsd = 249.99M,
                    RegistrationDate = new DateTime(2003, 01, 12)
                },
                new MySQLiteProduct
                {
                    ProductId = "F9113",
                    InStock = 24,
                    NetPriceUsd = 119,
                    RegistrationDate = new DateTime(2015, 08, 02)
                },
                new MySQLiteProduct
                {
                    ProductId = "F3388",
                    InStock = 500,
                    NetPriceUsd = 109,
                    RegistrationDate = new DateTime(2011, 12, 17)
                }
            });
        }
    }

    [SQLite, RESTar]
    public class MySQLiteProduct : SQLiteTable
    {
        [Column] public string ProductId { get; set; }
        [Column] public int InStock { get; set; }
        [Column] public decimal NetPriceUsd { get; set; }
        [Column] public DateTime RegistrationDate { get; set; }

        public bool ImportedFromOldDb => ProductId.StartsWith("OLD_");
        public bool InShortSupply => InStock < 10;
        public int DaysSinceRegistration => (DateTime.Now - RegistrationDate).Days;
    }


    [SQLite, RESTar]
    public class BidRequestArchive : SQLiteTable
    {
        [Column] public string BidRequestId { get; set; }
        [Column] public string UserId { get; set; }
        [Column] public string IP { get; set; }
        [Column] public string SiteDomain { get; set; }
        [Column] public string AppDomain { get; set; }
        [Column] public DateTime Time { get; set; }
    }

    [RESTar(Methods.GET)]
    public class MyThing : ResourceWrapper<Table> { }

    [SQLite, RESTar]
    public class SQLTable : SQLiteTable
    {
        [Column] public string STR { get; set; }
        [Column] public long INT { get; set; }
        [Column] public DateTime? DATE { get; set; }
        [Column] public int INT2 { get; set; }
        [Column] public bool BOOL { get; set; }
        [Column] public double DOU { get; set; }
        [Column] public float SING { get; set; }
        [Column] public decimal DEC { get; set; }
        [Column] public short SHORT { get; set; }
        [Column] public byte BYTE { get; set; }
    }

    [Database]
    public class Table
    {
        public string STR;
        public DateTime? DT;
        public DateTime DT2;
    }

    [Database, RESTar]
    public class MyResource
    {
        public int MyId;
        public decimal MyDecimal;
        public string MyMember;
        public string SomeMember;

        [RESTar(Methods.GET, Description = "Returns a fine object")]
        public class Get : JObject, ISelector<Get>
        {
            public IEnumerable<Get> Select(IRequest<Get> request) => new[] {new Get {["Soo"] = 123}};
        }
    }


    [Database, RESTar]
    public class MyClass
    {
        public int MyInt;
        private int prInt;

        public int OtherInt
        {
            get => prInt;
            set => prInt = value;
        }

        public MyResource Resource { get; }

        public int ThirdInt
        {
            get => prInt;
            set
            {
                if (value > 10)
                    prInt = value;
                else prInt = 0;
            }
        }
    }

    [RESTar]
    public class R : IInserter<R>, ISelector<R>, IUpdater<R>, IDeleter<R>
    {
        public string S { get; set; }
        public string[] Ss { get; set; }

        public int Insert(IEnumerable<R> entities, IRequest<R> request)
        {
            return entities.Count();
        }

        public IEnumerable<R> Select(IRequest<R> request)
        {
            return new[] {new R {S = "Swoo", Ss = new[] {"S", "Sd"}}};
        }

        public int Update(IEnumerable<R> entities, IRequest<R> request)
        {
            return entities.Count();
        }

        public int Delete(IEnumerable<R> entities, IRequest<R> request)
        {
            return entities.Count();
        }
    }

    public enum EE
    {
        A,
        B,
        C
    }

    [Database, RESTar]
    public class MyOther
    {
        public string Str;
    }

    [DList(typeof(MyElement))]
    public class MyList : DList
    {
        protected override DElement NewElement(DList list, int index, object value = null)
        {
            return new MyElement(list, index, value);
        }
    }

    public class MyElement : DElement
    {
        public MyElement(DList list, int index, object value = null) : base(list, index, value) { }
    }

    [RESTar(Methods.GET)]
    public class MyDynamicTable : DDictionary, IDDictionary<MyDynamicTable, MyDynamicTableKvp>
    {
        public MyDynamicTableKvp NewKeyPair(MyDynamicTable dict, string key, object value = null) =>
            new MyDynamicTableKvp(dict, key, value);
    }

    public class MyDynamicTableKvp : DKeyValuePair
    {
        public MyDynamicTableKvp(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }
}