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
using CommonModule.Helpers;

namespace CommonModule.Views
{
    /// <summary>
    /// Interaction logic for MsgDlgView.xaml
    /// </summary>
    public partial class DogListView : UserControl
    {
        public DogListView()
        {
            InitializeComponent();
        }

        //private void UserControl_Loaded(object sender, RoutedEventArgs e)
        //{
        //    dgDogs.SetSortInfo(
        //        new System.ComponentModel.SortDescription("ModelRef.NaiOsn", System.ComponentModel.ListSortDirection.Ascending),
        //        new System.ComponentModel.SortDescription("ModelRef.DopOsn", System.ComponentModel.ListSortDirection.Ascending)
        //        );
        //}
    }
}
