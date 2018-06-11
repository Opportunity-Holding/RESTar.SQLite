using System;
using System.Reflection;
using System.Reflection.Emit;
using static System.Reflection.TypeAttributes;

namespace RESTar.SQLite
{
    internal static class TypeBuilder
    {
        private static AssemblyName AssemblyName { get; }
        private static AssemblyBuilder AssemblyBuilder { get; }
        private static ModuleBuilder ModuleBuilder { get; }
        private const string _assemblyName = "RESTar.SQLite.Dynamic";
        internal static Assembly Assembly => AssemblyBuilder;

        static TypeBuilder()
        {
            AssemblyName = new AssemblyName("DynamicAssemblyExample");
            AssemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(AssemblyName, AssemblyBuilderAccess.RunAndSave);
            ModuleBuilder = AssemblyBuilder.DefineDynamicModule(AssemblyName.Name, AssemblyName.Name + ".dll");
        }

        internal static Type GetType(ProceduralResource resource)
        {
            var existing = AssemblyBuilder.GetType(resource.Name);
            if (existing != null) return existing;
            var baseType = Type.GetType(resource.BaseTypeName);
            if (baseType == null) return null;
            return MakeType(resource.Name, baseType);
        }

        private static Type MakeType(string name, Type baseType) => ModuleBuilder
            .DefineType(name, Class | Public | Sealed, baseType)
            .CreateType();
    }
}