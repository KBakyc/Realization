using System.Windows.Controls;
using System.Windows.Input;
using PredoplModule.ViewModels;

namespace PredoplModule.Views
{
    /// <summary>
    /// Interaction logic for PredoplsView.xaml
    /// </summary>
    public partial class dlg_SelKaWithDebts : UserControl
    {
        public dlg_SelKaWithDebts()
        {
            InitializeComponent();
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var dc = DataContext as SelKaWithDebtsDlgViewModel;
            if (dc != null && dc.SubmitCommand != null && dc.SubmitCommand.CanExecute(null))
                dc.SubmitCommand.Execute(null);
        }
    }
}