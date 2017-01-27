using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.Helpers;
using DataObjects.Interfaces;
using DataObjects;
using System.ComponentModel;

namespace PredoplModule.ViewModels
{
    public class PredoplSchetViewModel : BasicNotifier, ITrackable, IDataErrorInfo
    {
        private IDbService repository;
        private PredoplSchetModel schet;

        public PredoplSchetViewModel()
        {
            repository = CommonModule.CommonSettings.Repository;
            schet = new PredoplSchetModel();
        }

        public PredoplSchetViewModel(IDbService _repository, PredoplSchetModel _schet)
        {
            repository = _repository;
            schet = _schet;
        }

        public PredoplSchetModel SchetModel { get { return schet; } }

        public int Poup 
        {
            get { return schet.Poup; }
            set
            {
                if (value != schet.Poup)
                {
                    schet.Poup = value;
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("Poup");
                    NotifyPropertyChanged("AvailablePkods");
                }
            } 
        }

        public PkodModel[] AvailablePkods { get { return repository.GetPkods(Poup); } }

        public short Pkod
        {
            get { return schet.Pkod; }
            set
            {
                if (value != schet.Pkod)
                {
                    schet.Pkod = value;
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("Pkod");
                }
            }
        }

        public string Deb
        {
            get { return schet.Deb; }
            set
            {
                if (value != schet.Deb)
                {
                    schet.Deb = value;
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("Deb");
                }
            }
        }

        public string Kre
        {
            get { return schet.Kre; }
            set
            {
                if (value != schet.Kre)
                {
                    schet.Kre = value;
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("Kre");
                }
            }
        }

        public string Kodnap
        {
            get { return schet.Kodnap; }
            set
            {
                if (value != schet.Kodnap)
                {
                    schet.Kodnap = value;
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("Kodnap");
                }
            }
        }

        public string Kodvozv
        {
            get { return schet.Kodvozv; }
            set
            {
                if (value != schet.Kodvozv)
                {
                    schet.Kodvozv = value;
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("Kodvozv");
                }
            }
        }

        public string KodvalFrom
        {
            get { return schet.KodvalFrom; }
            set
            {
                if (value != schet.KodvalFrom)
                {
                    schet.KodvalFrom = value;
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("KodvalFrom");
                }
            }
        }

        public string KodvalTo
        {
            get { return schet.KodvalTo; }
            set
            {
                if (value != schet.KodvalTo)
                {
                    schet.KodvalTo = value;
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("KodvalTo");
                }
            }
        }

        public string RealSch
        {
            get { return schet.RealSch; }
            set
            {
                if (value != schet.RealSch)
                {
                    schet.RealSch = value;
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("RealSch");
                }
            }
        }

        public int IdBankGroup
        {
            get { return schet.IdBankGroup; }
            set
            {
                if (value != schet.IdBankGroup)
                {
                    schet.IdBankGroup = value;
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("IdBankGroup");
                }
            }
        }
        
        public byte RecType
        {
            get { return schet.RecType; }
            set
            {
                if (value != schet.RecType)
                {
                    schet.RecType = value;
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("RecType");
                }
            }
        }

        public bool IsActive
        {
            get { return schet.IsActive; }
            set
            {
                if (value != schet.IsActive)
                {
                    schet.IsActive = value;
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("IsActive");
                }
            }
        }

        public TrackingInfo TrackingState
        {
            get
            {
                return schet.TrackingState;
            }
            set
            {
                if (value != schet.TrackingState)
                {
                    schet.TrackingState = value;
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
                    case "Poup": if (Poup <= 0) res = "Значение должно быть больше 0"; break;
                    case "Deb": if (!(Deb != null && Deb.Length == 8 && Deb.All(c => Char.IsDigit(c)))) res = "Значение должно быть 8-значным"; break;
                    case "Kre": if (!(Kre != null && Kre.Length == 8 && Kre.All(c => Char.IsDigit(c)))) res = "Значение должно быть 8-значным"; break;
                    case "KodvalFrom": if (string.IsNullOrEmpty(KodvalFrom) || KodvalFrom.Length != 2) res = "Код валюты неверен"; break;
                    case "KodvalTo": if (string.IsNullOrEmpty(KodvalTo) || KodvalTo.Length != 2) res = "Код валюты неверен"; break;
                    case "Kodnap": if (!string.IsNullOrEmpty(Kodnap) && Kodnap.Length > 4) res = "Слишком длинный код направления"; break;
                    case "Kodvozv": if (!string.IsNullOrEmpty(Kodvozv) && Kodvozv.Length > 4) res = "Слишком длинный код направления"; break;
                    case "RealSch": if (!(string.IsNullOrWhiteSpace(RealSch) || RealSch.Length == 6 && RealSch.All(c => Char.IsDigit(c)))) res = "Значение должно быть 6-значным"; break;
                }
                validations[columnName] = res == "";
                IsValid = validations.Values.All (v => v);
                return res;
            }
        }

        #endregion

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


    }
}
