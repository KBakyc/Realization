using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using CommonModule.Commands;
using DataObjects;
using DataObjects.Interfaces;
using CommonModule.Helpers;


namespace CommonModule.ViewModels
{
    /// <summary>
    /// Модель диалога выбора интервала дат и контрагента.
    /// </summary>
    public class KpokDatesDlgViewModel : BaseDlgViewModel
    {
        private DateRangeDlgViewModel datesSelection;
        private KaSelectionViewModel kaSelection;
        private IDbService repository;
        public KpokDatesDlgViewModel(IDbService _rep, bool _save)
        {
            repository = _rep;
            datesSelection = new DateRangeDlgViewModel(_save);
            kaSelection = new KaSelectionViewModel(repository);           
            KaTitle = "Контрагент";
            IsKaTypeSelection = true;
        }

        public KpokDatesDlgViewModel(IDbService _rep)
            : this(_rep, true)
        { }

        public override bool IsValid()
        {
            return base.IsValid()
                && datesSelection.IsValid()
                && kaSelection.IsValid()
                && (IsKpok || IsKgr);

        }

        public DateRangeDlgViewModel DatesSelection
        {
            get { return datesSelection; }
        }

        public KaSelectionViewModel KaSelection
        {
            get { return kaSelection; }
        }

        public bool IsKaTypeSelection { get; set; }

        public bool IsKpok { get; set; }

        public bool IsKgr { get; set; }

        public string KaTitle { get; set; }
    }
}
