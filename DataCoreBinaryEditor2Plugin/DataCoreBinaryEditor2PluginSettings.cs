using Crucible;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace DataCoreBinary2
{
    [Serializable()]
    public class DataCoreBinaryEditor2PluginSettings : INotifyPropertyChanged
    {
        public DataCoreBinaryEditor2PluginSettings()
        {

        }

        public DataCoreBinaryEditor2PluginSettings(bool enablePropertyChanged)
        {
            if(enablePropertyChanged)
            {
                PropertyChanged += DataCoreBinaryEditor2PluginSettings_PropertyChanged;
            }
        }

        private static string SettingsFilepath => Path.Combine(CrucibleApplication.ApplicationDirectory, "datacorebinaryeditor2settings.xml");
        public static DataCoreBinaryEditor2PluginSettings Settings = LoadDataCoreBinaryEditor2PluginSettings();

        private bool _UseDatabaseCache = true;
        [XmlElement]
        public bool UseDatabaseCache { get => _UseDatabaseCache; set => _UseDatabaseCache = SetProperty(ref _UseDatabaseCache, value); }

        private void Save()
        {
            CrucibleUtil.WriteToXmlFile<DataCoreBinaryEditor2PluginSettings>(SettingsFilepath, this);
        }

        private void DataCoreBinaryEditor2PluginSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Save();
        }

        private static DataCoreBinaryEditor2PluginSettings LoadDataCoreBinaryEditor2PluginSettings()
        {
            var settings = CrucibleUtil.ReadFromXmlFile<DataCoreBinaryEditor2PluginSettings>(SettingsFilepath, false);

            if(settings != null)
            {
                settings.PropertyChanged += settings.DataCoreBinaryEditor2PluginSettings_PropertyChanged;
            }
            else
            {
                settings = new DataCoreBinaryEditor2PluginSettings(true);
            }

            return settings;
        }

        #region INotifyPropertyChanged

        /// <summary>
        ///     Multicast event for property change notifications.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        ///     Checks if a property already matches a desired value.  Sets the property and
        ///     notifies listeners only when necessary.
        /// </summary>
        /// <typeparam name="T">Type of the property.</typeparam>
        /// <param name="storage">Reference to a property with both getter and setter.</param>
        /// <param name="value">Desired value for the property.</param>
        /// <param name="propertyName">
        ///     Name of the property used to notify listeners.  This
        ///     value is optional and can be provided automatically when invoked from compilers that
        ///     support CallerMemberName.
        /// </param>
        /// <returns>
        ///     True if the value was changed, false if the existing value matched the
        ///     desired value.
        /// </returns>
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

        /// <summary>
        ///     Notifies listeners that a property value has changed.
        /// </summary>
        /// <param name="propertyName">
        ///     Name of the property used to notify listeners.  This
        ///     value is optional and can be provided automatically when invoked from compilers
        ///     that support <see cref="CallerMemberNameAttribute" />.
        /// </param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
