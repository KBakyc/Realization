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

namespace PredoplModule.Views
{
    /// <summary>
    /// Interaction logic for PredoplSchetsDlgView.xaml
    /// </summary>
    public partial class PredoplSchetsDlgView : UserControl
    {
        public PredoplSchetsDlgView()
        {
            InitializeComponent();
        }

        private void DgSchets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && (e.RemovedItems.Count == 0 || DgSchets.Items.Contains(e.RemovedItems[0])))
            {               
                DgSchets.ScrollIntoView(e.AddedItems[0]);
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgSchets.SelectedItem != null)
            {
                DgSchets.ScrollIntoView(DgSchets.SelectedItem);
                Action mFocusToSelected = () =>
                {
                    DataGridRow row = (DataGridRow)DgSchets.ItemContainerGenerator.ContainerFromItem(DgSchets.SelectedItem);
                    if (row != null)
                        row.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                };
                Dispatcher.BeginInvoke(mFocusToSelected, System.Windows.Threading.DispatcherPriority.Background, null);
            }
        }

    }
}
