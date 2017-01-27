using System.Windows.Controls;
using System.Windows.Data;
using SfModule.ViewModels;
using CommonModule.Interfaces;
using CommonModule.Helpers;



namespace SfModule.Views
{
    /// <summary>
    /// Interaction logic for r635View.xaml
    /// </summary>
    public partial class r635View : UserControl
    {
        public r635View()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext != null)
            {
                var vm = DataContext as R635ViewModel;
                if (vm != null)
                    vm.ChangeFilter();
            }
        }
    }
}