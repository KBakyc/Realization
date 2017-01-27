using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using CommonModule.Helpers;

namespace CommonModule.Converters
{
    public class EnumToDescriptionConverter : IValueConverter
    {

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value is Enum ? Enumerations.GetEnumDescription(value as Enum) : value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
