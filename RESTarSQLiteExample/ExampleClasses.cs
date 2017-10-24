using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using RESTar;
using Starcounter;

#pragma warning disable 1591

namespace RESTarExample
{
    // Below is a common Starcounter-style database class describing an employee in 
    // an organization.

    [Database]
    public class Employee
    {
        public string Name;
        public EmployeeDetails Details;
        public string Title;
        public Employee Boss;

        public IEnumerable<Employee> Subordinates =>
            Db.SQL<Employee>($"SELECT t FROM {typeof(Employee)} t WHERE t.Boss =?", this);
    }

    [Database]
    public class EmployeeDetails
    {
        public int Salary;
        public DateTime DateOfEmployment;
    }

    // The 'Employee_01' table will, however, not serialize and deserialize 
    // to JSON very well due to its object references. RESTar uses JSON to generate 
    // representations of resources like this table, so we need to prep the table 
    // definition a bit in order for it to work well with RESTar. Below is a more 
    // suitable data definition for the employee that works well with serialization.

    /// <summary>
    /// By assigning the RESTar attribute and providing either the methods we want
    /// to enable, or a preset like ReadAndWrite (enables all methods), we register
    /// this class as a RESTar resource. This also makes it subject for serialization
    /// and deserialization to JSON objects, which we need to keep in mind when 
    /// designing our Starcounter data model and decide what should be exposed to RESTar.
    /// </summary>
    [Database, RESTar]
    public class BetterEmployee
    {
        public string Name;
        public string Title;

        public EmployeeDetails Details;

        /// <summary>
        /// 'Boss' is an object reference. When serializing an entity in the resource 
        /// BetterEmployee (a row in this table), RESTar will serialize the 'Boss' column
        /// as an inner JSON object, like so:
        /// {
        ///     "Name": "Tim Canterbury",
        ///     "Title": "Sales rep",
        ///     "Boss": {
        ///         "Name": "David Brent",
        ///         "Title": "Mentor, Entertainer, Renaissance man"
        ///         // David has no boss
        ///     } 
        /// }
        /// Often this is acceptable, but it can be problematic. It depends on what kind
        /// of object reference this is. Generally, Starcounter object references can be
        /// of two types:
        /// 
        /// 1. Foreign reference/child table
        /// 
        /// A foreign reference is used when we want to establish a 0..1 relation from a
        /// table to another, and that relation is 0..* in the other direction (a child
        /// table reference). The 'Boss' object reference above is an instance of this.
        ///  
        /// 2. Inner object
        /// 
        /// Sometimes, however, when designing Starcounter data models, the relation is
        /// 0..1 to another table, and there is no object reference in the other direction.
        /// See the 'EmployeeDetails' object above. This Starcounter object reference really 
        /// is just a nullable set of properties belonging to the parent object. If we delete 
        /// the parent object, there is no real need to keep the inner object around. More 
        /// importantly, an inner object can be considered a part of the parent object in a 
        /// way that the foreign reference is not. It would be awkward to not include the
        /// 'Details' object when serializing the employee entity. Had this object included
        /// its own object references, however, maybe we would have to revise this decision.
        ///  
        /// It would, however, be strange to include a JSON object describing the boss of
        /// an employee when serializing the employee. It would be better (perhaps not perfect) 
        /// to just ignore this member when serializing the object. If we want to ignore a 
        /// column (field or property) in a RESTar resource when serializing or deserializing, 
        /// we can decorate the corresponding member with the .NET standard 'IgnoreDataMember' 
        /// attribute. 
        /// 
        /// We can, however, include a new member holding the boss' ObjectNo though if we still 
        /// want to print useful information from the 'Boss' column. Below is a useful pattern
        /// when dealing with Starcounter object references of type 1 (Foreign reference/child 
        /// table) Substituting ObjectNo for object references in tables is also, by the way,
        /// how Starcounter Administrator handles this when printing tables, so let's implement 
        /// a similar pattern here where the 'Boss' column is represented as an ObjectNo, and not 
        /// by a Starcounter object reference. In the CLR domain, the column 'Boss' will still
        /// be of type 'BetterEmployee', but in the JSON domain it will be a nullable UInt64.
        /// </summary>
        [IgnoreDataMember] // first we ignore this member
        public BetterEmployee Boss // then we make it into a property like so:
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            get => DbHelper.FromID(BossObjectNo.GetValueOrDefault()) as BetterEmployee;
            set => BossObjectNo = value.GetObjectNo();
        }

        /// <summary>
        /// If we want to give the serializer another name than 'bossObjectNo' to use as 
        /// JSON member name, we can provide an alias using the .NET standard DataMember 
        /// attribute. This way we still have control over what the resulting JSON will 
        /// look like.
        /// </summary>
        [DataMember(Name = "Boss")] public ulong? BossObjectNo;

        // This means that an entity of this resource will be serialized as (for example):
        // {
        //     "Name": "Dawn Tinsley",
        //     "Title": "Receptionist",
        //     "Boss": 39132
        // }
        // ... and that JSON object would deserialize properly to this resource since
        // the serializer will match "Boss" with the 'bossObjectNo' column, and write the
        // ulong to that column, in turn making the 'Boss' column work. Since the FromID 
        // method is lightning fast, this works well. 

        /// <summary>
        /// Since the 'Boss' member is ignored, we can decide whether to include the 
        /// 'Subordinates' column or not. Had we included both, the serializer would detect
        /// the infinite recursion and throw an exception when matching entities in this 
        /// resource. In this case, let's include 'Subordinates'. In general, child-tables
        /// should be considered more essential to an entity than foreign references. Also,
        /// it could sometimes be useful to be able to not only serialize an existing 
        /// 'Subordinates' enumerable to JSON, but also deserialize a JSON formatted array 
        /// of objects describing BetterEmployees to the 'Subordinates' column. If we do not 
        /// implement a setter, the serializer will just skip this field when deserializing 
        /// from JSON, which could be what we want. To enable this functionality, however,
        /// all we need to do is add an empty setter. The serializer will call the the
        /// constructor for 'BetterEmployee' (within a database transaction) when 
        /// deserializing to this type, which will generate the database rows and establish
        /// the object references (through the 'bossObjectNo' column being populated by
        /// the JSON field 'Boss'). 
        /// </summary>
        public IEnumerable<BetterEmployee> Subordinates
        {
            get => Db.SQL<BetterEmployee>($"SELECT t FROM {typeof(BetterEmployee).FullName} t WHERE t.Boss =?",
                this);
            set { }
        }
    }

    // The above class is well-prepped to work as a RESTar resource. It is important to keep
    // the serialization in mind when registering database class definitions as RESTar resources.
    // Failing to mark possibly problematic internal object references with IgnoreDataMember 
    // can generate serious issues when serializing entities. Imagine if we include a list of 
    // other objects, which in turn include more objects etc. A simple GET request could then 
    // return thousands of JSON objects. There is no built-in protection against such requests 
    // (sometimes that is what you want to do), so a well designed data model is essential.
}