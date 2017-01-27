using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.Helpers;
using DataObjects.Interfaces;
using DataObjects;
using System.ComponentModel;

namespace InfoModule.ViewModels
{
    public class SignerViewModel : BasicNotifier, ITrackable, IDataErrorInfo
    {
        private IDbService repository;
        private SignatureInfo signer;

        public SignerViewModel()
        {
            repository = CommonModule.CommonSettings.Repository;
            signer = new SignatureInfo();
            trackingState = TrackingInfo.Created;
        }

        public SignerViewModel(IDbService _repos, SignatureInfo _signer, TrackingInfo _state, bool _lazy)
        {
            repository = _repos;
            signer = _signer;
            trackingState = _state;
            if (!_lazy)
                LoadData();
        }

        public SignatureInfo SignerModel { get { return signer; } }

        private void LoadData()
        {
            LoadPoups();
        }


        private int[] oldpoupsdata;
        private bool isPoupsLoaded;

        private void LoadPoups()
        {
            if (poups != null)
                for(int i = 0; i< poups.Length; i++)
                    poups[i].PropertyChanged -= new PropertyChangedEventHandler(SelectedPoupChanged);

            oldpoupsdata = null;
            if (signer.Id > 0)
                oldpoupsdata = repository.GetSignerPoups(signer.Id);
            var allpoups = repository.Poups.Values.Where(p => p.IsActive).ToArray();
            poups = allpoups.Select(pm => new Selectable<PoupModel>(pm, oldpoupsdata != null && oldpoupsdata.Length > 0 && oldpoupsdata.Contains(pm.Kod))).ToArray();
            isPoupsLoaded = true;
            GeneratePoupsStrings();
            
            for(int i = 0; i< poups.Length; i++)
                poups[i].PropertyChanged += new PropertyChangedEventHandler(SelectedPoupChanged);
        }

        void SelectedPoupChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsSelected")
            {
                if (trackingState == TrackingInfo.Unchanged && IsPoupsChanged())
                        TrackingState = TrackingInfo.Updated;
                GeneratePoupsStrings();
                NotifyPropertyChanged("PoupsString");
                NotifyPropertyChanged("FullPoupsString");
            }
        }

        private bool IsPoupsChanged()
        {
            var newpoups = GetPoups();
            bool isolddata = oldpoupsdata != null && oldpoupsdata.Length != 0;
            bool isnewdata = newpoups != null && newpoups.Length != 0;

            if (isolddata && !isnewdata || !isolddata && isnewdata) return true;
            if (oldpoupsdata.Length != newpoups.Length) return true;
            for (int i = 0; i < newpoups.Length; i++)
                if (newpoups[i] != oldpoupsdata[i]) return true;
            return false;
        }
        
        private Selectable<PoupModel>[] poups;
        public Selectable<PoupModel>[] Poups
        {
            get 
            {
                if (!isPoupsLoaded)
                    LoadPoups();
                return poups; 
            }
        }

        private string poupsString;
        public string PoupsString
        {
            get 
            {
                return poupsString; 
            }
        }

        private void GeneratePoupsStrings()
        {
            if (Poups == null)
            {
                poupsString = fullPoupsString = String.Empty;
                return; 
            }
            else
            {
                var selPoups = Poups.Where(spm => spm.IsSelected).ToDictionary(spm => spm.Value.Kod.ToString(), spm => spm.Value.Name);
                poupsString = String.Join(",", selPoups.Keys.ToArray());
                fullPoupsString = String.Join("\n", selPoups.Select(kv => String.Format("[{0}] {1}", kv.Key, kv.Value)).ToArray());
            }
        }

        private string fullPoupsString;
        public string FullPoupsString
        {
            get { return fullPoupsString; }
        }

        public string Fio
        {
            get { return signer.Fio; }
            set
            {
                if (value != signer.Fio)
                {
                    signer.Fio = value;
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("Fio");
                }
            }
        }

        public string Position
        {
            get { return signer.Position; }
            set
            {
                if (value != signer.Position)
                {
                    signer.Position = value;
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("Position");
                }
            }
        }

        public string ShortPos
        {
            get { return signer.Short; }
            set
            {
                if (value != signer.Short)
                {
                    signer.Short = value;
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("ShortPos");
                }
            }
        }

        public byte SignTypeId
        {
            get { return signer.SignTypeId; }
            set
            {
                if (value != signer.SignTypeId)
                {
                    signer.SignTypeId = value;
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("SignTypeId");
                }
            }
        }

        private TrackingInfo trackingState;
        public TrackingInfo TrackingState
        {
            get
            {
                return trackingState;
            }
            set
            {
                if (value != trackingState)
                {
                    trackingState = value;
                    CheckIfValid();
                    NotifyPropertyChanged("TrackingState");
                }
            }
        }

        #region IDataErrorInfo

        private string error;
        public string Error
        {
            get
            {
                return error;
            }
            set
            {
                SetAndNotifyProperty("Error", ref error, value);
            }
        }

        private Dictionary<string, bool> validations = new Dictionary<string, bool>();

        public string this[string columnName]
        {
            get
            {
                string res = "";
                switch (columnName)
                {
                    case "Fio": if (String.IsNullOrEmpty(Fio)) res = "Фамилия и инициалы не указаны"; 
                                else 
                                if (Fio.Length > 50) res = "Фамилия и инициалы должны быть не длиннее 50 символов";
                                break;
                    case "Position": if (String.IsNullOrEmpty(Position)) res = "Должность не указана"; 
                                     else 
                                     if (Position.Length > 150) res = "Наименование должности должно быть не длиннее 150 символов";
                                     break;
                    case "ShortPos": if (ShortPos != null && ShortPos.Length > 50) res = "Сокращённое наименование должности должно быть не длиннее 50 символов"; break;
                    case "SignTypeId": if (SignTypeId == 0) res = "Не указан тип подписи"; break;
                }
                validations[columnName] = res == "";
                CheckIfValid();
                return res;
            }
        }

        #endregion

        private bool CheckIfValid()
        {
            IsValid = trackingState == TrackingInfo.Deleted || validations.Values.All(v => v);
            return isValid;            
        }

        private bool isValid = true;
        public bool IsValid
        {
            get
            {
                Error = isValid ? "" : "Неверные данные";
                return isValid;
            }
            set
            {
                SetAndNotifyProperty("IsValid", ref isValid, value);
            }
        }

        public int[] GetPoups()
        {
            int[] res = null;
            if (poups != null)
                res = poups.Where(sp => sp.IsSelected).Select(sp => sp.Value.Kod).ToArray();
            return res;
        }
    }
}
