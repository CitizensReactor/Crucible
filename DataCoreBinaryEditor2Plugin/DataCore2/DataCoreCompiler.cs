//#define USECOMPARISON
using DataCore2.ManagedTypeConstruction;
using DataCore2.Structures;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DataCore2
{
    class DataCoreCompiler
    {
        DataCoreDatabase Database;
        private RawStructure[] structureDefinitions;
        private List<RawProperty> propertyDefinitions;
        private RawEnum[] enumDefinitions;
        private List<RawDataMapping> datamappingDefinitions;
        private RawRecord[] records;
        private List<sbyte> int8Values;
        private List<short> int16Values;
        private List<int> int32Values;
        private List<long> int64Values;
        public List<byte> UInt8Values;
        public List<ushort> UInt16Values;
        public List<uint> UInt32Values;
        public List<ulong> UInt64Values;
        private List<bool> booleanValues;
        private List<float> singleValues;
        private List<double> doubleValues;
        private List<Guid> guidValues;
        private List<RawStringReference> stringValues;
        private List<RawLocaleReference> localeValues;
        private List<RawEnumNameReference> enumValues;
        private List<RawStrongPointer> strongValues;
        private List<RawWeakPointer> weakValues;
        private List<RawReference> referenceValues;
        private List<RawEnumNameReference> enumValueNameTable;
        private TextBlock textBlock;
        private Dictionary<object, RawEnumNameReference> EnumValueTable;

        public DataCoreCompiler(DataCoreDatabase database)
        {
            Database = database;

            structureDefinitions = new RawStructure[Database.ManagedStructureTypes.Count];
            propertyDefinitions = new List<RawProperty>();
            enumDefinitions = new RawEnum[Database.ManagedEnumTypes.Count];
            datamappingDefinitions = new List<RawDataMapping>();
            records = new RawRecord[Database.ManagedRecords.Length];
            int8Values = new List<sbyte>();
            int16Values = new List<short>();
            int32Values = new List<int>();
            int64Values = new List<long>();
            UInt8Values = new List<byte>();
            UInt16Values = new List<ushort>();
            UInt32Values = new List<uint>();
            UInt64Values = new List<ulong>();
            booleanValues = new List<bool>();
            singleValues = new List<float>();
            doubleValues = new List<double>();
            guidValues = new List<Guid>();
            stringValues = new List<RawStringReference>();
            localeValues = new List<RawLocaleReference>();
            enumValues = new List<RawEnumNameReference>();
            strongValues = new List<RawStrongPointer>();
            weakValues = new List<RawWeakPointer>();
            referenceValues = new List<RawReference>();
            enumValueNameTable = new List<RawEnumNameReference>();
            textBlock = new TextBlock();


            EnumValueTable = new Dictionary<object, RawEnumNameReference>();
        }

        private void CompileStructure(CustomBinaryWriter structureDataBinaryWriter, object structureInstance)
        {
            var managedStructureType = structureInstance.GetType();
            var structureProperties = Database.ManagedStructureInheritedProperties[managedStructureType];

            foreach (var propertyInfo in structureProperties)
            {
                var value = propertyInfo.GetValue(structureInstance);
                var propertyType = propertyInfo.PropertyType;

                if (typeof(IDataCoreCollection).IsAssignableFrom(propertyType))
                {
                    var collection = value as ICollection;
                    var datacoreCollection = value as IDataCoreCollection;
                    var collectionType = propertyType;
                    propertyType = collectionType.GenericTypeArguments[0];

                    RawArrayPointer rawArrayPointer = new RawArrayPointer()
                    {
                        ArrayCount = collection.Count,
                        FirstIndex = -1
                    };

                    if (propertyType.IsPrimitive)
                    {
                        ICollection destinationCollection = null;

                        if (propertyType == typeof(bool)) destinationCollection = booleanValues;
                        else if (propertyType == typeof(SByte)) destinationCollection = int8Values;
                        else if (propertyType == typeof(Int16)) destinationCollection = int16Values;
                        else if (propertyType == typeof(Int32)) destinationCollection = int32Values;
                        else if (propertyType == typeof(Int64)) destinationCollection = int64Values;
                        else if (propertyType == typeof(Byte)) destinationCollection = UInt8Values;
                        else if (propertyType == typeof(UInt16)) destinationCollection = UInt16Values;
                        else if (propertyType == typeof(UInt32)) destinationCollection = UInt32Values;
                        else if (propertyType == typeof(UInt64)) destinationCollection = UInt64Values;
                        else if (propertyType == typeof(Single)) destinationCollection = singleValues;
                        else if (propertyType == typeof(Double)) destinationCollection = doubleValues;
                        else throw new NotSupportedException();

                        rawArrayPointer.FirstIndex = destinationCollection.Count;
                        (destinationCollection as dynamic).AddRange((dynamic)collection);
                    }
                    else if (propertyType == typeof(Guid)) guidValues.AddRange(collection as IEnumerable<Guid>);
                    else if (propertyType.IsEnum)
                    {
                        RawEnumNameReference[] rawEnumNameReferences = new RawEnumNameReference[collection.Count];

                        for (int i = 0; i < collection.Count; i++)
                        {
                            var enumValue = (object)(collection as dynamic)[i];

                            if (!EnumValueTable.ContainsKey(enumValue)) throw new Exception();

                            RawEnumNameReference rawEnumNameReference = EnumValueTable[enumValue];

                            rawEnumNameReferences[i] = rawEnumNameReference;
                        }

                        rawArrayPointer.ArrayCount = rawEnumNameReferences.Length;
                        rawArrayPointer.FirstIndex = enumValues.Count;
                        enumValues.AddRange(rawEnumNameReferences);
                    }
                    else if (propertyType == typeof(Object))
                    {
                        throw new NotSupportedException();
                    }
                    else if (propertyType == typeof(String))
                    {
                        throw new NotSupportedException();
                    }
                    else if (typeof(IDataCoreString).IsAssignableFrom(propertyType))
                    {
                        RawStringReference[] rawStringReferences = new RawStringReference[collection.Count];

                        for (int i = 0; i < collection.Count; i++)
                        {
                            var dataCoreString = (collection as IEnumerable<DataCoreString>).ElementAt(i);
                            var stringOffset = textBlock.AddString(dataCoreString);

                            RawStringReference rawStringReference = new RawStringReference();
                            rawStringReference.NameOffset = stringOffset;
                            rawStringReferences[i] = rawStringReference;
                        }

                        rawArrayPointer.ArrayCount = rawStringReferences.Length;
                        rawArrayPointer.FirstIndex = stringValues.Count;
                        stringValues.AddRange(rawStringReferences);

#if DEBUG
                        if (!structureDataBinaryWriter.IsValid(rawArrayPointer))
                        {
                            var original = structureDataBinaryWriter.GetOriginalValue<RawArrayPointer>();

                            Console.WriteLine();
                        }
#endif
                    }
                    else if (typeof(IDataCoreLocale).IsAssignableFrom(propertyType))
                    {
                        RawLocaleReference[] rawLocaleReferences = new RawLocaleReference[collection.Count];

                        for (int i = 0; i < collection.Count; i++)
                        {
                            var dataCoreLocale = (collection as IEnumerable<DataCoreLocale>).ElementAt(i);
                            var stringOffset = textBlock.AddString(dataCoreLocale);

                            RawLocaleReference rawLocaleReference = new RawLocaleReference();
                            rawLocaleReference.NameOffset = stringOffset;
                            rawLocaleReferences[i] = rawLocaleReference;
                        }

                        rawArrayPointer.ArrayCount = rawLocaleReferences.Length;
                        rawArrayPointer.FirstIndex = localeValues.Count;
                        if (rawArrayPointer.ArrayCount == 0)
                        {
                            rawArrayPointer.FirstIndex = 0;
                        }
                        localeValues.AddRange(rawLocaleReferences);

#if DEBUG
                        if (!structureDataBinaryWriter.IsValid(rawArrayPointer))
                        {
                            var original = structureDataBinaryWriter.GetOriginalValue<RawArrayPointer>();

                            Console.WriteLine();
                        }
#endif
                    }
                    else if (typeof(IDataCoreStrongPointer).IsAssignableFrom(propertyType))
                    {
                        RawStrongPointer[] rawStrongPointers = new RawStrongPointer[collection.Count];

                        int index = 0;
                        foreach (var pointer in collection)
                        {
                            var rawStrongPointer = DataCore2Util.ConvertStrongPointer(this, Database, pointer as IDataCorePointer);
                            rawStrongPointers[index++] = rawStrongPointer;
                        }

                        rawArrayPointer.ArrayCount = rawStrongPointers.Length;
                        rawArrayPointer.FirstIndex = strongValues.Count;
                        strongValues.AddRange(rawStrongPointers);
                    }
                    else if (typeof(IDataCoreWeakPointer).IsAssignableFrom(propertyType))
                    {
                        RawWeakPointer[] rawWeakPointers = new RawWeakPointer[collection.Count];

                        int index = 0;
                        foreach (var pointer in collection)
                        {
                            var rawWeakPointer = DataCore2Util.ConvertWeakPointer(this, Database, pointer as IDataCorePointer);
                            rawWeakPointers[index++] = rawWeakPointer;
                        }

                        rawArrayPointer.ArrayCount = rawWeakPointers.Length;
                        rawArrayPointer.FirstIndex = weakValues.Count;


#if DEBUG // validate

                        //var originalWeakPointer2 = Database.RawDatabase.weakValues[weakValues.Count];
                        //if (collection.Count == 1 && rawWeakPointers[0].StructureIndex == -1 && rawWeakPointers[0].VariantIndex == -1)
                        //{
                        //    if (rawWeakPointers[0].StructureIndex != originalWeakPointer2.StructureIndex)
                        //    {
                        //        throw new Exception("RawWeakPointer StructureIndex sanity check failed");
                        //    }
                        //    if (rawWeakPointers[0].VariantIndex != originalWeakPointer2.VariantIndex)
                        //    {
                        //        throw new Exception("RawWeakPointer VariantIndex sanity check failed");
                        //    }
                        //}

                        ////for (int i = 0; i < rawWeakPointers.Length; i++)
                        ////{
                        ////    bool requiresFixup = true;
                        ////    requiresFixup &= rawWeakPointers[i].StructureIndex == -1;
                        ////    requiresFixup &= rawWeakPointers[i].VariantIndex == -1;
                        ////    requiresFixup &= collection.Count > 0;

                        ////    if(requiresFixup)
                        ////    {
                        ////        var structureType = propertyType.GenericTypeArguments[0];
                        ////        if(Database.ManagedDataTable.ContainsKey(structureType))
                        ////        {
                        ////            var structureIndex = Database.ManagedStructureTypes.IndexOf(structureType);
                        ////            rawWeakPointers[i].StructureIndex = structureIndex;
                        ////        }
                        ////    }
                        ////}

                        //for(int i=0;i<rawWeakPointers.Length;i++)
                        //{
                        //    var newWeakPointer = rawWeakPointers[i];
                        //    var originalWeakPointer = Database.RawDatabase.weakValues[weakValues.Count + i];

                        //    if (newWeakPointer.StructureIndex != originalWeakPointer.StructureIndex)
                        //    {
                        //        throw new Exception("RawWeakPointer StructureIndex sanity check failed");
                        //    }
                        //    if (newWeakPointer.VariantIndex != originalWeakPointer.VariantIndex)
                        //    {
                        //        throw new Exception("RawWeakPointer VariantIndex sanity check failed");
                        //    }
                        //}
#endif

                        weakValues.AddRange(rawWeakPointers);
                    }
                    else if (typeof(IDataCoreStructure).IsAssignableFrom(propertyType))
                    {
                        var structureType = propertyType;

                        //TODO: Instead of looking these up, the ordering of all of the structures needs to be generated.

                        rawArrayPointer.FirstIndex = -1;
                        if (collection.Count > 0)
                        {
                            //var index = this.managedStructuresArrayLookup[datacoreCollection];

                            var structureDataTable = ManagedDataTable[structureType];
                            var index = (int)structureDataTable.IndexOf((collection as dynamic)[0]);

                            var structureDataTable2 = Database.ManagedDataTable[structureType];
                            var index2 = (int)structureDataTable2.IndexOf((collection as dynamic)[0]);

                            if((index == -1 || index2 == -1) && index != index2)
                            {
                                throw new Exception("This shouldn't happen");
                            }

                            rawArrayPointer.FirstIndex = index;
                        }

#if DEBUG
                        if (!structureDataBinaryWriter.IsValid(rawArrayPointer))
                        {
                            var original = structureDataBinaryWriter.GetOriginalValue<RawArrayPointer>();

                            Console.WriteLine();
                        }
#endif
                    }
                    else if (typeof(DataCoreRecord).IsAssignableFrom(propertyType))
                    {
                        RawReference[] rawReferences = new RawReference[collection.Count];

                        for (int i = 0; i < collection.Count; i++)
                        {
                            var record = (collection as dynamic)[i] as DataCoreRecord;
                            RawReference rawReference = new RawReference();

                            rawReference.Value = record.ID;

                            var structureType = record.StructureType;
                            if (structureType != null)
                            {
                                var structureInstnaces = ManagedDataTable[structureType];
                                var variantIndex = structureInstnaces.IndexOf(record.Instance);
                                rawReference.VariantIndex = variantIndex;
                            }
                            else
                            {
                                rawReference.VariantIndex = -1;
                            }

                            rawReferences[i] = rawReference;
                        }

                        rawArrayPointer.ArrayCount = rawReferences.Length;
                        rawArrayPointer.FirstIndex = referenceValues.Count;

//#if DEBUG

//                        for (int i = 0; i < rawReferences.Length; i++)
//                        {
//                            var newReference = rawReferences[i];
//                            var oldReference = Database.RawDatabase.referenceValues[referenceValues.Count + i];

//                            if (newReference.Value != oldReference.Value)
//                            {
//                                throw new Exception("Reference Invalid Value");
//                            }
//                            if (newReference.VariantIndex != oldReference.VariantIndex)
//                            {
//                                throw new Exception("Reference Invalid VariantIndex");
//                            }
//                        }

//#endif


                        referenceValues.AddRange(rawReferences);
                    }
                    else throw new NotSupportedException();

#if DEBUG
                    if (!structureDataBinaryWriter.IsValid(rawArrayPointer))
                    {
                        var original = structureDataBinaryWriter.GetOriginalValue<RawArrayPointer>();

                        Console.WriteLine();
                    }
#endif

                    structureDataBinaryWriter.Write(rawArrayPointer);
                }
                else
                {
                    if (propertyType.IsPrimitive)
                    {
                        if (propertyType == typeof(bool)) structureDataBinaryWriter.Write((bool)value);
                        else if (propertyType == typeof(SByte)) structureDataBinaryWriter.Write((SByte)value);
                        else if (propertyType == typeof(Int16)) structureDataBinaryWriter.Write((Int16)value);
                        else if (propertyType == typeof(Int32)) structureDataBinaryWriter.Write((Int32)value);
                        else if (propertyType == typeof(Int64)) structureDataBinaryWriter.Write((Int64)value);
                        else if (propertyType == typeof(Byte)) structureDataBinaryWriter.Write((Byte)value);
                        else if (propertyType == typeof(UInt16)) structureDataBinaryWriter.Write((UInt16)value);
                        else if (propertyType == typeof(UInt32)) structureDataBinaryWriter.Write((UInt32)value);
                        else if (propertyType == typeof(UInt64)) structureDataBinaryWriter.Write((UInt64)value);
                        else if (propertyType == typeof(Single)) structureDataBinaryWriter.Write((Single)value);
                        else if (propertyType == typeof(Double)) structureDataBinaryWriter.Write((Double)value);
                        else throw new NotSupportedException();
                    }
                    else if (propertyType.IsEnum)
                    {
                        int integerValue = (int)value;

                        if (integerValue == -1)
                        {
                            integerValue = textBlock.AddString("");
                        }

                        if (EnumValueTable.ContainsKey(value))
                        {
                            var enumNameOffsetAndValue = EnumValueTable[value];
                            integerValue = enumNameOffsetAndValue.NameOffset;
                        }



                        //else
                        //{
                        //    integerValue = 0;
                        //}

#if DEBUG
                        if (!structureDataBinaryWriter.IsValid(integerValue))
                        {
                            var original = structureDataBinaryWriter.GetOriginalValue<int>();

                            Console.WriteLine();
                        }
#endif
                        structureDataBinaryWriter.Write(integerValue);
                    }


                    else if (propertyType == typeof(Object))
                    {
                        throw new NotSupportedException();
                    }
                    else if (propertyType == typeof(String))
                    {
                        throw new NotSupportedException();
                    }
                    else if (typeof(IDataCoreString).IsAssignableFrom(propertyType))
                    {
                        var datacoreStringValue = value as DataCoreString;
                        var stringValue = datacoreStringValue.String;
                        var stringOffset = textBlock.AddString(stringValue);

                        RawStringReference rawStringReference = new RawStringReference();
                        rawStringReference.NameOffset = stringOffset;
                        structureDataBinaryWriter.Write(rawStringReference);
                    }
                    else if (typeof(IDataCoreLocale).IsAssignableFrom(propertyType))
                    {
                        var datacoreLocaleValue = value as DataCoreLocale;
                        var stringValue = datacoreLocaleValue.String;
                        var stringOffset = textBlock.AddString(stringValue);

                        RawLocaleReference rawLocaleReference = new RawLocaleReference();
                        rawLocaleReference.NameOffset = stringOffset;
                        structureDataBinaryWriter.Write(rawLocaleReference);
                    }
                    else if (typeof(IDataCoreStrongPointer).IsAssignableFrom(propertyType))
                    {
                        var rawStrongPointer = DataCore2Util.ConvertStrongPointer(this, Database, value as IDataCorePointer);

                        structureDataBinaryWriter.Write(rawStrongPointer);
                    }
                    else if (typeof(IDataCoreWeakPointer).IsAssignableFrom(propertyType))
                    {
                        var rawWeakPointer = DataCore2Util.ConvertWeakPointer(this, Database, value as IDataCorePointer);

#if DEBUG
                        //NOTE: Can't currently reconstruct the StructIndex
                        if (!structureDataBinaryWriter.IsValid(rawWeakPointer) && rawWeakPointer.StructureIndex != -1)
                        {
                            var original = structureDataBinaryWriter.GetOriginalValue<RawWeakPointer>();

                            Console.WriteLine();
                        }
#endif

#if DEBUG
                        structureDataBinaryWriter.DebugNoCheckWrite(rawWeakPointer);
                        continue;
#else
                            structureDataBinaryWriter.Write(rawWeakPointer);
#endif
                    }
                    else if (typeof(IDataCoreStructure).IsAssignableFrom(propertyType))
                    {
                        //var instanceType = value.GetType();
                        //var variantIndex = Database.ManagedDataTable[instanceType].IndexOf(instanceType);
                        //var rawStrongPointer = new RawStrongPointer();

                        // compile in place
                        CompileStructure(structureDataBinaryWriter, value);
                    }
                    else if (typeof(DataCoreRecord).IsAssignableFrom(propertyType))
                    {
                        var record = value as DataCoreRecord;

                        var recordGUID = record?.ID ?? Guid.Empty;
                        int variantIndex = -1;
                        if (record?.Instance != null)
                        {
                            //NOTE: This should ALWAYS be valid. If this causes an error, we should handle it. Please see ReferenceFixup
                            variantIndex = ManagedDataTable[record.StructureType].IndexOf(record.Instance);
                        }

                        var rawReference = new RawReference();
                        rawReference.Value = recordGUID;
                        rawReference.VariantIndex = variantIndex;

                        structureDataBinaryWriter.Write(rawReference);
                    }
                    else throw new NotSupportedException();
                }

            }
        }

        int CalculateStructureSize(Type structureType)
        {
            IEnumerable<PropertyInfo> properties = Database.ManagedStructureInheritedProperties[structureType];

            int size = 0;
            foreach (var propertyInfo in properties)
            {
                size += CalculatePropertySize(propertyInfo);
            }

            return size;
        }

        unsafe int CalculatePropertySize(PropertyInfo propertyInfo)
        {
            var propertyType = propertyInfo.PropertyType;

            if (typeof(IDataCoreCollection).IsAssignableFrom(propertyType))
            {
                return 8;
            }

            if (propertyType.IsPrimitive)
            {
                if (propertyType == typeof(bool)) return sizeof(bool);
                else if (propertyType == typeof(SByte)) return sizeof(SByte);
                else if (propertyType == typeof(Int16)) return sizeof(Int16);
                else if (propertyType == typeof(Int32)) return sizeof(Int32);
                else if (propertyType == typeof(Int64)) return sizeof(Int64);
                else if (propertyType == typeof(Byte)) return sizeof(Byte);
                else if (propertyType == typeof(UInt16)) return sizeof(UInt16);
                else if (propertyType == typeof(UInt32)) return sizeof(UInt32);
                else if (propertyType == typeof(UInt64)) return sizeof(UInt64);
                else if (propertyType == typeof(Single)) return sizeof(Single);
                else if (propertyType == typeof(Double)) return sizeof(Double);
                else throw new NotImplementedException();
            }
            else if (propertyType.IsEnum)
            {
                return sizeof(int);
            }
            else if (typeof(IDataCoreString).IsAssignableFrom(propertyType))
            {
                return sizeof(RawStringReference);
            }
            else if (typeof(IDataCoreLocale).IsAssignableFrom(propertyType))
            {
                return sizeof(RawLocaleReference);
            }
            else if (typeof(IDataCoreStrongPointer).IsAssignableFrom(propertyType))
            {
                return sizeof(RawStrongPointer);
            }
            else if (typeof(IDataCoreWeakPointer).IsAssignableFrom(propertyType))
            {
                return sizeof(RawWeakPointer);
            }
            else if (typeof(DataCoreRecord).IsAssignableFrom(propertyType))
            {
                return sizeof(RawReference);
            }
            else if (typeof(IDataCoreStructure).IsAssignableFrom(propertyType))
            {
                return CalculateStructureSize(propertyType);
            }
            else throw new NotSupportedException();

            throw new Exception();
        }

        public Dictionary<Type, List<object>> ManagedDataTable = new Dictionary<Type, List<object>>();
        //public Dictionary<IDataCoreCollection, int> managedStructuresArrayLookup = new Dictionary<IDataCoreCollection, int>();

        public void EvaluateStructureInstance(object instance)
        {
            var instanceType = instance.GetType();
            var properties = Database.ManagedStructureInheritedProperties[instanceType];
            foreach (var property in properties)
            {
                var propertyType = property.PropertyType;
                var value = property.GetValue(instance);
                if (typeof(IDataCoreStrongPointer).IsAssignableFrom(propertyType))
                {
                    IDataCoreStrongPointer strongPointer = value as IDataCoreStrongPointer;
                    StructureSearch(strongPointer.InstanceObject);
                }
                else if (typeof(IDataCoreWeakPointer).IsAssignableFrom(propertyType))
                {
                    IDataCoreWeakPointer weakPointer = value as IDataCoreWeakPointer;
                    StructureSearch(weakPointer.InstanceObject);
                }
                else if (typeof(IDataCoreStructure).IsAssignableFrom(propertyType))
                {
                    // NOTE: Essentially, just loop again for this structure as we need to treat these as regular properties
                    // this makes this function recursive
                    EvaluateStructureInstance(value);
                }
                else if (typeof(IDataCoreCollection).IsAssignableFrom(propertyType))
                {
                    IDataCoreCollection dataCoreCollection = value as IDataCoreCollection;
                    var collectionType = dataCoreCollection.GetCollectionType();
                    var instancesArray = dataCoreCollection.ToObjectArray();

                    if (typeof(IDataCoreStructure).IsAssignableFrom(collectionType))
                    {
                        int arrayDataIndex = 0;
                        if (ManagedDataTable.ContainsKey(collectionType))
                        {
                            arrayDataIndex = ManagedDataTable[collectionType].Count;
                        }
                        //managedStructuresArrayLookup[dataCoreCollection] = arrayDataIndex;
                    }

                    foreach (var arrayInstance in instancesArray)
                    {
                        var arrayInstanceType = arrayInstance.GetType();

                        if (typeof(IDataCoreStrongPointer).IsAssignableFrom(arrayInstanceType))
                        {
                            IDataCoreStrongPointer arrayStrongPointer = arrayInstance as IDataCoreStrongPointer;
                            StructureSearch(arrayStrongPointer.InstanceObject);
                        }
                        else if (typeof(IDataCoreWeakPointer).IsAssignableFrom(arrayInstanceType))
                        {
                            IDataCoreWeakPointer arrayWeakPointer = arrayInstance as IDataCoreWeakPointer;
                            StructureSearch(arrayWeakPointer.InstanceObject);
                        }
                        else if (typeof(IDataCoreStructure).IsAssignableFrom(arrayInstanceType))
                        {
                            StructureSearch(arrayInstance);
                        }
                    }
                }
            }
        }

        public void StructureSearch(object instance)
        {
            if (instance == null) return;
            var instanceType = instance.GetType();

            if (!typeof(IDataCoreStructure).IsAssignableFrom(instanceType))
            {
                throw new Exception("Invalid instance");
            }

            if (!ManagedDataTable.ContainsKey(instanceType))
            {
                ManagedDataTable[instanceType] = new List<object>();
            }
            var hashSet = ManagedDataTable[instanceType];
            var alreadyExists = hashSet.Contains(instance);
            if (alreadyExists)
            {
                return;
            }
            hashSet.Add(instance);

            var type2 = Database.Assembly.GetType("datacore_0_0_0_0_debug.SGeometryResourceParams");
            if (instanceType == type2)
            {
                var index = this.ManagedDataTable[instanceType].IndexOf(instance);
                if (index == 2)
                {
                    Console.WriteLine();
                }
            }

            EvaluateStructureInstance(instance);
        }


        public void CreateDataTable()
        {
            //foreach (var structureType in Database.ManagedStructureTypes)
            //{
            //    ManagedDataTable[structureType] = new List<object>();
            //}

            foreach (var record in Database.ManagedRecords)
            {
                var instance = record.Instance;

                StructureSearch(instance);
            }

            // sort the data table to align with the structure indices

            var sortedDataTable = ManagedDataTable.Where(collection => collection.Value.Count > 0).ToList();
            sortedDataTable.Sort((keypairA, keypairB) =>
            {

                var indexA = Database.ManagedStructureTypes.IndexOf(keypairA.Key);
                var indexB = Database.ManagedStructureTypes.IndexOf(keypairB.Key);

                return indexA.CompareTo(indexB);
            });
            ManagedDataTable = new Dictionary<Type, List<object>>();
            foreach (var keypair in sortedDataTable)
            {
                ManagedDataTable[keypair.Key] = keypair.Value;
            }
        }

        public byte[] Compile(byte[] comparison = null)
        {
            using (MemoryStream ms = new MemoryStream())
            {
#if USECOMPARISON
                CustomBinaryWriter bw = new CustomBinaryWriter(ms, comparison);
#else
                CustomBinaryWriter bw = new CustomBinaryWriter(ms);
#endif
                CreateDataTable();

                ManagedDataTable = new Dictionary<Type, List<object>>();
                foreach (var keypair in Database.ManagedDataTable)
                {
                    ManagedDataTable[keypair.Key] = keypair.Value.ToList();
                }

                foreach (var newDataTableEntryKeyPair in ManagedDataTable)
                {
                    var entryIndex = ManagedDataTable.ToList().IndexOf(newDataTableEntryKeyPair);
                    var oldArray = Database.ManagedDataTable[newDataTableEntryKeyPair.Key];
                    var newArray = newDataTableEntryKeyPair.Value;

                    int[] newOrder = new int[oldArray.Count];
                    for (int i = 0; i < oldArray.Count; i++)
                    {
                        newOrder[i] = oldArray.IndexOf(newArray[i]);
                    }

                    for (int i = 0; i < oldArray.Count; i++)
                    {
                        var oldInstance = oldArray[i];
                        var newInstance = newArray[i];

                        //bool newInstanceIsRecord = false;
                        //foreach(var record in Database.ManagedRecords)
                        //{
                        //    newInstanceIsRecord |= newInstance == record.Instance;
                        //}

                        if (oldInstance != newInstance)
                        {
                            //throw new Exception("Invalid data");
                        }
                    }
                }

                // structure names in order
                for (int structureIndex = 0; structureIndex < Database.ManagedStructureTypes.Count; structureIndex++)
                {
                    var managedStructureType = Database.ManagedStructureTypes[structureIndex];
                    var structureNameOffset = textBlock.AddString(managedStructureType.Name);
                }

                // enum names in order
                for (int enumIndex = 0; enumIndex < Database.ManagedEnumTypes.Count; enumIndex++)
                {
                    var managedEnumType = Database.ManagedEnumTypes[enumIndex];
                    var enumNameOffset = textBlock.AddString(managedEnumType.Name);

                    var enumOptions = Enum.GetValues(managedEnumType);
                    foreach (var enumOption in enumOptions)
                    {
                        var enumOptionName = enumOption.ToString();
                        textBlock.AddString(enumOptionName);
                    }
                }

                for (int structureIndex = 0; structureIndex < Database.ManagedStructureTypes.Count; structureIndex++)
                {
                    var managedStructureType = Database.ManagedStructureTypes[structureIndex];
                    var structureNameOffset = textBlock.AddString(managedStructureType.Name);
                    var structureSize = CalculateStructureSize(managedStructureType);

                    RawStructure rawStructure = new RawStructure();
                    if (managedStructureType.BaseType != typeof(DataCoreStructureBase))
                    {
                        rawStructure.ParentTypeIndex = Database.ManagedStructureTypes.IndexOf(managedStructureType.BaseType);
                    }
                    else
                    {
                        rawStructure.ParentTypeIndex = -1;
                    }

                    List<RawProperty> structurePropertyDefinitions = new List<RawProperty>();

                    var structureProperties = Database.ManagedStructureProperties[managedStructureType];

                    foreach (var propertyInfo in structureProperties)
                    {
                        var propertyDataType = DataCoreDatabase.PropertyInfoToDataType(propertyInfo);
                        var propertyNameOffset = textBlock.AddString(propertyInfo.Name);

                        var rawProperty = new RawProperty();
                        rawProperty.DataType = propertyDataType;

                        var conversionType = DataCoreDatabase.GetPropertyInfoConversionType(propertyInfo);
                        var propertyType = propertyInfo.PropertyType;
                        Type collectionType = null;
                        if (typeof(IDataCoreCollection).IsAssignableFrom(propertyType))
                        {
                            collectionType = propertyType;
                            propertyType = collectionType.GenericTypeArguments[0];
                        }

                        switch (propertyDataType)
                        {
                            case DataType.Enum:
                                {
                                    var enumTypeIndex = Database.ManagedEnumTypes.IndexOf(propertyType);
                                    rawProperty.DefinitionIndex = (ushort)enumTypeIndex;
                                }
                                break;
                            case DataType.Class:
                                {
                                    var structureTypeIndex = Database.ManagedStructureTypes.IndexOf(propertyType);
                                    rawProperty.DefinitionIndex = (ushort)structureTypeIndex;
                                }
                                break;
                            case DataType.StrongPointer:
                                {
                                    var instanceType = propertyType.GenericTypeArguments[0];
                                    var structureTypeIndex = Database.ManagedStructureTypes.IndexOf(instanceType);
                                    rawProperty.DefinitionIndex = (ushort)structureTypeIndex;
                                }
                                break;
                            case DataType.WeakPointer:
                                {
                                    var instanceType = propertyType.GenericTypeArguments[0];
                                    var structureTypeIndex = Database.ManagedStructureTypes.IndexOf(instanceType);
                                    rawProperty.DefinitionIndex = (ushort)structureTypeIndex;
                                }
                                break;
                            case DataType.Reference:
                                {
                                    var instanceType = propertyType.GenericTypeArguments[0];
                                    var structureTypeIndex = Database.ManagedStructureTypes.IndexOf(instanceType);
                                    rawProperty.DefinitionIndex = (ushort)structureTypeIndex;
                                }
                                break;
                        }

                        rawProperty.ConversionType = conversionType;
                        rawProperty.NameOffset = propertyNameOffset;
                        structurePropertyDefinitions.Add(rawProperty);
                    }

                    rawStructure.FirstPropertyIndex = (ushort)propertyDefinitions.Count;
                    rawStructure.PropertyCount = (ushort)structurePropertyDefinitions.Count;
                    rawStructure.NameOffset = structureNameOffset;
                    rawStructure.StructureSize = structureSize;
                    structureDefinitions[structureIndex] = rawStructure;
                    propertyDefinitions.AddRange(structurePropertyDefinitions);
                }

                for (int enumIndex = 0; enumIndex < Database.ManagedEnumTypes.Count; enumIndex++)
                {
                    var managedEnumType = Database.ManagedEnumTypes[enumIndex];
                    var enumNameOffset = textBlock.AddString(managedEnumType.Name);

                    RawEnum rawEnum = new RawEnum();
                    var enumOptions = Enum.GetValues(managedEnumType);

                    rawEnum.FirstValueIndex = (ushort)enumValueNameTable.Count;
                    rawEnum.ValueCount = (ushort)enumOptions.Length;

                    foreach (var enumOption in enumOptions)
                    {
                        var enumOptionName = enumOption.ToString();
                        var enumOptionNameReference = new RawEnumNameReference { NameOffset = textBlock.AddString(enumOptionName) };
                        EnumValueTable[enumOption] = enumOptionNameReference;
                        enumValueNameTable.Add(enumOptionNameReference);
                    }

                    rawEnum.NameOffset = enumNameOffset;
                    enumDefinitions[enumIndex] = rawEnum;
                }

                //List<object> dataMappings = new List<object>();
                //for (int structureIndex = 0; structureIndex < Database.ManagedStructureTypes.Count; structureIndex++)
                //{
                //    var managedStructureType = Database.ManagedStructureTypes[structureIndex];
                //    if (Database.ManagedDataTable.ContainsKey(managedStructureType))
                //    {
                //        var structureInstances = Database.ManagedDataTable[managedStructureType];

                //        var rawDataMapping = new RawDataMapping();

                //        rawDataMapping.StructureIndex = (ushort)dataMappings.Count;
                //        rawDataMapping.StructCount = (ushort)(structureInstances.Count);

                //        //TODO
                //        dataMappings.AddRange(structureInstances);

                //        datamappingDefinitions.Add(rawDataMapping);
                //    }
                //}

                List<object> dataMappings = new List<object>();
                for (int datamappingIndex = 0; datamappingIndex < ManagedDataTable.Count; datamappingIndex++)
                {
                    var managedDataMap = ManagedDataTable.ElementAt(datamappingIndex);
                    var structureType = managedDataMap.Key;
                    var structureIndex = Database.ManagedStructureTypes.IndexOf(structureType);
                    var structureInstances = managedDataMap.Value;

                    var rawDataMapping = new RawDataMapping();
                    rawDataMapping.StructureIndex = (ushort)structureIndex;
                    rawDataMapping.StructCount = (ushort)structureInstances.Count;
                    datamappingDefinitions.Add(rawDataMapping);

                    dataMappings.AddRange(structureInstances);

#if DEBUG
                    var originalDataMapping = Database.RawDatabase.datamappingDefinitions[datamappingIndex];

                    if (rawDataMapping.StructCount != originalDataMapping.StructCount)
                    {
                        throw new Exception("DataMapping StructCount sanity check failed");
                    }
                    if (rawDataMapping.StructureIndex != originalDataMapping.StructureIndex)
                    {
                        throw new Exception("DataMapping StructureIndex sanity check failed");
                    }
#endif
                }

#if DEBUG

                foreach (var newDataTableEntryKeyPair in ManagedDataTable)
                {
                    var originalDataMapping = Database.ManagedDataTable[newDataTableEntryKeyPair.Key];
                    var newDataMapping = newDataTableEntryKeyPair.Value;

                    if (newDataMapping.Count != originalDataMapping.Count)
                    {
                        throw new Exception("DataMapping StructCount sanity check failed");
                    }
                }

                for (int datamappingIndex = 0; datamappingIndex < ManagedDataTable.Count; datamappingIndex++)
                {
                    var originalDataMapping = Database.RawDatabase.datamappingDefinitions[datamappingIndex];
                    var newDataMapping = datamappingDefinitions[datamappingIndex];

                    if (newDataMapping.StructCount != originalDataMapping.StructCount)
                    {
                        throw new Exception("DataMapping StructCount sanity check failed");
                    }
                    if (newDataMapping.StructureIndex != originalDataMapping.StructureIndex)
                    {
                        throw new Exception("DataMapping StructureIndex sanity check failed");
                    }
                }

#endif

                for (int recordIndex = 0; recordIndex < Database.ManagedRecords.Length; recordIndex++)
                {
                    var managedRecord = Database.ManagedRecords[recordIndex];
                    var recordNameOffset = textBlock.AddString(managedRecord.Name);
                    var recordFilenameOffset = textBlock.AddString(managedRecord.FileName);
                    var recordStructureType = managedRecord.StructureType;
                    var recordStructureIndex = Database.ManagedStructureTypes.IndexOf(recordStructureType);

                    var managedDataMap = ManagedDataTable[recordStructureType];
                    var variantIndex = managedDataMap.IndexOf(managedRecord.Instance);

                    var rawRecord = new RawRecord();

                    rawRecord.NameOffset = recordNameOffset;
                    rawRecord.FileNameOffset = recordFilenameOffset;
                    rawRecord.StructureIndex = recordStructureIndex;
                    rawRecord.ID = managedRecord.ID;
                    rawRecord.VariantIndex = (ushort)variantIndex;

#if DEBUG
                    var originalRecord = Database.RawDatabase.records[recordIndex];

                    if (originalRecord.NameOffset != rawRecord.NameOffset)
                    {
                        throw new Exception("Record NameOffset sanity check failed");
                    }
                    if (originalRecord.FileNameOffset != rawRecord.FileNameOffset)
                    {
                        throw new Exception("Record NameOffset sanity check failed");
                    }
                    if (originalRecord.StructureIndex != rawRecord.StructureIndex)
                    {
                        throw new Exception("Record StructureIndex sanity check failed");
                    }
                    if (originalRecord.ID != rawRecord.ID)
                    {
                        throw new Exception("PropeRecordrty ID sanity check failed");
                    }
                    if (originalRecord.VariantIndex != rawRecord.VariantIndex)
                    {
                        throw new Exception("Record VariantIndex sanity check failed");
                    }
                    rawRecord.OtherIndex = originalRecord.OtherIndex; //TODO: Figure this shit out!!!
                    if (originalRecord.OtherIndex != rawRecord.OtherIndex)
                    {
                        throw new Exception("Record OtherIndex sanity check failed");
                    }
#endif

                    records[recordIndex] = rawRecord;
                }

                byte[] StructureDataBlock;
                using (MemoryStream structureDataMemoryStream = new MemoryStream())
                {
#if USECOMPARISON
                    CustomBinaryWriter structureDataBinaryWriter = new CustomBinaryWriter(structureDataMemoryStream, Database.RawDatabase.DataChunk);
#else
                    CustomBinaryWriter structureDataBinaryWriter = new CustomBinaryWriter(structureDataMemoryStream);
#endif

                    //foreach (var structureInstance in dataMappings)
                    for (int j = 0; j < dataMappings.Count; j++)
                    {
                        var structureInstance = dataMappings[j];

                        //var type = structureInstance.GetType();
                        //var type2 = Database.Assembly.GetType("datacore_0_0_0_0_debug.SGeometryResourceParams");
                        //var index = this.ManagedDataTable[type].IndexOf(structureInstance);

                        CompileStructure(structureDataBinaryWriter, structureInstance);
                    }

                    StructureDataBlock = structureDataMemoryStream.ToArray();
                }


#if DEBUG
                // sanity check on structure sizes
                // sanity check on structure name order
                for (int structureIndex = 0; structureIndex < Database.ManagedStructureTypes.Count; structureIndex++)
                {
                    var originalStructure = Database.RawDatabase.structureDefinitions[structureIndex];
                    var newStructure = structureDefinitions[structureIndex];

                    if (newStructure.StructureSize != originalStructure.StructureSize)
                    {
                        throw new Exception("Structure StructureSize sanity check failed");
                    }
                    if (newStructure.NameOffset != originalStructure.NameOffset)
                    {
                        throw new Exception("Structure NameOffset sanity check failed");
                    }
                }

                for (int enumIndex = 0; enumIndex < enumDefinitions.Length; enumIndex++)
                {
                    var originalEnum = Database.RawDatabase.enumDefinitions[enumIndex];
                    var newEnum = enumDefinitions[enumIndex];

                    if (originalEnum.NameOffset != newEnum.NameOffset)
                    {
                        throw new Exception("Enum NameOffset sanity check failed");
                    }
                }

                for (int propertyIndex = 0; propertyIndex < propertyDefinitions.Count; propertyIndex++)
                {
                    var originalProperty = Database.RawDatabase.propertyDefinitions[propertyIndex];
                    var newProperty = propertyDefinitions[propertyIndex];

                    if (originalProperty.NameOffset != newProperty.NameOffset)
                    {
                        throw new Exception("Property NameOffset sanity check failed");
                    }
                    if (originalProperty.DefinitionIndex != newProperty.DefinitionIndex)
                    {
                        throw new Exception("Property DefinitionIndex sanity check failed");
                    }
                }

                for (int recordIndex = 0; recordIndex < records.Length; recordIndex++)
                {
                    var originalRecord = Database.RawDatabase.records[recordIndex];
                    var newRecord = records[recordIndex];

                    if (originalRecord.NameOffset != newRecord.NameOffset)
                    {
                        throw new Exception("Property NameOffset sanity check failed");
                    }
                    if (originalRecord.FileNameOffset != newRecord.FileNameOffset)
                    {
                        throw new Exception("Property NameOffset sanity check failed");
                    }
                }

#endif



                RawHeader header = new RawHeader();
                header.unknown1 = 0;
                header.version = 4;

                header.unknown2 = 13616; // !IsLegacy
                header.unknown3 = 7878; // !IsLegacy
                header.unknown4 = 36033; // !IsLegacy
                header.unknown5 = 468; // !IsLegacy

                header.structDefinitionCount = structureDefinitions.Length;
                header.propertyDefinitionCount = propertyDefinitions.Count;
                header.enumDefinitionCount = enumDefinitions.Length;
                header.dataMappingCount = datamappingDefinitions.Count;
                header.recordDefinitionCount = records.Length;

                header.booleanValueCount = booleanValues.Count;
                header.int8ValueCount = int8Values.Count;
                header.int16ValueCount = int16Values.Count;
                header.int32ValueCount = int32Values.Count;
                header.int64ValueCount = int64Values.Count;
                header.uInt8ValueCount = UInt8Values.Count;
                header.uInt16ValueCount = UInt16Values.Count;
                header.uInt32ValueCount = UInt32Values.Count;
                header.uInt64ValueCount = UInt64Values.Count;

                header.singleValueCount = singleValues.Count;
                header.doubleValueCount = doubleValues.Count;
                header.guidValueCount = guidValues.Count;
                header.stringValueCount = stringValues.Count;
                header.localeValueCount = localeValues.Count;
                header.enumValueCount = enumValues.Count;
                header.strongValueCount = strongValues.Count;
                header.weakValueCount = weakValues.Count;

                header.referenceValueCount = referenceValues.Count;
                header.enumOptionCount = enumValueNameTable.Count;

                header.textLength = textBlock.Size;

                header.unknown6 = -1; // !IsLegacy

                bw.Write(header);

                bw.Write(structureDefinitions);

#if DEBUG
                for (int i = 0; i < propertyDefinitions.Count; i++)
                {
                    var propertyDefinition = propertyDefinitions[i];

                    if (!bw.IsValid(propertyDefinition))
                    {
                        var original = bw.GetOriginalValue<RawProperty>();


                        Console.WriteLine();
                    }

                    bw.Write(propertyDefinition);
                }
#else
                bw.Write(propertyDefinitions);
#endif



                bw.Write(enumDefinitions);


#if DEBUG
                for (int i = 0; i < datamappingDefinitions.Count; i++)
                {
                    var datamappingDefinition = datamappingDefinitions[i];

                    if (!bw.IsValid(datamappingDefinition))
                    {
                        var original = bw.GetOriginalValue<RawDataMapping>();


                        Console.WriteLine();
                    }

                    bw.Write(datamappingDefinition);
                }
#else
                bw.Write(datamappingDefinitions);
#endif

#if DEBUG
                for (int i = 0; i < records.Length; i++)
                {
                    var record = records[i];

                    if (!bw.IsValid(record))
                    {
                        var original = bw.GetOriginalValue<RawRecord>();


                        Console.WriteLine();
                    }

                    bw.Write(record);
                }
#else
                bw.Write(records);
#endif



                bw.Write(int8Values);
                bw.Write(int16Values);
                bw.Write(int32Values);
                bw.Write(int64Values);
                bw.Write(UInt8Values);
                bw.Write(UInt16Values);
                bw.Write(UInt32Values);
                bw.Write(UInt64Values);
                bw.Write(booleanValues);

                bw.Write(singleValues);
                bw.Write(doubleValues);

                bw.Write(guidValues);

                bw.Write(stringValues);
                bw.Write(localeValues);
                bw.Write(enumValues);
                bw.Write(strongValues);
#if DEBUG
                bw.DebugNoCheckWrite(weakValues);
#else
                bw.Write(weakValues);
#endif

#if DEBUG
                for (int i = 0; i < referenceValues.Count; i++)
                {
                    var reference = referenceValues[i];

                    if (!bw.IsValid(reference))
                    {
                        var original = bw.GetOriginalValue<RawReference>();


                        Console.WriteLine();
                    }

                    bw.DebugNoCheckWrite(reference);
                }
#else
                bw.Write(referenceValues);
#endif


                bw.Write(enumValueNameTable);

                textBlock.WaitForInitialization();
                int bytes_written = 0;
                while (bytes_written < textBlock.Size)
                {
                    var str = textBlock.GetString(bytes_written);
                    var strdata = Encoding.UTF8.GetBytes(str);
                    bw.Write(strdata);
                    bw.Write((byte)0); // null terminate
                    bytes_written += strdata.Length + 1;
                }

#if DEBUG
                bw.DebugNoCheckWrite(StructureDataBlock);
#else
                bw.Write(StructureDataBlock);
#endif

#if DEBUG
                Console.WriteLine("--------------------------- WRITE DCB --------------------------");
                Console.WriteLine($"structureDefinitions: {structureDefinitions.Length}");
                Console.WriteLine($"propertyDefinitions: {propertyDefinitions.Count}");
                Console.WriteLine($"enumDefinitions: {enumDefinitions.Length}");
                Console.WriteLine($"datamappingDefinitions: {datamappingDefinitions.Count}");
                Console.WriteLine($"records: {records.Length}");
                Console.WriteLine();
                Console.WriteLine($"int8Values: {int8Values.Count}");
                Console.WriteLine($"int16Values: {int16Values.Count}");
                Console.WriteLine($"int32Values: {int32Values.Count}");
                Console.WriteLine($"int64Values: {int64Values.Count}");
                Console.WriteLine($"UInt8Values: {UInt8Values.Count}");
                Console.WriteLine($"UInt16Values: {UInt16Values.Count}");
                Console.WriteLine($"UInt32Values: {UInt32Values.Count}");
                Console.WriteLine($"UInt64Values: {UInt64Values.Count}");
                Console.WriteLine($"booleanValues: {booleanValues.Count}");
                Console.WriteLine();
                Console.WriteLine($"singleValues: {singleValues.Count}");
                Console.WriteLine($"doubleValues: {doubleValues.Count}");
                Console.WriteLine();
                Console.WriteLine($"stringValues: {stringValues.Count}");
                Console.WriteLine($"localeValues: {localeValues.Count}");
                Console.WriteLine();
                Console.WriteLine($"enumValues: {enumValues.Count}");
                Console.WriteLine($"strongValues: {strongValues.Count}");
                Console.WriteLine($"weakValues: {weakValues.Count}");
                Console.WriteLine($"referenceValues: {referenceValues.Count}");
                Console.WriteLine($"enumValueNameTable: {enumValueNameTable.Count}");
                Console.WriteLine();
                Console.WriteLine($"textBlock: {textBlock.Size}");
                Console.WriteLine("------------------------- END WRITE DCB ------------------------");
#endif

                var compiledDCB = ms.ToArray();
                return compiledDCB;
            }
        }

        private DataCoreCompiler()
        {

        }







    }
}
