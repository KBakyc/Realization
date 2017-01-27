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
using SfModule.ViewModels;

namespace SfModule.Views
{
    /// <summary>
    /// Interaction logic for dlg_OtgrDocSelect.xaml
    /// </summary>
    public partial class CorrSf2OtgrDocsView : UserControl
    {
        public CorrSf2OtgrDocsView()
        {
            InitializeComponent();
        }

        private void DataGrid_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Space:
                    Corrsf2OtgrDocsViewModel model = DataContext as Corrsf2OtgrDocsViewModel;
                    if (model != null)
                    {
                        if (model.SelectedOtgr != null)
                            model.SelectedOtgr.IsSelected = !model.SelectedOtgr.IsSelected;
                    }
                    break;
            }
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DgOtgrRows.RowDetailsVisibilityMode =
                DgOtgrRows.RowDetailsVisibilityMode == DataGridRowDetailsVisibilityMode.Collapsed
                ? DataGridRowDetailsVisibilityMode.VisibleWhenSelected
                : DataGridRowDetailsVisibilityMode.Collapsed;
        }

    }
}
