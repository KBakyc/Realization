using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Markup;
using System.Windows.Data;
using System.Globalization;

namespace CommonModule.Converters
{
    public class EmptyStringConverter : MarkupExtension, IValueConverter
    {
        public EmptyStringConverter()
        {}

        public object Convert(object value, Type targetType,
                              object parameter, CultureInfo culture)
        {
            return value == null || string.IsNullOrWhiteSpace(value.ToString()) ? parameter : value;
        }

        public object ConvertBack(object value, Type targetType,
                                  object parameter, CultureInfo culture)
        {
            return value == null || string.IsNullOrWhiteSpace(value.ToString()) ? parameter : value;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}
