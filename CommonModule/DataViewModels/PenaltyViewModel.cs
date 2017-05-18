using System;
using CommonModule.ViewModels;
using DAL;
using DataObjects;
using DataObjects.Interfaces;
using DataObjects.Helpers;

//using Realization.ViewModels;

namespace CommonModule.DataViewModels
{
    /// <summary>
    /// Модель отображения штрафной санкции.
    /// </summary>
    public class PenaltyViewModel : BasicViewModel
    {
        private IDbService repository;

        public PenaltyViewModel(IDbService _rep, PenaltyModel _pen)
        {
            repository = _rep;
            penRef = _pen;
            sumOpl = penRef.Sumopl;
        }

        private PenaltyModel penRef;
        public PenaltyModel PenRef
        {
            get { return penRef; }
        }


        private bool isSelected;
        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                if (value != isSelected)
                    isSelected = value;
                NotifyPropertyChanged("IsSelected");
            }
        }

        // Плательщик
        private KontrAgent platelschik;
        public KontrAgent Platelschik
        {
            get
            {
                if (platelschik == null)
                    platelschik = repository.GetKontrAgent(penRef.Kpok);
                return platelschik;
            }
        }

        public string Nomish 
        {
            get { return penRef.Nomish; }
        }

        public int Nomkro
        {
            get { return penRef.Nomkro; }
        }

        public DateTime Datkro 
        {
            get { return penRef.Datkro; }
        }
        
        public DateTime Datgr
        {
            get { return penRef.Datgr; }
        }
        
        public decimal Sumpenalty 
        {
            get { return penRef.Sumpenalty; } 
        }
        
        public string Kodval 
        {
            get { return penRef.Kodval; }
        }

        public int NomKRO
        {
            get { return penRef.Nomkro; }
        }

        private decimal sumOpl;
        public decimal SumOpl
        {
            get { return sumOpl; }
            set 
            { 
                sumOpl = value;
                NotifyPropertyChanged("SumOpl");
                NotifyPropertyChanged("SumOst");
            }
        }

        public decimal SumOst
        {
            get { return Sumpenalty - SumOpl; }
        }


        public bool IsClosed { get { return Sumpenalty <= SumOpl; } }

    }
}