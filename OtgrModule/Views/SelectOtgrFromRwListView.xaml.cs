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
    /// Interaction logic for SelectOtgrFromRwListView.xaml
    /// </summary>
    public partial class SelectOtgrFromRwListView : UserControl
    {
        public SelectOtgrFromRwListView()
        {
            InitializeComponent();
        }
        
        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DgOtgrRows.RowDetailsVisibilityMode =
                DgOtgrRows.RowDetailsVisibilityMode == DataGridRowDetailsVisibilityMode.Collapsed
                ? DataGridRowDetailsVisibilityMode.VisibleWhenSelected
                : DataGridRowDetailsVisibilityMode.Collapsed;
        }

        //private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        //{
        //    if (DataContext == null) return;          
        //    var dc = DataContext as SelectOtgrFromRwListViewModel;
        //    if (dc == null) return;
        //    else dc.ChangeFilter();
        //}

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext == null) return;
            var dc = DataContext as SelectOtgrFromRwListViewModel;
            if (dc == null) return;
            else dc.ChangeFilter();
        }
    }
}
