using System;
using System.Globalization;
using System.Windows.Data;

namespace InvEntry.Converters;

public sealed class MaskedValueConverter : IValueConverter
{
    public int VisibleCharacters { get; set; } = 4;

    public object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        if (value is null)
            return string.Empty;

        var text = value.ToString();

        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var visible =
            VisibleCharacters;

        if (parameter is not null &&
            int.TryParse(
                parameter.ToString(),
                out var parameterValue))
        {
            visible = parameterValue;
        }

        if (text.Length <= visible)
            return text;

        var maskedLength =
            text.Length - visible;

        return
            new string('•', maskedLength) +
            text[^visible..];
    }

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}