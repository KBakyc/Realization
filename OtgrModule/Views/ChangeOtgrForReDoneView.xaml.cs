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
    /// Interaction logic for SelectableOtgrListView.xaml
    /// </summary>
    public partial class ChangeOtgrForReDoneView : UserControl
    {
        public ChangeOtgrForReDoneView()
        {
            InitializeComponent();
        }
        
        //private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        //{
        //    DgOtgrRows.RowDetailsVisibilityMode =
        //        DgOtgrRows.RowDetailsVisibilityMode == Microsoft.Windows.Controls.DataGridRowDetailsVisibilityMode.Collapsed
        //        ? Microsoft.Windows.Controls.DataGridRowDetailsVisibilityMode.VisibleWhenSelected
        //        : Microsoft.Windows.Controls.DataGridRowDetailsVisibilityMode.Collapsed;
        //}

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext == null) return;
            var dc = DataContext as ChangeOtgrForReDoneViewModel;
            if (dc == null) return;
            else dc.ChangeFilter();
        }
    }
}
