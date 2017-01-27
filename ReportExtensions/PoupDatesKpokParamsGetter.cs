using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using CommonModule;
using CommonModule.Helpers;
using CommonModule.Interfaces;
using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Interfaces;
using Microsoft.Reporting.WinForms;
using DataObjects.SeachDatas;


namespace ReportExtensions
{
    [Export("ReportExtensions.PoupDatesKpokParamsGetter", typeof(IReportParametersGetter))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class PoupDatesKpokParamsGetter : BaseReportParametersGetter
    {
        private const string KPOK_PARAM_NAME = "Kpok";
        private IDbService repository;

        public IDbService Repository
        {
            get
            {
                if (repository == null)
                    repository = CommonSettings.Repository;
                return repository;
            }
        }

        public PoupDatesKpokParamsGetter()
        {
        }

        private IEnumerable<KontrAgent> GetKas(PoupDatesKpokDlgViewModel _dlg)
        {
            if (_dlg == null) return null;
            IEnumerable<KontrAgent> res = null;
            var poup = _dlg.SelPoup.Kod;
            short pkod = 0;
            if (_dlg.SelPkods != null && _dlg.SelPkods.Length > 0)
                pkod = _dlg.SelPkods[0].Pkod;
            var datefrom = _dlg.DateFrom;
            var dateto = _dlg.DateTo;
            var sfs = Repository.GetSfsList(new SfSearchData { Poup = poup, Pkod = pkod, DateFrom = datefrom, DateTo = dateto }).Where(s => s.Status == LifetimeStatuses.Accepted || s.Status == LifetimeStatuses.Edited).ToArray();
            if (_dlg.IsDateFormSelected)
            {
                var sfswdtform = sfs.Select(s => new { Sf = s, DtForm = Repository.GetSfStatusLastDateTime(s.IdSf, LifetimeStatuses.Accepted) })
                                    .Where(sdt => sdt.DtForm != null && sdt.DtForm.SfStatusDateTime.Date == _dlg.DateForm);
                if (_dlg.IsOnlyLast)
                {
                    DateTime lastdt = sfswdtform.Select(sdt => sdt.DtForm.SfStatusDateTime).Max();
                    sfswdtform = sfswdtform.Where(sdt => sdt.DtForm.SfStatusDateTime == lastdt);
                }
                sfs = sfswdtform.Select(sdt => sdt.Sf).ToArray();
            }
            res = sfs.Select(s => s.Kpok).Distinct()
                     .Select(k => Repository.GetKontrAgent(k));
            return res;
        }

        public override BaseDlgViewModel GetDialog(ReportModel _repModel, Action _onSubmit)
        {
            ReportInfo = _repModel;
            string title = ReportInfo == null ? "" : ReportInfo.Title;

            bool issavedate = true;

            var dialog = new PoupDatesKpokDlgViewModel(Repository, issavedate)
            {
                Title = "Параметры отчёта\n" + title,
                DatesLabel = "Дата отгрузки",
                GetKas = GetKas,
                OnSubmit = pd =>
                {
                    OnParamsSubmitted(pd);
                    _onSubmit();
                }
            };

            if (ReportInfo.Parameters != null && ReportInfo.Parameters.Count > 0)
                Init(dialog, ReportInfo.Parameters);

            return dialog;
        }

        private void Init(PoupDatesKpokDlgViewModel _dialog, System.Collections.Generic.Dictionary<string, string> _params)
        {            
            DateTime dtparsed;
            bool bparsed;

            if (_params.ContainsKey("Poup"))
            {
                var spoup = _params["Poup"];
                _dialog.SelPoup = _dialog.Poups.FirstOrDefault(p => p.Kod.ToString() == spoup);
            }

            if (_params.ContainsKey("Date1"))
            {
                var sdate = _params["Date1"];
                if (DateTime.TryParse(sdate, out dtparsed))
                    _dialog.DateFrom = dtparsed;
            }

            if (_params.ContainsKey("Date2"))
            {
                var sdate = _params["Date2"];
                if (DateTime.TryParse(sdate, out dtparsed))
                    _dialog.DateTo = dtparsed;
            }

            if (_params.ContainsKey("DateForm"))
            {
                var sdate = _params["DateForm"];
                if (sdate != null && DateTime.TryParse(sdate, out dtparsed))
                    _dialog.DateForm = dtparsed;
                else
                    _dialog.IsDateFormSelected = false;
            }

            if (_params.ContainsKey("IsOnlyLast"))
            {
                var sbool = _params["IsOnlyLast"];
                if (Boolean.TryParse(sbool, out bparsed))
                    _dialog.IsOnlyLast = bparsed;
            }

            if (_params.ContainsKey(KPOK_PARAM_NAME))
            {
                var skpok = _params[KPOK_PARAM_NAME];
                if (skpok != "0")
                {
                    _dialog.IsAllKas = false;
                    _dialog.SelectedKA = _dialog.KaList.FirstOrDefault(k => k.Kgr.ToString() == skpok);
                }
            }
        }

        private void OnParamsSubmitted(Object _dlg)
        {
            var dlg = _dlg as PoupDatesKpokDlgViewModel;
            if (dlg == null) return;

            ReportParameter kpokPar = null;
            if (dlg.IsAllKas && dlg.IsPerKa && dlg.KaList != null && dlg.KaList.Count > 0)
            {
                string[] selkas = dlg.KaList.Select(k => k.Kgr.ToString()).ToArray();
                kpokPar = new ReportParameter(KPOK_PARAM_NAME, selkas);
                isSpliting = true;
            }
            else
                if (dlg.SelectedKA != null)
                    kpokPar = new ReportParameter(KPOK_PARAM_NAME, dlg.SelectedKA.Kgr.ToString());
            if (kpokPar == null)
                kpokPar = new ReportParameter(KPOK_PARAM_NAME, "0");

            var oldConnString = ReportInfo.Parameters != null ? ReportInfo.Parameters["ConnString"] : null;

            repParams.Clear();
            repParams.Add(new ReportParameter("Poup", dlg.SelPoup.Kod.ToString()));
            repParams.Add(new ReportParameter("Date1", dlg.DateFrom.ToString("yyyy-MM-dd")));
            repParams.Add(new ReportParameter("Date2", dlg.DateTo.ToString("yyyy-MM-dd")));
            repParams.Add(kpokPar);
            if (dlg.IsDateFormSelected)
                repParams.Add(new ReportParameter("DateForm", dlg.DateForm.ToString("yyyy-MM-dd")));
            repParams.Add(new ReportParameter("IsOnlyLast", dlg.IsOnlyLast ? "True" : "False"));

            repParams.Add(new ReportParameter("ConnString", oldConnString ?? CommonSettings.ConnectionString));
        }

        private bool isSpliting = false;

        public override DataObjects.ReportMode GetReportFeatures()
        {
            var fea = base.GetReportFeatures();
            if (isSpliting && !String.IsNullOrEmpty(KPOK_PARAM_NAME))
                fea.SplitingParameter = KPOK_PARAM_NAME;
            return fea;
        }
    }
}
