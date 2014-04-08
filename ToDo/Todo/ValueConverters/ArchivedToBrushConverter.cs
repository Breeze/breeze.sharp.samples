using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows.Controls;

namespace Todo_Net
{
    public class ArchivedToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var isArchived = (value is bool && (bool)value);
            if (targetType.IsAssignableFrom(typeof(System.Windows.Media.Brush)))
            {
                return (isArchived) ? Brushes.Goldenrod : Brushes.Black;
            }
            throw new InvalidOperationException("Cannot apply ArchivedToBrushConverter to property of type " + targetType.Name);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
