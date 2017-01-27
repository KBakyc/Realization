using System;
using CommonModule.Commands;
using DataObjects;
using System.ComponentModel;
using System.Windows.Input;
using System.Linq;
using DAL;
using System.Collections.Generic;
using DataObjects.Interfaces;

namespace CommonModule.ViewModels
{
    public class KursesListViewModel : BaseDlgViewModel
    {
        private IDbService repository;
        private DateTime onDate;

        public KursesListViewModel(IDbService _rep)
        {
            repository = _rep;
        }

        public KursesListViewModel(IDbService _rep, string _kodval, DateTime _ondate)
            : this(_rep)
        {
            onDate = _ondate;
            LoadData(_kodval, _ondate);
        }

        private void LoadData(string _kodval, DateTime _ondate)
        {
            Kurses = repository.GetKurses(_kodval, _ondate);
            SelKurs = kurses.Where(k => k.Item1 <= _ondate).FirstOrDefault();
            SelVal = repository.GetValutaByKod(_kodval);
        }

        private Tuple<DateTime, decimal, int>[] kurses;
        public Tuple<DateTime, decimal, int>[] Kurses
        {
            get { return kurses; }
            set
            {
                if (value != kurses)
                {
                    kurses = value;
                    NotifyPropertyChanged("Kurses");
                }
            }
        }

        private Tuple<DateTime, decimal, int> selKurs;
        public Tuple<DateTime, decimal, int> SelKurs
        {
            get { return selKurs; }
            set { SetAndNotifyProperty("SelKurs", ref selKurs, value); }
        }

        private Valuta selVal;
        public Valuta SelVal
        {
            get { return selVal; }
            set
            {
                SetAndNotifyProperty("SelVal", ref selVal, value);
            }
        }

    }
}