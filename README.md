_By Erik von Krusenstierna (erik.von.krusenstierna@mopedo.com)_

# What is RESTar.SQLite?

RESTar.SQLite is a free to use open-source resource provider for [RESTar](https://github.com/Mopedo/Home/tree/master/RESTar) that integrates the [System.Data.SQLite](https://system.data.sqlite.org/index.html/doc/trunk/www/index.wiki) .NET library with the RESTar framework and enables web resources that use SQLite as the underlying persistent data storage. This means that developers can use SQLite tables as resources for their RESTar applications, just like they can use Starcounter database tables.

This documentation will cover the basics of RESTar.SQLite and how to set it up in a Visual Studio project.

## Getting started

RESTar.SQLite is, like RESTar, distributed as a [package](https://www.nuget.org/packages/RESTar.SQLite) on the NuGet Gallery, and an easy way to install it in an active Visual Studio project is by entering the following into the NuGet Package Manager console:

```
Install-Package RESTar.SQLite
```

## Using RESTar.SQLite

RESTar.SQLite defines a **resource provider** for RESTar, which should be included in the call to `RESTarConfig.Init()` in applications that wish to use it. Resource providers are essentially add-ons for RESTar, enabling – for example – database technologies like SQLite to work with RESTar just like native database technologies like Starcounter. For more on resource providers, see the [RESTar Specification](https://github.com/Mopedo/Home/blob/master/RESTar/Developing%20a%20RESTar%20API/Developing%20entity%20resources/Resource%20providers.md).

### Resource declarations

All SQLite resources are classes, and need to (1) be decorated with the `SQLiteAttribute` and (2) inherit from the abstract class `SQLiteTable`. This is important to ensure that RESTar.SQLite can do O/R mapping between the SQLite tables and the object model defined in the RESTar application. During startup, RESTar will collect and validate these resource types and make them available in the REST API.

A simple example resource:

```csharp
using System;
using RESTar;
using RESTar.SQLite;

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
```

Public instance properties decorated with the `ColumnAttribute` attribute will automatically be bound to corresponding columns in an SQLite table. Public instance properties not decorated with the `ColumnAttribute` will not have corresponding columns in the generated SQLite database table, but can still be referenced in RESTar requests as any regular property.

### Data types

The following .NET data types are allowed in RESTar.SQLite resource types:

```
System.Byte
System.Int16
System.Int32
System.Int64
System.Single
System.Double
System.Boolean
System.DateTime
System.Nullable<T> // where T is one of the types above
System.String
```

### Instantiating the resource provider

Here is how to instantiate the resource provider and use it in the call to `RESTarConfig.Init()`:

```csharp
public class Program
{
    public static void Main()
    {
        var sqliteProvider = new SQLiteProvider
        (
            databaseDirectory: @"C:\MyDb",
            databaseName: "MyDatabaseName"
        );
        RESTarConfig.Init(resourceProviders: new [] {sqliteProvider});
    }
}
```

The database name may only contain letters, numbers and underscores. If there is no directory matching the database directory given in the `SQLiteProvider` constructor, it will be created automatically. The database file will be placed in the given `databaseDirectory` directory and have the filename `<databaseName>.sqlite`. Any existing file in the directory with that name will be reused.

### Resource validation rules

Apart from the rules defined by RESTar, RESTar.SQLite resource types must be classes and:

1. Be decorated with the `SQLiteAttribute` attribute.
2. Inherit from the abstract class `SQLiteTable`.
3. Contain at least one public instance property declared as column using the `ColumnAttribute`.
4. Have a public parameterless constructor – if the resource supports POST
5. Not contain any property with the name `RowId`, including any case variants.

## Database management

RESTar.SQLite will create a new `.sqlite3` file in the directory provided in the `SQLiteProvider` constructor, with the given database name (unless one already exists). This file contains all tables used by RESTar.SQLite. If the name and/or directory is changed, RESTar.SQLite will simply create a new database file and any old data will be unreachable. There are some important things to keep in mind regarding how RESTar.SQLite works with the SQLite database:

1. During execution of `RESTarConfig.Init()`, RESTar.SQLite will create one table for each well-defined RESTar.SQLite resource type, if one with the same name does not already exist.
2. If properties are added to some well-defined RESTar.SQLite resource type between two executions of `RESTarConfig.Init()`, they will be added to the corresponding SQLite table.
3. If property types are changed in some well-defined RESTar.SQLite resource type between two executions of `RESTarConfig.Init()`, you will see a runtime error. Such table alterations cannot be handled automatically. Instead you should load the SQLite file manually and perform the necessary operations there.
4. If properties are removed from some resource type between two executions of `RESTarConfig.Init()`, they will not be removed from the corresponding SQLite table.
5. RESTar.SQLite never drops tables from the SQLite database.

For operations on the SQLite database that are not performed by RESTar.SQLite, the developer is encouraged to manually connect to the SQLite database. The connection string is available as a public static property of the `RESTar.SQLite.Settings` class.

### Helper methods

To access the SQLite database from inside your application, use the generic static class `SQLite<T>`. It has methods for selecting rows (including the proper O/R mapping to the RESTar resource type) and inserting, updating and deleting rows.

## Indexing

RESTar.SQLite is integrated with the RESTar `DatabaseIndex` resource, and can create and remove indexes in the SQLite database. To register an SQLite index, simply do as you would if the resource was a regular Starcounter resource – by posting the following to the `RESTar.Admin.DatabaseIndex` resource (example):

```json
{
    "Name": "MyIndexName",
    "Table": "MySQLiteProduct",
    "Columns": [{
        "Name": "MyColumnName",
        "Descending": false
    }]
}
```

If the `Table` property refers to a SQLite resource, the index will be registered on the SQLite database table.
