using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.ViewModels;
using DataObjects.Interfaces;
using CommonModule.DataViewModels;
using DataObjects;
using DotNetHelper;

namespace SfModule.ViewModels
{
    public class LinkSfToIncomeDlgViewModel : BaseDlgViewModel
    {
        private IDbService repository;
        private SfViewModel sfv;

        public LinkSfToIncomeDlgViewModel(IDbService _repository, SfViewModel _sfv)
        {
            repository = _repository;
            sfv = _sfv;
            LoadData();
        }

        private SelectESFNDlgViewModel selectESFN;
        public SelectESFNDlgViewModel SelectESFN
        {
            get { return selectESFN; }
        }

        private Tuple<int, string, DateTime?, string, string, decimal?, string>[] allIncomeESFNs { get; set; }
        public IEnumerable<Tuple<int, string, DateTime?, string, string, decimal?, string>> IncomeESFNs
        {
            get { return selectedProvider == null ? allIncomeESFNs : allIncomeESFNs.Where(i => i.Item4 == selectedProvider.Value && i.Item5 == selectedProvider.Key); }
        }

        public int SelectedIncomeInvoiceId { get { return selectESFN.SelectedESFN == null ? 0 : selectESFN.SelectedInvoiceId; } }

        private KeyValueObj<string,string> selectedProvider;
        public KeyValueObj<string,string> SelectedProvider
        {
            get { return selectedProvider; }
            set 
            { 
                SetAndNotifyProperty("SelectedProvider", ref selectedProvider, value);
                //NotifyPropertyChanged("IncomeESFNs");
                selectESFN.LoadData(IncomeESFNs.ToArray());
            }
        }

        private KeyValueObj<string,string>[] providers;
        public KeyValueObj<string, string>[] Providers
        {
            get { return providers; }
            set { SetAndNotifyProperty("Providers", ref providers, value); }
        }

        private void LoadData()
        {
            allIncomeESFNs = repository.Get_Income_ESFN(sfv.SfRef.IdSf);
            Providers = IncomeESFNs.Select(i => new KeyValueObj<string, string>(i.Item5, i.Item4)).DistinctBy(kv => kv.Key + kv.Value).OrderBy(kv => kv.Key).ToArray();
            selectESFN = new SelectESFNDlgViewModel(allIncomeESFNs);
        }

        public override bool IsValid()
        {
            return base.IsValid()
                && selectESFN.IsValid()
                && SelectedIncomeInvoiceId != 0
                && selectESFN.TotalSumOfSelectedESFN >= sfv.SumPltr;
        }
    }
}
