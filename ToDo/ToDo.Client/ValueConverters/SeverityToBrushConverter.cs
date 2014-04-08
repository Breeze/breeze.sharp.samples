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
    public class SeverityToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var severity = (Severity)value;
            switch(severity)
            {
                case Severity.Info:         return Brushes.DodgerBlue;
                case Severity.Warning:      return Brushes.Orange;
                case Severity.Error:        return Brushes.Coral;
                default:                    return Brushes.DarkGray;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
