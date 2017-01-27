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

namespace ServiceModule.Views
{
    /// <summary>
    /// Interaction logic for UserAdminView.xaml
    /// </summary>
    public partial class UserAdminView : UserControl
    {
        public UserAdminView()
        {
            InitializeComponent();
        }

        private void ListBox_Loaded(object sender, RoutedEventArgs e)
        {
            var lb = sender as ListBox;
            if (lb == null) return;
            lb.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("IsOnline", System.ComponentModel.ListSortDirection.Descending));
            lb.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("Id", System.ComponentModel.ListSortDirection.Ascending));
        }
    }
}
