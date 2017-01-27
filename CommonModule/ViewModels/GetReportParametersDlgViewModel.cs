using System.Collections.Generic;
using System.Linq;
using CommonModule.Commands;
using System.Collections.ObjectModel;
using DataObjects;
using System;
using Microsoft.Reporting.WinForms;

namespace CommonModule.ViewModels
{
    public class ParameterGroup : BasicViewModel
    {
        public ObservableCollection<ReportParameterViewModel> Parameters { get; set; }

        private bool? isVisible = null;
        public bool IsVisible
        {
            get
            {
                return isVisible.HasValue ? isVisible.Value
                                          : Parameters != null && Parameters.Count > 0 && Parameters.Any(p => p.IsVisible);
            }
            set
            {
                if (isVisible == null || isVisible.Value != value)
                {
                    isVisible = value;
                    NotifyPropertyChanged("IsVisible");
                }
            }
        }

        public ParameterGroup(string _ttl, IEnumerable<ReportParameterViewModel> _pars)
        {
            Title = _ttl;
            Parameters = new ObservableCollection<ReportParameterViewModel>(_pars);
        }

        public void Refresh()
        {
            NotifyPropertyChanged("IsVisible");
        }
    }

    public class GetReportParametersDlgViewModel : BaseDlgViewModel
    {        
        private ParameterGroup[] parameterGroups;
        private ReportParameterViewModel[] allParameters;


        public GetReportParametersDlgViewModel(IEnumerable<ReportParameterViewModel> _pars)
        {            
            LoadParameters(_pars);            
        }

        public override bool IsValid()
        {
            var isval = !isBusy && allParameters.All(p => !p.IsEditing && p.IsParamsValueValid);
            return isval;
        }

        private bool isBusy;
        public bool IsBusy
        {
            get { return isBusy; }
            set { SetAndNotifyProperty("IsBusy", ref isBusy, value); }
        }

        public void LoadParameters(IEnumerable<ReportParameterViewModel> _pars)
        {
            allParameters = _pars.ToArray();
            var newpars = allParameters.GroupBy(p => String.IsNullOrEmpty(p.GroupTitle) ? new { Group = String.Empty, Param = p.ParamName }
                                                                                        : new { Group = p.GroupTitle, Param = String.Empty })
                                       .Select(g => new ParameterGroup(g.Key.Group == String.Empty ? null : g.Key.Group, g))
                                       .ToArray();
            SetAndNotifyProperty("ParameterGroups", ref parameterGroups, newpars);
        }

        public void UpdateParameters(params ReportParameterViewModel[] _pars)
        {
            if (_pars == null) return;
            for (int i = 0; i < _pars.Length; i++ )
            {
                var newpar = _pars[i];
                var pargroup = parameterGroups.FirstOrDefault(pg => pg.Parameters.Any(p => p.ParamName == newpar.ParamName));
                if (pargroup != null)
                    pargroup.Refresh();
                newpar.NotifyChanges();
            }
           //allParameters = parameterGroups.SelectMany(g => g.Parameters).ToArray();           
        }


        public ParameterGroup[] ParameterGroups
        {
            get { return parameterGroups; }
        }
        
        public ReportParameterViewModel[] AllParameters
        {
            get { return allParameters.Where(p => p.IsVisible).ToArray(); }
        }


    }
}