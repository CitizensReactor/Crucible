using DataCore2.ManagedTypeConstruction;
using DataCore2.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DataCore2
{
    public partial class DataCoreDatabase
    {
        public static IEnumerable<PropertyInfo> GetDataCoreTypePropertyInformation(Type type, bool inherit)
        {
            List<PropertyInfo> propertyInfos = new List<PropertyInfo>();

            Type currentType = type;
            while (true)
            {
                if (currentType == null) throw new Exception();
                if (currentType == typeof(DataCoreStructureBase)) break;
                if (currentType == typeof(Enum)) break;

                var properties = currentType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToList();

                // must sort on each inheritance layer and then reconstruct as dynamically created objects share same metadata token ranges
                properties.Sort((x, y) => x.MetadataToken - y.MetadataToken);

                propertyInfos.InsertRange(0, properties);

                currentType = currentType.BaseType;

                if (!inherit) break;
            }

            return propertyInfos;
        }

        internal static ConversionType GetDataCoreCollectionConversionType(Type type)
        {
            if (!type.IsGenericType)
            {
                throw new NotSupportedException();
            }
            if (!typeof(IDataCoreCollection).IsAssignableFrom(type))
            {
                throw new NotSupportedException();
            }

            var genericArgument1 = type.GenericTypeArguments[1];

            if (genericArgument1 == typeof(IConversionAttribute)) return ConversionType.Attribute;
            else if (genericArgument1 == typeof(IConversionClassArray)) return ConversionType.ClassArray;
            else if (genericArgument1 == typeof(IConversionComplexArray)) return ConversionType.ComplexArray;
            else if (genericArgument1 == typeof(IConversionSimpleArray)) return ConversionType.SimpleArray;
            else throw new NotSupportedException();

        }

        internal object GetRawRecordInstance(RawRecord record)
        {
            var structureType = ManagedStructureTypes[record.StructureIndex];
            var instance = ManagedDataTable[structureType][record.VariantIndex];

            return instance;
        }

        internal static ConversionType GetPropertyInfoConversionType(PropertyInfo propertyInfo)
        {
            var propertyType = propertyInfo.PropertyType;

            if (typeof(IDataCoreCollection).IsAssignableFrom(propertyType))
            {
                return GetDataCoreCollectionConversionType(propertyType);
            }

            return ConversionType.Attribute;
        }

        internal static DataType PropertyInfoToDataType(PropertyInfo propertyInfo)
        {
            var propertyType = propertyInfo.PropertyType;
            if (typeof(IDataCoreCollection).IsAssignableFrom(propertyType))
            {
                propertyType = propertyType.GenericTypeArguments[0];
            }

            if (propertyType.IsPrimitive)
            {
                if (propertyType == typeof(bool)) return DataType.Boolean;
                else if (propertyType == typeof(SByte)) return DataType.SByte;
                else if (propertyType == typeof(Int16)) return DataType.Int16;
                else if (propertyType == typeof(Int32)) return DataType.Int32;
                else if (propertyType == typeof(Int64)) return DataType.Int64;
                else if (propertyType == typeof(Byte)) return DataType.Byte;
                else if (propertyType == typeof(UInt16)) return DataType.UInt16;
                else if (propertyType == typeof(UInt32)) return DataType.UInt32;
                else if (propertyType == typeof(UInt64)) return DataType.UInt64;
                else if (propertyType == typeof(Single)) return DataType.Single;
                else if (propertyType == typeof(Double)) return DataType.Double;
                else throw new NotSupportedException();
            }
            else if (propertyType == typeof(Guid)) return DataType.Guid;
            else if (propertyType.IsEnum) return DataType.Enum;
#if DEBUG
            else if (propertyType == typeof(String))
            {
                throw new NotSupportedException();
            }
            else if (propertyType == typeof(Object))
            {
                throw new NotSupportedException();
            }
#endif
            else if (typeof(IDataCoreString).IsAssignableFrom(propertyType))
            {
                return DataType.String;
            }
            else if (typeof(IDataCoreLocale).IsAssignableFrom(propertyType))
            {
                return DataType.Locale;
            }
            else if (typeof(IDataCoreStrongPointer).IsAssignableFrom(propertyType))
            {
                return DataType.StrongPointer;
            }
            else if (typeof(IDataCoreWeakPointer).IsAssignableFrom(propertyType))
            {
                return DataType.WeakPointer;
            }
            else if (typeof(IDataCoreStructure).IsAssignableFrom(propertyType)) return DataType.Class;
            else if (typeof(DataCoreRecord).IsAssignableFrom(propertyType)) return DataType.Reference;

            else throw new NotSupportedException();
        }

        private string RawPropertyToTypeString(RawProperty propertyDefinition)
        {
            switch (propertyDefinition.DataType)
            {
                case DataType.Boolean: return typeof(bool).Name;
                case DataType.SByte: return typeof(SByte).Name;
                case DataType.Int16: return typeof(Int16).Name;
                case DataType.Int32: return typeof(Int32).Name;
                case DataType.Int64: return typeof(Int64).Name;
                case DataType.Byte: return typeof(Byte).Name;
                case DataType.UInt16: return typeof(UInt16).Name;
                case DataType.UInt32: return typeof(UInt32).Name;
                case DataType.UInt64: return typeof(UInt64).Name;
                case DataType.Single: return typeof(Single).Name;
                case DataType.Double: return typeof(Double).Name;
                case DataType.Guid: return typeof(Guid).Name;
                case DataType.String: return typeof(DataCoreString).Name;
                case DataType.Locale: return typeof(DataCoreLocale).Name;
                case DataType.Enum:
                    {
                        var enumDefinition = RawDatabase.enumDefinitions[propertyDefinition.DefinitionIndex];
                        var enumName = RawDatabase.textBlock.GetString(enumDefinition.NameOffset);
                        return enumName;
                    }
                case DataType.Class:
                    {
                        var structureDefinition = RawDatabase.structureDefinitions[propertyDefinition.DefinitionIndex];
                        var structureName = RawDatabase.textBlock.GetString(structureDefinition.NameOffset);

                        return structureName;
                    }
                case DataType.StrongPointer:
                    {
                        var structureDefinition = RawDatabase.structureDefinitions[propertyDefinition.DefinitionIndex];
                        var structureName = RawDatabase.textBlock.GetString(structureDefinition.NameOffset);

                        return $"DataCoreStrongPointer<{structureName}>";
                    }
                case DataType.WeakPointer:
                    {
                        var structureDefinition = RawDatabase.structureDefinitions[propertyDefinition.DefinitionIndex];
                        var structureName = RawDatabase.textBlock.GetString(structureDefinition.NameOffset);

                        return $"DataCoreWeakPointer<{structureName}>";
                    }
                case DataType.Reference:
                    {
                        var structureDefinition = RawDatabase.structureDefinitions[propertyDefinition.DefinitionIndex];
                        var structureName = RawDatabase.textBlock.GetString(structureDefinition.NameOffset);

                        return $"DataCoreRecord<{structureName}>";
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        internal string RawPropertyToPropertyString(RawProperty propertyDefinition)
        {
            string propertyName = RawDatabase.textBlock.GetString(propertyDefinition.NameOffset);
            string fieldName = $"_{propertyName}";

            bool isArray = propertyDefinition.ConversionType != ConversionType.Attribute;

            // conflicting types
            if (propertyName == "base") propertyName = "@base";
            if (propertyName == "params") propertyName = "@params";
            if (propertyName == "fixed") propertyName = "@fixed";
            if (propertyName == "default") propertyName = "@default";
            if (propertyName == "string") propertyName = "@string";
            if (propertyName == "switch") propertyName = "@switch";

            string typeName = RawPropertyToTypeString(propertyDefinition);

            StringBuilder stringBuilder = new StringBuilder();

            switch (propertyDefinition.DataType)
            {
                case DataType.Boolean:
                case DataType.SByte:
                case DataType.Int16:
                case DataType.Int32:
                case DataType.Int64:
                case DataType.Byte:
                case DataType.UInt16:
                case DataType.UInt32:
                case DataType.UInt64:
                case DataType.String:
                case DataType.Single:
                case DataType.Double:
                case DataType.Locale:
                case DataType.Guid:
                case DataType.Reference:
                case DataType.Enum:
                case DataType.Class:
                case DataType.StrongPointer:
                case DataType.WeakPointer:
                    break;
                default:
                    throw new NotImplementedException();
            }

            switch (propertyDefinition.ConversionType)
            {
                case ConversionType.Attribute:
                    break;
                case ConversionType.ComplexArray:
                    typeName = $"DataCoreCollection<{typeName}, IConversionComplexArray>";
                    break;
                case ConversionType.SimpleArray:
                    typeName = $"DataCoreCollection<{typeName}, IConversionSimpleArray>";
                    break;
                case ConversionType.ClassArray:
                    typeName = $"DataCoreCollection<{typeName}, IConversionClassArray>";
                    break;
            }

#if !DEBUG
            stringBuilder.AppendLine("[DebuggerBrowsable(DebuggerBrowsableState.Never)]");
#endif
            stringBuilder.AppendLine($"{typeName} {fieldName};");
            stringBuilder.AppendLine($"public {typeName} {propertyName} {{ get {{ return {fieldName}; }} set {{ SetProperty(ref {fieldName}, value); }} }}");

            var result = stringBuilder.ToString();
            return result;
        }




    }
}
