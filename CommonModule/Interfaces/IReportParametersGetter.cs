using System;
using CommonModule.ViewModels;
using Microsoft.Reporting.WinForms;
using System.Collections.Generic;
using DataObjects;
namespace CommonModule.Interfaces
{
    public interface IReportParametersGetter
    {
        BaseDlgViewModel GetDialog(ReportModel _repModel, Action onSubmit);
        IEnumerable<ReportParameter> ReportParameters { get; }
        IEnumerable<ReportParameter[]> GetSplittedParams(ReportParameter par);
//        void SetActualReport(Report rep);
        ReportMode GetReportFeatures();
    }
}
