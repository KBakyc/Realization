using System;
using System.Linq;
using CommonModule.Commands;
using CommonModule.Helpers;
using DataObjects;
using DataObjects.Interfaces;

namespace CommonModule.ViewModels
{
    public class CountrySelectionViewModel : BaseDlgViewModel
    {
        private IDbService repository;
        private bool useMemory;

        public CountrySelectionViewModel(IDbService _rep, bool _useMemory)
        {
            repository = _rep;
            useMemory = _useMemory;
            countryList = repository.GetCountries(0);
            if (useMemory)
            {
                int kstr = Remember.GetValue<int>("SelCountry");
                if (kstr != 0)
                    SelectCountryByCode(kstr);
            }
        }

        public CountrySelectionViewModel(IDbService _rep)
            :this(_rep, true)
        {
        }

        private Country[] countryList;
        public Country[] CountryList
        {
            get
            {
                return countryList;
            }
        }

        public string CountrySelectionTitle { get; set; }

        private Country selCountry;
        public Country SelCountry
        {
            get
            {
                if (selCountry == null)
                    SelectCountryByCode(3); // РБ
                return selCountry;
            }
            set
            {
                if (value != null && value != selCountry)
                {
                    selCountry = value;
                    if (useMemory)
                        Remember.SetValue("SelCountry", selCountry.Kstr);
                    NotifyPropertyChanged("SelCountry");
                }
            }
        }

        public void SelectCountryByCode(int _kstr)
        {
            SelCountry = countryList.FirstOrDefault(c => c.Kstr == _kstr);
        }

        public override bool IsValid()
        {
            return base.IsValid()
                && SelCountry != null;
        }
    }
}
