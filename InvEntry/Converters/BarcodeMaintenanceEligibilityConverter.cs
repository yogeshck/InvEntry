using System;
using System.Globalization;
using System.Windows.Data;
using InvEntry.Models;
using InvEntry.ViewModels;

namespace InvEntry.Converters;

public sealed class BarcodeMaintenanceEligibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        BarcodeMaintenanceEligibilityEvaluator.Evaluate(value as ProductStock).DisplayText;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
