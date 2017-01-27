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
using System.Collections.Generic;


namespace PredoplModule.Helpers
{
    [Export("PredoplModule.PoupValDates2ParamsGetter", typeof(IReportParametersGetter))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class PoupValDates2ParamsGetter : BaseReportParametersGetter
    {
        private IDbService repository = CommonSettings.Repository;

        public override BaseDlgViewModel GetDialog(ReportModel _repInfo, Action _onSubmit)
        {
            ReportInfo = _repInfo;
            bool issavedate = true;

            var dialog = new BaseCompositeDlgViewModel 
            { 
                Title = "Параметры отчёта" + Environment.NewLine + _repInfo.Title,
                OnSubmit = pd =>
                {
                    OnParamsSubmitted(pd);
                    _onSubmit();
                }
            };

            ProcessCustomOptions();
            ProcessAdditionalChoices();

            if (poupselEnabled)
            {
                var poupsel = new PoupSelectionViewModel(repository)
                {
                    Name = "poupsel",
                    Title = "Направление реализации",
                    PoupTitle = null
                };
                dialog.Add(poupsel);
                if (ReportInfo.Parameters != null && ReportInfo.Parameters.ContainsKey("Poup"))
                {
                    poupsel.SelPoup = poupsel.Poups.FirstOrDefault(p => p.Kod.ToString() == ReportInfo.Parameters["Poup"]);
                    if (ReportInfo.Parameters.ContainsKey("Pkod"))
                    {
                        var spkods = ReportInfo.Parameters["Pkod"].Split(',');
                        foreach (var spk in poupsel.Pkods.Where(pk => spkods.Contains(pk.Value.Pkod.ToString())))
                            spk.IsSelected = true;
                    }
                }
            }

            if (valselEnabled)
            {
                var valsel = new ValSelectionViewModel(repository)
                {
                    Name = "valsel",
                    Title = "Валюта",
                    ValSelectionTitle = null
                };
                dialog.Add(valsel);
                if (ReportInfo.Parameters != null && ReportInfo.Parameters.ContainsKey("Kodval"))
                    valsel.SelVal = valsel.ValList.FirstOrDefault(v => v.Kodval == ReportInfo.Parameters["Kodval"]);
            }

            if (dates1selEnabled)
            {
                var datsrealsel = new DateRangeDlgViewModel(issavedate)
                {
                    Name = "dates1sel",
                    Title = dates1Header,
                    DatesLabel = null
                };
                dialog.Add(datsrealsel);
                if (ReportInfo.Parameters != null && ReportInfo.Parameters.ContainsKey("Date1") && ReportInfo.Parameters.ContainsKey("Date2"))
                {
                    DateTime pdate;
                    if (DateTime.TryParse(ReportInfo.Parameters["Date1"], out pdate))
                        datsrealsel.DateFrom = pdate;
                    if (DateTime.TryParse(ReportInfo.Parameters["Date2"], out pdate))
                        datsrealsel.DateTo = pdate;
                }
            }

            if (dates2selEnabled)
            {
                var datsotgrsel = new DateRangeDlgViewModel(false)
                {
                    Name = "dates2sel",
                    Title = dates2Header,
                    DatesLabel = null
                };
                dialog.Add(datsotgrsel);
                if (ReportInfo.Parameters != null && ReportInfo.Parameters.ContainsKey("Date11") && ReportInfo.Parameters.ContainsKey("Date22"))
                {
                    DateTime pdate;
                    if (DateTime.TryParse(ReportInfo.Parameters["Date11"], out pdate))
                        datsotgrsel.DateFrom = pdate;
                    if (DateTime.TryParse(ReportInfo.Parameters["Date22"], out pdate))
                        datsotgrsel.DateTo = pdate;
                }
            }

            if (ChoicesselEnabled)
            {
                var choicesSel = new ChoicesDlgViewModel(additionalChoices.ToArray())
                {
                    Name = "choicessel",
                    Title = "Дополнительно"                    
                };
                dialog.Add(choicesSel);
                if (ReportInfo.Parameters != null && additionalChoices.Any(ch => ReportInfo.Parameters.ContainsKey(ch.Name)))
                {
                    bool pbool;
                    foreach (var ch in additionalChoices.Where(ch => ReportInfo.Parameters.ContainsKey(ch.Name)))
                        if (Boolean.TryParse(ReportInfo.Parameters[ch.Name], out pbool))
                            ch.IsChecked = pbool;
                }
            }


            return dialog;
        }

        private void OnParamsSubmitted(Object _dlg)
        {
            var dlg = _dlg as BaseCompositeDlgViewModel;
            if (dlg == null) return;

            repParams.Clear();
            string compname = "poupsel";
            var poupsel = dlg.DialogViewModels.SingleOrDefault(vm => vm.Name == compname) as PoupSelectionViewModel;
            if (poupsel != null)
            {
                repParams.Add(new ReportParameter("Poup", poupsel.SelPoup.Kod.ToString()));
                if (poupsel.IsPkodEnabled)
                    repParams.Add(new ReportParameter("Pkod", poupsel.SelPkods[0].Pkod.ToString()));
            }

            compname = "valsel";
            var valsel = dlg.DialogViewModels.SingleOrDefault(vm => vm.Name == compname) as ValSelectionViewModel;
            if (valsel != null)
            {
                repParams.Add(new ReportParameter("Kodval", valsel.SelVal.Kodval));
            }

            compname = "dates1sel";
            var datsrealsel = dlg.DialogViewModels.SingleOrDefault(vm => vm.Name == compname) as DateRangeDlgViewModel;
            if (datsrealsel != null)
            {
                repParams.Add(new ReportParameter("Date1", datsrealsel.DateFrom.ToString("yyyy-MM-dd")));
                repParams.Add(new ReportParameter("Date2", datsrealsel.DateTo.ToString("yyyy-MM-dd")));
            }

            compname = "dates2sel";
            var datsotgrsel = dlg.DialogViewModels.SingleOrDefault(vm => vm.Name == compname) as DateRangeDlgViewModel;
            if (datsotgrsel != null)
            {
                repParams.Add(new ReportParameter("Date11", datsotgrsel.DateFrom.ToString("yyyy-MM-dd")));
                repParams.Add(new ReportParameter("Date22", datsotgrsel.DateTo.ToString("yyyy-MM-dd")));
            }

            compname = "choicessel";
            var choicessel = dlg.DialogViewModels.SingleOrDefault(vm => vm.Name == compname) as ChoicesDlgViewModel;
            if (choicessel != null)
            {
                foreach (var ch in choicessel.Groups.Values.SelectMany(c => c))
                    repParams.Add(new ReportParameter(ch.Name, ch.IsChecked.ToString()));
            }

            repParams.Add(new ReportParameter("ConnString", CommonSettings.ConnectionString));
        }

        private bool poupselEnabled = true;
        private bool valselEnabled = true;
        private bool dates1selEnabled = true;
        private string dates1Header = "Период 1";
        private bool dates2selEnabled = true;
        private string dates2Header = "Период 2";

        private bool ChoicesselEnabled { get { return additionalChoices != null && additionalChoices.Count > 0; } }

        private void ProcessCustomOptions()
        {
            if (!GetterOptions.ContainsKey("CustomOptions")) return;
            bool onoff = true;
            foreach (var o in GetterOptions["CustomOptions"])
            {
                string comp = o.Key.ToLower();
                switch(comp)
                {
                    case "poupselenabled": if (Boolean.TryParse(o.Value[0].Value, out onoff))
                                             poupselEnabled = onoff;
                                           break;
                    case "valselenabled":  if (Boolean.TryParse(o.Value[0].Value, out onoff))
                                             valselEnabled = onoff;
                                           break;
                    case "dates1selenabled": if (Boolean.TryParse(o.Value[0].Value, out onoff))
                                               dates1selEnabled = onoff;
                                             break;
                    case "dates2selenabled": if (Boolean.TryParse(o.Value[0].Value, out onoff))
                                               dates2selEnabled = onoff;
                                             break;
                    case "dates1header": dates1Header = o.Value[0].Value; break;
                    case "dates2header": dates2Header = o.Value[0].Value; break;
                }

            }
        }

        private List<Choice> additionalChoices = null;

        private void ProcessAdditionalChoices()
        {
            if (!GetterOptions.ContainsKey("choices")) return;

            additionalChoices = new List<Choice>();

            bool onoff = true;
            foreach (var c in GetterOptions["choices"])
            {

                var newChoice = new Choice() 
                {
                    Name = c.Key
                };

                foreach (var ca in c.Value)
                {
                    var caname = ca.Key;
                    switch (caname)
                    {
                        case "header": newChoice.Header = ca.Value;
                            break;
                        case "name": newChoice.Name = ca.Value;
                            break;
                        case "groupname": newChoice.GroupName = ca.Value;
                            break;
                        case "ischecked": if (Boolean.TryParse(ca.Value, out onoff))
                                newChoice.IsChecked = onoff;
                            break;
                        case "issingleingroup": if (Boolean.TryParse(ca.Value, out onoff))
                                newChoice.IsSingleInGroup = onoff;
                            break;
                    }
                }

                if (!String.IsNullOrEmpty(newChoice.Name))
                    additionalChoices.Add(newChoice);
            }
        }
    }
}
