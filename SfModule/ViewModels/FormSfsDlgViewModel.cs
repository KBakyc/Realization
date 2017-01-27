using System;
using System.ComponentModel;
using System.Windows.Input;
using CommonModule.Commands;
using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Interfaces;
using CommonModule.Helpers;


namespace SfModule.ViewModels
{
    public class FormSfsDlgViewModel : BaseDlgViewModel
    {
        private PoupAndDatesDlgViewModel poupdatesVm;
        private IDbService repository;

        public FormSfsDlgViewModel(IDbService _rep, bool _save)
        {
            repository = _rep;
            poupdatesVm = new PoupAndDatesDlgViewModel(repository);
            IsUseOldNumSf = true;
            isMyMode = true;
        }

        public FormSfsDlgViewModel(IDbService _rep)
            : this(_rep, true)
        { }

        public PoupAndDatesDlgViewModel PoupDatesSelection { get { return poupdatesVm; } }

        public override bool IsValid()
        {
            return base.IsValid() 
                && PoupDatesSelection.IsValid();
        }

        public PoupModel SelPoup
        {
            get { return poupdatesVm.SelPoup; }
        }

        public PkodModel[] SelPkods
        {
            get { return poupdatesVm.SelPkods; }
        }

        /// <summary>
        /// Сохранять ли введённые даты
        /// </summary>

        public DateTime DateFrom
        {
            get { return poupdatesVm.DateFrom; }
        }

        public DateTime DateTo
        {
            get { return poupdatesVm.DateTo; }
        }

        /// <summary>
        /// Признак использования старых номеров счетов
        /// </summary>
        public bool IsUseOldNumSf { get; set; }

        /// <summary>
        /// Дата формируемых счетов
        /// </summary>
        public DateTime? DateSf { get; set; }

        private bool isUseAcceptTime;
        /// <summary>
        /// Признак разделения счетов по времени приёмки отгрузки
        /// </summary>
        public bool IsUseAcceptTime 
        {
            get { return isUseAcceptTime; }
            set
            {
                SetAndNotifyProperty("IsUseAcceptTime", ref isUseAcceptTime, value);
                if (value)
                    SetAndNotifyProperty("IsOnlyLastAccepted", ref isOnlyLastAccepted, false);
            }
        }

        private bool isOnlyLastAccepted;
        /// <summary>
        /// Формировать счета только по последним принятым отгрузкам
        /// </summary>
        public bool IsOnlyLastAccepted
        {
            get { return isOnlyLastAccepted; }
            set
            {
                SetAndNotifyProperty("IsOnlyLastAccepted", ref isOnlyLastAccepted, value);
                if (value)
                    SetAndNotifyProperty("IsUseAcceptTime", ref isUseAcceptTime, false);

            }
        }

        private bool isAllMode;
        private bool isMyMode;

        /// <summary>
        /// Формировать счета по всей отгрузке
        /// </summary>
        public bool IsAllMode
        {
            get { return isAllMode; }
            set
            {
                SetAndNotifyProperty("IsAllMode", ref isAllMode, value);
                SetAndNotifyProperty("IsMyMode", ref isMyMode, !value);
            }
        }

        /// <summary>
        /// Формировать счета по принятой мной отгрузке
        /// </summary>
        public bool IsMyMode
        {
            get { return isMyMode; }
            set
            {
                SetAndNotifyProperty("IsMyMode", ref isMyMode, value);
                SetAndNotifyProperty("IsAllMode", ref isAllMode, !value);
            }
        }


        /// <summary>
        /// Режим формирования в зависимости от времени приёмки отгрузки
        /// </summary>
        public byte DtAcceptedMode 
        {
            get 
            { return IsUseAcceptTime ? (byte)1
                                     : (IsOnlyLastAccepted ? (byte)2 
                                                           : (byte)0); }
        }

    }
}
