using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CommonModule.Commands;
using CommonModule.Helpers;
using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Interfaces;
using System;
using System.Collections.ObjectModel;

namespace SfModule.ViewModels
{
    public class OtgrDocsDlgViewModel : BaseDlgViewModel
    {
        protected OtgrDocListViewModel otgrDocsVM;
        protected IDbService repository;

        public OtgrDocsDlgViewModel(IDbService _rep, IEnumerable<OtgrDocModel> _docs)
        {
            repository = _rep;
            var docsVMs = _docs.Select(d => new Selectable<OtgrDocModel>(d, false));
            otgrDocsVM = new OtgrDocListViewModel(repository, docsVMs, false);
            otgrDocsVM.PropertyChanged += BasePropertyChanged;
        }

        Func<OtgrDocModel, bool> chkPredicate;

        public OtgrDocsDlgViewModel(IDbService _rep, IEnumerable<OtgrDocModel> _docs, Func<OtgrDocModel, bool> _chkPredicate)
        {
            repository = _rep;
            chkPredicate = _chkPredicate;
            var docsVMs = _docs.Select(d => new Selectable<OtgrDocModel>(d, _chkPredicate == null ? false : _chkPredicate(d)));
            otgrDocsVM = new OtgrDocListViewModel(repository, docsVMs, false);
            otgrDocsVM.PropertyChanged += BasePropertyChanged;
        }

        protected void BasePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            NotifyPropertyChanged(e.PropertyName);
        }

        public override bool IsValid()
        {
            return base.IsValid()
                && (chkPredicate == null && SelectedOtgrDocs.Any() || chkPredicate != null && SelectedOtgrDocs.Any(chkPredicate));
        }

        /// <summary>
        /// Отгрузочные документы
        /// </summary>
        public ObservableCollection<Selectable<OtgrDocViewModel>> OtgrDocs
        {
            get { return otgrDocsVM.OtgrDocs; }
        }

        public decimal Kolf
        {
            get { return otgrDocsVM.Kolf; }
        }

        public decimal Count
        {
            get { return otgrDocsVM.Count; }
        }

        /// <summary>
        /// Активный (текущий документ)
        /// </summary>
        public Selectable<OtgrDocViewModel> SelectedOtgr
        {
            get { return otgrDocsVM.SelectedOtgr; }
            set
            {
                if (value != otgrDocsVM.SelectedOtgr)
                    otgrDocsVM.SelectedOtgr = value;
            }
        }


        /// <summary>
        /// Выбранная отгрузка
        /// </summary>
        public IEnumerable<OtgrDocModel> SelectedOtgrDocs
        {
            get { return otgrDocsVM.SelectedOtgrDocs; }
        }
    }
}
