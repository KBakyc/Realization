using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.Helpers;
using RwModule.Models;
using DataObjects;
using System.ComponentModel;
using DataObjects.Interfaces;
using DAL;

namespace RwModule.ViewModels
{
    public class RwFromBankSettingViewModel : BasicNotifier, IDataErrorInfo
    {
        private RwFromBankSetting modelRef;
        private IDbService dbserv;

        public RwFromBankSettingViewModel(IDbService _dbserv, RwFromBankSetting _model)
        {
            modelRef = _model;
            dbserv = _dbserv;
        }

        public RwUslType IdUslType         
        { 
            get { return modelRef.IdUslType; }
            set
            {
                if (value != modelRef.IdUslType)
                {
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    modelRef.IdUslType = value;
                    NotifyPropertyChanged("IdUslType");
                }
            }
        }

        public string Debet
        {
            get { return modelRef.Debet; }
            set
            {
                if (value != modelRef.Debet)
                {
                    modelRef.Debet = value;
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("Debet");
                }
            }
        }

        public string Kredit
        {
            get { return modelRef.Credit; }
            set
            {
                if (value != modelRef.Credit)
                {
                    modelRef.Credit = value;
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("Kredit");
                }
            }
        }

        public string FinNapr
        {
            get { return modelRef.FinNapr; }
            set
            {
                if (value != modelRef.FinNapr)
                {
                    modelRef.FinNapr = value;
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("FinNapr");
                }
            }
        }

        public int IdBankGroup
        {
            get { return modelRef.IdBankGroup; }
            set
            {
                if (value != modelRef.IdBankGroup)
                {
                    modelRef.IdBankGroup = (byte)value;
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("IdBankGroup");
                }
            }
        }

        public RwFromBankSetting Model { get { return modelRef; } }

        private TrackingInfo trackingState;
        public TrackingInfo TrackingState
        {
            get { return trackingState; }
            set { SetAndNotifyProperty("TrackingState", ref trackingState, value); }
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

        private Dictionary<string, string> validations = new Dictionary<string, string>();

        public string this[string columnName]
        {
            get
            {
                string res = "";
                switch (columnName)
                {
                    case "Debet": if (!(Debet != null && Debet.Length == 8 && Debet.All(c => Char.IsDigit(c)))) res = "Значение должно быть 8-значным"; break;
                    case "Kredit": if (!(Kredit != null && Kredit.Length == 8 && Kredit.All(c => Char.IsDigit(c)))) res = "Значение должно быть 8-значным"; break;
                }
                validations[columnName] = res;
                IsValid = validations.Values.All(v => v == "");
                return res;
            }
        }

        #endregion

        private bool isValid = true;
        public bool IsValid
        {
            get
            {
                Error = isValid ? "" : String.Join("\n", validations.Values.Where(v => v != "").ToArray());
                return isValid;
            }
            set
            {
                SetAndNotifyProperty("IsValid", ref isValid, value);
            }
        }
    }
}
