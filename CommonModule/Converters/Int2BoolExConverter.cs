using System;
using System.Windows.Data;

namespace CommonModule.Converters
{
    public class Int2BoolExConverter : IValueConverter
    {
        #region IValueConverter Members

        public bool CheckForNotEqual { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int par = 1;
            int val = 0;
            bool ret = false;
            try
            {
                val = (int)value;
                string spar = parameter as string;
                if (!String.IsNullOrEmpty(spar))
                    par = int.Parse(spar);
            }
            finally
            {
                ret = val == par ? !CheckForNotEqual 
                                 : CheckForNotEqual;
            }
            return ret;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int ret = 1;
            bool par = false;
            try
            {
                par = (bool)value;
                string sret = parameter as string;
                if (!String.IsNullOrEmpty(sret))
                    ret = int.Parse(sret);
            }
            finally
            {
                ret = par && !CheckForNotEqual ? ret : 0;
            }
            return ret;
        }

        #endregion
    }
}