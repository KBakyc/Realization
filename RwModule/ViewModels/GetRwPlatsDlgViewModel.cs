using System.Linq;
using CommonModule.Commands;
using CommonModule.Helpers;
using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Interfaces;
using System.Collections.Generic;
using RwModule.Models;
using System;
using DAL;

namespace RwModule.ViewModels
{
    public class GetRwPlatsDlgViewModel : BaseDlgViewModel
    {
        private IDbService repository;
        private DateRangeDlgViewModel datesVM;

        public GetRwPlatsDlgViewModel(IDbService _rep)
        {
            repository = _rep;
            datesVM = new DateRangeDlgViewModel(true);
            rwUslTypes = Enumerations.GetAllValuesAndDescriptions<RwUslType>();
            bankGroups = repository.GetBankGroups();
            GetBanksList();
            Title = "Получение платежей по банку";
        }

        public DateRangeDlgViewModel DatesSelection { get { return datesVM; } }

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

        //private void ChangeBankList()
        //{
        //    isBankListDirty = true; ;
        //    NotifyPropertyChanged("BanksList");
        //}

        private BankInfo selectedBank;
        public BankInfo SelectedBank
        {
            get { return selectedBank; }
            set { SetAndNotifyProperty("SelectedBank", ref selectedBank, value); }
        }

        private RwUslType selRwUslType;
        public RwUslType SelRwUslType
        {
            get { return selRwUslType; }
            set
            {
                if (value != selRwUslType)
                {
                    selRwUslType = value;
                    NotifyPropertyChanged("SelRwUslType");
                    GetBanksList();
                }
            }
        }

        Dictionary<RwUslType, string> rwUslTypes;
        public Dictionary<RwUslType, string> RwUslTypes
        {
            get { return rwUslTypes; }
        }

        private void GetBanksList()
        {
            BankInfo all = new BankInfo { Id = 0, BankName = "Все" };
            BankInfo[] res = new BankInfo[] { all };
            var newbanks = GetBanksByRwUsl(selRwUslType);
            if (newbanks != null && newbanks.Length > 0)
                res = res.Concat(newbanks).ToArray();
            if (SelectedBank != null)
                SelectedBank = res.FirstOrDefault(b => b.Id == SelectedBank.Id);
            if (SelectedBank == null)
                SelectedBank = all;
            banksList = res;
            isBankListDirty = false;
        }

        private BankInfo[] bankGroups;

        private BankInfo[] GetBanksByRwUsl(RwUslType _rwUslType)
        {
            BankInfo[] res = null;
            RwFromBankSetting[] settByType = null;
            using (var db = new RealContext())
            {
                settByType = (from s in db.RwFromBankSettings
                              where s.IdUslType == _rwUslType
                              select s).ToArray();
            }
            if (settByType != null && settByType.Length > 0)
            {
                res = settByType.Join(bankGroups, s => s.IdBankGroup, b => b.Id, (s, b) => new BankInfo
                {
                    Id = (int)s.IdBankGroup,
                    BankName = String.Format("{0} - {1}", s.Credit, b.BankName)
                }).ToArray();
            }
            return res;
        }

        

        public override bool IsValid()
        {
            return base.IsValid()
                && DatesSelection.IsValid();
        }
    }
}