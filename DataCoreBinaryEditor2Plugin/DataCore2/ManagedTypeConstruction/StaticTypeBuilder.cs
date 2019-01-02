using DataCore2.Structures;
using System;
using System.Collections.Generic;

using System.Reflection;
using System.Reflection.Emit;

namespace DataCore2.ManagedTypeConstruction
{
    //internal class StaticTypeBuilder
    //{
    //    public AssemblyName AssemblyName { get; private set; }
    //    public AssemblyBuilder AssemblyBuilder { get; private set; }
    //    public ModuleBuilder moduleBuilder { get; private set; }
    //    public ConstructorInfo UnderlyingTypeConstructorInfo { get; private set; }

    //    public StaticTypeBuilder(string assemblyName)
    //    {
    //        AssemblyName = new AssemblyName(assemblyName);
    //        AssemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(AssemblyName, AssemblyBuilderAccess.RunAndSave);
    //        moduleBuilder = AssemblyBuilder.DefineDynamicModule("MainModule", assemblyName);
    //        UnderlyingTypeConstructorInfo = typeof(UnderlyingTypeAttribute).GetConstructor(new Type[] { typeof(Type) });
    //    }

    //    //internal void CreateTypeBuilder2(
    //    //    string typeSignature,
    //    //    Type parentType,
    //    //    out TypeBuilder typeBuilder,
    //    //    out Type declaringType
    //    //    )
    //    //{
    //    //    typeBuilder = GetTypeBuilder(typeSignature, parentType);
    //    //    ConstructorBuilder constructorBuilder = typeBuilder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
    //    //    declaringType = constructorBuilder.DeclaringType;
    //    //}

    //    //internal Type BuildType(
    //    //    TypeBuilder typeBuilder,
    //    //    Type declaringType,
    //    //    IEnumerable<PropertyCreationInfo> customProperties
    //    //    )
    //    //{
    //    //    // NOTE: assuming your list contains Field objects with fields FieldName(string) and FieldType(Type)
    //    //    foreach (var property in customProperties)
    //    //    {
    //    //        var propertyType = property.MemberType;
    //    //        if (property.IsDeclaringType)
    //    //        {
    //    //            propertyType = declaringType;
    //    //        }
    //    //        Type underlyingType = null;
    //    //        if (property.HideUnderlyingType)
    //    //        {
    //    //            underlyingType = propertyType;
    //    //            propertyType = typeof(object);
    //    //        }
    //    //        if (property.IsArray)
    //    //        {
    //    //            propertyType = typeof(ObservableCollection<>).MakeGenericType(new Type[] { propertyType });
    //    //        }

    //    //        CreateProperty(typeBuilder, property.Name, propertyType, underlyingType, property.PropertyDefinition);
    //    //    }

    //    //    Type objectType = typeBuilder.CreateType();
    //    //    return objectType;
    //    //}

    //    internal Type CreateEnumType(string typeSignature, EnumValueCreationInfo[] enumValues)
    //    {
    //        EnumBuilder eb = moduleBuilder.DefineEnum(typeSignature, TypeAttributes.Public, typeof(int));

    //        foreach (var enumValue in enumValues)
    //        {
    //            eb.DefineLiteral(enumValue.Name, enumValue.Value);
    //        }

    //        var newType = eb.CreateType();
    //        return newType;
    //    }

    //    public Type CreateClassType(string typeSignature, IEnumerable<PropertyCreationInfo> customProperties, Type parentType)
    //    {
    //        var isUsingUnderlyingTypes = false;
    //        foreach(var property in customProperties)
    //        {
    //            isUsingUnderlyingTypes |= property.HideUnderlyingType;
    //        }

    //        List<Type> interfaceTypes = new List<Type>();
    //        if(isUsingUnderlyingTypes)
    //        {
    //            interfaceTypes.Add(typeof(IHasUnderlyingTypeAttributes));
    //        }

    //        TypeBuilder typeBuilder = moduleBuilder.DefineType(
    //            typeSignature,
    //            TypeAttributes.Public |
    //            TypeAttributes.Class |
    //            TypeAttributes.AutoClass |
    //            TypeAttributes.AnsiClass |
    //            TypeAttributes.BeforeFieldInit |
    //            TypeAttributes.AutoLayout,
    //            parentType ?? typeof(DataCoreStructureBase),
    //            interfaceTypes.ToArray());

            
    //        ConstructorBuilder constructorBuilder = typeBuilder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
    //        var declaringType = constructorBuilder.DeclaringType;

    //        // NOTE: assuming your list contains Field objects with fields FieldName(string) and FieldType(Type)
    //        foreach (var property in customProperties)
    //        {
    //            var propertyType = property.MemberType;
    //            if (property.IsDeclaringType)
    //            {
    //                propertyType = declaringType;
    //            }
    //            Type underlyingType = null;
    //            if (property.HideUnderlyingType)
    //            {
    //                underlyingType = propertyType;
    //                propertyType = property.OvertType ?? typeof(object);
    //            }
    //            switch (property.ConversionType)
    //            {
    //                case ConversionType.Attribute:
    //                    break;
    //                case ConversionType.ComplexArray:
    //                    propertyType = typeof(DataCoreCollection<,>).MakeGenericType(propertyType, typeof(IConversionComplexArray));
    //                    break;
    //                case ConversionType.SimpleArray:
    //                    propertyType = typeof(DataCoreCollection<,>).MakeGenericType(propertyType, typeof(IConversionSimpleArray));
    //                    break;
    //                case ConversionType.ClassArray:
    //                    propertyType = typeof(DataCoreCollection<,>).MakeGenericType(propertyType, typeof(IConversionClassArray));
    //                    break;
    //                default:
    //                    throw new NotImplementedException();
    //            }

    //            CreateProperty(typeBuilder, property.Name, propertyType, underlyingType, property.PropertyDefinition);
    //        }



    //        Type objectType = typeBuilder.CreateType();
    //        bool test = declaringType == objectType;
    //        return objectType;
    //    }

    //    private unsafe void CreateProperty(TypeBuilder tb, string propertyName, Type propertyType, Type underlyingType, RawProperty propertyDefinition)
    //    {
    //        FieldBuilder fieldBuilder = tb.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);

    //        MethodBuilder getPropMthdBldr;
    //        MethodBuilder setPropMthdBldr;
    //        {
    //            getPropMthdBldr = tb.DefineMethod(
    //                "get_" + propertyName,
    //                MethodAttributes.Public |
    //                MethodAttributes.SpecialName |
    //                MethodAttributes.HideBySig,
    //                propertyType,
    //                Type.EmptyTypes);

    //            ILGenerator getIl = getPropMthdBldr.GetILGenerator();
    //            getIl.Emit(OpCodes.Ldarg_0);
    //            getIl.Emit(OpCodes.Ldfld, fieldBuilder);
    //            getIl.Emit(OpCodes.Ret);
    //        }

    //        var bindableBaseSetPropertyMethodBase = typeof(DataCoreStructureBase).GetMethod("_SetPropertyDynamic", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    //        var bindableBaseSetPropertyMethod = bindableBaseSetPropertyMethodBase.MakeGenericMethod(new Type[] { propertyType });
    //        {
    //            setPropMthdBldr = tb.DefineMethod(
    //                "set_" + propertyName,
    //                MethodAttributes.Public |
    //                MethodAttributes.SpecialName |
    //                MethodAttributes.HideBySig,
    //                null, new[] { propertyType });

    //            ILGenerator setIl = setPropMthdBldr.GetILGenerator();
    //            setIl.Emit(OpCodes.Ldarg_0);
    //            setIl.Emit(OpCodes.Ldarg_1);
    //            setIl.Emit(OpCodes.Ldstr, propertyName);
    //            setIl.Emit(OpCodes.Callvirt, bindableBaseSetPropertyMethod);
    //            setIl.Emit(OpCodes.Ret);
    //        }

    //        PropertyBuilder propertyBuilder = tb.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);
    //        propertyBuilder.SetGetMethod(getPropMthdBldr);
    //        propertyBuilder.SetSetMethod(setPropMthdBldr);

    //        if (underlyingType != null)
    //        {
    //            CustomAttributeBuilder customAttributeBuilder = new CustomAttributeBuilder(UnderlyingTypeConstructorInfo, new object[] { underlyingType });
    //            propertyBuilder.SetCustomAttribute(customAttributeBuilder);
    //        }
    //    }
    //}
}
