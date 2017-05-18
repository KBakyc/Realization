using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.Helpers;
using ServiceModule.DAL.Models;
using DataObjects;

namespace ServiceModule.ViewModels
{
    /// <summary>
    /// Модель отображения строки из статистики отчёта.
    /// </summary>
    public class UserReportStatViewModel : BasicNotifier
    {
        private UserReportStat model;

        public UserReportStatViewModel(UserReportStat _model)
        {
            model = _model;
            ParseParameters();
        }

        private void ParseParameters()
        {
            if (String.IsNullOrWhiteSpace(Parameters)) return;
            var ps = System.Web.HttpUtility.ParseQueryString(Parameters); ;            
            parsedParameters = ps.AllKeys.Select(k => new KeyValueObj<string, string>(k, ps[k])).ToList();

            for (int i = 0; i < parsedParameters.Count; i++)
            {
                var k = parsedParameters[i].Key;
                if (k.EndsWith(":isnull"))
                {
                    var compKey = k.Split(':');
                    var newKey = compKey[0];
                    var oldValue = parsedParameters[i].Value;
                    var newValue = oldValue.ToLower() == "true" ? null : oldValue;
                    parsedParameters[i].Key = newKey;
                    parsedParameters[i].Value = newValue;
                }
            }

            //parsedParameters = Parameters.Split('&').Select(pp => pp.Split('=')).Select(pa => new KeyValueObj<string, string>(pa[0], pa[1])).OrderBy(kv => kv.Key == "ConnString" ? 1 : 0).ToArray();
        }

        public int? UserId { get { return model.UserId; } }
        public string UserName { get { return model.UserName; } }
        public string UserFIO { get { return model.UserFIO; } }
        public bool? IsActiveUser { get { return model.IsActiveUser; } }
        public string ReportPath { get { return model.ReportPath; } }
        public string Parameters { get { return model.Parameters; } }
        public DateTime TimeStart { get { return model.TimeStart; } }
        public DateTime TimeEnd { get { return model.TimeEnd; } }
        public string Format { get { return model.Format; } }
        public string ReportAction { get { return model.ReportAction; } }

        private List<KeyValueObj<string, string>> parsedParameters;
        public List<KeyValueObj<string, string>> ParsedParameters
        {
            get { return parsedParameters; }
        }
    }
}
