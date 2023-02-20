using System;
using System.Globalization;
using System.Reflection.Metadata;
using System.Windows;
using System.Windows.Data;

namespace Client.Infrastructure;

public class InvertableBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool valueBool)
        { if (parameter is true)
                return valueBool ? Visibility.Collapsed : Visibility.Visible;
            else
                return valueBool ? Visibility.Visible : Visibility.Collapsed;
        }

        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}