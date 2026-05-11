using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace RDO.App.Converters;

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is null || (value is string s && string.IsNullOrEmpty(s))
            ? Visibility.Visible
            : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
