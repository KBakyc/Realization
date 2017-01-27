using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Globalization;

namespace RwModule.Views
{

    public partial class EditRwPlatDlgView : UserControl
    {
        public EditRwPlatDlgView()
        {
            InitializeComponent();
            this.dlgRoot.AddHandler(UIElement.KeyUpEvent, new KeyEventHandler(dlgRoot_KeyUp));
        }

        private void dlgRoot_KeyUp(Object sender, KeyEventArgs e)
        {
            if (Keyboard.FocusedElement is TextBox && ((TextBox)Keyboard.FocusedElement).AcceptsReturn == true) return;
            if (e.Key == Key.Return)
            {
                UIElement focusedElement = Keyboard.FocusedElement as UIElement;
                if (focusedElement != null)
                {
                    focusedElement.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                }
                e.Handled = true;
            }
        }

        private void DatePicker_DateValidationError(object sender, DatePickerDateValidationErrorEventArgs e)
        {
            DatePicker dp = sender as DatePicker;
            DateTime dt;

            if (DateTime.TryParseExact(e.Text, "ddMMyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt)
                || DateTime.TryParseExact(e.Text, "ddMMyyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
            {
                dp.SelectedDate = dt;
                e.ThrowException = false;
            }
        }
    }
}
