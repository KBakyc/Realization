using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace CommonModule.Converters
{
    public class IsHolydayConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var dt = (DateTime)values[0];
            var dates = values[1] as Dictionary<DateTime, bool>;
            bool res;
            if (dates != null && dates.TryGetValue(dt, out res))
                return res;
            else
                return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
