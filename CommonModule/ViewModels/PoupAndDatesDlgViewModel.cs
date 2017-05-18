using System;
using System.Linq;
using System.ComponentModel;
using System.Windows.Input;
using CommonModule.Commands;
using DataObjects;
using DataObjects.Interfaces;
using CommonModule.Helpers;


namespace CommonModule.ViewModels
{
    /// <summary>
    /// Модель диалога выбора направления реализации и интервала дат.
    /// </summary>
    public class PoupAndDatesDlgViewModel : BaseDlgViewModel
    {
        private PoupSelectionViewModel poupSelVm;
        private DateRangeDlgViewModel dateRangeVm;
        private IDbService repository;

        public PoupAndDatesDlgViewModel(IDbService _rep, bool _save, bool _isMultiPkod)
        {
            repository = _rep;
            poupSelVm = new PoupSelectionViewModel(_rep, _isMultiPkod);
            dateRangeVm = new DateRangeDlgViewModel(_save);            
        }

        public PoupAndDatesDlgViewModel(IDbService _rep, bool _save)
            :this(_rep, _save, false)
        {}

        public PoupAndDatesDlgViewModel(IDbService _rep)
            :this(_rep, true)
        {}

        public override bool IsValid()
        {
            return base.IsValid()
                && PoupSelection.IsValid()
                && DatesSelection.IsValid();
        }

        public PoupSelectionViewModel PoupSelection { get { return poupSelVm; } }
        public DateRangeDlgViewModel DatesSelection { get { return dateRangeVm; } }

        public PoupModel SelPoup
        {
            get { return poupSelVm.SelPoup; }
        }

        public PkodModel[] SelPkods
        {
            get { return poupSelVm.SelPkods; }
        }        

        public bool IsPkodEnabled
        {
            get { return poupSelVm.IsPkodEnabled; }
        }

        public DateTime DateFrom
        {
            get { return dateRangeVm.DateFrom; }
            set { dateRangeVm.DateFrom = value; }
        }

        public DateTime DateTo
        {
            get { return dateRangeVm.DateTo; }
            set { dateRangeVm.DateTo = value; }
        }

    }
}
