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
    public partial class ReportView : UserControl
    {
        private ReportViewer viewer;
        private ReportViewModel model;

        public ReportView()
        {
            InitializeComponent();
        }

        private void InitReportViewer()
        {
            viewer.Messages = new ReportViewerMessages();            
            if (model.Mode == ReportModes.Server)
            {
                viewer.ServerReport.ReportServerUrl = model.ReportServerUri;
                viewer.ProcessingMode = ProcessingMode.Remote;
                viewer.ServerReport.ReportPath = model.ReportPath;
                if (model.ReportParameters != null)
                    try
                    {
                        viewer.ServerReport.SetParameters(model.ReportParameters);
                    }
                    catch (Exception e)
                    {
                        WorkFlowHelper.OnCrash(e);
                    }
            }
            else
            {
                viewer.ProcessingMode = ProcessingMode.Local;
                viewer.LocalReport.ReportPath = model.ReportPath;
                if (model.DataSources != null && model.DataSources.Length > 0)
                    for (int i = 0; i < model.DataSources.Length; i++)
                        viewer.LocalReport.DataSources
                            .Add(model.DataSources[i]);
                if (model.ReportParameters != null)
                    viewer.LocalReport.SetParameters(model.ReportParameters);
            }

            viewer.SetDisplayMode(DisplayMode.PrintLayout);
            viewer.ZoomMode = model.Zoom;
            viewer.ZoomChange += new ZoomChangedEventHandler(viewer_ZoomChange);
        }

        /// <summary>
        /// Запрещаем масштаб > 200. Иначе программа падает с ошибкой
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void viewer_ZoomChange(object sender, ZoomChangeEventArgs e)
        {
            if (e.ZoomMode == ZoomMode.Percent && e.ZoomPercent > 200)
                e.Cancel = true;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            SetDataAndInit();
        }

        private void SetDataAndInit()
        {
            if (DataContext == null) return;

            model = DataContext as ReportViewModel;
            viewer = host.Child as ReportViewer;
            InitReportViewer();

            if (viewer == null || model == null || !model.IsValid) return;
            //viewer.RefreshReport();
            this.SetLoaded(true);
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != null)
                SetDataAndInit();            
        }
    }
}
