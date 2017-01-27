using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace CommonModule.Converters
{
    public class Bool2VisibilityConverter : MarkupExtension, IValueConverter
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        #region IValueConverter Members

        public bool HiddenState { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool par = false;
            Visibility ret;
            Visibility hid = parameter as String == null || !((String)parameter).StartsWith("Collapse", true, System.Globalization.CultureInfo.InvariantCulture) ? 
                Visibility.Hidden : Visibility.Collapsed;
            try
            {
                par = (bool)value;
            }
            finally
            {
                ret = par==HiddenState ? hid : Visibility.Visible;
            }
            return ret;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool ret = false;
            Visibility par = Visibility.Hidden;
            try
            {
                par = (Visibility)value;
            }
            finally
            {
                ret = (par==Visibility.Visible) ? !HiddenState : HiddenState;
            }
            return ret;
        }

        #endregion
    }
}