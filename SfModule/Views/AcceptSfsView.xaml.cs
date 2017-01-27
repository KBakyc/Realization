using System.Windows.Controls;
using System.Windows.Data;
using SfModule.ViewModels;
using CommonModule.Helpers;

namespace SfModule.Views
{
    /// <summary>
    /// Interaction logic for r635View.xaml
    /// </summary>
    public partial class AcceptSfsView : UserControl
    {
        public AcceptSfsView()
        {
            InitializeComponent();
        }

        private void DataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SfListGrid.RowDetailsVisibilityMode =
                            SfListGrid.RowDetailsVisibilityMode == DataGridRowDetailsVisibilityMode.Collapsed
                            ? DataGridRowDetailsVisibilityMode.VisibleWhenSelected
                            : DataGridRowDetailsVisibilityMode.Collapsed;
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            this.SetLoaded(true);
        }

        private void SfListGrid_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case System.Windows.Input.Key.Space:
                    AcceptSfsViewModel model = DataContext as AcceptSfsViewModel;
                    if (model != null)
                    {
                        if (model.SelectedSf != null)
                            model.SelectedSf.IsSelected = !model.SelectedSf.IsSelected;
                    }
                    break;
            }
        }

        private void UserControl_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null && e.OldValue != null)
                this.SetLoaded(true);
            else if (e.NewValue == null && e.OldValue != null)
                this.SetLoaded(e.OldValue as CommonModule.Interfaces.ILoaded, false);
        }
    }
}