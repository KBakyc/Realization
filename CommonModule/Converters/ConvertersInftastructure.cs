using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;

namespace CommonModule.Converters
{
    public class CaseSet : List<ICase>
    {
        public static readonly object UndefinedObject = new object();
    }

    public interface ICase
    {
        object Key { get; set; }
        object Value { get; set; }
        Type KeyType { get; set; }
    }

    public class Case : ICase
    {
        public object Key { get; set; }
        public object Value { get; set; }
        public Type KeyType { get; set; }
    }

    public interface ISwitchConverter : IValueConverter
    {
        CaseSet Cases { get; }
        object Default { get; set; }
        bool TypeMode { get; set; }
    }

    public interface ICompositeConverter : IValueConverter
    {
        IValueConverter PostConverter { get; set; }
        object PostConverterParameter { get; set; }
    }

    public class ConverterEventArgs : EventArgs
    {
        public object ConvertedValue { get; set; }
        public object Value { get; private set; }
        public Type TargetType { get; private set; }
        public object Parameter { get; private set; }
        public CultureInfo Culture { get; private set; }

        public ConverterEventArgs(object value, Type targetType, object parameter, CultureInfo culture)
        {
            TargetType = targetType;
            Parameter = parameter;
            Culture = culture;
            Value = value;
        }
    }

    public interface IInlineConverter : IValueConverter
    {
        event EventHandler<ConverterEventArgs> Converting;
        event EventHandler<ConverterEventArgs> ConvertingBack;
    }


}
