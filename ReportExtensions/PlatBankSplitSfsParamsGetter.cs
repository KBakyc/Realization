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


namespace ReportExtensions
{
    [Export("ReportExtensions.PlatBankSplitSfsParamsGetter", typeof(IReportParametersGetter))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class PlatBankSplitSfsParamsGetter : GenericReportParametersGetter
    {
        private const string IDSFS_PARAM_NAME = "idsfs";
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

        public PlatBankSplitSfsParamsGetter()
        {
        }

        private bool isSpliting = true;

        public override DataObjects.ReportMode GetReportFeatures()
        {
            var fea = base.GetReportFeatures();
            if (isSpliting && !String.IsNullOrEmpty(IDSFS_PARAM_NAME))
                fea.SplitingParameter = IDSFS_PARAM_NAME;
            return fea;
        }

        public override IEnumerable<ReportParameter[]> GetSplittedParams(ReportParameter _par)
        {
            if (_par == null || _par.Name != "idsfs") yield break;
            var parInd = repParams.IndexOf(_par);
            var oldValues = _par.Values;

            Dictionary<int, string[]> groups = oldValues.OfType<string>()
                                                  .Select(s => Repository.GetSfModel(int.Parse(s)))
                                                  .Select(m => new { Idsf = m.IdSf, Idporsh = Repository.GetDogInfo(m.IdDog, false).Idporsh })
                                                  .GroupBy(i => i.Idporsh)
                                                  .ToDictionary(g => g.Key, g => g.Select(i => i.Idsf.ToString()).ToArray());

            foreach (var kv in groups)
            {
                var newPar = new ReportParameter(_par.Name, kv.Value);
                var pars = ReportParameters.ToArray();
                pars[parInd] = newPar;
                yield return pars;
            }
        }

    }
}
