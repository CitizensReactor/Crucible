using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DataCoreBinary2.FieldEditors
{
    /// <summary>
    /// Interaction logic for UnsupportedField.xaml
    /// </summary>
    public partial class UnsupportedField : UserControl
    {
        public PropertyInfo PropertyInfo { get; set; }

        public UnsupportedField(PropertyInfo propertyInfo)
        {
            PropertyInfo = propertyInfo;
            InitializeComponent();
        }
    }
}
