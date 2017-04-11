using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.ViewModels;
using DataObjects.Interfaces;
using CommonModule.DataViewModels;
using DataObjects;
using DotNetHelper;
using System.Windows.Input;
using CommonModule.Commands;

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
            providerSeekCommand = new DelegateCommand(ExecuteProviderSeek);
            numberSeekCommand = new DelegateCommand(ExecuteNumberSeek);
        }

        private void ExecuteNumberSeek()
        {
            SeekEsfns();
        }

        private void SeekEsfns()
        {
            var providerIncomeEsfns = IncomeESFNs;
            var incomeEsfns = String.IsNullOrWhiteSpace(numberSeekPat) ? providerIncomeEsfns : providerIncomeEsfns.Where(s => s.Item2.EndsWith(numberSeekPat));

            var esfns = incomeEsfns.ToArray();
            selectESFN.LoadData(esfns);

            if (esfns.Length == 1) selectESFN.SelectedESFN = esfns[0];
        }

        private void ExecuteProviderSeek()
        {
            var oldSelectedProvider = selectedProvider;
            var foundProviders = Providers;            
            NotifyPropertyChanged(() => Providers);
            if (foundProviders.Length == 0) return;
            if (foundProviders.Length == 1) SelectedProvider = foundProviders[0];
            else
                if (foundProviders.Contains(oldSelectedProvider)) SelectedProvider = oldSelectedProvider;
        }

        private string numberSeekPat;
        public string NumberSeekPat
        {
            get { return numberSeekPat; }
            set { SetAndNotifyProperty("NumberSeekPat", ref numberSeekPat, value); }
        }

        private string providerSeekPat;
        public string ProviderSeekPat
        {
            get { return providerSeekPat; }
            set { SetAndNotifyProperty("ProviderSeekPat", ref providerSeekPat, value); }
        }

        private ICommand providerSeekCommand;
        public ICommand ProviderSeekCommand
        {
            get { return providerSeekCommand; }
        }

        private ICommand numberSeekCommand;
        public ICommand NumberSeekCommand
        {
            get { return numberSeekCommand; }
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
                SeekEsfns();
            }
        }

        private KeyValueObj<string,string>[] providers;
        public KeyValueObj<string, string>[] Providers
        {
            get
            {
                var res = providers;
                if (!String.IsNullOrWhiteSpace(providerSeekPat))
                {
                    var pat = providerSeekPat.Trim().ToLowerInvariant();
                    res = (pat.All(c => char.IsDigit(c))) ? providers.Where(p => p.Key == pat).ToArray()
                                                          : providers.Where(p => p.Value.ToLowerInvariant().Contains(pat)).ToArray();
                }
                return res;
            }
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
