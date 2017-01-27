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
    public class RwBuhSchetViewModel : BasicNotifier, IDataErrorInfo
    {
        private RwBuhSchet modelRef;
        private IDbService dbserv;

        public RwBuhSchetViewModel(IDbService _dbserv, RwBuhSchet _model)
        {
            modelRef = _model;
            dbserv = _dbserv;
            if (_model != null && _model.KodUsl > 0) UpdateUsl(_model.KodUsl);
        }

        public int Poup
        {
            get { return modelRef.Poup; }
            set
            {
                if (value != modelRef.Poup)
                {
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    modelRef.Poup = value;
                    NotifyPropertyChanged("Poup");
                }
            }
        }

        public RefundTypes VidUsl
        {
            get { return modelRef.VidUsl; }
            set
            {
                if (value != modelRef.VidUsl)
                {
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    modelRef.VidUsl = value;
                    NotifyPropertyChanged("VidUsl");
                }
            }
        }
        
        public int KodUsl
        {
            get { return modelRef.KodUsl; }
            set
            {
                if (value != modelRef.KodUsl)
                {
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    modelRef.KodUsl = value;
                    UpdateUsl(value);
                    NotifyPropertyChanged("KodUsl");
                    NotifyPropertyChanged("NameUsl");
                }
            }
        }

        private void UpdateUsl(int _kod)
        {
            usluga = _kod == 0 ? null : dbserv.GetProductInfo(_kod);
        }

        private ProductInfo usluga;
        //public ProductInfo Usluga
        //{
        //    get { return usluga; }
        //}

        public string NameUsl
        {
            get { return KodUsl == 0 ? "Все" 
                                     : (usluga == null ? "Не найдена в справочнике" 
                                                       : usluga.Name); }
        }

        public RwBuhSchet Model { get { return modelRef; } }

        private TrackingInfo trackingState;
        public TrackingInfo TrackingState 
        {
            get { return trackingState; }
            set { SetAndNotifyProperty("TrackingState", ref trackingState, value); }
        }

        //private SumType docSumType;
        //public SumType DocSumType
        //{
        //    get 
        //    {
        //        if (docSumType == null) LoadDocSumType();                    
        //        return docSumType; 
        //    }
        //    set 
        //    {
        //        if (value != docSumType)
        //        {
        //            docSumType = value;
        //            modelRef.SumType = value.Id;
        //            if (TrackingState == TrackingInfo.Unchanged)
        //                TrackingState = TrackingInfo.Updated;
        //            NotifyPropertyChanged("DocSumType");
        //        }
        //    }
        //}
        
        //private void LoadDocSumType()
        //{
        //    using (var db = new RealContext())
        //    {
        //        docSumType = db.GetSumType(modelRef.SumType);
        //    }
        //}

        public byte SumType
        {
            get { return modelRef.SumType; }
            set
            {
                if (value != modelRef.SumType)
                {
                    modelRef.SumType = value;
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("SumType");
                }
            }
        }

        public string DebUsl
        {
            get { return modelRef.DebUsl; }
            set
            {
                if (value != modelRef.DebUsl)
                {
                    modelRef.DebUsl = value;
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("DebUsl");
                }
            }
        }

        public string KreUsl
        {
            get { return modelRef.KreUsl; }
            set
            {
                if (value != modelRef.KreUsl)
                {
                    modelRef.KreUsl = value;
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("KreUsl");
                }
            }
        }

        public string DebOpl
        {
            get { return modelRef.DebOpl; }
            set
            {
                if (value != modelRef.DebOpl)
                {
                    modelRef.DebOpl = value;
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("DebOpl");
                }
            }
        }

        public string KreOpl
        {
            get { return modelRef.KreOpl; }
            set
            {
                if (value != modelRef.KreOpl)
                {
                    modelRef.KreOpl = value;
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("KreOpl");
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

        private Dictionary<string, string> validations = new Dictionary<string, string>();

        public string this[string columnName]
        {
            get
            {
                string res = "";
                switch (columnName)
                {
                    case "Poup": if (Poup <= 0) res = "Значение должно быть больше 0"; break;
                    case "DebUsl": if (!(DebUsl != null && DebUsl.Length == 8 && DebUsl.All(c => Char.IsDigit(c)))) res = "Значение должно быть 8-значным"; break;
                    case "KreUsl": if (!(KreUsl != null && KreUsl.Length == 8 && KreUsl.All(c => Char.IsDigit(c)))) res = "Значение должно быть 8-значным"; break;
                    case "DebOpl": if (!(DebOpl != null && DebOpl.Length == 8 && DebOpl.All(c => Char.IsDigit(c)))) res = "Значение должно быть 8-значным"; break;
                    case "KreOpl": if (!(KreOpl != null && KreOpl.Length == 8 && KreOpl.All(c => Char.IsDigit(c)))) res = "Значение должно быть 8-значным"; break;
                    case "KodUsl": if (KodUsl != 0 && usluga == null) res = "Услуга не найдена в справочнике"; break;
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
