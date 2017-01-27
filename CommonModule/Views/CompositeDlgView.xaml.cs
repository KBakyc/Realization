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
    /// Interaction logic for MsgDlgView.xaml
    /// </summary>
    public partial class CompositeDlgView : UserControl
    {
        public CompositeDlgView()
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
    }
}
