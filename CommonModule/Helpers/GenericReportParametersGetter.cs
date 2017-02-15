using System;
using System.Collections.Generic;
using System.Linq;
using CommonModule.ViewModels;
using DataObjects;
using Microsoft.Reporting.WinForms;
using CommonModule.Interfaces;


namespace CommonModule.Helpers
{
    public class GenericReportParametersGetter : BaseReportParametersGetter
    {
        private ReportParameterInfo[] allParameterInfos;
        private ReportParameterInfo[] userParameterInfos;

        private Dictionary<string, string[]> coercedDefaultValues = new Dictionary<string, string[]>();

        public GenericReportParametersGetter()
        {
        }

        private Report actualReport;
        public Report ActualReport
        {
            get { return actualReport; }
        }

        public virtual void SetActualReport(Report rep)
        {
            actualReport = rep;
        }

        private void InitParameters()
        {
            ReportParameter newconnstr = null;

            if (ReportInfo.Parameters != null && ReportInfo.Parameters.Count > 0)
                foreach (var rp in ReportInfo.Parameters)
                {
                    var values = rp.Value != null ? rp.Value.Split(',') : new string[]{};
                    var par = CreateReportParameter(rp.Key, values);
                    if (rp.Key == CONSTR_PARAM_NAME)
                        newconnstr = par;
                    try
                    {
                        actualReport.SetParameters(par);
                    }
                    catch { }
                }

            GetParamInfos();

            bool needRefresh = false;

            if (newconnstr == null)
            {
                var connstr = allParameterInfos.SingleOrDefault(p => p.Name == CONSTR_PARAM_NAME);
                if (connstr != null && connstr.Values[0] != CommonSettings.ConnectionString)
                {
                    newconnstr = CreateReportParameter(CONSTR_PARAM_NAME, CommonSettings.ConnectionString);
                    actualReport.SetParameters(newconnstr);
                    needRefresh = true;
                }
            }

            GetAlternateDefaults();
            if (coercedDefaultValues.Any())
            {
                SetAlternateDefaults();
                needRefresh = true;
            }

            if (needRefresh)
                GetParamInfos();                
        }

        private void GetParamInfos()
        {
            allParameterInfos = actualReport.GetParameters().ToArray();
            userParameterInfos = allParameterInfos.Where(p => p.PromptUser == true).ToArray();
        }

        private IEnumerable<ReportParameterViewModel> Init(ReportParameterInfo[] _pars = null)
        {
            IEnumerable<ReportParameterViewModel> res = null;

            if (_pars == null || visibleParams == null || !visibleParams.Any())
                res = visibleParams = GetParametersViewModels().ToArray();
            else
            {
                res = GetParametersViewModels(_pars).ToArray();
                List<ReportParameterViewModel> newVisiblePars = new List<ReportParameterViewModel>();
                foreach (var oldVisiblePar in visibleParams)
                {
                    var newOldToVP = res.SingleOrDefault(p => p.ParamName == oldVisiblePar.ParamName);
                    newVisiblePars.Add(newOldToVP ?? oldVisiblePar);
                }
                visibleParams = newVisiblePars.ToArray();
            }
            return res;
        }

        private Dictionary<ReportParameterViewModel, bool> notifiers = new Dictionary<ReportParameterViewModel, bool>();

        private void UnsubscribeNotifierIfNeeded(ReportParameterViewModel _notifier)
        {
            if (notifiers.ContainsKey(_notifier) && notifiers[_notifier])
            {
                _notifier.PropertyChanged -= parvm_PropertyChanged;
                notifiers[_notifier] = false;
            }
        }

        private void SubscribeNotifierIfNotYet(ReportParameterViewModel _notifier)
        {
            if (!notifiers.ContainsKey(_notifier) || !notifiers[_notifier])
            {
                _notifier.PropertyChanged += parvm_PropertyChanged;
                notifiers[_notifier] = true;
            }
        }

        private void parvm_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var vmsend = sender as ReportParameterViewModel;
            //ReportParameterViewModel[] newdep = null;
            List<ReportParameterViewModel> params4update = new List<ReportParameterViewModel>();

            if (vmsend.IsEditing || !vmsend.IsChanged) return;

            if (e.PropertyName == "SelectedValue" || e.PropertyName == "SelectedLabel" && !vmsend.IsEditing)// || e.PropertyName == "IsEditing" && !vmsend.IsEditing)
            {
                var oldparams = userParameterInfos;

                Action work = () =>
                    {        
                        if (vmsend != null && vmsend.IsParamsValueValid)
                        {
                            var depsNameLst = new List<String>() { vmsend.ParamName };
                            depsNameLst.AddRange(vmsend.Dependents.Select(d => d.Name));
                            
                            var vals = vmsend.GetValues();
                            actualReport.SetParameters(new ReportParameter(vmsend.ParamName, vals));         
                            
                            ReportParameter[] invalids = null;
                            do
                            {
                                GetParamInfos();

                                var linkNewOld =  userParameterInfos.Join(visibleParams, p => p.Name, o => o.ParamName, (p, o) => new {npar = p, opar = o})
                                                          .Where
                                                          (no => no.npar.State != no.opar.ParamState || depsNameLst.Contains(no.npar.Name)
                                                          || (vmsend.HasCoersedDependents && vmsend.CoersedDependents.Contains(no.opar)))
                                                          .ToArray();

                                foreach (var no in linkNewOld)
                                {
                                    if (!params4update.Contains(no.opar))
                                    {
                                        UnsubscribeNotifierIfNeeded(no.opar);
                                        params4update.Add(no.opar);
                                    }
                                    no.opar.SetNewParamData(no.npar);                                    
                                }

                                ProcessEditorsOptions(false, params4update.ToArray());                            
                                
                                var newinvalids = userParameterInfos.Where(pi => pi.State == ParameterState.MissingValidValue)
                                .Join(visibleParams, rpi => rpi.Name, d => d.ParamName, (rpi, d) => d)
                                .Where(d => d.IsParamsValueValid)
                                .Select(d => new ReportParameter(d.ParamName, d.GetValues()))
                                .ToArray();

                                // если некорректные значения не правятся, то выходим
                                if (invalids != null && invalids.Length == newinvalids.Length && Array.TrueForAll(newinvalids, ni => Array.Exists(invalids , i => ni.Name == i.Name)))
                                    break;
                                else
                                    invalids = newinvalids;

                                if (invalids.Length > 0)
                                    ActualReport.SetParameters(invalids);
                            } while (invalids.Length > 0);
                        }
                    };
                if (dialog != null)
                {
                    Action afterwork = () =>
                        {
                            dialog.Parent.ShellModel.UpdateUi(() => 
                            { 
                                if (params4update != null && params4update.Count > 0)
                                    dialog.UpdateParameters(params4update.ToArray());
                                foreach (var no in params4update)
                                    if (notifiers.ContainsKey(no)) SubscribeNotifierIfNotYet(no);
                                dialog.IsBusy = false;
                                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
                            }, false, false);
                        };
                    dialog.IsBusy = true;
                    dialog.Parent.Services.DoAsyncAction(work, afterwork);
                }
                else
                    work();
            }
        }

        private ReportParameterViewModel[] visibleParams;

        private GetReportParametersDlgViewModel dialog;

        public override BaseDlgViewModel GetDialog(ReportModel _repModel, Action _onSubmit)
        {
            ReportInfo = _repModel;
            if (actualReport != null)
            {
                InitParameters();
                Init();
                // установка значений параметров с валидными значениями, но с невалидным первоначальным состоянием, у которых есть зависимые параметры
                bool reinit = false;
                foreach (var vispar in visibleParams.Where(p => p.IsParamsValueValid && p.ParamState == ParameterState.MissingValidValue && p.Dependents.Length > 0))
                {
                    actualReport.SetParameters(new ReportParameter(vispar.ParamName, vispar.GetValues()));
                    reinit = true;
                }
                if (reinit)
                {
                    foreach (var vispar in visibleParams.Where(p => p.Dependents.Length > 0))
                        UnsubscribeNotifierIfNeeded(vispar);
                    GetParamInfos();
                    Init();
                }
                //--
            }

            ProcessEditorsOptions(true, visibleParams);
            dialog = new GetReportParametersDlgViewModel(visibleParams)
            {
                Title = "Укажите параметры отчёта:\n" + _repModel.Title ,
                OnSubmit = (o) =>
                {
                    OnMainDialogSubmit(o);
                    _onSubmit();
                }
            };
            return dialog;
        }

        private void OnMainDialogSubmit(Object _o)
        {
            var dlg = _o as GetReportParametersDlgViewModel;
            if (dlg == null) return;
            CollectReportParams(dlg.AllParameters);
            CoerceParameters();
        }

        private void CollectReportParams(IEnumerable<ReportParameterViewModel> _parVms)
        {
            repParams.Clear();
            for (int i = 0; i < userParameterInfos.Length; i++)
            {
                string pName = userParameterInfos[i].Name;
                var chpvm = _parVms.SingleOrDefault(vm => vm.ParamName == pName);
                string[] pValue = null;
                if (chpvm != null)
                    pValue = chpvm.GetValues();
                else
                    pValue = GetActualDefaultValue(userParameterInfos[i]);
                var par = CreateReportParameter(pName, pValue);
                if (par != null)
                {
                    repParams.Add(par);
                    if (chpvm != null && defsmappings.ContainsKey(pName))
                        SavePersistedValue(defsmappings[pName], chpvm.IsMultiValued ? String.Join(",", pValue) : chpvm.GetParameterValue());
                }
            }
        }

        private string[] GetActualDefaultValue(ReportParameterInfo _pi)
        {
            string[] res = null;
            coercedDefaultValues.TryGetValue(_pi.Name, out res);
            if (res == null)
                res = _pi.Values.ToArray();
            return res;
        }

        private IEnumerable<ReportParameterViewModel> GetParametersViewModels(ReportParameterInfo[] _pars = null)
        {
            var pars2process = _pars ?? userParameterInfos;
            IEnumerable<ReportParameterViewModel> res = null;
            if (pars2process != null && pars2process.Length > 0)
                res = pars2process.Where(pi => !String.IsNullOrEmpty(pi.Prompt))
                    .Select(pi => MakeReportParameterViewModel(pi));
            return res;
        }

        private ReportParameterViewModel MakeReportParameterViewModel(ReportParameterInfo _pinfo)
        {
            ReportParameterViewModel res = null;
            if (_pinfo != null)
            {
                string[] coercedDefs = null;
                coercedDefaultValues.TryGetValue(_pinfo.Name, out coercedDefs);
                res = new ReportParameterViewModel(_pinfo, coercedDefs);
                if (_pinfo.Dependents.Any())
                    SubscribeNotifierIfNotYet(res);
            }
            return res;
        }

        private Dictionary<string, string> defsmappings = new Dictionary<string, string>();

        private void GetAlternateDefaults()
        {
            if (GetterOptions.ContainsKey("DefaultValues"))
                foreach (var o in GetterOptions["DefaultValues"])
                {
                    // не устанавливаем сохранённые значения для уже установленных параметров
                    if (ReportInfo.Parameters != null && ReportInfo.Parameters.ContainsKey(o.Key))
                        continue;

                    var par = userParameterInfos.SingleOrDefault(pi => pi.Name == o.Key);
                    if (par != null)
                    {
                        string defvalsKey = o.Value[0].Value;//.Split(',');
                        if (!String.IsNullOrEmpty(defvalsKey))
                        {
                            string[] defval = FindPersistedDefValue(par, defvalsKey);
                            if (defval != null && defval.Length > 0)
                                coercedDefaultValues[o.Key] = defval;
                            defsmappings[o.Key] = defvalsKey;
                        }
                    }
                }
        }

        private void SetAlternateDefaults()
        {
            foreach (var cv in coercedDefaultValues)
            {
                var newpar = CreateReportParameter(cv.Key, cv.Value);
                actualReport.SetParameters(newpar);
            }
        }

        private void ProcessEditorsOptions(bool _init, params ReportParameterViewModel[] _params)
        {
            if (_params == null || _params.Length == 0 || !GetterOptions.ContainsKey("Editors")) return;

            var editors = GetterOptions["Editors"];

            for (int i = 0; i < _params.Length; i++)
            {
                var par = _params[i];
                if (editors.ContainsKey(par.ParamName))
                {
                    var ed = editors.First(e => e.Key == par.ParamName).Value;

                    // выполнять только при инициализации (не выполнять при изменении парметров)
                    if (_init)
                    {
                        var group = ed.FirstOrDefault(p => p.Key == "group");
                        if (!String.IsNullOrEmpty(group.Value))
                        {
                            par.GroupTitle = group.Value;
                            var issingle = ed.FirstOrDefault(p => p.Key == "single");
                            if (!String.IsNullOrEmpty(issingle.Value))
                            {
                                bool bsingle = false;
                                Boolean.TryParse(issingle.Value, out bsingle);
                                par.IsSingleInGroup = bsingle;
                            }
                        }
                        
                    }
                    if (par.IsMultiValued && par.IsAvailableExists && par.AvailableValues.Count > 2)
                    {
                        var selAllName = ed.FirstOrDefault(p => p.Key == "selectallname").Value;
                        if (!String.IsNullOrEmpty(selAllName))
                            par.SelectAllName = selAllName;
                    }
                    else
                        par.SelectAllName = null;

                    if (!par.IsParamsValueValid && par.IsAvailableExists)
                    {
                        var select = ed.FirstOrDefault(p => p.Key == "select");
                        if (!String.IsNullOrEmpty(select.Value))
                        {
                            switch(select.Value)
                            {
                                case "single": if (par.AvailableValues.Count == 1)
                                {
                                    par.AvailableValues.First().Key.IsSelected = true;
                                }
                                break;
                                default: break;
                            }
                        }
                    }

                    if (par.IsParamsValueValid || !String.IsNullOrWhiteSpace(par.SelectedValue) || (!_init && !par.IsVisible))
                    {
                        var hideifopt = ed.FirstOrDefault(p => p.Key == "hideif");
                        if (!String.IsNullOrEmpty(hideifopt.Value))
                        {
                            var hif = hideifopt.Value.Split(':');
                            var hifkind = hif[0];
                            if (hifkind == "single" && par.IsAvailableExists && par.AvailableValues.Count == 1)
                            {
                                // если установлено SelectedValue, но не выбрано допустимое значение, то выбираем
                                if (par.IsMultiValued)
                                {
                                    var sval = par.AvailableValues.First();
                                    if (sval.Value == hif[1] && par.SelectedValue == hif[1] && !sval.Key.IsSelected)
                                        sval.Key.IsSelected = true;
                                }
                                //
                                
                                var values = par.GetValues();
                                if (values.Length == 1 && values[0] == hif[1])
                                    par.IsVisible = false;
                            }
                            else if (hifkind[0] == '!' && !String.IsNullOrWhiteSpace(hif[1]))
                            {
                                var hifParamName = hifkind.TrimStart('!');
                                var hifParam = visibleParams.SingleOrDefault(p => p.ParamName == hifParamName);
                                if (hifParam != null && hifParam.IsParamsValueValid)
                                {
                                    if (_init)
                                    {
                                        hifParam.AddDependentParameter(par);
                                        if (hifParam.Dependents.Length == 0)
                                            SubscribeNotifierIfNotYet(hifParam);
                                    }
                                    par.IsVisible =  Array.IndexOf<String>(hifParam.GetValues(), hif[1]) < 0;
                                }
                            }
                            else
                                par.IsVisible = true;

                        }
                    }

                    if (par.IsMultiValued && par.IsAvailableExists && par.AvailableValues.Count > 2)
                    {
                        var singleValues = ed.FirstOrDefault(p => p.Key == "singlevals");
                        if (!String.IsNullOrEmpty(singleValues.Value))
                        {
                            var svals = singleValues.Value.Split(',');
                            if (svals != null && svals.Length > 0)
                            {
                                var svalsToSet = par.AvailableValues.Values.Join(svals, a => a, sv => sv, (a, sv) => sv).ToArray();
                                if (svalsToSet.Length > 0)
                                    par.SetSingleValues(svalsToSet);
                            }
                        }
                    }
                }
            }
        }

        private string[] FindPersistedDefValue(ReportParameterInfo _par, string _name)
        {
            if (String.IsNullOrEmpty(_name)) return null;
            string[] res = null;
            Object ores = null;
            IPersister persister = CommonSettings.Persister;
            if (persister != null)
            {
                ores = persister.GetValue(_name);
                if (ores != null)
                {
                    var oresstr = ores.ToString();
                    if (_par.MultiValue)
                        res = oresstr.Split(',');
                    else
                        res = new string[] { oresstr };
                }
            }

            return res;
        }

        private void SavePersistedValue(string _name, Object _value)
        {
            if (!String.IsNullOrEmpty(_name))
            {
                IPersister persister = CommonSettings.Persister;
                if (persister != null)
                    persister.SetValue(_name, _value);
            }
        }
    }
}
