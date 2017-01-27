using System.Collections.Generic;
using System.Linq;
using CommonModule.Commands;
using CommonModule.Helpers;
using DataObjects;
using DataObjects.Interfaces;
using System.Collections.ObjectModel;

namespace CommonModule.ViewModels
{
    public class OtgrDocListViewModel : BaseDlgViewModel
    {
        private IDbService repository;

        public OtgrDocListViewModel(IDbService _rep)
        {
            repository = _rep;           
        }

        public OtgrDocListViewModel(IDbService _rep, IEnumerable<OtgrDocModel> _docs)
            :this(_rep)
        {
            var oDocsVm = _docs.Select(d => new OtgrDocViewModel(d, repository));
            OtgrDocs = new ObservableCollection<Selectable<OtgrDocViewModel>>(oDocsVm.Select(d => new Selectable<OtgrDocViewModel>(d, true)));
        }

        public OtgrDocListViewModel(IDbService _rep, IEnumerable<Selectable<OtgrDocModel>> _docs, bool _lazy)
            :this(_rep)
        {
            OtgrDocs = new ObservableCollection<Selectable<OtgrDocViewModel>>(
                _docs.Select(sd => new Selectable<OtgrDocViewModel>(new OtgrDocViewModel(sd.Value, repository, _lazy),sd.IsSelected)));
        }

        public override bool IsValid()
        {
            return base.IsValid()
                && SelectedOtgrDocs.Any();
        }

        public void SubscribeToSelection()
        {
            if (OtgrDocs != null && OtgrDocs.Count > 0)
                foreach (var sod in OtgrDocs)
                {
                    sod.PropertyChanged -= new System.ComponentModel.PropertyChangedEventHandler(sod_PropertyChanged);
                    sod.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(sod_PropertyChanged);
                }
        }

        void sod_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsSelected")
                CalcItogs();
        }

        private void CalcItogs()
        {
            decimal _kolf = 0;
            int _count = 0;
            foreach (var sod in OtgrDocs.Where(d => d.IsSelected))
            {
                _count++;
                _kolf += sod.Value.ModelRef.Kolf;
            }
            Kolf = _kolf;
            Count = _count;
        }

        private decimal? kolf;
        public decimal Kolf
        {
            get 
            {
                if (kolf == null)
                    CalcItogs();
                return kolf ?? 0; 
            }
            set { SetAndNotifyProperty("Kolf", ref kolf, value); }
        }

        private decimal? count;
        public decimal Count
        {
            get 
            {
                if (count == null)
                    CalcItogs();
                return count ?? 0; 
            }
            set { SetAndNotifyProperty("Count", ref count, value); }
        }

        /// <summary>
        /// Отгрузочные документы
        /// </summary>
        public ObservableCollection<Selectable<OtgrDocViewModel>> OtgrDocs { get; private set; }

        /// <summary>
        /// Активный (текущий документ)
        /// </summary>
        private Selectable<OtgrDocViewModel> selectedOtgr;
        public Selectable<OtgrDocViewModel> SelectedOtgr
        {
            get { return selectedOtgr; }
            set
            {
                if (value != selectedOtgr)
                {
                    selectedOtgr = value;
                    NotifyPropertyChanged("SelectedOtgr");
                }
            }
        }


        /// <summary>
        /// Выбранная отгрузка
        /// </summary>
        public IEnumerable<OtgrDocModel> SelectedOtgrDocs
        {
            get
            {
                return OtgrDocs.Where(so => so.IsSelected).Select(so => so.Value.ModelRef);
            }
        }
    }
}
