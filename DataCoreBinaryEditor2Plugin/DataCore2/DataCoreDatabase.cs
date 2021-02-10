using Binary;
using DataCore2.ManagedTypeConstruction;
using DataCore2.Structures;
using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DataCore2
{
    public partial class DataCoreDatabase
    {
        public enum CacheMode
        {
            NoCache,
            UseCache,
            ExportBinary
        }
        private Int64[] Signature;
        private Assembly _Assembly;
        public Assembly Assembly { get => _Assembly; set => _Assembly = value; }
        internal DataCoreBinary RawDatabase = null;

        public string AssemblyName { get; private set; }
        public string AssemblyNamespace { get; private set; }
        public string AssemblyFilepath { get; private set; }

        public System.Collections.ObjectModel.ObservableCollection<Type> ManagedStructureTypes; // = new ObservableCollection<Type>();
        public System.Collections.ObjectModel.ObservableCollection<Type> ManagedEnumTypes; // = new ObservableCollection<Type>();
        public Dictionary<Type, IEnumerable<PropertyInfo>> ManagedStructureInheritedProperties = new Dictionary<Type, IEnumerable<PropertyInfo>>();
        public Dictionary<Type, IEnumerable<PropertyInfo>> ManagedStructureProperties = new Dictionary<Type, IEnumerable<PropertyInfo>>();
        public Dictionary<Type, System.Collections.ObjectModel.ObservableCollection<object>> ManagedDataTable = new Dictionary<Type, System.Collections.ObjectModel.ObservableCollection<object>>();



        public Dictionary<Guid, DataCoreRecord> ManagedGUIDTable = new Dictionary<Guid, DataCoreRecord>();
        internal DataCoreRecord[] ManagedRecords { get; private set; }

        internal object ResolvePointer(RawStrongPointer pointer)
        {
            if (pointer.StructIndex == -1 && pointer.VariantIndex == -1) // difference between weak and strong
            {
                return null;
            }

            var managedStructureType = ManagedStructureTypes[pointer.StructIndex];
            var table = ManagedDataTable[managedStructureType];
            var instance = table[pointer.VariantIndex];
            return instance;
        }

        internal object ResolvePointer(RawWeakPointer pointer)
        {
            if (pointer.StructureIndex == -1 || pointer.VariantIndex == -1) // difference between weak and strong
            {
                return null;
            }

            var managedStructureType = ManagedStructureTypes[pointer.StructureIndex];
            var instance = ManagedDataTable[managedStructureType][pointer.VariantIndex];
            return instance;
        }

        public DataCoreDatabase(int major, int minor, int revision, int build, string assemblyDirectory, byte[] data, CacheMode cacheMode, bool allowOutOfDateAssembly = false)
        {
            Crucible.MainWindow.SetStatus("Readng database", -1);

            RawDatabase = new DataCoreBinary(data);

#if DEBUG
            AssemblyNamespace = $"datacore_{major}_{minor}_{revision}_{build}_debug";
#else
            AssemblyNamespace = $"datacore_{major}_{minor}_{revision}_{build}";
#endif
            AssemblyName = $"{AssemblyNamespace}.bin";
            AssemblyFilepath = Path.Combine(assemblyDirectory, AssemblyName);

            InitializeSignature(out Signature);
            if (cacheMode == CacheMode.UseCache)
            {
                Crucible.MainWindow.SetStatus("DataCore2: Reading Cache...", -1);
                InitializeAssembly(AssemblyFilepath, AssemblyName, allowOutOfDateAssembly, out _Assembly);
            }
            if (Assembly == null)
            {
                if (cacheMode != CacheMode.NoCache)
                {
                    Crucible.MainWindow.SetStatus("DataCore2: Compiling Cache...", -1);
                }
                else
                {
                    Crucible.MainWindow.SetStatus("DataCore2: Compiling...", -1);
                }
                CreateAssembly(AssemblyFilepath, AssemblyName, cacheMode, out _Assembly);
            }
            if (Assembly != null)
            {
                Crucible.MainWindow.SetStatus("DataCore2: Initializing Types", -1);
                InitializeAssemblyTypes(Assembly);
            }
            else
            {
                Crucible.MainWindow.SetStatus("DataCore2: Failed to load Database");
                Console.WriteLine("Failed to load Database");
                return;
            }

            Crucible.MainWindow.SetStatus("DataCore2: Caching Structure Properties", -1);
            CreateCachedStructureProperties();

            Crucible.MainWindow.SetStatus("DataCore2: Reading Structures", -1);
            ProcessDataTable(out List<IClassFixup> classFixups);

            Crucible.MainWindow.SetStatus("DataCore2: Reading Records", -1);
            ReadRecords();

            //NOTE: Do these fixups absolutely last so the records GUID table is correct
            Crucible.MainWindow.SetStatus("DataCore2: Processing Structure Fixups", -1);
            ProcessFixups(classFixups);
        }

        internal void ProcessFixups(List<IClassFixup> classFixups)
        {
#if DEBUG
            foreach (var classFixup in classFixups)
            {
                classFixup.Run();
            }
#else
            Parallel.ForEach(classFixups, classFixup => classFixup.Run());
#endif
        }

        internal void ProcessDataTable(out List<IClassFixup> classFixups)
        {
            classFixups = new List<IClassFixup>();
            for (int i = 0; i < RawDatabase.header.dataMappingCount; i++)
            {
                var datamappingDefinition = RawDatabase.datamappingDefinitions[i];
                var structure = RawDatabase.structureDefinitions[datamappingDefinition.StructureIndex];
                var structureName = RawDatabase.textBlock.GetString(structure.NameOffset);

                var managedStructureType = this.ManagedStructureTypes[datamappingDefinition.StructureIndex];
                var managedStructureTypeArray = managedStructureType.MakeArrayType();

                var managedStructures = new object[datamappingDefinition.StructCount];

                //dataTableMap[datamappingDefinition.StructureIndex] = i;
                for (int j = 0; j < datamappingDefinition.StructCount; j++)
                {
                    managedStructures[j] = ReadStructure(managedStructureType, this, classFixups);
                }

                ManagedDataTable[managedStructureType] = new System.Collections.ObjectModel.ObservableCollection<object>(managedStructures);
            }
        }

        internal static void InitializeSignature(out Int64[] signature)
        {
            var thisAssembley = Assembly.GetAssembly(typeof(DataCoreDatabase));
            var thisVersion = thisAssembley.GetName().Version;
            var hash = DataCore2Util.SHA256(Encoding.UTF8.GetBytes(thisVersion.ToString()));
            signature = new long[4] {
                BitConverter.ToInt64(hash, 0),
                BitConverter.ToInt64(hash, 8),
                BitConverter.ToInt64(hash, 16),
                BitConverter.ToInt64(hash, 24)
            };
        }

        internal void InitializeAssembly(string assemblyFilepath, string assemblyName, bool allowOutOfDateAssembly, out Assembly assembly)
        {
            assembly = null;

            byte[] dynamicAssemblyData = null;

            if (File.Exists(assemblyFilepath))
            {
                dynamicAssemblyData = File.ReadAllBytes(assemblyFilepath);
            }

            if (dynamicAssemblyData != null)
            {

                BinaryBlobReader binaryBlobReader = new BinaryBlobReader(dynamicAssemblyData, 0);
                var storedA = binaryBlobReader.Read<long>();
                var storedB = binaryBlobReader.Read<long>();
                var storedC = binaryBlobReader.Read<long>();
                var storedD = binaryBlobReader.Read<long>();

                var assemblyData = binaryBlobReader.Read<byte>(dynamicAssemblyData.Length - binaryBlobReader.Position);

                var magic = BitConverter.ToUInt16(assemblyData, 0);

                //#if !DEBUG
                assemblyData = DataCore2Util.Decompress(assemblyData);
                //#endif

                bool overrideAssembly = assemblyData.Length > 64 && allowOutOfDateAssembly;
                bool signatureMatches = Signature[0] == storedA && Signature[1] == storedB && Signature[2] == storedC && Signature[3] == storedD;
                if (overrideAssembly || signatureMatches)
                {
                    assembly = Assembly.Load(assemblyData);
                }
            }
        }

        private void InitializeAssemblyTypes(Assembly assembly)
        {
            ManagedEnumTypes = new System.Collections.ObjectModel.ObservableCollection<Type>(new Type[RawDatabase.enumDefinitions.Length]);
            for (int enumDefinitionIndex = 0; enumDefinitionIndex < RawDatabase.enumDefinitions.Length; enumDefinitionIndex++)
            {
                var enumDefinition = RawDatabase.enumDefinitions[enumDefinitionIndex];
                var enumName = RawDatabase.textBlock.GetString(enumDefinition.NameOffset);

                //TODO: Local Types

                var assemblyType = assembly.GetType($"{AssemblyNamespace}.{enumName}");
                if (assemblyType != null)
                {
                    ManagedEnumTypes[enumDefinitionIndex] = assemblyType;
                    continue;
                }

                throw new Exception($"Failed to load Enum type {enumName}");
            }

            ManagedStructureTypes = new System.Collections.ObjectModel.ObservableCollection<Type>(new Type[RawDatabase.structureDefinitions.Length]);
            for (int structureIndex = 0; structureIndex < RawDatabase.structureDefinitions.Length; structureIndex++)
            {
                var structureDefinition = RawDatabase.structureDefinitions[structureIndex];
                var structureName = RawDatabase.textBlock.GetString(structureDefinition.NameOffset);

                //TODO: Local Types

                var assemblyType = assembly.GetType($"{AssemblyNamespace}.{structureName}");
                if (assemblyType != null)
                {
                    ManagedStructureTypes[structureIndex] = assemblyType;
                    continue;
                }

                throw new Exception($"Failed to load Structure type: {structureName}");
            }
        }

        private string CreateEnumTypeSource(int enumDefinitionIndex)
        {
            var enumDefinition = RawDatabase.enumDefinitions[enumDefinitionIndex];
            var enumName = RawDatabase.textBlock.GetString(enumDefinition.NameOffset);

            EnumValueCreationInfo[] enumProperties = new EnumValueCreationInfo[enumDefinition.ValueCount];
            for (int i = 0; i < enumDefinition.ValueCount; i++)
            {
                var valueIndex = enumDefinition.FirstValueIndex + i;
                var stringRefValue = RawDatabase.enumValueNameTable[valueIndex];
                var stringValue = RawDatabase.textBlock.GetString(stringRefValue);
                var value = valueIndex;

                EnumValueCreationInfo enumProperty = new EnumValueCreationInfo();
                enumProperty.Name = stringValue;
                enumProperty.Value = value;

                enumProperties[i] = enumProperty;
            }

            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"public enum {enumName} : int");
            stringBuilder.AppendLine("{");

            foreach (var enumProperty in enumProperties)
            {
                stringBuilder.AppendLine($"\t{enumProperty.Name},");
            }

            stringBuilder.AppendLine("};");

            var result = stringBuilder.ToString();
            return result;
        }

        private string CreateStructureTypeString(int structureIndex)
        {
#if DEBUG
            if (structureIndex == -1)
            {
                throw new Exception("Invalid structureIndex");
            }
#endif

            var structureDefinition = RawDatabase.structureDefinitions[structureIndex];
            var structureName = RawDatabase.textBlock.GetString(structureDefinition.NameOffset);
            List<PropertyCreationInfo> customProperties = new List<PropertyCreationInfo>();

            string parentClass = "DataCoreStructureBase";
            if (structureDefinition.ParentTypeIndex != -1)
            {
                var parentStructureDefinition = RawDatabase.structureDefinitions[structureDefinition.ParentTypeIndex];
                var parentStructureName = RawDatabase.textBlock.GetString(parentStructureDefinition.NameOffset);
                parentClass = parentStructureName;
            }

            StringBuilder stringBuilder = new StringBuilder();


            stringBuilder.Append($"public class {structureName}");
            if (!string.IsNullOrWhiteSpace(parentClass))
            {
                stringBuilder.Append($" : {parentClass}");
            }
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("{");

            for (int i = 0; i < structureDefinition.PropertyCount; i++)
            {
                var propertyIndex = structureDefinition.FirstPropertyIndex + i;
                var propertyDefinition = RawDatabase.propertyDefinitions[propertyIndex];
                var propertyName = RawDatabase.textBlock.GetString(propertyDefinition.NameOffset);

                var propertyString = RawPropertyToPropertyString(propertyDefinition);

                stringBuilder.AppendLine(propertyString);
            }

            stringBuilder.AppendLine("};");

            var result = stringBuilder.ToString();
            return result;


            //// get the parent type
            //Type parentType = null;
            //if (structureDefinition.ParentTypeIndex != -1)
            //{
            //    parentType = CreateStructureType(structureDefinition.ParentTypeIndex);
            //}

            //for (int i = 0; i < structureDefinition.PropertyCount; i++)
            //{
            //    var propertyIndex = structureDefinition.FirstPropertyIndex + i;
            //    var propertyDefinition = RawDatabase.propertyDefinitions[propertyIndex];
            //    var propertyName = RawDatabase.textBlock.GetString(propertyDefinition.NameOffset);

            //    PropertyCreationInfo customProperty = new PropertyCreationInfo();
            //    customProperty.Name = propertyName;
            //    customProperty.PropertyDefinition = propertyDefinition;


            //    Type unmanagedPropertyType = null;
            //    bool isDeclaringType = false;
            //    if (propertyDefinition.DataType == DataType.Class && propertyDefinition.DefinitionIndex == structureIndex)
            //    {
            //        isDeclaringType = true;
            //    }
            //    else
            //    {
            //        unmanagedPropertyType = LEGACY_RawPropertyToType(propertyDefinition);
            //    }

            //    customProperty.MemberType = unmanagedPropertyType;
            //    customProperty.IsDeclaringType = isDeclaringType;
            //    customProperty.HideUnderlyingType = false;

            //    bool isUnderlyingPointer = false;
            //    isUnderlyingPointer |= unmanagedPropertyType == typeof(RawStrongPointer);
            //    isUnderlyingPointer |= unmanagedPropertyType == typeof(RawWeakPointer);
            //    if (isUnderlyingPointer)
            //    {
            //        customProperty.HideUnderlyingType = true;
            //        customProperty.OvertType = typeof(object);
            //    }

            //    bool isUnderlyingString = false;
            //    isUnderlyingString |= unmanagedPropertyType == typeof(RawStringReference);
            //    isUnderlyingString |= unmanagedPropertyType == typeof(RawLocaleReference);
            //    if (isUnderlyingString)
            //    {
            //        customProperty.HideUnderlyingType = true;
            //        customProperty.OvertType = typeof(String);
            //    }

            //    customProperty.ConversionType = propertyDefinition.ConversionType;

            //    customProperties.Add(customProperty);
            //}

            //var type = TypeBuilder.CreateClassType($"{AssemblyName}.{structureName}", customProperties, parentType);
            //ManagedStructureTypes[structureIndex] = type;

            //return type;
        }



        internal void CreateAssembly(string assemblyFilepath, string assemblyName, CacheMode cacheMode, out Assembly assembly)
        {
            assembly = null;

            List<string> enumSources = new List<string>();
            for (int enumIndex = 0; enumIndex < RawDatabase.enumDefinitions.Length; enumIndex++)
            {
                var result = CreateEnumTypeSource(enumIndex);
                enumSources.Add(result);
            }

            List<string> structureSources = new List<string>();
            for (int structureIndex = 0; structureIndex < RawDatabase.structureDefinitions.Length; structureIndex++)
            {
                var result = CreateStructureTypeString(structureIndex);
                structureSources.Add(result);
            }

            List<string> sourcesList = new List<string>();
            sourcesList.AddRange(enumSources);
            sourcesList.AddRange(structureSources);

            var sources = sourcesList.ToArray();
            var sourceCode = string.Join("\n", sources);

            // wrap namespaces
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"namespace {AssemblyNamespace}");
            stringBuilder.AppendLine("{");
            stringBuilder.AppendLine($"using System;");
            stringBuilder.AppendLine($"using DataCore2;");
            stringBuilder.AppendLine($"using DataCore2.ManagedTypeConstruction;");
            stringBuilder.AppendLine($"using System.Diagnostics;");

            stringBuilder.AppendLine(sourceCode);
            stringBuilder.AppendLine("}");
            sourceCode = stringBuilder.ToString();

            using (var csc = new CSharpCodeProvider(new Dictionary<string, string>() { { "CompilerVersion", "v4.0" } }))
            {
                var currentAssemblyFilepath = Assembly.GetAssembly(typeof(DataCoreDatabase)).Location;
                var libraries = new[] {
                    "mscorlib.dll",
                    "System.Core.dll",
                    "System.dll",
                    //"DataCore2.dll" // DataCore2 (Old)
                    currentAssemblyFilepath
                };
                var parameters = new CompilerParameters(libraries, AssemblyName)
                {
                    GenerateExecutable = false,
                    GenerateInMemory = true,
#if DEBUG
                    IncludeDebugInformation = true,
                    //TreatWarningsAsErrors = true,
#else
                    IncludeDebugInformation = false,
                    //TreatWarningsAsErrors = false,
                    CompilerOptions = "/target:library /optimize",
#endif
                };

                var res = csc.CompileAssemblyFromSource(parameters, new string[] { sourceCode });

                if (res.Errors.Count > 0)
                {
#if DEBUG
                    foreach (CompilerError error in res.Errors)
                    {
                        throw new Exception(error.ToString());
                    }
#else
                    foreach (CompilerError error in res.Errors)
                    {
                        Console.WriteLine(error.ErrorText);
                    }
                    throw new Exception("Compilation errors occured");
#endif
                }

                assembly = res.CompiledAssembly;
            }

            //            return;

            //#pragma warning disable CS0162

            //TODO: Figure out how to use the cache again

            if (cacheMode != CacheMode.NoCache)
            {
                byte[] bytes = File.ReadAllBytes(assembly.ManifestModule.ScopeName);
                File.Delete(assembly.ManifestModule.ScopeName);

                //#if !DEBUG
                bytes = DataCore2Util.Compress(bytes);
                //#endif

                Directory.CreateDirectory(Path.GetDirectoryName(AssemblyFilepath));

                using (var fs = new FileStream(AssemblyFilepath, FileMode.Create, FileAccess.ReadWrite))
                using (var bw = new BinaryWriter(fs))
                {
                    bw.Write(Signature[0]);
                    bw.Write(Signature[1]);
                    bw.Write(Signature[2]);
                    bw.Write(Signature[3]);
                    bw.Write(bytes);
                }
            }
            else
            {
                File.Delete(assembly.ManifestModule.ScopeName);
            }
        }

        internal void CreateCachedStructureProperties()
        {
            for (int i = 0; i < ManagedStructureTypes.Count; i++)
            {
                var managedStructure = ManagedStructureTypes[i];
                var structureInheritedProperties = GetStructureProperties(managedStructure, true);
                ManagedStructureInheritedProperties[managedStructure] = structureInheritedProperties;
                var structureProperties = GetStructureProperties(managedStructure, false);
                ManagedStructureProperties[managedStructure] = structureProperties;

#if DEBUG
                var structureDefinition = RawDatabase.structureDefinitions[i];
                for (int propertyOffset = 0; propertyOffset < structureDefinition.PropertyCount; propertyOffset++)
                {
                    var propertyIndex = structureDefinition.FirstPropertyIndex + propertyOffset;
                    var propertyDefinition = RawDatabase.propertyDefinitions[propertyIndex];
                    var propertyName = RawDatabase.textBlock.GetString(propertyDefinition.NameOffset);

                    var propertyInfo = structureProperties.ElementAt(propertyOffset);

                    if (propertyInfo.Name != propertyName)
                    {
                        throw new Exception();
                    }

                }
#endif
            }
        }

        public static IEnumerable<PropertyInfo> GetStructureProperties(Type type, bool inherit)
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

        private void ReadRecords()
        {
            ManagedRecords = new DataCoreRecord[RawDatabase.records.Length];
            for (int i = 0; i < RawDatabase.records.Length; i++)
            {
                var record = RawDatabase.records[i];
                var structureType = ManagedStructureTypes[record.StructureIndex];

                var dataCoreRecordGenericType = typeof(DataCoreRecord<>);
                var dataCoreRecordType = dataCoreRecordGenericType.MakeGenericType(structureType);
                var dataCoreRecord = Activator.CreateInstance(dataCoreRecordType, (object)this, (object)record) as DataCoreRecord;

                ManagedGUIDTable[record.ID] = dataCoreRecord;
                ManagedRecords[i] = dataCoreRecord;
            }
        }

        internal static IEnumerable<FieldInfo> GetBlockProperties(Type type)
        {
            List<FieldInfo> fieldInfos = new List<FieldInfo>();

            Type currentType = type;
            while (true)
            {
                if (currentType == null) throw new Exception();
                if (currentType == typeof(DataCoreStructureBase)) break;
                if (currentType == typeof(Enum)) break;

                var baseTypeFields = currentType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToList();

                var fields = baseTypeFields.Select(property => type.GetField(property.Name, BindingFlags.Public | BindingFlags.Instance)).ToList();
                if (baseTypeFields.Count != fields.Count)
                {
                    throw new Exception();
                }

                // must sort on each inheritance layer and then reconstruct as dynamically created objects share same metadata token ranges
                fields.Sort((x, y) => x.MetadataToken - y.MetadataToken);

                fieldInfos.InsertRange(0, fields);

                currentType = currentType.BaseType;
            }

            return fieldInfos;
        }

        internal static IEnumerable<FieldInfo> GetBlockFields(Type type)
        {
            List<FieldInfo> fieldInfos = new List<FieldInfo>();

            Type currentType = type;
            while (true)
            {
                if (currentType == null) throw new Exception();
                if (currentType == typeof(DataCoreStructureBase)) break;
                if (currentType == typeof(Enum)) break;

                var baseTypeFields = currentType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToList();

                var fields = baseTypeFields.Select(property => type.GetField(property.Name, BindingFlags.Public | BindingFlags.Instance)).ToList();
                if (baseTypeFields.Count != fields.Count)
                {
                    throw new Exception();
                }

                // must sort on each inheritance layer and then reconstruct as dynamically created objects share same metadata token ranges
                fields.Sort((x, y) => x.MetadataToken - y.MetadataToken);

                fieldInfos.InsertRange(0, fields);

                currentType = currentType.BaseType;
            }

            return fieldInfos;
        }

        internal static object ReadStructure(Type managedStructureType, DataCoreDatabase database, List<IClassFixup> classFixups)
        {
            var reader = database.RawDatabase.Reader;
            var instance = Activator.CreateInstance(managedStructureType);
            var rawDatabase = database.RawDatabase;
            var typeProperties = database.ManagedStructureInheritedProperties[managedStructureType];

            foreach (var propertyInfo in typeProperties)
            {
                var propertyType = propertyInfo.PropertyType;
                //var dataCorePropertyAttribute = property.GetCustomAttribute<DataCorePropertyAttribute>();

                var startPos = reader.Position;

                var isArray = typeof(IDataCoreCollection).IsAssignableFrom(propertyType);

                if (!isArray)
                {
                    if (propertyType.IsEnum)
                    {
                        var enumStringOffset = reader.ReadInt32();
                        var enumString = database.RawDatabase.textBlock.GetString(enumStringOffset);
                        if (enumString != "")
                        {
                            var enumValue = Enum.Parse(propertyType, enumString);
                            propertyInfo.SetValue(instance, enumValue);
                        }
                        else
                        {
                            propertyInfo.SetValue(instance, Enum.ToObject(propertyType, -1));
                        }
                    }
                    else if (propertyType.IsPrimitive || propertyType.IsValueType)
                    {
                        if (propertyType == typeof(Boolean)) propertyInfo.SetValue(instance, reader.Read<Boolean>());
                        else if (propertyType == typeof(Byte)) propertyInfo.SetValue(instance, reader.Read<Byte>());
                        else if (propertyType == typeof(SByte)) propertyInfo.SetValue(instance, reader.Read<SByte>());
                        else if (propertyType == typeof(Int16)) propertyInfo.SetValue(instance, reader.Read<Int16>());
                        else if (propertyType == typeof(UInt16)) propertyInfo.SetValue(instance, reader.Read<UInt16>());
                        else if (propertyType == typeof(Int32)) propertyInfo.SetValue(instance, reader.Read<Int32>());
                        else if (propertyType == typeof(UInt32)) propertyInfo.SetValue(instance, reader.Read<UInt32>());
                        else if (propertyType == typeof(Int64)) propertyInfo.SetValue(instance, reader.Read<Int64>());
                        else if (propertyType == typeof(UInt64)) propertyInfo.SetValue(instance, reader.Read<UInt64>());
                        else if (propertyType == typeof(IntPtr)) propertyInfo.SetValue(instance, reader.Read<IntPtr>());
                        else if (propertyType == typeof(UIntPtr)) propertyInfo.SetValue(instance, reader.Read<UIntPtr>());
                        else if (propertyType == typeof(Char)) propertyInfo.SetValue(instance, reader.Read<Char>());
                        else if (propertyType == typeof(Double)) propertyInfo.SetValue(instance, reader.Read<Double>());
                        else if (propertyType == typeof(Single)) propertyInfo.SetValue(instance, reader.Read<Single>());
                        else propertyInfo.SetValue(instance, reader.Read(propertyType));
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
                        var value = reader.Read<RawStringReference>();
                        var @string = rawDatabase.textBlock.GetString(value);
                        var datacoreStringValue = new DataCoreString(@string);
                        propertyInfo.SetValue(instance, datacoreStringValue);
                    }
                    else if (typeof(IDataCoreLocale).IsAssignableFrom(propertyType))
                    {
                        var value = reader.Read<RawLocaleReference>();
                        var @string = rawDatabase.textBlock.GetString(value);
                        var datacoreLocaleValue = new DataCoreLocale(@string);
                        propertyInfo.SetValue(instance, datacoreLocaleValue);
                    }
                    else if (typeof(IDataCoreStrongPointer).IsAssignableFrom(propertyType))
                    {
                        var value = reader.Read<RawStrongPointer>();
                        classFixups.Add(new StrongPointerFixup(database, instance, propertyInfo, value));
                    }
                    else if (typeof(IDataCoreWeakPointer).IsAssignableFrom(propertyType))
                    {
                        var value = reader.Read<RawWeakPointer>();
                        classFixups.Add(new WeakPointerFixup(database, instance, propertyInfo, value));
                    }
                    else if (typeof(DataCoreRecord).IsAssignableFrom(propertyType))
                    {
                        var reference = reader.Read<RawReference>();
                        classFixups.Add(new ReferenceFixup(database, instance, propertyInfo, reference));
                    }
                    else if (typeof(IDataCoreStructure).IsAssignableFrom(propertyType))
                    {
                        var structureValue = ReadStructure(propertyType, database, classFixups);
                        propertyInfo.SetValue(instance, structureValue);
                    }
                    else throw new NotImplementedException();
                }
                else
                {
                    var arrayCount = reader.ReadInt32();
                    var firstIndex = reader.ReadInt32();

                    var collectionType = propertyType;
                    propertyType = propertyType.GenericTypeArguments[0];

                    if (propertyType.IsPrimitive)
                    {
                        if (propertyType == typeof(SByte))
                        {
                            var int8Values = BinaryBlobReader.FastCopySafe(rawDatabase.int8Values, firstIndex, arrayCount);
                            var int8Collection = Activator.CreateInstance(collectionType, int8Values);
                            propertyInfo.SetValue(instance, int8Collection);
                        }
                        else if (propertyType == typeof(Int16))
                        {
                            var int16Values = BinaryBlobReader.FastCopySafe(rawDatabase.int16Values, firstIndex, arrayCount);
                            var int16Collection = Activator.CreateInstance(collectionType, int16Values);
                            propertyInfo.SetValue(instance, int16Collection);
                        }
                        else if (propertyType == typeof(Int32))
                        {
                            var int32Values = BinaryBlobReader.FastCopySafe(rawDatabase.int32Values, firstIndex, arrayCount);
                            var int32Collection = Activator.CreateInstance(collectionType, int32Values);
                            propertyInfo.SetValue(instance, int32Collection);
                        }
                        else if (propertyType == typeof(Int64))
                        {
                            var int64Values = BinaryBlobReader.FastCopySafe(rawDatabase.int64Values, firstIndex, arrayCount);
                            var int64Collection = Activator.CreateInstance(collectionType, int64Values);
                            propertyInfo.SetValue(instance, int64Collection);
                        }
                        else if (propertyType == typeof(Byte))
                        {
                            var uint8Values = BinaryBlobReader.FastCopySafe(rawDatabase.UInt8Values, firstIndex, arrayCount);
                            var uint8Collection = Activator.CreateInstance(collectionType, uint8Values);
                            propertyInfo.SetValue(instance, uint8Collection);
                        }
                        else if (propertyType == typeof(UInt16))
                        {
                            var uint16Values = BinaryBlobReader.FastCopySafe(rawDatabase.UInt16Values, firstIndex, arrayCount);
                            var uint16Collection = Activator.CreateInstance(collectionType, uint16Values);
                            propertyInfo.SetValue(instance, uint16Collection);
                        }
                        else if (propertyType == typeof(UInt32))
                        {
                            var uint32Values = BinaryBlobReader.FastCopySafe(rawDatabase.UInt32Values, firstIndex, arrayCount);
                            var uint32Collection = Activator.CreateInstance(collectionType, uint32Values);
                            propertyInfo.SetValue(instance, uint32Collection);
                        }
                        else if (propertyType == typeof(UInt64))
                        {
                            var uint64Values = BinaryBlobReader.FastCopySafe(rawDatabase.UInt64Values, firstIndex, arrayCount);
                            var uint64Collection = Activator.CreateInstance(collectionType, uint64Values);
                            propertyInfo.SetValue(instance, uint64Collection);
                        }
                        else if (propertyType == typeof(Single))
                        {
                            var singleValues = BinaryBlobReader.FastCopySafe(rawDatabase.singleValues, firstIndex, arrayCount);
                            var singleCollection = Activator.CreateInstance(collectionType, singleValues);
                            propertyInfo.SetValue(instance, singleCollection);
                        }
                        else if (propertyType == typeof(Double))
                        {
                            var doubleValues = BinaryBlobReader.FastCopySafe(rawDatabase.doubleValues, firstIndex, arrayCount);
                            var doubleCollection = Activator.CreateInstance(collectionType, doubleValues);
                            propertyInfo.SetValue(instance, doubleCollection);
                        }
                        else if (propertyType == typeof(Boolean))
                        {
                            var booleanValues = BinaryBlobReader.FastCopySafe(rawDatabase.booleanValues, firstIndex, arrayCount);
                            var booleanCollection = Activator.CreateInstance(collectionType, booleanValues);
                            propertyInfo.SetValue(instance, booleanCollection);
                        }
                    }
                    //else if (propertyType == typeof(Enum))
                    else if (propertyType.IsEnum)
                    {
                        var enumNameOffsetValues = BinaryBlobReader.FastCopySafe(rawDatabase.enumValues, firstIndex, arrayCount);
                        var enumArrayType = propertyType.MakeArrayType();
                        dynamic enumArray = Activator.CreateInstance(enumArrayType, new object[] { enumNameOffsetValues.Length });
                        for (int i = 0; i < enumNameOffsetValues.Length; i++)
                        {
                            var enumNameOffsetValue = enumNameOffsetValues[i];
                            var enumStringValue = rawDatabase.textBlock.GetString(enumNameOffsetValue);
                            //enumStringValue null fix
                            if (enumStringValue != "")
                            {
                                dynamic enumValue = Enum.Parse(propertyType, enumStringValue);
                                enumArray[i] = enumValue;
                            }
                        }
                        var enumCollection = Activator.CreateInstance(collectionType, new object[] { enumArray });
                        propertyInfo.SetValue(instance, enumCollection);
                    }
                    else if (typeof(IDataCoreString).IsAssignableFrom(propertyType))
                    {
                        var stringReferenceValues = BinaryBlobReader.FastCopySafe(rawDatabase.stringValues, firstIndex, arrayCount);
                        var datacoreStringArray = new DataCoreString[stringReferenceValues.Length];
                        for (int i = 0; i < stringReferenceValues.Length; i++)
                        {
                            var stringReferenceValue = stringReferenceValues[i];
                            var stringValue = rawDatabase.textBlock.GetString(stringReferenceValue);
                            datacoreStringArray[i] = new DataCoreString(stringValue);
                        }
                        var stringCollection = Activator.CreateInstance(collectionType, new object[] { datacoreStringArray });
                        propertyInfo.SetValue(instance, stringCollection);
                    }
                    else if (typeof(IDataCoreLocale).IsAssignableFrom(propertyType))
                    {
                        var localeReferenceValues = BinaryBlobReader.FastCopySafe(rawDatabase.localeValues, firstIndex, arrayCount);
                        var datacoreLocaleArray = new DataCoreLocale[localeReferenceValues.Length];
                        for (int i = 0; i < localeReferenceValues.Length; i++)
                        {
                            var localeReferenceValue = localeReferenceValues[i];
                            var stringValue = rawDatabase.textBlock.GetString(localeReferenceValue);
                            datacoreLocaleArray[i] = new DataCoreLocale(stringValue);
                        }
                        var localeCollection = Activator.CreateInstance(collectionType, new object[] { datacoreLocaleArray });
                        propertyInfo.SetValue(instance, localeCollection);
                    }
                    else if (typeof(IDataCoreStrongPointer).IsAssignableFrom(propertyType))
                    {
                        classFixups.Add(new StrongPointerArrayFixup(database, instance, propertyInfo, firstIndex, arrayCount));
                    }
                    else if (typeof(IDataCoreWeakPointer).IsAssignableFrom(propertyType))
                    {
                        classFixups.Add(new WeakPointerArrayFixup(database, instance, propertyInfo, firstIndex, arrayCount));
                    }
                    else if (propertyType == typeof(RawStringReference)) throw new NotSupportedException();
                    else if (propertyType == typeof(RawLocaleReference)) throw new NotSupportedException();
                    else if (typeof(DataCoreRecord).IsAssignableFrom(propertyType))
                    {
                        classFixups.Add(new ReferenceArrayFixup(database, instance, propertyInfo, firstIndex, arrayCount));
                    }
                    else if (propertyType == typeof(String))
                    {
                        throw new NotSupportedException();
                    }
                    else if (propertyType == typeof(Object))
                    {
                        throw new NotSupportedException();
                    }
                    else if (propertyType == typeof(RawStrongPointer)) throw new Exception("RawStrongPointer should be used with an UnderlyingTypeAttribute");
                    else if (propertyType == typeof(RawWeakPointer)) throw new Exception("RawWeakPointer should be used with an UnderlyingTypeAttribute");
                    else if (propertyType.IsClass)
                    {
                        classFixups.Add(new ClassArrayFixup(database, instance, propertyInfo, firstIndex, arrayCount));
                    }
                    else throw new NotImplementedException();
                }

                var endPos = reader.Position;

                //Console.WriteLine($"{startPos} {endPos} {propertyType.Name} {propertyType}");

            }

            return instance;
        }



        public unsafe byte[] GetDatabaseBinary(byte[] comparison = null)
        {
            var compiler = new DataCoreCompiler(this);
            var data = compiler.Compile(comparison);
            return data;
        }
    }
}
