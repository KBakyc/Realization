using System;
using System.Linq;
using System.ComponentModel.Composition;
using CommonModule;
using CommonModule.Helpers;
using CommonModule.Interfaces;
using CommonModule.ViewModels;
using DataObjects;
using Microsoft.Reporting.WinForms;
using DataObjects.Interfaces;


namespace ReportExtensions
{
    [Export("ReportExtensions.PoupDatesParamsGetter", typeof(IReportParametersGetter))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class PoupDatesParamsGetter : BaseReportParametersGetter
    {
        private IDbService repository = CommonSettings.Repository;

        public override BaseDlgViewModel GetDialog(ReportModel _repInfo, Action _onSubmit)
        {
            ReportInfo = _repInfo;
            bool issavedate = true;

            var dialog = new PoupAndDatesDlgViewModel(repository, issavedate)
            {
                Title = "Параметры отчёта",
                OnSubmit = pd =>
                {
                    OnParamsSubmitted(pd);
                    _onSubmit();
                }
            };

            if (_repInfo.Parameters != null && _repInfo.Parameters.Count > 0)
                Init(dialog, _repInfo.Parameters);

            return dialog;
        }

        private void Init(PoupAndDatesDlgViewModel _dialog, System.Collections.Generic.Dictionary<string, string> _params)
        {
            foreach (var par in _params)
            {
                DateTime dtparsed;                
                switch(par.Key)
                {
                    case "Poup":
                        _dialog.PoupSelection.SelPoup = _dialog.PoupSelection.Poups.FirstOrDefault(p => p.Kod.ToString() == par.Value);
                        break;
                    case "Pkod":
                        if (!String.IsNullOrWhiteSpace(par.Value))
                        {
                            var spkods = par.Value.Split(',');
                            foreach(var pk in _dialog.PoupSelection.Pkods.Where(pk => spkods.Contains(pk.Value.Pkod.ToString())))
                                pk.IsSelected = true;
                        }
                        break;
                    case "Date1":                        
                        if (DateTime.TryParse(par.Value, out dtparsed))
                            _dialog.DateFrom = dtparsed;
                        break;
                    case "Date2":
                        if (DateTime.TryParse(par.Value, out dtparsed))
                            _dialog.DateTo = dtparsed;
                        break;
                    default:
                        break;
                }
            }
        }

        private void OnParamsSubmitted(Object _dlg)
        {
            var dlg = _dlg as PoupAndDatesDlgViewModel;
            if (dlg == null) return;

            var oldConnString = ReportInfo.Parameters != null ? ReportInfo.Parameters["ConnString"] : null;

            repParams.Clear();
            repParams.Add(new ReportParameter("Poup", dlg.SelPoup.Kod.ToString()));
            if (dlg.IsPkodEnabled)
                repParams.Add(new ReportParameter("Pkod", dlg.SelPkods[0].Pkod.ToString()));
            repParams.Add(new ReportParameter("Date1", dlg.DateFrom.ToString("yyyy-MM-dd")));
            repParams.Add(new ReportParameter("Date2", dlg.DateTo.ToString("yyyy-MM-dd")));
            repParams.Add(new ReportParameter("ConnString", oldConnString ?? CommonSettings.ConnectionString));
        }
    }
}
