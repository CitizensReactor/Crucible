using Crucible;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace Crucible
{
    [Serializable()]
    public class CrucibleSettings : INotifyPropertyChanged
    {
        public CrucibleSettings()
        {

        }

        public CrucibleSettings(bool enablePropertyChanged)
        {
            if(enablePropertyChanged)
            {
                PropertyChanged += CrucibleSettings_PropertyChanged;
            }
        }

        private static string SettingsFilepath => Path.Combine(CrucibleApplication.ApplicationDirectory, "settings.xml");
        public static CrucibleSettings Settings = LoadCrucibleSettings();

        private bool _StartReadOnly = true;
        [XmlElement]
        public bool StartReadOnly { get => _StartReadOnly; set => _StartReadOnly = SetProperty(ref _StartReadOnly, value); }

        private void Save()
        {
            CrucibleUtil.WriteToXmlFile<CrucibleSettings>(SettingsFilepath, this);
        }

        private void CrucibleSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Save();
        }

        private static CrucibleSettings LoadCrucibleSettings()
        {
            var settings = CrucibleUtil.ReadFromXmlFile<CrucibleSettings>(SettingsFilepath, false);

            if(settings != null)
            {
                settings.PropertyChanged += settings.CrucibleSettings_PropertyChanged;
            }
            else
            {
                settings = new CrucibleSettings(true);
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
