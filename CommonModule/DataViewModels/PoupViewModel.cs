using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Interfaces;

namespace CommonModule.DataViewModels
{
    public class PoupViewModel : BasicViewModel
    {
        private IDbService repository;
        private int poupKod;
        private short pkodKod;
        private PoupModel poup;
        private PkodModel pkod;

        public PoupViewModel(IDbService _repository, int _poup, short _pkod)
        {
            repository = _repository;
            poupKod = _poup;
            pkodKod = _pkod;
            LoadData();
        }

        
        public PoupModel Poup
        {
            get { return poup; }
        }

        public PkodModel Pkod
        {
            get { return pkod; }
        }
        
        public string ShortTitle { get; set; }
        
        private void LoadData()
        {
            if (repository != null)
            {
                poup = repository.Poups[poupKod];
                pkod = repository.GetPkod(poupKod, pkodKod);
            }
            FormatTitles();
        }

        private void FormatTitles()
        {
            Title = String.Format("[{0}] {1}", poupKod, (poup == null ? "" : poup.Name));
            if (pkod != null)
            {
                Title += String.Format(" / ({0}) {1}", pkodKod, pkod.Name);
                ShortTitle = String.Format("{0}/{1}", poupKod, pkodKod);
            }
            else
                ShortTitle = poupKod.ToString();
        }

        public int SortBy
        {
            get { return poupKod * 1000 + pkodKod; }
        }

    }
}
