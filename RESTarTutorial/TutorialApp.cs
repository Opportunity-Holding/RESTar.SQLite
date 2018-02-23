using System.Collections.Generic;
using System.Linq;
using RESTar.SQLite;
using static RESTar.Methods;

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
                resourceProviders: new[] {new SQLiteProvider(projectFolder, "data")}
            );

            // The 'port' argument sets the HTTP port on which to register the REST handlers
            // The 'uri' argument sets the root uri of the REST API
            // The 'requireApiKey' parameter is set to 'true'. API keys are required in all incoming requests.
            // The 'configFilePath' points towards the configuration file, which contains API keys. In this case,
            //   this file is located in the project folder.
            // The 'resourceProviders' parameter is used for SQLite integration
        }
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
        [Column] public string Name { get; set; }
        [Column] public string Id { get; set; }
        [Column] public string Sex { get; set; }
        [Column] public int Year { get; set; }
    }
}