using DataCore2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace DataCoreBinary2.DatabaseFieldEditors
{
    internal class DatabaseFieldEditorBase : UserControl, INotifyPropertyChanged
    {
        public object Instance { get; private set; }
        public PropertyInfo Property { get; internal set; }

        public bool IsArray => IsTypeArray(Property.PropertyType);
        public Type PropertyType => IsArray ? Property.PropertyType.GenericTypeArguments[0] : Property.PropertyType;
        public Type ArrayType => IsArray ? Property.PropertyType : null;
        public string PropertyName => ArrayIndex != -1 ? $"{Property.Name}[{ArrayIndex}]" : Property.Name;
        public string PropertyTypeName => PrettyTypeName(PropertyType);
        public string PropertyTooltip { get; }

        public int ArrayIndex { get; }
        public DataCoreDatabase Database { get; internal set; }

        public static bool IsTypeArray(Type type)
        {
            bool isArray = true;
            isArray &= type != typeof(string);
            isArray &= typeof(IDataCoreCollection).IsAssignableFrom(type);
            return isArray;
        }

        public object GetRawData()
        {
            if (ArrayIndex == -1)
            {
                return Property.GetValue(Instance);
            }
            else
            {
                dynamic array = Property.GetValue(Instance);
                return array?[ArrayIndex];
            }
        }

        public void SetRawData(object value)
        {
            if(!IsArray)
            {
                if (value.GetType() != PropertyType)
                {
                    throw new Exception("Invalid data type");
                }
            }
            else
            {
                if (value.GetType() != ArrayType)
                {
                    throw new Exception("Invalid data type");
                }
            }
            

            if (ArrayIndex == -1)
            {
                Property.SetValue(Instance, value);
            }
            else
            {
                dynamic array = Property.GetValue(Instance);
                if (array)
                {
                    array[ArrayIndex] = value;
                }
            }
        }
        public T GetData<T>() { return (T)GetRawData(); }
        public void SetData<T>(T value) { SetRawData(value); }

        public DatabaseFieldEditorBase(DataCoreDatabase database, object instance, PropertyInfo property, int arrayIndex = -1)
        {
            Instance = instance;
            Property = property;
            this.ArrayIndex = arrayIndex;
            this.Database = database;
        }

        static string PrettyTypeName(Type t)
        {
            if (t.IsGenericType)
            {
                return string.Format(
                    "{0}<{1}>",
                    t.Name.Substring(0, t.Name.LastIndexOf("`", StringComparison.InvariantCulture)),
                    string.Join(", ", t.GetGenericArguments().Select(PrettyTypeName)));
            }

            return t.Name;
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetPropertyDep<T>(ref T storage, T value, IEnumerable<string> deps, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return false;
            }

            foreach (var str in deps)
            {
                var prop = this.GetType().GetProperty(str);
                if (prop == null) throw new Exception("Invalid property");


            }

            storage = value;
            this.OnPropertyChanged(propertyName);
            return true;
        }

        protected bool SetProperty<T>(T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(Property.GetValue(Instance), value))
            {
                return false;
            }

            Property.SetValue(Instance, value);
            this.OnPropertyChanged(propertyName);
            return true;
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return false;
            }

            storage = value;
            this.OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
