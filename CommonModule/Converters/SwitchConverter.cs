using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows;
using System.Globalization;

namespace CommonModule.Converters
{
    [ContentProperty("Cases")]
    public class SwitchConverter : DependencyObject, ISwitchConverter, ICompositeConverter
    {
        public static readonly DependencyProperty DefaultProperty = DependencyProperty.Register(
            "Default", typeof(object), typeof(SwitchConverter), new PropertyMetadata(CaseSet.UndefinedObject));

        public SwitchConverter()
        {
            Cases = new CaseSet();
        }

        public object Default
        {
            get { return GetValue(DefaultProperty); }
            set { SetValue(DefaultProperty, value); }
        }

        public CaseSet Cases { get; private set; }

        public bool TypeMode { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (TypeMode) value = value == null ? null : value.GetType();
            var pair = Cases.FirstOrDefault(p => Equals(p.Key, value) || SafeCompareAsStrings(p.Key, value));
            var result = pair == null ? Default : pair.Value;
            value = result == CaseSet.UndefinedObject ? value : result;
            return PostConverter == null
                ? value
                : PostConverter.Convert(value, targetType, PostConverterParameter, culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (TypeMode) value = value == null ? null : value.GetType();
            var pair = Cases.FirstOrDefault(p => Equals(p.Value, value) || SafeCompareAsStrings(p.Value, value));
            value = pair == null ? Default : pair.Key;
            return PostConverter == null
                ? value
                : PostConverter.ConvertBack(value, targetType, PostConverterParameter, culture);
        }

        private static bool SafeCompareAsStrings(object a, object b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            return string.Compare(a.ToString(), b.ToString(), StringComparison.OrdinalIgnoreCase) == 0;
        }

        public IValueConverter PostConverter { get; set; }
        public object PostConverterParameter { get; set; }
    }
}
