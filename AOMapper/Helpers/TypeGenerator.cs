using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace AOMapper.Helpers
{
    internal static class TypeGenerator
    {        
        private static readonly Dictionary<string, Type> RegistredTypes = new Dictionary<string, Type>(); 

        public static Type GetResultType(IEnumerable<FieldMetadata> metadata)
        {
            var fieldMetadatas = metadata as FieldMetadata[] ?? metadata.ToArray();
            var name = "id" + fieldMetadatas.Sum(o => o.GetHashCode());
            if (!RegistredTypes.ContainsKey(name))                
                CompileResultType(name, fieldMetadatas);

            return RegistredTypes[name];
        }        

        public static Type GetResultType(Type parent, IEnumerable<FieldMetadata> metadata)
        {
            var fieldMetadatas = metadata as FieldMetadata[] ?? metadata.ToArray();
            var name = parent.Name + "_id" + fieldMetadatas.Sum(o => o.GetHashCode());
            if (!RegistredTypes.ContainsKey(name))
                CompileResultType(name, parent, fieldMetadatas);

            return RegistredTypes[name];
        }

        private static Type CompileResultType(string name, IEnumerable<FieldMetadata> metadata)
        {
            var fieldMetadatas = metadata as FieldMetadata[] ?? metadata.ToArray();
            var typeName = name;
            TypeBuilder tb = GetTypeBuilder(typeName);            
            ConstructorBuilder constructor = tb.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            // NOTE: assuming your list contains Field objects with fields FieldName(string) and FieldType(Type)
            foreach (var field in fieldMetadatas)
                CreateProperty(tb, field.FieldName, field.FieldType, field);

            Type objectType = tb.CreateType();
            RegistredTypes.Add(typeName, objectType);
            return objectType;
        }        

        private static Type CompileResultType(string name, Type parent, IEnumerable<FieldMetadata> metadata)
        {
            var fieldMetadatas = metadata as FieldMetadata[] ?? metadata.ToArray();
            var typeName = name;//parent.Name + "_id" + fieldMetadatas.Sum(o => o.GetHashCode());
            TypeBuilder tb = GetTypeBuilder(typeName);
            tb.SetParent(parent);            
            ConstructorBuilder constructor = tb.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            // NOTE: assuming your list contains Field objects with fields FieldName(string) and FieldType(Type)
            foreach (var field in fieldMetadatas)
                CreateProperty(tb, field.FieldName, field.FieldType, field);

            Type objectType = tb.CreateType();
            RegistredTypes.Add(typeName, objectType);
            return objectType;
        }

        private static TypeBuilder GetTypeBuilder(string typeName, Type parent = null)
        {
            const string assemblySignature = "DynamicTypeAssembly";

            var an = new AssemblyName(assemblySignature);
            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
            TypeBuilder tb = moduleBuilder.DefineType(typeName,
                                TypeAttributes.Public |
                                TypeAttributes.Class |
                                TypeAttributes.AutoClass |
                                TypeAttributes.AnsiClass |
                                TypeAttributes.BeforeFieldInit |
                                TypeAttributes.AutoLayout
                                , parent);
            return tb;
        }

        private static void CreateProperty(TypeBuilder tb, string propertyName, Type propertyType, FieldMetadata field)
        {
            FieldBuilder fieldBuilder = tb.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);

            PropertyBuilder propertyBuilder = tb.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);
            MethodBuilder getPropMthdBldr = tb.DefineMethod("get_" + propertyName, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);
            ILGenerator getIl = getPropMthdBldr.GetILGenerator();

            if (field.MappedPropertyGetter != null)
            {
                getIl.Emit(OpCodes.Ldarg_0);
                getIl.Emit(OpCodes.Call, field.MappedPropertyGetter.Method);
            }
            else
            {
                getIl.Emit(OpCodes.Ldarg_0);
                getIl.Emit(OpCodes.Ldfld, fieldBuilder);
            }
            getIl.Emit(OpCodes.Ret);

            MethodBuilder setPropMthdBldr =
                tb.DefineMethod("set_" + propertyName,
                  MethodAttributes.Public |
                  MethodAttributes.SpecialName |
                  MethodAttributes.HideBySig,
                  null, new[] { propertyType });

            ILGenerator setIl = setPropMthdBldr.GetILGenerator();
            Label modifyProperty = setIl.DefineLabel();
            Label exitSet = setIl.DefineLabel();

            setIl.MarkLabel(modifyProperty);
            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldarg_1);
            if (field.MappedPropertySetter != null)
            {
                setIl.Emit(OpCodes.Ldarg_0);
                setIl.Emit(OpCodes.Ldarg_1);
                setIl.Emit(OpCodes.Call, field.MappedPropertySetter.Method);
            }
            else
            {
                setIl.Emit(OpCodes.Ldarg_0);
                setIl.Emit(OpCodes.Ldarg_1);
                setIl.Emit(OpCodes.Stfld, fieldBuilder);
            }
            setIl.Emit(OpCodes.Nop);
            setIl.MarkLabel(exitSet);
            setIl.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getPropMthdBldr);
            propertyBuilder.SetSetMethod(setPropMthdBldr);
        }        
    }
}