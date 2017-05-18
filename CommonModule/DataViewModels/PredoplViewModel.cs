using System;
using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Interfaces;
using System.Collections.Generic;


namespace CommonModule.DataViewModels
{
    /// <summary>
    /// Модель отображения предоплаты.
    /// </summary>
    public class PredoplViewModel : BasicViewModel, ITrackable
    {
        private IDbService repository;

        public PredoplViewModel(IDbService _rep, PredoplModel _p)
        {
            PredoplRef = _p;
            repository = _rep;
        }

        private PredoplModel predoplRef;
        public PredoplModel PredoplRef
        {
            get
            {
                if (predoplRef == null)
                    predoplRef = new PredoplModel(0, new byte[]{0});
                return predoplRef;
            }
            set
            {
                if (value != predoplRef)
                    predoplRef = value;
            }
        }

        public int Idpo 
        {
            get { return PredoplRef.Idpo; }
        }

        // Предоплата или возврат
        public short Direction
        {
            get { return PredoplRef.Direction; }
        }

        // плательщик
        private KontrAgent platelschik;
        public KontrAgent Platelschik
        {
            get
            {
                if (platelschik == null)
                    platelschik = repository.GetKontrAgent(PredoplRef.Kgr);
                return platelschik;
            }
            set
            {
                if (value.Kgr != platelschik.Kgr)
                {
                    platelschik = value;
                    NotifyPropertyChanged("Platelschik");
                    TrackingState = TrackingInfo.Updated;
                }
            }
        }

        public int NomDok 
        {
            get { return PredoplRef.Ndok; }
        }

        public DateTime DatDok
        {
            get { return PredoplRef.DatVvod; }
        }

        public decimal SumBank
        {
            get { return PredoplRef.SumBank; }
        }

        public string ValBank
        {
            get { return PredoplRef.KodValB; }
        }

        public DateTime DatPost
        {
            get { return PredoplRef.DatPropl; }
        }
        
        public DateTime? DatZakr
        {
            get { return PredoplRef.DatZakr; }
        }

        public decimal SumPropl
        {
            get { return PredoplRef.SumPropl; }
        }

        private Valuta valPropl;
        public Valuta ValPropl
        {
            get 
            {
                if (valPropl == null)
                    valPropl = repository.GetValutaByKod(PredoplRef.KodVal);
                return valPropl; 
            }
        }

        public decimal SumOtgr
        {
            get { return PredoplRef.SumOtgr; }
            set 
            { 
                PredoplRef.SumOtgr = value;
                NotifyPropertyChanged("SumOtgr");
                NotifyPropertyChanged("Ostatok");
            }
        }

        public decimal Ostatok
        {
            get { return SumPropl - SumOtgr; }
        }

        public string Whatfor
        {
            get { return PredoplRef.Whatfor; }
        }

        public string Prim
        {
            get { return PredoplRef.Prim; }
        }

        private string osntxt;
        public string Osntxt 
        {
            get 
            { 
                if (osntxt == null)
                    osntxt = repository.GetPredoplOsn(Idpo);
                return osntxt;
            }
        }

        private Dictionary<string, object> payDocInfo = null;

        private BankInfo predoplBankInfo;
        public BankInfo PredoplBankInfo
        {
            set
            {
                SetAndNotifyProperty("PredoplBankInfo", ref predoplBankInfo, value);
            }
            get 
            {
                if (predoplBankInfo == null)
                    LoadBankInfo();
                return predoplBankInfo; 
            }
        }

        private void LoadBankInfo()
        {
            if (payDocInfo == null)
            {
                Action work = () =>
                {
                    LoadPayDocInfo();
                    ParseBankInfo();
                };
                work.BeginInvoke(null,null);
            }
            else
                ParseBankInfo();
        }

        private void LoadPayDocInfo()
        {
            payDocInfo = repository.GetPayDocInfo(predoplRef.IdRegDoc);
        }

        private void ParseBankInfo()
        {
            if (payDocInfo == null || !payDocInfo.ContainsKey("bankinfo")) return;
            var bi = payDocInfo["bankinfo"] as BankInfo;
            if (bi != null)
                PredoplBankInfo = bi;
        }


        public int Poup
        {
            get { return PredoplRef.Poup; }
        }

        private PoupViewModel poupVmRef;
        public PoupViewModel PoupVmRef
        {
            get
            {
                if (poupVmRef == null && Poup != 0)
                    poupVmRef = new PoupViewModel(repository, Poup, Pkod);
                return poupVmRef;
            }
        }


        public short Pkod
        {
            get { return PredoplRef.Pkod; }
        }

        /// <summary>
        /// Количество оплаченных счетов
        /// </summary>
        public int CountSfs
        {
            get
            {
                return IsVozvrat ? 0 : PayedSfs.Length;
            }
        }

        private SfModel[] payedSfs;

        /// <summary>
        /// Оплаченные счета
        /// </summary>
        public SfModel[] PayedSfs
        {
            get
            {
                if (payedSfs == null)
                    payedSfs = repository.GetSfsPayedByPredopl(Idpo);
                return payedSfs;
            }
        }

        public bool IsVozvrat { get { return Direction == 1; } }

        private PredoplModel[] linkedByVozvrats;

        /// <summary>
        /// Возвраты на предоплату или предоплаты, погашенные возвратом
        /// </summary>
        public PredoplModel[] LinkedByVozvrats
        {
            get
            {
                if (linkedByVozvrats == null)
                {
                    if (IsVozvrat)
                        linkedByVozvrats = repository.GetPredoplsPayedByVozvrat(PredoplRef.Idpo);
                    else
                        linkedByVozvrats = repository.GetPredoplVozvrats(PredoplRef.Idpo);
                }
                return linkedByVozvrats;
            }
        }

        private bool isAgreeLoaded;
        private AgreementViewModel agreement;
        public AgreementViewModel Agreement
        {
            get
            {
                if (!isAgreeLoaded)
                {
                    if (PredoplRef != null && PredoplRef.IdAgree != 0)
                    {
                        var aModel = repository.GetAgreementById(PredoplRef.IdAgree);
                        agreement = new AgreementViewModel(aModel, repository);
                    }
                    isAgreeLoaded = true;
                }
                return agreement;
            }
        }

        /// <summary>
        /// Допустимо ли изменение предоплаты
        /// </summary>
        public bool IsPaysExist
        {
            get { return CountSfs != 0 && LinkedByVozvrats.Length != 0; }
        }

        public bool IsKursChanged
        {
            get { return predoplRef != null && predoplRef.DatKurs != null && predoplRef.DatKurs != predoplRef.DatVvod; }
        }

        #region ITrackable Members

        public TrackingInfo TrackingState
        {
            get
            {
                return PredoplRef.TrackingState;
            }
            set
            {
                PredoplRef.TrackingState = value;
            }
        }
        #endregion

    }
}