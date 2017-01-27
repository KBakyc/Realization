using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace CommonModule.Converters
{
    public class BoolNotConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            object ret;
            try
            {
                ret = !(bool)value;
            }
            catch 
            {
                ret = value;
            }
            return ret;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            object ret;
            try
            {
                ret = !(bool)value;
            }
            catch
            {
                ret = value;
            }
            return ret;            
        }

        #endregion
    }
}
