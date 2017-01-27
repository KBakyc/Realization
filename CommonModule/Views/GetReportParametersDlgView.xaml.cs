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

namespace CommonModule.Views
{
    /// <summary>
    /// Interaction logic for GetReportParametersDlgView.xaml
    /// </summary>
    public partial class GetReportParametersDlgView : UserControl
    {
        public GetReportParametersDlgView()
        {
            InitializeComponent();
            this.dlgRoot.AddHandler(UIElement.KeyUpEvent, new KeyEventHandler(dlgRoot_KeyUp));
        }

        private void dlgRoot_KeyUp(Object sender, KeyEventArgs e)
        {
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

        private void TextBox_Loaded(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            tb.SelectAll();
        }

        // при смене IsEnabled сбрасывается текущий фокус. Далее мой метод его восстановления.
        //private UIElement savedFocusedElement;

        //private void Border_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        //{
        //    bool old = (bool)(e.OldValue);
        //    if (old == true)
        //        savedFocusedElement = Keyboard.FocusedElement as UIElement;
        //    else
        //    {
        //        Dispatcher.BeginInvoke(new Action(() =>
        //        {
        //            Keyboard.Focus(savedFocusedElement);
        //            savedFocusedElement = null;
        //        }), System.Windows.Threading.DispatcherPriority.Background, null);
        //    }
        //}
    }
}
