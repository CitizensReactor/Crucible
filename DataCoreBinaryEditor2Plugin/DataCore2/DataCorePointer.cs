using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DataCore2
{
    public abstract class DataCorePointerBase<TPointerType> : IDataCorePointer, INotifyPropertyChanged
    {
        public DataCorePointerBase()
        {
            this.PropertyChanged += DataCorePointerBase_PropertyChanged;
        }

        private void DataCorePointerBase_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case "Instance":
                    OnPropertyChanged("InstanceObject");
                break;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

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

        private TPointerType _instance;
        public TPointerType Instance { get => _instance; set => SetProperty(ref _instance, value); }
        public IDataCoreStructure InstanceObject { get => Instance as IDataCoreStructure; set => Instance = (dynamic)value; }
    }
}
