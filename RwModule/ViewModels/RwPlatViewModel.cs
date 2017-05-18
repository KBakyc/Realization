using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.Helpers;
using RwModule.Models;
using DataObjects;
using DataObjects.Interfaces;

namespace RwModule.ViewModels
{
    /// <summary>
    /// Модель отображения банковского платежа по ЖД услугам.
    /// </summary>
    public class RwPlatViewModel : BasicNotifier
    {
        private RwPlat modelRef;
        IDbService repository;

        public RwPlatViewModel(RwPlat _model)
        {
            modelRef = _model;
        }

        public RwPlat GetModel()
        {
            return modelRef;
        }
        
        public int Idrwplat
        { 
            get { return modelRef.Idrwplat; } 
        }

        public int Numplat 
        { 
            get { return modelRef.Numplat; } 
        }

        public DateTime Datplat 
        { 
            get { return modelRef.Datplat; } 
        }

        public DateTime Datbank 
        { 
            get { return modelRef.Datbank; } 
        }

        public decimal Sumplat 
        { 
            get { return modelRef.Sumplat; } 
        }
        
        public decimal Sumopl
        { 
            get { return modelRef.Sumplat - modelRef.Ostatok; } 
        }

        public decimal Ostatok
        { 
            get { return modelRef.Ostatok; }
            set 
            { 
                modelRef.Ostatok = value;
                NotifyPropertyChanged("Ostatok");
                NotifyPropertyChanged("Sumopl");
            }
        }

        public DateTime? Datzakr
        {
            get { return modelRef.Datzakr; }
        }
        
        public string Whatfor
        {
            get { return modelRef.Whatfor; }
        }

        public RwPlatDirection Direction
        {
            get { return modelRef.Direction; }
        }

        public string Notes
        {
            get { return modelRef.Notes; }
        }

        public RwUslType Idusltype
        {
            get { return modelRef.Idusltype; }
        }

        private Dictionary<string, object> payDocInfo = null;

        private BankInfo platBankInfo;
        public BankInfo PlatBankInfo
        {
            set
            {
                SetAndNotifyProperty("PlatBankInfo", ref platBankInfo, value);
            }
            get
            {
                if (platBankInfo == null)
                    LoadBankInfo();
                return platBankInfo;
            }
        }

        private void LoadBankInfo()
        {
            if (payDocInfo == null)
            {
                System.Threading.Tasks.Task.Factory
                    .StartNew(LoadPayDocInfo)
                    .ContinueWith(T => ParseBankInfo(),System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
            }
            else
                ParseBankInfo();
        }

        private void LoadPayDocInfo()
        {
            var idreg = modelRef.Idpostes;
            if (idreg == null) return;
            if (repository == null) repository = CommonModule.CommonSettings.Repository;
            payDocInfo = repository.GetPayDocInfo(idreg.Value, true);
        }

        private void ParseBankInfo()
        {
            if (payDocInfo == null || !payDocInfo.ContainsKey("bankinfo")) return;
            var bi = payDocInfo["bankinfo"] as BankInfo;
            if (bi != null)
                PlatBankInfo = bi;
        }

    }
}
