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
    public partial class KursesListView : UserControl
    {
        public KursesListView()
        {
            InitializeComponent();
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && (e.RemovedItems.Count == 0 || dgKurses.Items.Contains(e.RemovedItems[0])))
            {
                dgKurses.ScrollIntoView(e.AddedItems[0]);
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (dgKurses.SelectedItem != null)
            {
                Action mFocusToSelected = () =>
                    {
                        DataGridRow row = (DataGridRow)dgKurses.ItemContainerGenerator.ContainerFromItem(dgKurses.SelectedItem);
                        row.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    };
                Dispatcher.BeginInvoke(mFocusToSelected, System.Windows.Threading.DispatcherPriority.Background, null);
            }
        }
    }
}
