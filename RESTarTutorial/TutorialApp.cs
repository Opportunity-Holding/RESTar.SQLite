using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Resources.Operations;
using RESTar.SQLite;
using static RESTar.Method;

namespace RESTarTutorial
{
    using RESTar;
    using Starcounter;

    /// <summary>
    /// A simple RESTar application
    /// </summary>
    public class TutorialApp
    {
        public static void Main()
        {
            var projectFolder = Application.Current.WorkingDirectory;
            RESTarConfig.Init
            (
                port: 8282,
                uri: "/api",
                requireApiKey: true,
                configFilePath: projectFolder + "/Config.xml",
                entityResourceProviders: new[] {new SQLiteProvider(projectFolder, "data3")}
            );

            // The 'port' argument sets the HTTP port on which to register the REST handlers
            // The 'uri' argument sets the root uri of the REST API
            // The 'requireApiKey' parameter is set to 'true'. API keys are required in all incoming requests.
            // The 'configFilePath' points towards the configuration file, which contains API keys. In this case,
            //   this file is located in the project folder.
            // The 'resourceProviders' parameter is used for SQLite integration
        }
    }

    [RESTar, Database]
    public class ScResource
    {
        public string STR { get; set; }
        public int INT { get; set; }
        public DateTime DATETIME { get; set; }
        public decimal DECIMAL { get; set; }
    }

    [RESTar, SQLite]
    public class SQLiteResource : SQLiteTable
    {
        public string STR { get; set; }
        public int INT { get; set; }
        public DateTime DATETIME { get; set; }
        public decimal DECIMAL { get; set; }

        public int STRLength => STR.Length;
    }

    [SQLite]
    public class SQLiteResource2 : SQLiteTable
    {
        public string STR { get; set; }
        public int INT { get; set; }
        public DateTime DATETIME { get; set; }
        public decimal DECIMAL { get; set; }

        public int STRLength => STR.Length;
    }

    [RESTar(GET)]
    public class SuperheroReport : ISelector<SuperheroReport>
    {
        public long NumberOfSuperheroes { get; private set; }
        public Superhero FirstSuperheroInserted { get; private set; }
        public Superhero LastSuperheroInserted { get; private set; }

        /// <inheritdoc />
        /// <summary>
        /// This method returns an IEnumerable of the resource type. RESTar will call this 
        /// on GET requests and send the results back to the client as e.g. JSON.
        /// </summary>
        public IEnumerable<SuperheroReport> Select(IRequest<SuperheroReport> request)
        {
            var superHeroesOrdered = SQLite<Superhero>
                .Select()
                .OrderBy(r => r.RowId)
                .ToList();
            return new[]
            {
                new SuperheroReport
                {
                    NumberOfSuperheroes = SQLite<Superhero>.Count(),
                    FirstSuperheroInserted = superHeroesOrdered.FirstOrDefault(),
                    LastSuperheroInserted = superHeroesOrdered.LastOrDefault(),
                }
            };
        }
    }

    [SQLite(CustomTableName = "Heroes"), RESTar]
    public class Superhero : SQLiteTable
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public string Sex { get; set; }

        [SQLiteMember(columnName: "YearIntroduced")]
        public int Year { get; set; }
    }

    [SQLite, RESTar]
    public class MyElastic : ElasticSQLiteTable
    {
        public string Name { get; set; }
        public int Id { get; set; }
    }

    [RESTar]
    public class Event : SQLiteResourceController<Event, MyElastic> { }
}