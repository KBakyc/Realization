using System;
using System.Linq;
using System.ComponentModel;
using CommonModule.Commands;
using CommonModule.Helpers;
using DataObjects;
using DataObjects.Interfaces;

namespace CommonModule.ViewModels
{
    public class PoupValDateDlgViewModel : BaseDlgViewModel
    {
        private PoupSelectionViewModel poupSelVm;
        private ValSelectionViewModel valSelVm;
        private IDbService repository;
        private bool isSaveDate;

        public PoupValDateDlgViewModel(IDbService _rep, DateTime? _date)
        {
            repository = _rep;
            isSaveDate = _date != null;

            if (isSaveDate)
                selDate = Remember.GetValue<DateTime>("SelDate");
            else
                selDate = _date;
            
            if (selDate == null || selDate.Value == default(DateTime))
                selDate = DateTime.Now;

            poupSelVm = new PoupSelectionViewModel(repository);
            valSelVm = new ValSelectionViewModel(repository);
        }

        public PoupSelectionViewModel PoupSelection { get { return poupSelVm; } }
        public ValSelectionViewModel ValSelection { get { return valSelVm; } }

        public PoupModel SelPoup
        {
            get { return poupSelVm.SelPoup; }
        }

        public PkodModel[] SelPkods
        {
            get { return poupSelVm.SelPkods; }
        }        

        public Valuta SelVal
        {
            get
            {
                return valSelVm.SelVal;
            }
        }

        public string DateLabel { get; set; }

        private DateTime? selDate;
        public DateTime? SelDate
        {
            get
            {
                if (selDate == null || selDate.Value == default(DateTime))
                    selDate = DateTime.Now;
                return selDate;
            }
            set
            {
                if (value != selDate)
                {
                    selDate = value;
                    if (isSaveDate)
                        Remember.SetValue("SelDate", selDate);
                    NotifyPropertyChanged("SelDate");
                }
            }
        }

        public override bool IsValid()
        {
            return base.IsValid()
                && SelDate != null
                && PoupSelection.IsValid()
                && ValSelection.IsValid();
        }
    }
}
