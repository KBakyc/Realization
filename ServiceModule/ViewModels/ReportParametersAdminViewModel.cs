using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.Helpers;
using Microsoft.Reporting.WinForms;
using DataObjects;
using System.Xml.Linq;
using System.Windows.Input;
using CommonModule.Commands;

namespace ServiceModule.ViewModels
{
    /// <summary>
    /// Модель отображения режима настройки параметров отчёта.
    /// </summary>
    public class ReportParametersAdminViewModel : BasicNotifier
    {
        private EditedReportInfoViewModel eReportInfo;
        private ServerReport sReport;        

        public ReportParametersAdminViewModel(EditedReportInfoViewModel _ereport)
        {
            eReportInfo = _ereport;
            Init();            
        }

        private void Init()
        {
            if (eReportInfo != null && !String.IsNullOrWhiteSpace(eReportInfo.Path))
            {
                sReport = new ServerReport();
                sReport.InitActualReport(new ReportModel { Mode = ReportModes.Server, Path = eReportInfo.Path });
                parameters = sReport.GetParameters();
                InitParamSettings();
            }            
        }

        private void InitParamSettings()
        {
            if (eReportInfo.ParamsGetterOptionsXML != null)
            {
                xEditors = eReportInfo.ParamsGetterOptionsXML.Elements("Editors").FirstOrDefault();
                xDefaults = eReportInfo.ParamsGetterOptionsXML.Elements("DefaultValues").FirstOrDefault();
            }
            foreach (var p in parameters)
            {
                var ps = ParseParamSettings(p);
                if (ps != null)
                    paramSettings[p] = ps;
            }
            paramSettingsInitialCount = paramSettings.Count;
        }

        private XElement xEditors;
        private XElement xDefaults;

        private ReportParameterInfoCollection parameters;
        public ReportParameterInfoCollection Parameters
        {
            get { return parameters; }
        }

        private ReportParameterInfo selParameter;
        public ReportParameterInfo SelParameter
        {
            get { return selParameter; }
            set 
            { 
                if (SetAndNotifyProperty(() => SelParameter, ref selParameter, value))
                    GetSelParameterSettings();
            }
        }

        private int paramSettingsInitialCount = 0;
        private Dictionary<ReportParameterInfo, ParameterSettingsViewModel> paramSettings = new Dictionary<ReportParameterInfo, ParameterSettingsViewModel>();

        private void GetSelParameterSettings()
        {
            selParameterSettings = selParameter != null && paramSettings.ContainsKey(selParameter) ? paramSettings[selParameter] : null;
            isSelParameterSettingsEnabled = selParameterSettings != null;
            NotifyPropertyChanged(() => SelParameterSettings); 
            NotifyPropertyChanged(() => IsSelParameterSettingsEnabled);
        }

        private ParameterSettingsViewModel selParameterSettings;
        public ParameterSettingsViewModel SelParameterSettings
        {
            get { return  selParameterSettings; }
            set 
            {
                selParameterSettings = (selParameter == null) ? null : value;
                NotifyPropertyChanged(() => SelParameterSettings);
                NotifyPropertyChanged(() => IsSelParameterSettingsEnabled);
            }
        }

        private bool isSelParameterSettingsEnabled;
        public bool IsSelParameterSettingsEnabled
        {
            get { return isSelParameterSettingsEnabled || selParameterSettings != null; }
            set 
            {
                if (selParameter != null)
                {
                    if (!value)
                    {
                        paramSettings.Remove(selParameter);
                        SelParameterSettings = null;
                    }
                    else
                    {
                        SelParameterSettings = ParseParamSettings(selParameter) ?? new ParameterSettingsViewModel(selParameter);
                        paramSettings[selParameter] = selParameterSettings;
                    }
                }         
                SetAndNotifyProperty(() => IsSelParameterSettingsEnabled, ref isSelParameterSettingsEnabled, value);
            }
        }

        private ParameterSettingsViewModel ParseParamSettings(ReportParameterInfo _param)
        {            
            if (_param == null) return null;
            ParameterSettingsViewModel res = null;                 
            if (xEditors != null || xDefaults != null)
            {
                var editorOpt = xEditors != null ? xEditors.Descendants(_param.Name).FirstOrDefault() : null;
                var defaultOpt = xDefaults != null ? xDefaults.Descendants(_param.Name).FirstOrDefault() : null;
                if (editorOpt != null || defaultOpt != null)
                {
                    res = new ParameterSettingsViewModel(_param);       
                    res.Parse(editorOpt, defaultOpt);
                }
            }
            return res;
        }

        public bool ParamsSettingsChanged
        {
            get { return paramSettings.Count < paramSettingsInitialCount || paramSettings.Any(kv => kv.Value != null && kv.Value.IsChanged()); }
        }       

        public void SaveParamsSettings()
        {
            if (xDefaults != null)
            {
                xDefaults.Remove();
                xDefaults = null;
            }            

            if (xEditors != null)
            {
                xEditors.Remove();
                xEditors = null;
            }

            var newEditors = new XElement("Editors");
            var newDefaults = new XElement("DefaultValues");
            foreach (var ps in paramSettings.Where(kv => kv.Value != null && kv.Value.IsValid))
            {
                newEditors.Add(ps.Value.EditorSettings);
                newDefaults.Add(ps.Value.DefaultSettings);
            }

            string getterOpt = "";
            if (newDefaults.Descendants().Any())
                getterOpt += newDefaults.ToString();
            if (newEditors.Descendants().Any())
                getterOpt += newEditors.ToString();
            eReportInfo.ParamsGetterOptions = getterOpt;
            InitParamSettings();            
        }
    }
}
