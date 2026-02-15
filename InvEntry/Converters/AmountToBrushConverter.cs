using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Data;

namespace InvEntry.Converters
{
    public class PositiveAmountBrushConverter : IValueConverter
    {
        public Brush PositiveBrush { get; set; } = Brushes.Red;
        public Brush DefaultBrush { get; set; } = Brushes.Black;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return DefaultBrush;

            if (decimal.TryParse(value.ToString(), out var number))
                return number > 0 ? PositiveBrush : DefaultBrush;

            return DefaultBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

}