using DataCore2;
using System.Reflection;

namespace DataCoreBinary2.DatabaseFieldEditors
{
    /// <summary>
    /// Interaction logic for DatabaseBoolField.xaml
    /// </summary>
    internal partial class DatabaseBoolField : DatabaseFieldEditorBase
    {
        public bool Value
        {
            get => base.GetData<bool>();
            set => base.SetData(value);
        }

        public DatabaseBoolField(DataCoreDatabase database, object instance, PropertyInfo property, int arrayIndex = -1)
            : base(database, instance, property, arrayIndex)
        {
            Init();
        }

        void Init()
        {
            InitializeComponent();
            this.DataContext = this;

        }
    }
}
