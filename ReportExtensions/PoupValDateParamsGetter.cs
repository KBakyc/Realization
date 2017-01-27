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
    [Export("ReportExtensions.PoupValDateParamsGetter", typeof(IReportParametersGetter))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class PoupValDateParamsGetter : BaseReportParametersGetter
    {
        private IDbService repository = CommonSettings.Repository;

        public override BaseDlgViewModel GetDialog(ReportModel _repInfo, Action _onSubmit)
        {
            ReportInfo = _repInfo;

            var dialog = new PoupValDateDlgViewModel(repository, null)
            {
                Title = "Параметры отчёта",
                DateLabel = "На дату",
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

        private void Init(PoupValDateDlgViewModel _dialog, System.Collections.Generic.Dictionary<string, string> _params)
        {
            foreach (var par in _params)
            {
                DateTime dtparsed;
                switch (par.Key)
                {
                    case "Poup":
                        _dialog.PoupSelection.SelPoup = _dialog.PoupSelection.Poups.FirstOrDefault(p => p.Kod.ToString() == par.Value);
                        break;
                    case "Pkod":
                        if (!String.IsNullOrWhiteSpace(par.Value))
                        {
                            var spkods = par.Value.Split(',');
                            foreach (var pk in _dialog.PoupSelection.Pkods.Where(pk => spkods.Contains(pk.Value.Pkod.ToString())))
                                pk.IsSelected = true;
                        }
                        break;
                    case "Date1":
                        if (DateTime.TryParse(par.Value, out dtparsed))
                            _dialog.SelDate = dtparsed;
                        break;
                    case "Kodval":
                        _dialog.ValSelection.SelVal = _dialog.ValSelection.ValList.FirstOrDefault(v => v.Kodval == par.Value);
                        break;
                    default:
                        break;
                }
            }
        }

        private void OnParamsSubmitted(Object _dlg)
        {
            var dlg = _dlg as PoupValDateDlgViewModel;
            if (dlg == null) return;

            var oldConnString = ReportInfo.Parameters != null ? ReportInfo.Parameters["ConnString"] : null;

            repParams.Clear();
            repParams.Add(new ReportParameter("Poup", dlg.SelPoup.Kod.ToString()));
            if (dlg.PoupSelection.IsPkodEnabled)
                repParams.Add(new ReportParameter("Pkod", dlg.SelPkods[0].Pkod.ToString()));
            repParams.Add(new ReportParameter("Date1", dlg.SelDate.Value.ToString("yyyy-MM-dd")));
            repParams.Add(new ReportParameter("Kodval", dlg.SelVal.Kodval));
            repParams.Add(new ReportParameter("ConnString", oldConnString ?? CommonSettings.ConnectionString));
        }
    }
}
