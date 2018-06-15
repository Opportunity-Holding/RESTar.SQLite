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

### Table declarations

SQLite is a hard-disk based relational database with an SQL interface, with each database contained in a file, commonly with the `.sqlite` extension on the local hard drive. RESTar.SQLite creates **table mappings** between classes in your C# project, and tables in an SQLite database. Each table mapping contains a set of **column mappings**, mapping columns in the SQLite table with C# class properties. There are two main kinds of table mappings in RESTar.SQLite:

#### Static table mappings

Static table mappings have all their column mappings defined in compile-time. Mappings are created left-to-right, so to speak, with all public instance auto-implemented properties of the C# class are mapped to corresponding columns in an SQLite table. Any SQLite table column not mapped by this left-to-right mapping are ignored.

##### Example

The following C# class:

```csharp
public class Product : SQLiteTable
{
    public string ProductId { get; set; }
    public int InStock { get; set; }
    public decimal NetPriceUsd { get; set; }
    public DateTime RegistrationDate { get; set; }
}
```

Would create the following SQLite table, with table mappings from each property to the column with the same name:

Name             | Type
---------------- | ----------
ProductId        | `TEXT`
InStock          | `INT`
NetPriceUsd      | `DECIMAL`
RegistrationDate | `DATETIME`

#### Elastic table mappings

Elastic table mappings have a subset of their column mappings defined in compile-time, with the ability to add and remove additional mappings during runtime. Here, mappings are first made left-to-right, with each public instance auto-implemented property of the C# class mapped to an SQLite table column. Then each unmapped SQLite column is mapped right-to-left to a dynamic property of the C# class. To create an elastic table mapping, have the C# class inherit from the `RESTar.SQLite.ElasticSQLiteTable` class.

```csharp
public class Product : ElasticSQLiteTable
{
    public string ProductId { get; set; }
    public int InStock { get; set; }
    public decimal NetPriceUsd { get; set; }
    public DateTime RegistrationDate { get; set; }
}
```

The public instance auto-implemented properties above will be immutable during runtime, and all dynamic properties and values will be accessible from the inherited indexer.

```csharp
var product = SQLite<SQLiteResource>.Select().FirstOrDefault();
var productId = product.ProductId; // Static property access
var description = product["Description"]; // Dynamic property access. Will return null if no such property.
```

For more information, see the [Advanced](#advanced) section below.

### Using SQLite table mappings with RESTar

To register a RESTar.SQLite table mapping as a RESTar resource, we need to **add two attributes to its C# class definition** – the `RESTar.Resources.RESTarAttribute` and the `RESTar.SQLite.SQLiteAttribute`. The first tells RESTar to treat this class as a RESTar resource, and is used in all RESTar resource registrations. The second is the resource provider attribute associated with the `RESTar.SQLite.SQLiteProvider` resource provider, and instructs RESTar to associate the table mapping with the resource operations defined by that resource provider.

The `RESTar.SQLite.SQLiteAttribute` attribute also allows us to set a custom table name for the table mapping, which is useful when mapping against an existing SQLite database.

### Data types

The following C# data types are allowed in RESTar.SQLite table mappings:

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

When mapping SQLite table columns to an elastic table mapping, the following SQL data types are allowed:

```
SMALLINT
INT
BIGINT
SINGLE
DOUBLE
DECIMAL
TINYINT
TEXT
BOOLEAN
DATETIME
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

### Table mapping validation rules

RESTar.SQLite table mappings must be defined as classes that:

1. Inherit from the abstract classes `SQLiteTable` or `ElasticSQLiteTable`.
2. Contain at least one public instance auto-implemented property with a public getter and setter accessor.
3. Have a public parameterless constructor.
4. Not contain any property with the name `RowId`, including any case variants.
5. Do not contain any two properties with the same case insensitive name.

## Database management

RESTar.SQLite will create a new `.sqlite` file in the directory provided in the `SQLiteProvider` constructor, with the given database name (unless one already exists). This file contains all tables used by RESTar.SQLite. If the name and/or directory is changed, RESTar.SQLite will simply create a new database file and any old data will be unreachable. There are some important things to keep in mind regarding how RESTar.SQLite works with the SQLite database:

1. During execution of `RESTarConfig.Init()`, RESTar.SQLite will create one table for each well-defined RESTar.SQLite table mapping, if one with the same name does not already exist.
2. If public instance auto-implemented properties are added to some well-defined RESTar.SQLite resource type between two executions of `RESTarConfig.Init()`, corresponding SQLite columns will be added to the table.
3. If property data types are changed in some well-defined RESTar.SQLite resource type between two executions of `RESTarConfig.Init()`, you will see a runtime error. Such table alterations cannot be handled automatically. Instead you should load the SQLite file manually and perform the necessary `ALTER TABLE` operations there.
4. If properties are removed from some resource type between two executions of `RESTarConfig.Init()`, their corresponding SQLite table columns will not be dropped.
5. RESTar.SQLite never automatically drops tables from the SQLite database.

For operations on the SQLite database that are not performed by RESTar.SQLite, the developer is encouraged to manually connect to the SQLite database. The connection string is available as a public static property of the `RESTar.SQLite.Settings` class, and the System.Data.SQLite package is [well documented](https://system.data.sqlite.org/index.html/doc/trunk/www/index.wiki).

### Helper methods

To access the SQLite database from inside your application, use the generic static class `SQLite<T>`. It has methods for selecting rows (including the proper O/R mapping to the RESTar resource type) and inserting, updating and deleting rows.

## Indexing

RESTar.SQLite is integrated with the RESTar `DatabaseIndex` resource, and can create and remove indexes in the SQLite database. To register an SQLite index, simply do as you would if the resource was a regular Starcounter resource – by posting the following to the `RESTar.Admin.DatabaseIndex` resource (example):

```json
{
    "Name": "MyIndexName",
    "ResourceName": "MySQLiteProduct",
    "Columns": [{
        "Name": "MyColumnName",
        "Descending": false
    }]
}
```

If the `ResourceName` property refers to a SQLite resource, the index will be registered on the SQLite database table. As with all RESTar database indexes, only `SQLiteTable` classes that are registered as RESTar resources can be indexed using `RESTar.Admin.DatabaseIndex`.

## weaver.ignore

Add the following rows to the project's `weaver.ignore` file when using RESTar.SQLite (the first 3 are required by RESTar).

```
Newtonsoft.Json
System.ValueTuple
EPPlus

System.Data.SQLite
SQLite.Interop
```

## Advanced

### The `TableMapping` resource

RESTar.SQLite registers a RESTar resource `RESTar.SQLite.Meta.TableMapping` that can be used to read the current table mappings, which is useful when debugging and testing mappings. There is also a RESTar [terminal resource](https://github.com/Mopedo/Home/tree/master/RESTar/Terminal%20resources) `RESTar.SQLite.Meta.TableMapping.Options` that allows you to update the mappings during runtime – which is needed if changes are made to the SQLite database while the RESTar application is running.

### Controlling elastic table mappings

The abstract `RESTar.SQLite.ElasticSQLiteTableController` class can be subclassed to create a controller for a given elastic table type. This controller can then select table mappings and add and remove column mappings during runtime. You can easily register an elastic table controller as a RESTar resource, by just adding the `RESTarAttribute` to its definition, which lets REST clients modify the structure of elastic tables.

### Procedural elastic table mappings

You can add additional elastic table mappings, and corresponding RESTar resources, during runtime, by subclassing the `RESTar.SQLite.SQLiteResourceController` class. It defines a resource controller with RESTar, that allows runtime insertions of resources. To make it work, you need to define a subclass of `ElasticSQLiteTable` that has at least one public instance auto-implemented property, and use that type as the second generic type argument in the `SQLiteResourceController` definition. Each runtime resource will create a new subclass of this elastic table type during runtime.

### Customizing column mappings

To change how RESTar.SQLite reads the defined column mappings from a C# class definition, use the `RESTar.SQLite.SQLiteMemberAttribute`. It allows you to change the name of the corresponding column, or ignore the property altogether.

#### Example

```csharp
public class Product : SQLiteTable
{
    public string ProductId { get; set; }

    [SQLiteMember(columnName: "In_Stock")]
    public int InStock { get; set; }

    [SQLiteMember(ignore: true)]
    public decimal NetPriceUsd { get; set; }

    public DateTime RegistrationDate { get; set; }
}
```

Name             | Type
---------------- | ----------
ProductId        | `TEXT`
In_Stock         | `INT`
RegistrationDate | `DATETIME`
