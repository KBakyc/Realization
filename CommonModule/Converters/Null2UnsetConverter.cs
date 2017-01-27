using System;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows;

namespace CommonModule.Converters
{
    public class Null2UnsetConverter : MarkupExtension, IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value ?? DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value == DependencyProperty.UnsetValue ? null : value;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        #endregion
    }
}