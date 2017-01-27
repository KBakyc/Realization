using System.ComponentModel;
using System.Windows.Input;
using CommonModule.Commands;
using CommonModule.Helpers;
using DataObjects;
using DataObjects.Interfaces;
using System;
using DotNetHelper;
using System.Linq;
using System.Collections.Generic;

namespace CommonModule.ViewModels
{
    public class DogSelectViewModel : BaseDlgViewModel
    {
        private PoupSelectionViewModel poupSelVm;
        private DogListViewModel dogListVM;
        private IDbService repository;

        public DogSelectViewModel(IDbService _rep)
        {
            repository = _rep;
            poupSelVm = new PoupSelectionViewModel(_rep, false, true) { IsCanSelectPkod = false };
            poupSelVm.PropertyChanged += new PropertyChangedEventHandler(poupSelVm_PropertyChanged);
            dogListVM = new DogListViewModel(repository);
        }

        public DogSelectViewModel(IDbService _rep, KontrAgent _ka)
            :this(_rep)
        {            
            selKa = _ka;
            if (SelPoup != null && selKa != null)
                LoadDogInfos();
        }

        void poupSelVm_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelPoup" && SelPoup != null)
                LoadDogInfos();
        }

        public PoupSelectionViewModel PoupSelection { get { return poupSelVm; } }
        public DogListViewModel DogSelection { get { return dogListVM; } }

        public override bool IsValid()
        {
            return base.IsValid()
                && PoupSelection.IsValid()
                && DogSelection.IsValid();
        }

        private DateTime? selDate;
        public DateTime? SelDate
        {
            get { return selDate; }
            set
            {
                if (selDate != value)
                {
                    selDate = value;
                    LoadDogInfos();
                }
            }
        }

        public PoupModel SelPoup
        {
            get { return poupSelVm.SelPoup; }
        }

        private KontrAgent selKa;
        public KontrAgent SelKa 
        {
            get { return selKa; }
            set 
            { 
                selKa = value; 
            } 
        }

        /// <summary>
        /// Выбраный договор
        /// </summary>
        public DogInfoViewModel SelDogInfo
        {
            get { return dogListVM.SelDogInfo; }
        }

        private DateTime? fromDate;
        public DateTime? FromDate 
        {
            get { return fromDate; } 
            set 
            {
                if (fromDate != value)
                {
                    fromDate = value;
                    //LoadDogInfos();
                }
            }
        }

        private DogInfo[] cachedDogs;
        public DogInfo[] CachedDogs { get { return cachedDogs; } }

        private void LoadDogInfos()
        {
            var pdogs = repository.GetPDogInfosByKaPoup(SelKa.Kgr, SelPoup.Kod, 0);
            cachedDogs = pdogs.DistinctBy(p => p.Iddog).Select(p => new DogInfo 
            {       
                IdDog = p.Iddog,
                Kfond = p.Kfond,
                Kpok = p.Kpok,
                Provoz = p.Provoz,
                Srok = p.Srok, 
                KodVal = p.Kodval,
                NaiOsn = p.Osn,
                DatOsn = p.Datd,
                DopOsn = p.Dopdog,
                DatDop = p.Datdopdog
            }).ToArray();
            IEnumerable<DogInfo> models = cachedDogs;
            if (SelDate != null && models != null)
                models = models.Where(d => (String.IsNullOrEmpty(d.DopOsn) || d.DatDop == null ? d.DatOsn : d.DatDop.Value) == SelDate);
            else
                if (FromDate != null && models != null)
                {
                    models = models.Where(d => (String.IsNullOrEmpty(d.DopOsn) || d.DatDop == null ? d.DatOsn : d.DatDop.Value) >= FromDate);
                    cachedDogs = models.ToArray();
                }
            dogListVM.LoadData(models);
        }

    }
}