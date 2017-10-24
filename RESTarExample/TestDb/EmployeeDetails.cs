using System;
using RESTar;
using Starcounter;

#pragma warning disable 1591

namespace RESTarExample.TestDb
{
    [Database, RESTar]
    public class EmployeeDetails : TestBase
    {
        public int Salary;
        public DateTime DateOfEmployment;
    }
}