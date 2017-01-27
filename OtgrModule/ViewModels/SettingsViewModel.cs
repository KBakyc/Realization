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

namespace OtgrModule.ViewModels
{
    public class SettingsViewModel : BaseDlgViewModel
    {
        private IDbService repository;

        public SettingsViewModel(IDbService _rep)
        {
            repository = _rep;
            LoadData();
        }
        /// <summary>
        /// Загрузка данных для редактирования
        /// </summary>
        private void LoadData()
        {
            switch (Properties.Settings.Default.KodfsSelMode)
            {
                case 1: IsMyKodfSelectMode = true; break;
                case 2: IsNoneKodfSelectMode = true; break;
                default: IsAllKodfSelectMode = true; break;
            }
            IsShowUnchecked = Properties.Settings.Default.ShowUnchecked;
        }

        /// <summary>
        /// Изначальный режим выборки
        /// </summary>
        public short KodfsSelMode
        {
            get
            {
                if (IsMyKodfSelectMode) return 1;
                if (IsNoneKodfSelectMode) return 2;
                else return 0;
            }
        }

        private bool isAllKodfSelectMode;
        public bool IsAllKodfSelectMode 
        {
            get { return isAllKodfSelectMode; }
            set
            {
                if (value != isAllKodfSelectMode)
                {
                    isAllKodfSelectMode = value;
                    if (isAllKodfSelectMode == true)
                    {
                        IsMyKodfSelectMode = false;
                        IsNoneKodfSelectMode = false;
                    }
                    NotifyPropertyChanged("IsAllKodfSelectMode");
                }
            }
        }

        private bool isMyKodfSelectMode;
        public bool IsMyKodfSelectMode 
        {
            get { return isMyKodfSelectMode; }
            set
            {
                if (value != isMyKodfSelectMode)
                {
                    isMyKodfSelectMode = value;
                    if (isMyKodfSelectMode == true)
                    {
                        IsAllKodfSelectMode = false;
                        IsNoneKodfSelectMode = false;
                    }
                    NotifyPropertyChanged("IsMyKodfSelectMode");
                }
            }
        }

        private bool isNoneKodfSelectMode;
        public bool IsNoneKodfSelectMode
        {
            get { return isNoneKodfSelectMode; }
            set
            {
                if (value != isNoneKodfSelectMode)
                {
                    isNoneKodfSelectMode = value;
                    if (isNoneKodfSelectMode == true)
                    {
                        IsAllKodfSelectMode = false;
                        IsMyKodfSelectMode = false;
                    }
                    NotifyPropertyChanged("IsNoneKodfSelectMode");
                }
            }
        }

        public bool IsShowUnchecked { get; set; }

    }
}
