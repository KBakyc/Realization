using System.Linq;
using CommonModule.Commands;
using CommonModule.Helpers;
using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Interfaces;
using System.Collections.Generic;

namespace PredoplModule.ViewModels
{
    public class GetPredoplsDlgViewModel : BaseDlgViewModel
    {
        private IDbService repository;
        private PoupAndDatesDlgViewModel poupDatesVM;

        public GetPredoplsDlgViewModel(IDbService _rep)
        {
            repository = _rep;
            poupDatesVM = new PoupAndDatesDlgViewModel(_rep, true, true);
            poupDatesVM.PoupSelection.PropertyChanged += OnPoupChanged;
            ValList = repository.GetValutes().ToArray();
            bankVal = ValList.SingleOrDefault(
                v => v.Kodval == Remember.GetValue<string>("BankValKC"));
            //GetBanksList();
            predoplVal = ValList.SingleOrDefault(
                v => v.Kodval == Remember.GetValue<string>("PredoplValKC"));
            Title = "Получение предоплат из банка";
        }

        void OnPoupChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelPoup" || e.PropertyName == "SelectedPkodsLabel")
                ChangeBankList();
        }

        public PoupAndDatesDlgViewModel PoupDatesSelection { get { return poupDatesVM; } }

        private bool isBankListDirty = true;
        private BankInfo[] banksList;
        public BankInfo[] BanksList
        {
            get 
            {
                if (banksList == null || isBankListDirty)
                    GetBanksList();
                return banksList; 
            }
        }

        private void ChangeBankList()
        {
            isBankListDirty = true; ;
            NotifyPropertyChanged("BanksList");
        }

        private BankInfo selectedBank;
        public BankInfo SelectedBank 
        {
            get { return selectedBank; }
            set { SetAndNotifyProperty("SelectedBank", ref selectedBank, value); }
        }

        private void GetBanksList()
        {
            BankInfo all = new BankInfo { Id = 0, BankName = "Все" };
            BankInfo[] res = new BankInfo[] { all };
            short[] pkods = null;
            if (PoupDatesSelection.SelPkods != null)
                pkods = PoupDatesSelection.SelPkods.Select(p => p.Pkod).ToArray();
            var newbanks = repository.GetBanksFromSchet(PoupDatesSelection.SelPoup.Kod, pkods, BankVal.Kodval);
            res = res.Concat(newbanks).ToArray();
            if (SelectedBank != null)
                SelectedBank = res.FirstOrDefault(b => b.Id == SelectedBank.Id);
            if (SelectedBank == null)
                SelectedBank = all;
            banksList = res;
            isBankListDirty = false;
        }

        public Valuta[] ValList { get; set; }

        private Valuta bankVal;
        public Valuta BankVal
        {
            get
            {
                if (bankVal == null)
                    bankVal = ValList.SingleOrDefault(v => v.Kodval == "RB");
                return bankVal;
            }
            set
            {
                if (value != null && value != bankVal)
                {
                    bankVal = value;
                    Remember.SetValue("BankValKC", value.Kodval);
                    ChangeBankList();
                }
            }
        }

        private Valuta predoplVal;
        public Valuta PredoplVal
        {
            get
            {
                if (predoplVal == null)
                    predoplVal = ValList.SingleOrDefault(v => v.Kodval == "RB");
                return predoplVal;
            }
            set
            {
                if (value != null && value != predoplVal)
                {
                    predoplVal = value;
                    Remember.SetValue("PredoplValKC", value.Kodval);
                    //NotifyPropertyChanged("PredoplVal");
                }
            }
        }

        public override bool IsValid()
        {
            return base.IsValid()
                && PoupDatesSelection.IsValid()
                && BankVal != null && PredoplVal != null;
        }
    }
}