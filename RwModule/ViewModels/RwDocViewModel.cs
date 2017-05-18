using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RwModule.Models;
using CommonModule.Helpers;
using DataObjects;
using DAL;

namespace RwModule.ViewModels
{
    /// <summary>
    /// Модель отображения документа перечня.
    /// </summary>
    public class RwDocViewModel : BasicNotifier
    {
        public RwDocViewModel(RwDoc _model)
        {
            modelRef = _model;
            if (modelRef != null)
                sum_opl = modelRef.Sum_opl;
        }

        private RwDoc modelRef;
        public RwDoc ModelRef
        {
            get { return modelRef; }
        }
        
        public long Id_rwdoc  
        { 
            get { return modelRef.Id_rwdoc; } 
        }

        public int Id_rwlist  
        { 
            get { return modelRef.Id_rwlist; } 
        }
        
        public string Num_doc  
        { 
            get { return modelRef.Num_doc; } 
            set
            {
                modelRef.Num_doc = value;
                NotifyPropertyChanged("Num_doc");
            }
        }
               
        public DateTime Dat_doc  
        { 
            get { return modelRef.Dat_doc; }
            set
            {
                modelRef.Dat_doc = value;
                NotifyPropertyChanged("Dat_doc");
            }
        }
        
        private RwPayType rwPay;
        public RwPayType RwPay
        {
            get
            {
                if (rwPay == null)
                    rwPay = GetRwPay();
                return rwPay;
            }
        }

        private RwPayType GetRwPay()
        {
            RwPayType res = null;
            if (modelRef != null)
                using (var db = new RealContext())
                {
                    res = db.GetRwPayType(modelRef.Paycode);
                }
            return res;
        }

        private RwDocIncomeEsfn esfn;
        public RwDocIncomeEsfn Esfn
        {
            get { return modelRef.Esfn; }
        }

        public decimal Sum_doc  
        { 
            get { return modelRef.Sum_doc; }
            set
            {
                modelRef.Sum_doc = value;
                NotifyPropertyChanged("Sum_doc");
            } 
        }
        
        public decimal Sum_nds  
        { 
            get { return modelRef.Sum_nds; } 
            set
            {
                modelRef.Sum_nds = value;
                NotifyPropertyChanged("Sum_nds");
            } 
        }

        public decimal Sum_itog  
        { 
            get { return modelRef.Sum_doc + modelRef.Sum_nds; } 
        }
       
        public decimal Ndsrate  
        { 
            get { return modelRef.Ndsrate; } 
        }
        
        public string Note  
        { 
            get { return modelRef.Note; } 
        }
        
        public string Kodst  
        { 
            get { return modelRef.Kodst; } 
        }
        
        public long Keysbor  
        { 
            get { return modelRef.Keysbor; } 
        }
        
        public string Nkrt  
        { 
            get { return modelRef.Nkrt; } 
            set 
            { 
                modelRef.Nkrt = value;
                NotifyPropertyChanged("Nkrt");
            } 
        }

        public DateTime? Dzkrt  
        {
            get { return modelRef.Dzkrt; } 
            set 
            { 
                modelRef.Dzkrt = value;
                NotifyPropertyChanged("Dzkrt");
            } 
        }        
        
        public DateTime? Rep_date  
        { 
            get { return modelRef.Rep_date; }
            set 
            { 
                modelRef.Rep_date = value;
                NotifyPropertyChanged("Rep_date");
            } 
        }
        
        public bool Exclude  
        { 
            get { return modelRef.Exclude; }
            set 
            { 
                modelRef.Exclude = value;
                //if (!value) Sum_excl = 0;
                //else Sum_excl = Sum_itog;
                NotifyPropertyChanged("Exclude");
            }
        }

        public decimal Sum_excl
        {
            get { return modelRef.Sum_excl; }
            set
            {
                modelRef.Sum_excl = value;
                NotifyPropertyChanged("Sum_excl");
            }
        }

        public string Excl_info
        {
            get { return modelRef.Excl_info; }
            set
            {
                modelRef.Excl_info = value;
                NotifyPropertyChanged("Excl_info");
            }
        }

        public string Comments
        {
            get { return modelRef.Comments; }
            set
            {
                modelRef.Comments = value;
                NotifyPropertyChanged("Comments");
            }
        }

        public bool IsTransition
        {
            get 
            {
                return (modelRef == null || modelRef.RwList == null || !modelRef.RwList.Transition) ? false : modelRef.RwList.Dat_orc.Month != Dat_doc.Month;
            }
        }

        private bool infoLoaded = false;
        private RwDocInfo info;
        public RwDocInfo Info
        {
            get
            {
                if (!infoLoaded)
                    LoadInfo();
                return info;
            }
        }
        private void LoadInfo()
        {
            if (Id_rwdoc > 0)
                using (var db = new RealContext())
                {
                    info = db.GetRwDocInfo(Id_rwdoc);
                }
            infoLoaded = true;
        }

        private decimal sum_opl;
        public decimal Sum_opl
        {
            get { return sum_opl; }
            set
            {
                sum_opl = value;
                NotifyPropertyChanged("Sum_opl");
                NotifyPropertyChanged("Ostatok");
            }
        }

        public decimal CalcSumOpl()
        {
            using (var db = new RealContext())
            {
                sum_opl = db.CalcRwDocSumOpl(Id_rwdoc);
            }
            return sum_opl;
        }

        public decimal Ostatok
        {
            get { return Sum_itog - Sum_excl - Sum_opl; }
        }
    }
}
