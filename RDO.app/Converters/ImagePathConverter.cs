using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;

namespace RDO.App.Converters;

public class ImagePathConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not string s || string.IsNullOrEmpty(s)) return null;
        if (s.StartsWith("http://") || s.StartsWith("https://"))
            return new BitmapImage(new Uri(s));
        if (s.StartsWith(@"\\"))
            return new BitmapImage(new Uri("file:" + s.Replace('\\', '/')));
        if (File.Exists(s))
            return new BitmapImage(new Uri(s));
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}