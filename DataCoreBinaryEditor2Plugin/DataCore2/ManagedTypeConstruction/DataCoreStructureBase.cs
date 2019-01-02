using Binary;
using DataCore2.Structures;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DataCore2.ManagedTypeConstruction
{
    public class DataCoreStructureBase : IDataCoreStructure, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void _SetPropertyDynamic<T>(T value, string propertyName)
        {
            SetProperty<T>(value, propertyName);
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

        protected bool SetProperty<T>(T value, [CallerMemberName] string propertyName = null)
        {
            var fieldName = $"_{propertyName}";
            var type = this.GetType();
            FieldInfo fieldInfo = null;

            {
                Type currentType = type;
                while (true)
                {
                    if (currentType == null) throw new Exception();
                    if (currentType == typeof(DataCoreStructureBase)) break;
                    if (currentType == typeof(Enum)) break;

                    var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
                    var fields = currentType.GetFields(bindingFlags).ToList();

                    foreach (var currentFieldInfo in fields)
                    {
                        if (currentFieldInfo.Name == fieldName)
                        {
                            fieldInfo = currentFieldInfo;
                            goto _continue;
                        }
                    }

                    currentType = currentType.BaseType;
                }
                _continue: { }
            }

            if (fieldInfo == null)
            {
                throw new Exception("Field shouldn't be null");
            }

            var oldValue = fieldInfo.GetValue(this);
            if (Equals(oldValue, value))
            {
                return false;
            }

            fieldInfo.SetValue(this, value);
            this.OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
