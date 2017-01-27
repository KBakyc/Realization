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
using OtgrModule.ViewModels;

namespace OtgrModule.Views
{
    /// <summary>
    /// Interaction logic for OtgrView.xaml
    /// </summary>
    public partial class p623View : UserControl
    {
        public p623View()
        {
            InitializeComponent();
        }

        private void DataGrid_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Space:
                    p623ViewModel model = DataContext as p623ViewModel;
                    if (model != null)
                    {
                        if (model.SelectedOtgr != null)
                        {
                            model.SelectedOtgr.IsChecked = !model.SelectedOtgr.IsChecked;
                            model.OnCheckItemChangeCommand.Execute(null);
                        }
                    }
                    break;
            }
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            P623DgOtgrRows.RowDetailsVisibilityMode =
                P623DgOtgrRows.RowDetailsVisibilityMode == DataGridRowDetailsVisibilityMode.Collapsed 
                ? DataGridRowDetailsVisibilityMode.VisibleWhenSelected 
                : DataGridRowDetailsVisibilityMode.Collapsed;
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext == null) return;
            var dc = DataContext as p623ViewModel;
            if (dc == null) return;
            else dc.ChangeFilter();
        }

        private void ComboBox_DropDownClosed(object sender, EventArgs e)
        {
            if (DataContext == null) return;
            var dc = DataContext as p623ViewModel;
            if (dc == null) return;
            if (dc.IsNeedRefresh)
                dc.RefreshView();
        }
    }
}