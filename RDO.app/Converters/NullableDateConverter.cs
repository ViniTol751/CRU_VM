using Microsoft.UI.Xaml.Data;
using System;

namespace RDO.App.Converters;

public class NullableDateConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is DateTime dt)
            return dt.ToString("dd/MM/yyyy");

        if (value is DateTimeOffset dto)
            return dto.ToString("dd/MM/yyyy");

        return "—";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}