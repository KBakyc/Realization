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

        public EditRwPlatDlgViewModel(IDbService _repository, RwPlatViewModel _rvm)
        {
            rwpViewModel = _rvm;
            repository = _repository;
            LoadData();
        }

        private void LoadData()
        {
            rwUslTypes = Enumerations.GetAllValuesAndDescriptions<RwUslType>();
            directions = Enumerations.GetAllValuesAndDescriptions<RwPlatDirection>();
            LoadDogInfos();
            TypeDocs = repository.GetTypePlatDocs();

            if (rwpViewModel == null) return;
            if (allDogInfos != null && allDogInfos.Length > 0 && rwpViewModel.Idagree != null)
                selDogovor = allDogInfos.FirstOrDefault(d => d.IdAgree == rwpViewModel.Idagree);
            
            numPlat = rwpViewModel.Numplat;
            datPlat = rwpViewModel.Datplat;
            datBank = rwpViewModel.Datbank;
            sumPlat = rwpViewModel.Sumplat;
            ostatok = rwpViewModel.Ostatok;
            datZakr = rwpViewModel.Datzakr;
            whatfor = rwpViewModel.Whatfor;
            direction = rwpViewModel.Direction;
            notes = rwpViewModel.Notes;
            credit = rwpViewModel.Credit;
            debet = rwpViewModel.Debet;
            idusltype = rwpViewModel.Idusltype;
            idpostes = rwpViewModel.Idpostes;
            idrwplat = rwpViewModel.Idrwplat;
            IdTypeDoc = rwpViewModel.Idtypedoc;
        }

        private void LoadDogInfos()
        {
            allDogInfos = repository.GetDogInfos(Properties.Settings.Default.DogIdARM);
        }

        public RwPlat GetRwPlat()
        {
            var res = new RwPlat();
            res.Credit = credit;
            res.Debet = debet;
            res.Datbank = datBank ?? datPlat;
            res.Datplat = datPlat;
            res.Datzakr = DatZakr;
            res.Direction = direction;
            res.Idagree = selDogovor.IdAgree;
            res.Idpostes = idpostes;
            res.Idrwplat = idrwplat;
            res.Idusltype = idusltype;
            res.Notes = notes;
            res.Numplat = numPlat;
            res.Ostatok = ostatok;
            res.Sumplat = sumPlat;
            res.Whatfor = whatfor;
            res.Idtypedoc = idTypeDoc;

            return res;
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
                = isDirectionEdEnabled = isWhatforEdEnabled = isNotesEdEnabled = isIdusltypeEdEnabled = isDogEdEnabled 
                = isCreditEdEnabled = isDebetEdEnabled = isTypeDocEdEnabled = _state;
        }

        private bool isNumPlatEdEnabled = true;
        public bool IsNumPlatEdEnabled
        {
            get { return isNumPlatEdEnabled; }
            set { SetAndNotifyProperty("IsNumPlatEdEnabled", ref isNumPlatEdEnabled, value); }
        }

        private int idrwplat;
        private int? idpostes;

        private bool isTypeDocEdEnabled = true;
        public bool IsTypeDocEdEnabled
        {
            get { return isTypeDocEdEnabled; }
            set { SetAndNotifyProperty("IsTypeDocEdEnabled", ref isTypeDocEdEnabled, value); }
        }

        public TypePlatDoc[] TypeDocs { get; set; }

        private byte idTypeDoc;
        public byte IdTypeDoc
        {
            get { return idTypeDoc; }
            set { SetAndNotifyProperty("IdTypeDoc", ref idTypeDoc, value); }
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

        private DateTime? datBank;
        public DateTime? DatBank
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
            set
            {
                var diffsum = value - sumPlat;
                SetAndNotifyProperty("SumPlat", ref sumPlat, value);
                SetAndNotifyProperty("Ostatok", ref ostatok, ostatok + diffsum);
            }
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

        private bool isCreditEdEnabled = true;
        public bool IsCreditEdEnabled
        {
            get { return isCreditEdEnabled; }
            set { SetAndNotifyProperty("IsCreditEdEnabled", ref isCreditEdEnabled, value); }
        }

        private string credit;
        public string Credit
        {
            get { return credit; }
            set { SetAndNotifyProperty("Credit", ref credit, value); }
        }

        private bool isDebetEdEnabled = true;
        public bool IsDebetEdEnabled
        {
            get { return isDebetEdEnabled; }
            set { SetAndNotifyProperty("IsDebetEdEnabled", ref isDebetEdEnabled, value); }
        }

        private string debet;
        public string Debet
        {
            get { return debet; }
            set { SetAndNotifyProperty("Debet", ref debet, value); }
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

            tres = selDogovor != null;
            if (!tres)
            {
                res = false;
                errors.Add("Не выбран договор с ЖД");
            }

            tres = numPlat > 0;
            if (!tres)
            {
                res = false;
                errors.Add("Номер документа должен быть больше ноля");
            }

            tres = sumPlat != 0;
            if (!tres)
            {
                res = false;
                errors.Add("Отсутствует сумма по документу");
            }

            tres = !(ostatok == 0 && datZakr == null);
            if (!tres)
            {
                res = false;
                errors.Add("Платёж полностью погашен. Укажите дату закрытия");
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
