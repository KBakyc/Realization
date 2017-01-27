using System.Windows.Controls;

namespace CommonModule.Views
{
    /// <summary>
    /// Interaction logic for SfSeekView.xaml
    /// </summary>
    public partial class NumDlgView : UserControl
    {
        public NumDlgView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            var dc = DataContext as CommonModule.ViewModels.NumDlgViewModel;
            if (dc != null && dc.IsSelectAll)
                tbNum.SelectAll();
        }
    }
}