using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Reporting.WinForms;
using CommonModule.ViewModels;
using CommonModule.Interfaces;
using DataObjects;
using System.Xml.Linq;


namespace CommonModule.Helpers
{
    public abstract class BaseReportParametersGetter : IReportParametersGetter
    {
        protected const string CONSTR_PARAM_NAME = "ConnString";

        protected List<ReportParameter> repParams = new List<ReportParameter>();

        private ReportModel reportInfo;
        public ReportModel ReportInfo 
        {
            get { return reportInfo; }
            protected set { reportInfo = value; }
        }
        
        public BaseReportParametersGetter()
        {
        }

        public IEnumerable<ReportParameter> ReportParameters
        {
            get 
            {
                //CoerceParameters();
                ExplicitSetParamsFromOptions();
                return repParams; 
            }
        }

        public virtual ReportMode GetReportFeatures()
        {
            ReportMode res = new ReportMode();
            if (ReportInfo != null)
            {
                res.isCanA3 = ReportInfo.IsA3Enabled;
                res.IsCanPrint = true;
                res.isCanView = true;
            };
            return res;
        }

        protected virtual void CoerceParameters()
        {
            
            var connstr = repParams.SingleOrDefault(p => p.Name == CONSTR_PARAM_NAME);
            if (connstr != null && connstr.Values[0] != CommonSettings.ConnectionString)
            {
                var indConnStr = repParams.IndexOf(connstr);
                repParams[indConnStr] = new ReportParameter(CONSTR_PARAM_NAME, CommonSettings.ConnectionString);
            }

        }

        protected virtual void ExplicitSetParamsFromOptions()
        {
            if (!GetterOptions.ContainsKey("SetParams")) return;
            foreach (var o in GetterOptions["SetParams"])
            {
                var opar = repParams.SingleOrDefault(pi => pi.Name == o.Key);
                var npar = CreateReportParameter(o.Key, o.Value[0].Value);
                if (npar != null)
                {
                    if (opar != null)
                        repParams.Remove(opar);
                    repParams.Add(npar);
                }
            }
        }


        public abstract BaseDlgViewModel GetDialog(ReportModel _repInfo, Action _onSubmit);

        protected virtual ReportParameter CreateReportParameter(string _name, params string[] _values)
        { 
            if (String.IsNullOrEmpty(_name) || _values == null) return null;
            ReportParameter res = new ReportParameter(_name, _values);
            return res;
        }

        private Dictionary<string, Dictionary<string, KeyValuePair<string, string>[]>> getterOptions;
        protected Dictionary<string, Dictionary<string, KeyValuePair<string, string>[]>> GetterOptions
        {
            get
            {
                if (getterOptions == null)
                    getterOptions = GetOptions();
                return getterOptions;
            }
        }

        protected virtual Dictionary<string, Dictionary<string,KeyValuePair<string,string>[]>> GetOptions()
        {
            Dictionary<string, Dictionary<string, KeyValuePair<string, string>[]>> res = new Dictionary<string, Dictionary<string, KeyValuePair<string, string>[]>>();
            if (ReportInfo != null && !String.IsNullOrEmpty(ReportInfo.ParamsGetterOptions))
            {
                try
                {
                    //XElement parsed = XElement.Parse(ReportInfo.ParamsGetterOptions);
                    XElement getteropts = ParseGetterOptions(ReportInfo.ParamsGetterOptions);
                    var els = getteropts.Elements();
                    foreach (var optcont in els)
                    {
                        var optcname = optcont.Name.ToString();
                        Dictionary<string, KeyValuePair<string, string>[]> optsitems = null;
                        if (optcont.HasElements)
                        {
                            optsitems = new Dictionary<string, KeyValuePair<string, string>[]>();
                            foreach (var opt in optcont.Elements())
                            {
                                var optname = opt.Name.ToString();
                                KeyValuePair<string, string>[] optdata = null;
                                if (opt.HasAttributes)
                                    optdata = opt.Attributes().Select(a => new KeyValuePair<string,string>(a.Name.ToString(), a.Value)).ToArray();
                                if (optdata != null && optdata.Length > 0)
                                    optsitems[optname] = optdata;
                            }
                        }
                        if (optsitems != null && optsitems.Count > 0)
                            res.Add(optcname, optsitems);
                    }
                }
                catch (Exception e)
                {
                    WorkFlowHelper.OnCrash(e);
                }
            }
            return res;
        }

        private XElement ParseGetterOptions(string _optstr)
        {
            if (String.IsNullOrEmpty(_optstr)) return null;

            IEnumerable<XElement> data = null;
            XElement res = null;

            try
            {
                res = new XElement("GetterOptions", XElement.Parse(_optstr));
            }
            catch {}

            if (res == null)
                try
                {
                    data = XElement.Parse(String.Format(@"<r>{0}</r>", _optstr)).Elements();
                    res = new XElement("GetterOptions", data);
                }
                catch {}

            return res;
        }

        public virtual IEnumerable<ReportParameter[]> GetSplittedParams(ReportParameter _par)
        {
            if (_par == null) yield break;
            var parInd = repParams.IndexOf(_par);
            var oldValues = _par.Values;
            for (int i = 0; i < oldValues.Count; i++)
            {
                var newPar = new ReportParameter(_par.Name, oldValues[i]);
                repParams[parInd] = newPar;
                yield return ReportParameters.ToArray();
            }
            //reportParams[parInd] = splitPar;
        }
    }
}
