using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace NetworkService.Views
{
    public partial class NetworkEntitiesView : UserControl
    {
        public NetworkEntitiesView()
        {
            InitializeComponent();
        }
    }

    public class NullToNAConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "N/A";
            return ((double)value).ToString("F1");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
