using RESTar;
using RESTarExample.TestDb;
using Starcounter;
using static RESTar.Methods;

#pragma warning disable 1591

namespace RESTarExample
{
    [Database, RESTar(GET, PATCH)]
    public class TestDatabase
    {
        public bool Active
        {
            get => DB.Exists<TestBase>();
            set
            {
                if (value)
                    Generator.GenerateTestDatabase();
                else Generator.DeleteTestDatabase();
            }
        }

        internal static void Init()
        {
            Db.TransactAsync(() =>
            {
                foreach (var obj in DB.All<TestDatabase>())
                    obj?.Delete();
                new TestDatabase();
            });
        }
    }
}