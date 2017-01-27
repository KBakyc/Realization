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

namespace RwModule.Views
{
    /// <summary>
    /// Interaction logic for RwBuhSchetsDlgView.xaml
    /// </summary>
    public partial class RwBuhSchetsDlgView : UserControl
    {
        public RwBuhSchetsDlgView()
        {
            InitializeComponent();
        }

        private void DgSchets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && (e.RemovedItems.Count == 0 || DgRwBuhSchets.Items.Contains(e.RemovedItems[0])))
            {
                DgRwBuhSchets.ScrollIntoView(e.AddedItems[0]);
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgRwBuhSchets.SelectedItem != null)
            {
                Action mFocusToSelected = () =>
                {
                    DataGridRow row = (DataGridRow)DgRwBuhSchets.ItemContainerGenerator.ContainerFromItem(DgRwBuhSchets.SelectedItem);
                    row.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                };
                Dispatcher.BeginInvoke(mFocusToSelected, System.Windows.Threading.DispatcherPriority.Background, null);
            }
        }

    }
}
