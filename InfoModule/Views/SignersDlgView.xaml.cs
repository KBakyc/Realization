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

namespace InfoModule.Views
{
    /// <summary>
    /// Interaction logic for PredoplSchetsDlgView.xaml
    /// </summary>
    public partial class SignersDlgView : UserControl
    {
        public SignersDlgView()
        {
            InitializeComponent();
        }

        private void DgSigners_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && (e.RemovedItems.Count == 0 || DgSigners.Items.Contains(e.RemovedItems[0])))
            {
                DgSigners.ScrollIntoView(e.AddedItems[0]);
            }
        }

    }
}
