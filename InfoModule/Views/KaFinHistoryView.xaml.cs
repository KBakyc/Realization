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
using System.ComponentModel;
using CommonModule.Helpers;

namespace InfoModule.Views
{
    /// <summary>
    /// Interaction logic for KaFinHistoryView.xaml
    /// </summary>
    public partial class KaFinHistoryView : UserControl
    {
        public KaFinHistoryView()
        {
            InitializeComponent();
        }

        //private void UserControl_Loaded(object sender, RoutedEventArgs e)
        //{
        //    DataGridHelper.LoadSortDescr(FinsostSfs);
        //    DataGridHelper.LoadSortDescr(FinsostPreds);
        //    PanelsHelper.LoadGridColumns(FinsostGrid);
        //    PanelsHelper.LoadGridRows(FinsostSfsGrid);
        //}

        //private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        //{
        //    DataGridHelper.SaveSortDescr(FinsostSfs);
        //    DataGridHelper.SaveSortDescr(FinsostPreds);
        //    PanelsHelper.SaveGridColumns(FinsostGrid);
        //    PanelsHelper.SaveGridRows(FinsostSfsGrid);
        //}

        private void cbShowSfDetails_Checked(object sender, RoutedEventArgs e)
        {
            var conv = new GridLengthConverter();
            var newHeight = (GridLength)conv.ConvertFromString("*");
            FinsostSfsGrid.RowDefinitions[0].Height = newHeight;

            if (cbShowSfDetails.IsChecked ?? false)
                FinsostSfsGrid.RowDefinitions[2].Height = newHeight;
            else
                FinsostSfsGrid.RowDefinitions[2].Height = (GridLength)conv.ConvertFromString("Auto");
        }

    }
}
