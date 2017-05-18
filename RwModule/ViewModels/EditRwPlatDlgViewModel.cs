using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.ViewModels;
using System.Collections.ObjectModel;
using DataObjects;
using DataObjects.Interfaces;
using RwModule.Models;
using CommonModule.Helpers;

namespace RwModule.ViewModels
{
    /// <summary>
    /// Модель диалога изменения платежа за ЖД услуги.
    /// </summary>
    public class EditRwPlatDlgViewModel : BaseDlgViewModel
    {
        private IDbService repository;
        private RwPlatViewModel rwpViewModel;

        public EditRwPlatDlgViewModel(RwPlatViewModel _rvm)
        {
            rwpViewModel = _rvm;
            repository = CommonModule.CommonSettings.Repository;
            LoadData();
        }

        private void LoadData()
        {
            if (rwpViewModel == null) return;
            numPlat = rwpViewModel.Numplat;
            datPlat = rwpViewModel.Datplat;
            datBank = rwpViewModel.Datbank;
            sumPlat = rwpViewModel.Sumplat;
            ostatok = rwpViewModel.Ostatok;
            datZakr = rwpViewModel.Datzakr;
            whatfor = rwpViewModel.Whatfor;
            direction = rwpViewModel.Direction;
            notes = rwpViewModel.Notes;
            idusltype = rwpViewModel.Idusltype;
            
            rwUslTypes = Enumerations.GetAllValuesAndDescriptions<RwUslType>();
            directions = Enumerations.GetAllValuesAndDescriptions<RwPlatDirection>();
        }
        
        Dictionary<RwPlatDirection, string> directions;
        public Dictionary<RwPlatDirection, string> Directions
        {
            get { return directions; }
        }

        Dictionary<RwUslType, string> rwUslTypes;
        public Dictionary<RwUslType, string> RwUslTypes
        {
            get { return rwUslTypes; }
        }

        public void SwitchAllTo(bool _state)
        {
            isNumPlatEdEnabled = isDatPlatEdEnabled = isDatBankEdEnabled = isSumPlatEdEnabled = isOstatokEdEnabled = isDatZakrEdEnabled
                = isDirectionEdEnabled = isWhatforEdEnabled = isNotesEdEnabled = isIdusltypeEdEnabled = _state;
        }

        private bool isNumPlatEdEnabled = true;
        public bool IsNumPlatEdEnabled
        {
            get { return isNumPlatEdEnabled; }
            set { SetAndNotifyProperty("IsNumPlatEdEnabled", ref isNumPlatEdEnabled, value); }
        }

        private int numPlat;
        public int NumPlat
        {
            get { return numPlat; }
            set { SetAndNotifyProperty("NumPlat", ref numPlat, value); }
        }    

        private bool isDatPlatEdEnabled = true;
        public bool IsDatPlatEdEnabled
        {
            get { return isDatPlatEdEnabled; }
            set { SetAndNotifyProperty("IsDatPlatEdEnabled", ref isDatPlatEdEnabled, value); }
        }

        private DateTime datPlat;
        public DateTime DatPlat
        {
            get { return datPlat; }
            set { SetAndNotifyProperty("DatPlat", ref datPlat, value); }
        }
        
        private bool isDatBankEdEnabled = true;
        public bool IsDatBankEdEnabled
        {
            get { return isDatBankEdEnabled; }
            set { SetAndNotifyProperty("IsDatBankEdEnabled", ref isDatBankEdEnabled, value); }
        }

        private DateTime datBank;
        public DateTime DatBank
        {
            get { return datBank; }
            set { SetAndNotifyProperty("DatBank", ref datBank, value); }
        }
        
        private bool isSumPlatEdEnabled = true;
        public bool IsSumPlatEdEnabled
        {
            get { return isSumPlatEdEnabled; }
            set { SetAndNotifyProperty("IsSumPlatEdEnabled", ref isSumPlatEdEnabled, value); }
        }

        private decimal sumPlat;
        public decimal SumPlat
        {
            get { return sumPlat; }
            set { SetAndNotifyProperty("SumPlat", ref sumPlat, value); }
        }

        private bool isOstatokEdEnabled = true;
        public bool IsOstatokEdEnabled
        {
            get { return isOstatokEdEnabled; }
            set { SetAndNotifyProperty("IsOstatokEdEnabled", ref isOstatokEdEnabled, value); }
        }

        private decimal ostatok;
        public decimal Ostatok
        {
            get { return ostatok; }
            set { SetAndNotifyProperty("Ostatok", ref ostatok, value); }
        }

        private bool isDatZakrEdEnabled = true;
        public bool IsDatZakrEdEnabled
        {
            get { return isDatZakrEdEnabled; }
            set { SetAndNotifyProperty("IsDatZakrEdEnabled", ref isDatZakrEdEnabled, value); }
        }

        private DateTime? datZakr;
        public DateTime? DatZakr
        {
            get { return datZakr; }
            set { SetAndNotifyProperty("DatZakr", ref datZakr, value); }
        }
        
        private bool isWhatforEdEnabled = true;
        public bool IsWhatforEdEnabled
        {
            get { return isWhatforEdEnabled; }
            set { SetAndNotifyProperty("IsWhatforEdEnabled", ref isWhatforEdEnabled, value); }
        }

        private string whatfor;
        public string Whatfor
        {
            get { return whatfor; }
            set { SetAndNotifyProperty("Whatfor", ref whatfor, value); }
        }
        
        private bool isDirectionEdEnabled = true;
        public bool IsDirectionEdEnabled
        {
            get { return isDirectionEdEnabled; }
            set { SetAndNotifyProperty("IsDirectionEdEnabled", ref isDirectionEdEnabled, value); }
        }

        private RwPlatDirection direction;
        public RwPlatDirection Direction
        {
            get { return direction; }
            set { SetAndNotifyProperty("Direction", ref direction, value); }
        }

        private bool isNotesEdEnabled = true;
        public bool IsNotesEdEnabled
        {
            get { return isNotesEdEnabled; }
            set { SetAndNotifyProperty("IsNotesEdEnabled", ref isNotesEdEnabled, value); }
        }

        private string notes;
        public string Notes
        {
            get { return notes; }
            set { SetAndNotifyProperty("Notes", ref notes, value); }
        }

        private bool isIdusltypeEdEnabled = true;
        public bool IsIdusltypeEdEnabled
        {
            get { return isIdusltypeEdEnabled; }
            set { SetAndNotifyProperty("IsIdusltypeEdEnabled", ref isIdusltypeEdEnabled, value); }
        }

        private RwUslType idusltype;
        public RwUslType Idusltype
        {
            get { return idusltype; }
            set { SetAndNotifyProperty("Idusltype", ref idusltype, value); }
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

            tres = true; // check
            if (!tres)
            {
                res = false;
                errors.Add("текст ошибки");
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
