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
using Microsoft.Reporting.WinForms;
using CommonModule.ViewModels;
using CommonModule.Commands;
using CommonModule.Helpers;
using DataObjects;

namespace CommonModule.Views
{
    /// <summary>
    /// Interaction logic for ReportView.xaml
    /// </summary>
    public partial class HtmlPageView : UserControl
    {
        CommonModule.Interfaces.IModule module;
        MsgDlgViewModel waitdlg;

        public HtmlPageView()
        {
            InitializeComponent();
        }

        private void Frame_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            if (DataContext == null) return;
            if (module == null)
            {
                var vm = DataContext as BasicModuleContent;
                if (vm == null || vm.Parent == null) return;
                module = vm.Parent;
            }
            if (waitdlg == null)
                waitdlg = new MsgDlgViewModel { Title = "Подождите", Message = "Загрузка содержимого" };
            module.OpenDialog(waitdlg);
        }

        private void Frame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (waitdlg == null) return;
            else module.CloseDialog(waitdlg);
        }

        private void Frame_NavigationStopped(object sender, NavigationEventArgs e)
        {
            if (waitdlg == null) return;
            else module.CloseDialog(waitdlg);
        }

        private void Frame_LoadCompleted(object sender, NavigationEventArgs e)
        {
            if (waitdlg == null) return;
            else module.CloseDialog(waitdlg);
        }
    }
}
