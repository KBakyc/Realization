using System;
using System.Linq;
using System.Windows.Input;
using CommonModule.Commands;
using CommonModule.Helpers;
using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Interfaces;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace InfoModule.ViewModels
{
    /// <summary>
    /// Модель диалога управления рабочим календарём (Настройка праздничных дней и переносов рабочих/выходных дней).
    /// </summary>
    public class WorkCalendarViewModel : BaseDlgViewModel
    {
        private IDbService repository;

        public WorkCalendarViewModel(IDbService _rep, bool _readonly)
        {
            repository = _rep;
            isReadOnly = _readonly;
            LoadData();
            setHolydayCommand = new DelegateCommand<bool>(ExecSetHolyday, CanSetHolyday);
        }
        
        private bool isReadOnly;
        public bool IsReadOnly
        {
            get { return isReadOnly; }
            set { isReadOnly = value; }
        }

        /// <summary>
        /// Загрузка данных для редактирования
        /// </summary>
        private void LoadData()
        {
            displayDate = selectedDate = DateTime.Today;
            LoadDates();
        }

        private DateTime? displayDate;
        public DateTime? DisplayDate
        {
            get { return displayDate; }
            set 
            {
                if (SetAndNotifyProperty(() => DisplayDate, ref displayDate, value) && displayDate.HasValue)
                    LoadDates();
            }
        }

        private void LoadDates()
        {
            Dates = repository.GetDates(displayDate.Value.AddMonths(-2), displayDate.Value.AddMonths(2));
        }

        private DateTime? selectedDate;
        public DateTime? SelectedDate
        {
            get { return selectedDate; }
            set 
            { 
                SetAndNotifyProperty(() => SelectedDate, ref selectedDate, value);
                NotifyPropertyChanged(() => IsSelectedHoliday);
            }
        }

        private Dictionary<DateTime, bool> dates;
        public Dictionary<DateTime, bool> Dates
        {
            get { return dates; }
            set { SetAndNotifyProperty(() => Dates, ref dates, value); }
        }

        public bool? IsSelectedHoliday 
        { 
            get 
            { 
                bool res = false;
                if (selectedDate == null || dates == null || !dates.TryGetValue(selectedDate.Value, out res)) return null;
                else
                    return res;
            } 
        }

        private ICommand setHolydayCommand;
        
        public ICommand SetHolydayCommand
        {
            get { return setHolydayCommand; }
        }

        private bool CanSetHolyday(bool _on)
        {
            return selectedDate != null && !isReadOnly; 
        }

        private void ExecSetHolyday(bool _on)
        {
            repository.SetDateInfo(selectedDate.Value, _on);
            LoadDates();
        }

    }
}
