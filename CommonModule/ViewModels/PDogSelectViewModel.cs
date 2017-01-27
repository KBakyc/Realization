using System.ComponentModel;
using System.Windows.Input;
using CommonModule.Commands;
using DataObjects;
using DataObjects.Interfaces;
using System;
using System.Linq;
using System.Collections.Generic;

namespace CommonModule.ViewModels
{
    public class PDogSelectViewModel : BaseDlgViewModel
    {
        private PoupSelectionViewModel poupSelVm;
        private PDogListViewModel dogListVM;
        private IDbService repository;

        public PDogSelectViewModel(IDbService _rep)
        {
            repository = _rep;
            poupSelVm = new PoupSelectionViewModel(_rep) { IsCanSelectPkod = false };
            dogListVM = new PDogListViewModel(repository);            
        }

        public PoupSelectionViewModel PoupSelection { get { return poupSelVm; } }
        public PDogListViewModel DogSelection { get { return dogListVM; } }

        public override bool IsValid()
        {
            return base.IsValid()
                && PoupSelection.IsValid()
                && DogSelection.IsValid();
        }

        public DateTime? SelDate { get; set; }

        public PoupModel SelPoup
        {
            get { return poupSelVm.SelPoup; }
        }

        public KontrAgent SelKa { get; set; }

        /// <summary>
        /// Выбраный договор
        /// </summary>
        public PDogInfoViewModel SelPDogInfo 
        {
            get { return dogListVM.SelPDogInfo; }
        }

        private ICommand showPDogInfosCommand;
        public ICommand ShowPDogInfosCommand
        {
            get
            {
                if (showPDogInfosCommand == null)
                    showPDogInfosCommand = new DelegateCommand(ExecuteShowPDogInfos,CanExecuteShowPDogInfos);
                return showPDogInfosCommand;
            }
        }
        private bool CanExecuteShowPDogInfos()
        {
            return SelPoup != null;
        }
        private void ExecuteShowPDogInfos()
        {
            LoadPDogInfos();
        }

        public DateTime? FromDate { get; set; }

        private PDogInfoModel[] cachedPDogs;
        public PDogInfoModel[] CachedPDogs { get { return cachedPDogs; } }

        private void LoadPDogInfos()
        {
            cachedPDogs = repository.GetPDogInfosByKaPoup(SelKa.Kgr, SelPoup.Kod, 0);//, SelKaMode);
            IEnumerable<PDogInfoModel> models = cachedPDogs;
            if (SelDate != null && models != null)
                models = models.Where(d => (String.IsNullOrEmpty(d.SpecDog) ? (String.IsNullOrEmpty(d.AlterDog) ? (String.IsNullOrEmpty(d.Dopdog) ? d.Datd 
                                                                                                                                                 : d.Datdopdog)
                                                                                                                : d.DatAlterDog)
                                                                            : d.DatSpecDog)  == SelDate);
            else
                if (FromDate != null && models != null)
                {
                    models = models.Where(d => (String.IsNullOrEmpty(d.AlterDog) ? (String.IsNullOrEmpty(d.Dopdog) ? d.Datd
                                                                                                                   : d.Datdopdog)
                                                                                 : d.DatAlterDog) >= FromDate);
                    cachedPDogs = models.ToArray();
                }
            dogListVM.LoadData(models);
        }

    }
}