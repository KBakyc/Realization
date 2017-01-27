using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataObjects;
using Microsoft.Reporting.WinForms;
using CommonModule.Interfaces;

namespace CommonModule.Helpers
{
    public static class ReportHelper
    {
        public static Report GetReportFromModel(ReportModel _reportModel)
        {
            Report res = GetActualReport(_reportModel);
            res.InitActualReport(_reportModel);
            return res;        
        }

        private static Report GetActualReport(ReportModel _reportModel)
        {
            Report res = null;
            if (_reportModel.Mode == ReportModes.Server)
                res = new ServerReport();
            else
                res = new LocalReport();
            return res;
        }

        public static void InitActualReport(this Report _actualReport, ReportModel _reportModel)
        {
            if (_reportModel == null || _actualReport == null) return;
            if (_reportModel.Mode == ReportModes.Server)
            {
                var servrep = _actualReport as ServerReport;
                string rsUrl = Properties.Settings.Default.RS_ServerUrl.Trim();
                if (String.IsNullOrEmpty(rsUrl))
                    throw new ArgumentOutOfRangeException("RS_ServerUrl", "Не указан путь к серверу отчётов");
                servrep.ReportServerUrl = new Uri(rsUrl);
                servrep.ReportPath = _reportModel.Path;
            }
            else
                throw new NotImplementedException("Не реализован сервис для локальных отчётов");
        }

        public static void PrintReport(IModule _mod, ReportModel _reportModel)
        {
            Report _actualReport = GetReportFromModel(_reportModel);

            if (_actualReport == null) return;
            var reportParams = _reportModel.Parameters.Select(kv => new ReportParameter(kv.Key, kv.Value));

            if (reportParams != null && reportParams.Any())
                _actualReport.SetParameters(reportParams);
            new PrintHelper().PrintReport(_mod, _actualReport);
        }

        public static void PrintReport(IModule _mod, PrintHelper _helper, ReportModel _reportModel)
        {
            Report _actualReport = GetReportFromModel(_reportModel);

            if (_actualReport == null) return;
            var reportParams = _reportModel.Parameters.Select(kv => new ReportParameter(kv.Key, kv.Value));

            bool isParsOk = true;
            if (reportParams != null && reportParams.Any())
                try
                {
                    _actualReport.SetParameters(reportParams);
                }
                catch (Exception e)
                {
                    WorkFlowHelper.OnCrash(e);
                    isParsOk = false;
                }
            if (isParsOk)
            {
                if (_helper == null)
                    _helper = new PrintHelper();
                _helper.PrintReport(_mod, _actualReport);
            }
        }
    }
}
