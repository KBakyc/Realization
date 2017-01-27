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
    public class SalesJournalTypeViewModel : BasicNotifier, ITrackable, IDataErrorInfo
    {
        private IDbService repository;
        private JournalTypeModel jrn;

        public SalesJournalTypeViewModel()
        {
            repository = CommonModule.CommonSettings.Repository;
            jrn = new JournalTypeModel();
        }

        public SalesJournalTypeViewModel(IDbService _repository, JournalTypeModel _jrn)
        {
            repository = _repository;
            jrn = _jrn;
        }

        public JournalTypeModel JrnModel { get { return jrn; } }

        public int Poup
        {
            get { return jrn.Poup; }
            set
            {
                if (value != jrn.Poup)
                {
                    jrn.Poup = value;
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
            get { return jrn.Pkod; }
            set
            {
                if (value != jrn.Pkod)
                {
                    jrn.Pkod = value;
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("Pkod");
                }
            }
        }

        public string BalSchet
        {
            get { return jrn.BalSchet; }
            set
            {
                if (value != jrn.BalSchet)
                {
                    jrn.BalSchet = value;
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("BalSchet");
                }
            }
        }

        public string JournalType
        {
            get { return jrn.JournalType; }
            set
            {
                if (value != jrn.JournalType)
                {
                    jrn.JournalType = value;
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("JournalType");
                }
            }
        }

        public string JournalName
        {
            get { return jrn.JournalName; }
            set
            {
                if (value != jrn.JournalName)
                {
                    jrn.JournalName = value;
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("JournalName");
                }
            }
        }

        public string Ceh
        {
            get { return jrn.Ceh; }
            set
            {
                if (value != jrn.Ceh)
                {
                    jrn.Ceh = value;
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("Ceh");
                }
            }
        }

        public string TabIsp
        {
            get { return jrn.TabIsp; }
            set
            {
                if (value != jrn.TabIsp)
                {
                    jrn.TabIsp = value;
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("TabIsp");
                }
            }
        }

        public string TabNach
        {
            get { return jrn.TabNach; }
            set
            {
                if (value != jrn.TabNach)
                {
                    jrn.TabNach = value;
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("TabNach");
                }
            }
        }

        public string Kodval
        {
            get { return jrn.Kodval; }
            set
            {
                if (value != jrn.Kodval)
                {
                    jrn.Kodval = value;
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("Kodval");
                }
            }
        }

        public byte Prsng
        {
            get { return jrn.Prsng; }
            set
            {
                if (value != jrn.Prsng)
                {
                    jrn.Prsng = value;
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("Prsng");
                }
            }
        }
        
        public bool IsVozm
        {
            get { return jrn.IsVozm; }
            set
            {
                if (value != jrn.IsVozm)
                {
                    jrn.IsVozm = value;
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("IsVozm");
                }
            }
        }

        public decimal? Nds
        {
            get { return jrn.Nds; }
            set
            {
                if (value != jrn.Nds)
                {
                    jrn.Nds = value;
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("Nds");
                }
            }
        }

        public int Kstr
        {
            get { return jrn.Kstr; }
            set
            {
                if (value != jrn.Kstr)
                {
                    jrn.Kstr = value;
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("Kstr");
                }
            }
        }

        public TrackingInfo TrackingState
        {
            get
            {
                return jrn.TrackingState;
            }
            set
            {
                if (value != jrn.TrackingState)
                {
                    jrn.TrackingState = value;
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
                    case "JournalType": if (!string.IsNullOrWhiteSpace(JournalType) && JournalType.Length != 2 && !JournalType.All(c => Char.IsLetterOrDigit(c))) res = "Длина 2 символа. Буква или цифра."; break;
                    case "JournalName": if (string.IsNullOrWhiteSpace(JournalName) || JournalName.Length > 100) res = "Обязательно к заполнению. Не больше 30 символов"; break;
                    case "Poup": if (Poup <= 0) res = "Значение должно быть больше 0"; break;                    
                    case "Kodval": if (!string.IsNullOrWhiteSpace(Kodval) && Kodval.Length != 2) res = "Код валюты неверен"; break;
                    case "BalSchet": if (!string.IsNullOrWhiteSpace(BalSchet) && (BalSchet.Length > 8 || !BalSchet.All(c => Char.IsDigit(c)))) res = "Допускаются только цифры. Не больше 8 символов"; break;
                    case "Ceh": if (!string.IsNullOrWhiteSpace(Ceh) && Ceh.Length > 5) res = "Не больше 2 символов"; break;
                    case "TabIsp": if (!string.IsNullOrWhiteSpace(TabIsp) && TabIsp.Length > 10) res = "Не больше 5 символов"; break;
                    case "TabNach": if (!string.IsNullOrWhiteSpace(TabNach) && TabNach.Length > 10) res = "Не больше 5 символов"; break;

                }
                validations[columnName] = res == "";
                IsValid = validations.Values.All(v => v);
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
