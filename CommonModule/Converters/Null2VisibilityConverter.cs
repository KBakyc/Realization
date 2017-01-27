using System;
using System.Windows;
using System.Windows.Data;

namespace CommonModule.Converters
{
    public class Null2VisibilityConverter : IValueConverter
    {
        #region IValueConverter Members

        public Visibility NotnullVisibility { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Visibility nullVisibility = Visibility.Hidden;

            if (parameter != null) 
                if (parameter is Visibility)
                    nullVisibility = (Visibility)parameter;
                else
                {
                    try
                    {
                        nullVisibility = (Visibility)Enum.Parse(typeof(Visibility), parameter.ToString());                    
                    }
                    catch{}
                }

            if (nullVisibility == Visibility.Visible && NotnullVisibility == Visibility.Visible)
                NotnullVisibility = Visibility.Hidden;


            return value==null?nullVisibility:NotnullVisibility;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}