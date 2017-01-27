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

namespace InfoModule.Views
{
    /// <summary>
    /// Interaction logic for SalesJournalTypeDlgView.xaml
    /// </summary>
    public partial class SalesJournalTypeDlgView : UserControl
    {
        public SalesJournalTypeDlgView()
        {
            InitializeComponent();
        }

        private void DgJrns_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && (e.RemovedItems.Count == 0 || DgJrns.Items.Contains(e.RemovedItems[0])))
            {
                DgJrns.ScrollIntoView(e.AddedItems[0]);
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgJrns.SelectedItem != null)
            {
                Action mFocusToSelected = () =>
                {
                    DataGridRow row = (DataGridRow)DgJrns.ItemContainerGenerator.ContainerFromItem(DgJrns.SelectedItem);
                    row.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                };
                Dispatcher.BeginInvoke(mFocusToSelected, System.Windows.Threading.DispatcherPriority.Background, null);
            }
        }
    }
}
