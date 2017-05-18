using System;
using System.Linq;
using System.ComponentModel.Composition;
using CommonModule;
using CommonModule.Helpers;
using CommonModule.Interfaces;
using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Interfaces;
using System.Collections.Generic;
using Microsoft.Reporting.WinForms;
using DAL;


namespace InfoModule.Helpers
{
    /// <summary>
    /// Вспомогательный класс для запроса параметров отчёта по форме F744.
    /// </summary>
    [Export("InfoModule.F744ParamsGetter", typeof(IReportParametersGetter))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class F744ParamsGetter : BaseReportParametersGetter
    {
        private IDbService repository = CommonSettings.Repository;

        public override BaseDlgViewModel GetDialog(ReportModel _repInfo, Action _onSubmit)
        {
            ReportInfo = _repInfo;

            var dialog = new BaseCompositeDlgViewModel
            {
                Title = "Параметры отчёта" + Environment.NewLine + _repInfo.Title,
                OnSubmit = pd =>
                {
                    OnParamsSubmitted(pd, _onSubmit);
                }
            };

            DateDlgViewModel ddlg = new DateDlgViewModel(false)
            {
                Title = "На дату (включительно)"                
            };

            dialog.Add(ddlg);
            
            return dialog;
        }

        private DateTime toDate;
        private bool fExists;

        private void OnParamsSubmitted(Object _dlg, Action _continuation)
        {
            var dlg = _dlg as BaseCompositeDlgViewModel;
            if (dlg == null) return;

            var parent = dlg.Parent;
            parent.CloseDialog(dlg);

            var ddlg = dlg.DialogViewModels[0] as DateDlgViewModel;
            if (ddlg.SelDate == null) return;
            toDate = ddlg.SelDate.Value;
            
            parent.CloseDialog(dlg);

            repParams.Clear();
            repParams.Add(new ReportParameter("ToDate", toDate.ToString("yyyy-MM-dd")));
            repParams.Add(new ReportParameter("ConnString", CommonSettings.ConnectionString));

            MakeF744(parent, _continuation);
        }        
        
        private void MakeF744(IModule _parent, Action _continuation)
        {
            Action work = () => repository.MakeF744(toDate);
            if (_parent == null)
                work();
            else
                _parent.Services.DoWaitAction(work, "Подождите", "Формирование формы 744...", _continuation);
        }

    }
}
