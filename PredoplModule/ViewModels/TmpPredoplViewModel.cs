using System;
using System.Linq;
using System.ComponentModel;
using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Interfaces;
using System.Collections.Generic;

namespace PredoplModule.ViewModels
{
    public class TmpPredoplViewModel : BasicViewModel, IDataErrorInfo 
    {
        private IDbService repository;
        private AgreeSelectionViewModel agreeSelection;


        public TmpPredoplViewModel(IDbService _rep)
        {
            repository = _rep;
        }

        public AgreeSelectionViewModel AgreeSelection
        {
            get
            {
                if (agreeSelection == null)
                    CreateAgreeSelection();
                return agreeSelection;
            }
        }

        private void CreateAgreeSelection()
        {
            agreeSelection = new AgreeSelectionViewModel(repository, PredoplRef.Kgr, PredoplRef.IdAgree);
            agreeSelection.PropertyChanged += agreeSelection_PropertyChanged;
        }
        
        void  agreeSelection_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (agreeSelection.IsValid() 
                && e.PropertyName == "SelectedAgreement" 
                && (Agreement != null && agreeSelection.SelectedAgreeId != Agreement.AgreementRef.IdAgreement) || Agreement == null)
                Agreement = agreeSelection.SelectedAgreement;
        }

        private bool isAgreeLoaded;
        private AgreementViewModel agreement;
        public AgreementViewModel Agreement
        {
            get 
            {
                if (!isAgreeLoaded)
                {
                    LoadAgreeInfo();
                }
                return agreement;
            }
            set
            {
                if (value != agreement)
                {
                    agreement = value;
                    if (PredoplRef != null)
                    {
                        int newid = value == null ? 0 : value.AgreementRef.IdAgreement;
                        PredoplRef.IdAgree = newid;
                    }
                    NotifyChanges("Agreement");
                    NotifyPropertyChanged("CanAccept");
                }
            }
        }

        private void LoadAgreeInfo()
        {
            if (PredoplRef != null && PredoplRef.IdAgree != 0)
            {
                var aModel = repository.GetAgreementById(PredoplRef.IdAgree);
                agreement = new AgreementViewModel(aModel, repository);
            }
            isAgreeLoaded = true;
        }

        private PredoplModel predoplRef;
        public PredoplModel PredoplRef 
        {
            get { return predoplRef; }
            set { predoplRef = value; }
        }

        private AcceptableInfo info;
        public AcceptableInfo Info 
        {
            get { return info; }
            set
            {
                info = value;
                if (predoplRef != null && predoplRef.IdAgree == 0)
                    AddErrorInfo("Не указан договор");
            }
        }

        // плательщик (или за кого платят)
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
                if (value != null && value != platelschik)
                    platelschik = value;
            }
        }

        public int Poup
        {
            get { return PredoplRef.Poup; }
        }

        public short Pkod
        {
            get { return PredoplRef.Pkod; }
        }

        public string FullPoupNumberString
        {
            get { return GenerateFullPoupNumberString(); }
        }

        private string GenerateFullPoupNumberString()
        {
            string res = Poup.ToString();
            if (Pkod > 0)
                res += "/" + Pkod.ToString();
            return res;
        }

        // номер банковского документа
        public int NomDok 
        { 
            get
            {
                return PredoplRef.Ndok;
            } 
            set
            {
                if (value != PredoplRef.Ndok)
                    PredoplRef.Ndok = value;
            }
        }

        // дата банковского документа
        public DateTime DatDok
        {
            get
            {
                return PredoplRef.DatPropl;
            }
            set
            {
                if (value != PredoplRef.DatPropl)
                    PredoplRef.DatPropl = value;
            }

        }

        // Сумма по банку (в валюте банка)
        public decimal SumBank
        {
            get
            {
                return PredoplRef.SumBank;
            }
            set
            {
                if (value != PredoplRef.SumBank)
                    PredoplRef.SumBank = value;
            }

        }

        // валюта по банку 
        public string ValBank
        {
            get
            {
                return PredoplRef.KodValB;
            }
            set
            {
                if (value != PredoplRef.KodValB)
                    PredoplRef.KodValB = value;
            }
        }

        // дата банковского документа
        public DateTime DatPost
        {
            get
            {
                return PredoplRef.DatVvod;
            }
            set
            {
                if (value != PredoplRef.DatVvod)
                    PredoplRef.DatVvod = value;
            }
        }

        // Сумма предоплаты (в валюте договора)
        public decimal SumPropl
        {
            get
            {
                return PredoplRef.SumPropl;
            }
            set
            {
                if (value != PredoplRef.SumPropl)
                    PredoplRef.SumPropl = value;
            }
        }

        // валюта по договору
        public string ValPropl
        {
            get
            {
                return PredoplRef.KodVal;
            }
            set
            {
                if (value != PredoplRef.KodVal)
                    PredoplRef.KodVal = value;
            }
        }

        // за что предоплата
        public string Naznachenie
        {
            get
            {
                return PredoplRef.Whatfor;
            }
            set
            {
                if (value != PredoplRef.Whatfor)
                {
                    PredoplRef.Whatfor = value;
                    NotifyChanges("Naznachenie");
                }
            }
        }

        // Принять/отказатья от приёма предоплаты
        public bool IsAccepted
        {
            get
            {
                return Info.IsAccepted;
            }
            set
            {
                if (value != Info.IsAccepted)
                {
                    Info.IsAccepted = value;
                    NotifyChanges("IsAccepted");
                }
            }
        }

        public bool IsVozvrat { get { return predoplRef != null && predoplRef.Direction == 1; } }

        // Приём возможен
        public bool CanAccept 
        {
            get { return CheckedStatus < 2
                         && !HasErrors; }
        }

        // есть ли ошибки
        public int CheckedStatus
        {
            get
            {
                return Info.InfoType;
            }
        }

        private bool HasErrors
        {
            get { return !IsValid(); }
        }

        private bool IsValid()
        {
            if (!isAgreeLoaded && PredoplRef.IdAgree == 0 || isAgreeLoaded && Agreement == null)
            {
                return IsAccepted = false;
            }
            return true;
        }

        private void AddErrorInfo(string _msg)
        {
            Info.Infos = Info.Infos.Concat(Enumerable.Repeat(_msg, 1)).ToArray();
            if (IsAccepted && !CanAccept) IsAccepted = false;
            NotifyPropertyChanged("CanAccept");
            NotifyPropertyChanged("IsAccepted");
        }

        // текст ошибок
        public string[] Errors
        {
            get
            {
                return CheckedStatus > 0 ? Info.Infos : null;
            }
        }

        public string InfoText
        {
            get { return String.Join("\n", Info.Infos); }
        }

        public bool HasInfo
        {
            get { return Info.Infos != null && Info.Infos.Length > 0; }
        }

        private void NotifyChanges(string prop)
        {
            NotifyPropertyChanged(prop);
            IsChanged = true;
        }

        // изменена ли модель пользователем
        public bool IsChanged { get; set; }


        #region IDataErrorInfo Members

        public string Error
        {
            get { return CheckedStatus > 0 || HasErrors ? InfoText : ""; }
        }

        public string this[string columnName]
        {
            get { return CheckedStatus > 0 ? Info.Infos[0] : ""; }
        }

        #endregion
    }
}