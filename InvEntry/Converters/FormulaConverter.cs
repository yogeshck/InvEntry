using InvEntry.Store;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace InvEntry.Converters
{
    public class FormulaConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if(values is null || values.Length < 2) return null;

            if (values[0] is string key && values[1] is string fieldName)
            {
                var formula = FormulaStore.Instance.GetFormula(key, fieldName);
                return formula.Expression;
            }

            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
