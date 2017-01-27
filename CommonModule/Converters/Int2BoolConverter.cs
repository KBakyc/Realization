using System;
using System.Windows.Data;

namespace CommonModule.Converters
{
    public class Int2BoolConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int par = 0;
            bool ret = false;
            try
            {
                par = (int)value;
            }
            finally
            {
                ret = par == 0 ? false : true;
            }
            return ret;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int ret = 0;
            bool par = false;
            try
            {
                par = (bool)value;
            }
            finally
            {
                ret = par ? 1 : 0;
            }
            return ret;
        }

        #endregion
    }
}