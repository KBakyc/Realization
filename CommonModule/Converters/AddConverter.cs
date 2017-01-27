using System;
using System.Windows.Data;
using System.Windows.Markup;

namespace CommonModule.Converters
{
    public class AddConverter : MarkupExtension, IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            decimal obj = 0;
            decimal par = 0;
            if (parameter == null) return value;
            if (value != null)
                decimal.TryParse(value.ToString(), out obj);
            if (decimal.TryParse(parameter.ToString(), out par))
                obj += par;
            object res;
            try
            {
                res = System.Convert.ChangeType(obj, value.GetType());
            }
            catch
            {
                res = value;
            }

            return res;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int ret = 0;
            return ret;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        #endregion
    }
}