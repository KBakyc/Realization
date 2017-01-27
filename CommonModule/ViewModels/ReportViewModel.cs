using System;
using System.Linq;
using CommonModule.Interfaces;
using DataObjects;
using Microsoft.Reporting.WinForms;
using System.Collections.Generic;
using CommonModule.Helpers;

namespace CommonModule.ViewModels
{
    public class ReportViewModel : BasicModuleContent
    {
        /// <summary>
        /// Ссылка на отчёт
        /// </summary>
        private ReportModel report;
        //private ReportService service;

        public ReportViewModel(IModule _parent, ReportService _serv)
            :base(_parent)
        {
            if (_serv != null)
            {
                Init(_serv.ReportModel);
                var srv = _serv.ActualReport as ServerReport;
                if (srv != null)
                    ReportServerUri = srv.ReportServerUrl;
                reportParameters = _serv.ReportParameters.ToArray();
            }
        }

        public ReportViewModel(IModule _parent, ReportModel _rep)
            : base(_parent)
        {
            Init(_rep);
            ReportServerUri = new Uri(Properties.Settings.Default.RS_ServerUrl.Trim());
            if (_rep.Parameters != null)
                reportParameters = report.Parameters.Select(kv => new ReportParameter(kv.Key, kv.Value.Split(','))).ToArray();
        }
        
        private void Init(ReportModel _rep)
        {
            report = _rep;
            Title = _rep.Title;
            if (_rep.Mode == ReportModes.Local && _rep.DataSources != null)
            {
                dataSources = _rep.DataSources.Select(kv => new ReportDataSource(kv.Key, kv.Value)).ToArray();
            }
        }

        /// <summary>
        /// Полный путь к отчёту
        /// </summary>
        public string ReportPath
        { 
            get { return report.Path; } 
        }

        /// <summary>
        /// Данные для локального отчёта
        /// </summary>
        private ReportDataSource[] dataSources;
        public ReportDataSource[] DataSources { get { return dataSources; } }

        /// <summary>
        /// Параметры отчёта
        /// </summary>
        private ReportParameter[] reportParameters;
        public ReportParameter[] ReportParameters { get { return reportParameters; } }


        private string errMsg;
        public bool IsValid 
        {
            get
            {
                if (errMsg == null)
                    errMsg = Check();
                return String.Empty == errMsg;
            }
        }

        public string GetErrMsg()
        {
            return errMsg;
        }

        public Uri ReportServerUri { get; set; }

        public ReportModes Mode 
        {
            get { return report.Mode; }
        }

        private ZoomMode zoom = ZoomMode.PageWidth;
        public ZoomMode Zoom
        {
            get { return zoom; }
            set { zoom = value; }
        }

        private string Check()
        {
            if (report == null) return "Не данных для отчёта";
            if (report.Mode == ReportModes.Server)
            {
                if (ReportServerUri == null)
                    return "Не указан путь к серверу отчётов!";
            }
            else
                if (string.IsNullOrEmpty(ReportPath) || !System.IO.File.Exists(ReportPath))
                    return String.Format("Отчёт {0} не найден!", ReportPath);
            return String.Empty;
        }
     }
}