using System;
using System.Windows.Data;
using System.Windows;

namespace CommonModule.Converters
{
    public class Int2VisibilityConverter : IValueConverter
    {
        #region IValueConverter Members

        public int CheckedValue { get; set; }
        public Visibility EqualVisibility { get; set; }
        public Visibility DefaultVisibility { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int par = 0;
            Visibility ret;
            try
            {
                par = System.Convert.ToInt32(value);
            }
            finally
            {
                ret = par == CheckedValue ? EqualVisibility : DefaultVisibility;
            }
            return ret;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int ret = 0;
            Visibility par = DefaultVisibility;
            try
            {
                par = (Visibility)value;
            }
            finally
            {
                ret = (int)par;
            }
            return ret;
        }

        #endregion
    }
}