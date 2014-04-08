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
    public class DoneToStrikeoutConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var isDone = (value is bool && (bool)value);
            if (targetType.IsAssignableFrom(typeof(TextDecorationCollection)))
            {
                return (isDone) ? new TextDecorationCollection(TextDecorations.Strikethrough) : new TextDecorationCollection();
            }
            throw new InvalidOperationException("Cannot apply DoneToStrikeoutConverter to property of type " + targetType.Name);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
