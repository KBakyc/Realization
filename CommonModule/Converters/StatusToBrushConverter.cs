using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using DataObjects;
using System.Windows.Media;

namespace CommonModule.Converters
{
    public class StatusToBrushConverter : IValueConverter
    {
        public Brush NullStatusBrush { get; set; }
        public Brush GreenStatusBrush { get; set; }
        public Brush YellowStatusBrush { get; set; }
        public Brush RedStatusBrush { get; set; }

        public int NullStatus { get; set; }
        public int MaxGreenStatus { get; set; }
        public int MaxYellowStatus { get; set; }

        public StatusToBrushConverter()
        {
            NullStatusBrush = Brushes.Transparent;
            GreenStatusBrush = Brushes.YellowGreen;
            YellowStatusBrush = Brushes.Gold;
            RedStatusBrush = Brushes.Red;
            NullStatus = 0;
            MaxGreenStatus = 49;
            MaxYellowStatus = 99;
        }

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int par = 0;
            Brush res;
            try
            {
                par = System.Convert.ToInt32(value);
            }
            finally
            {
                if (par > MaxYellowStatus) res = RedStatusBrush;
                else
                    if (par > MaxGreenStatus) res = YellowStatusBrush;
                    else
                        if (par > NullStatus) res = GreenStatusBrush;
                        else
                            res = NullStatusBrush;
            }
            return res;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
