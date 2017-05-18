using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using CommonModule.Commands;
using DataObjects;
using DataObjects.Interfaces;
using CommonModule.Helpers;


namespace CommonModule.ViewModels
{
    /// <summary>
    /// Модель диалога выбора направления реализации, интервала дат и плательщика.
    /// </summary>
    public class PoupDatesKpokDlgViewModel : BaseDlgViewModel
    {
        private PoupSelectionViewModel poupSelVm;
        private DateRangeDlgViewModel dateRangeVm;
        private KaSelectionViewModel platVm;
        private IDbService repository;
        public PoupDatesKpokDlgViewModel(IDbService _rep, bool _save)
        {
            repository = _rep;
            poupSelVm = new PoupSelectionViewModel(_rep);
            poupSelVm.PropertyChanged += BasePropertyChanged;
            dateRangeVm = new DateRangeDlgViewModel(_save);
            dateRangeVm.PropertyChanged += BasePropertyChanged;
        }

        public PoupDatesKpokDlgViewModel(IDbService _rep)
            :this(_rep, true)
        {}

        public PoupSelectionViewModel PoupSelection { get { return poupSelVm; } }

        private void BasePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            NotifyPropertyChanged(e.PropertyName);
            isKasDirty = true;
            if (!IsAllKas)
                NotifyPropertyChanged("KaList"); ;
        }

        public override bool IsValid()
        {
            return base.IsValid() 
                && poupSelVm.IsValid()
                && dateRangeVm.IsValid() 
                && (SelectedKA!=null || IsAllKas);
        }

        public PoupModel[] Poups
        {
            get
            {
                return poupSelVm.Poups;
            }
        }

        public PoupModel SelPoup
        {
            get { return poupSelVm.SelPoup; }
            set
            {
                if (value != poupSelVm.SelPoup)
                {
                    poupSelVm.SelPoup = value;
                }
            }
        }

        public Selectable<PkodModel>[] Pkods
        {
            get
            {
                return poupSelVm.Pkods;
            }
        }

        public PkodModel[] SelPkods
        {
            get { return poupSelVm.SelPkods; }
        }        
        
        public bool IsPkodEnabled
        {
            get
            {
                return SelPoup != null && SelPoup.Kod == 33;
            }
        }

        /// <summary>
        /// Последние сформированные
        /// </summary>
        private bool isOnlyLast = true;
        public bool IsOnlyLast
        {
            get { return isOnlyLast; }
            set 
            { 
                SetAndNotifyProperty("IsOnlyLast", ref isOnlyLast, value);
                if (IsDateFormSelected)
                    SetNeedNewKaList();
            }
        }

        /// <summary>
        /// Указана дата формирования
        /// </summary>
        private bool isDateFormSelected = true;
        public bool IsDateFormSelected
        {
            get { return isDateFormSelected; }
            set { SetAndNotifyProperty("IsDateFormSelected", ref isDateFormSelected, value); }
        }

        /// <summary>
        /// Дата формирования
        /// </summary>
        private DateTime dateForm = DateTime.Now.Date;
        public DateTime DateForm
        {
            get { return dateForm; }
            set 
            { 
                SetAndNotifyProperty("DateForm", ref dateForm, value);
                if (IsDateFormSelected)
                    SetNeedNewKaList();
            }
        }

        public string DatesLabel 
        {
            get { return dateRangeVm.DatesLabel; }
            set { dateRangeVm.DatesLabel = value; } 
        }

        /// <summary>
        /// Сохранять ли введённые даты
        /// </summary>
        public bool IsSaveSettings
        {
            get { return dateRangeVm.IsSaveSettings; }
            set { dateRangeVm.IsSaveSettings = value; }
        }

        public DateTime DateFrom
        {
            get { return dateRangeVm.DateFrom; }
            set
            {
                if (value != dateRangeVm.DateFrom)
                {
                    dateRangeVm.DateFrom = value;
                }
            }
        }

        public DateTime DateTo
        {
            get { return dateRangeVm.DateTo; }
            set
            {
                if (value != dateRangeVm.DateTo)
                {
                    dateRangeVm.DateTo = value;
                }
            }
        }

        public ICommand CopyDateCommand
        {
            get
            {
                return dateRangeVm.CopyDateCommand;
            }
        }

        /// <summary>
        /// Функция для формирования списка контрагентов
        /// </summary>
        public Func<PoupDatesKpokDlgViewModel, IEnumerable<KontrAgent>> GetKas { get; set; }

        private bool isAllKas = true;

        /// <summary>
        /// Признак выбора всех контрагентов
        /// </summary>
        public bool IsAllKas
        {
            get { return isAllKas; }
            set
            {
                if (value != isAllKas)
                {
                    isAllKas = value;
                    if (!isAllKas)
                        NotifyPropertyChanged("KaList");
                    NotifyPropertyChanged("IsAllKas");
                }
            }
        }


        public bool isPerKa;
        /// <summary>
        /// Отчёт по каждому плательщику?
        /// </summary>
        public bool IsPerKa
        {
            get { return isPerKa; }
            set
            {
                SetAndNotifyProperty("IsPerKa", ref isPerKa, value);
                if (isPerKa && isKasDirty)
                    SetNeedNewKaList();
            }
        }

        /// <summary>
        /// Требуется обновление списка контрагентов
        /// </summary>
        private bool isKasDirty = true;

        private void SetNeedNewKaList()
        {
            isKasDirty = true;
            NotifyPropertyChanged("KaList");
        }

        private void LoadKas()
        {
            isKasDirty = false;
            if (GetKas == null || !poupSelVm.IsValid() && !dateRangeVm.IsValid()) return;
            var kas = GetKas(this);
            if (platVm == null)
                    platVm = new KaSelectionViewModel(repository, kas);
            else
                    platVm.PopulateKaList(kas);
        }

        /// <summary>
        /// Доступные контрагенты
        /// </summary>
        public ObservableCollection<KontrAgent> KaList
        {
            get
            {
                //if (IsAllKas) return null;
                if (isKasDirty && (!IsAllKas || IsAllKas && IsPerKa))
                    LoadKas();

                //if (platVm == null) System.Windows.Forms.MessageBox.Show("PLATVM is NULL \n" + (isKasDirty ? "isKasDirty\n" : "! isKasDirty\n") + (IsAllKas ? "IsAllKas\n" : "! IsAllKas\n") + (IsPerKa ? "IsPerKa\n" : "! IsPerKa\n"));
                return (platVm == null ? null : platVm.KaList);
            }
        }

        public KontrAgent SelectedKA
        {
            get
            {
                return platVm==null ? null : platVm.SelectedKA;
            }
            set
            {
                platVm.SelectedKA = value;
            }
        }
    }
}
