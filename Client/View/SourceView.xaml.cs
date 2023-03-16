using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Client.View
{
    /// <summary>
    /// Interaction logic for SourceView.xaml
    /// </summary>
    public partial class SourceView : UserControl
    {
        public SourceView()
        {
            InitializeComponent();
        }

        private void AvEditor_OnPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetData(typeof(string)) is string stringValue)
            {
                try
                {
                    var result = JObject.Parse(stringValue);
                    result.ToString(Formatting.Indented);
                    DataObject obj = new DataObject();
                    obj.SetData(typeof(string), obj);
                    e.DataObject = obj;
                }
                catch (JsonException ex)
                {
                    Debug.WriteLine(ex);
                }
            }
        }
    }
}
