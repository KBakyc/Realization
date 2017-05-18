using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.ViewModels;
using System.Collections.ObjectModel;
using DataObjects;
using DataObjects.Interfaces;

namespace RwModule.ViewModels
{
    /// <summary>
    /// Модель диалога изменения данных ЖД перечня.
    /// </summary>
    public class EditRwListInfoDlgViewModel : BaseDlgViewModel
    {
        private IDbService repository;
        private RwListViewModel rwlViewModel;

        public EditRwListInfoDlgViewModel(RwListViewModel _rvm)
        {
            rwlViewModel = _rvm;
            repository = CommonModule.CommonSettings.Repository;
            LoadData();
        }

        private void LoadData()
        {
            if (rwlViewModel == null) return;
            IsTransition = rwlViewModel.Transition;
            canBeTransition = rwlViewModel.RwDocsCollection.Any(d => d.Dat_doc.Month != d.Rep_date.Value.Month);
            if (!canBeTransition) IsTransition = false;
            LoadDogInfos();
            if (allDogInfos != null && allDogInfos.Length > 0 && rwlViewModel.Dogovor != null)
                selDogovor = allDogInfos.FirstOrDefault(d => d.IdDog == rwlViewModel.Dogovor.IdDog);
            acceptDate = rwlViewModel.Dat_accept;
            orcDate = rwlViewModel.Dat_orc;
            oplToDate = rwlViewModel.Dat_oplto;
        }

        private void LoadDogInfos()
        {
            allDogInfos = repository.GetDogInfos(Properties.Settings.Default.DogIdARM);
        }

        private bool canBeTransition;
        
        public bool CanBeTransition
        {
            get { return canBeTransition; }
            set 
            { 
                canBeTransition = value; 
                NotifyPropertyChanged("CanBeTransition");
            }
        }

        public bool IsTransition { get; set; }

        private bool isRepDateEdEnabled = true;
        public bool IsRepDateEdEnabled
        {
            get { return isRepDateEdEnabled; }
            set { SetAndNotifyProperty("IsRepDateEdEnabled", ref isRepDateEdEnabled, value); }
        }

        private DateTime? repDate;
        public DateTime? RepDate
        {
            get { return repDate; }
            set { repDate = value; }
        }

        private bool isOrcDateEdEnabled = true;
        public bool IsOrcDateEdEnabled
        {
            get { return isOrcDateEdEnabled; }
            set { SetAndNotifyProperty("IsOrcDateEdEnabled", ref isOrcDateEdEnabled, value); }
        }

        private DateTime orcDate;
        public DateTime OrcDate
        {
            get { return orcDate; }
            set { orcDate = value; }
        }

        private bool isAcceptDateEdEnabled = true;
        public bool IsAcceptDateEdEnabled
        {
            get { return isAcceptDateEdEnabled; }
            set { SetAndNotifyProperty("IsAcceptDateEdEnabled", ref isAcceptDateEdEnabled, value); }
        }

        private DateTime? acceptDate;
        public DateTime? AcceptDate
        {
            get { return acceptDate; }
            set 
            { 
                acceptDate = value;
                RefreshOplToDate();
            }
        }

        private void RefreshOplToDate()
        {
            if (selDogovor == null) return;
            if (acceptDate == null)
            {
                OplToDate = null;
                return;
            }
            Action work = () =>
            {
                OplToDate = repository.GetLastOplDate(acceptDate.Value, selDogovor.Srok, selDogovor.TypeRespite);
            };
            Parent.Services.DoWaitAction(work, "Подождите", "Обновление строков оплаты...");
        }
            

        private bool isOplToDateEdEnabled = true;
        public bool IsOplToDateEdEnabled
        {
            get { return isOplToDateEdEnabled; }
            set { SetAndNotifyProperty("IsOplToDateEdEnabled", ref isOplToDateEdEnabled, value); }
        }

        private DateTime? oplToDate;
        public DateTime? OplToDate
        {
            get { return oplToDate; }
            set { SetAndNotifyProperty("OplToDate", ref oplToDate, value); }
        }

        private bool isDogEdEnabled = true;
        public bool IsDogEdEnabled
        {
            get { return isDogEdEnabled; }
            set { SetAndNotifyProperty("IsDogEdEnabled", ref isDogEdEnabled, value); }
        }

        private DogInfo selDogovor;
        public DogInfo SelDogovor
        {
            get { return selDogovor; }
            set 
            { 
                selDogovor = value;
                NotifyPropertyChanged("SelDogovor");
            }
        }

        private DogInfo[] allDogInfos;
        public DogInfo[] AllDogInfos
        {
            get { return allDogInfos; }
            set { allDogInfos = value; }
        }

        public override bool IsValid()
        {
            return base.IsValid() && Validate();
        }

        private bool Validate()
        {
            bool res = true;
            bool tres;
            errors.Clear();

            tres = !IsTransition || canBeTransition;
            if (!tres)
            {
                res = false;
                errors.Add("Перечень не может быть переходным");
            }

            NotifyPropertyChanged("IsHasErrors");
            return res;
        }

        public bool IsHasErrors { get { return errors.Count > 0; } }

        private ObservableCollection<string> errors = new ObservableCollection<string>();
        public ObservableCollection<string> Errors
        {
            get { return errors; }
            set { errors = value; }
        }
    }
}
