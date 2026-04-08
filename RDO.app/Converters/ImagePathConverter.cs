using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;

namespace RDO.App.Converters;

public class ImagePathConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string path && !string.IsNullOrEmpty(path) && File.Exists(path))
            return new BitmapImage(new Uri(path));
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}