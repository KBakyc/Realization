using System.Windows.Controls;
using CommonModule.Helpers;

namespace PredoplModule.Views
{
    /// <summary>
    /// Interaction logic for PredoplsView.xaml
    /// </summary>
    public partial class ClosePredoplsView : UserControl
    {
        public ClosePredoplsView()
        {
            InitializeComponent();
        }

        private void SfsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DgCloseSfs.RowDetailsVisibilityMode =
                            DgCloseSfs.RowDetailsVisibilityMode == DataGridRowDetailsVisibilityMode.Collapsed
                            ? DataGridRowDetailsVisibilityMode.VisibleWhenSelected
                            : DataGridRowDetailsVisibilityMode.Collapsed;
        }

        private void DgPredopls_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DgClosePredopls.RowDetailsVisibilityMode =
                DgClosePredopls.RowDetailsVisibilityMode == DataGridRowDetailsVisibilityMode.Collapsed
                ? DataGridRowDetailsVisibilityMode.VisibleWhenSelected
                : DataGridRowDetailsVisibilityMode.Collapsed;
        }

    }
}