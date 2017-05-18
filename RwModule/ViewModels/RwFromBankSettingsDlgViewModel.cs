using System;
using System.Linq;
using CommonModule.Commands;
using CommonModule.Helpers;
using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Helpers;
using DataObjects.Interfaces;
using System.Windows.Input;
using System.Windows.Data;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using RwModule.Models;
using DAL;

namespace RwModule.ViewModels
{
    /// <summary>
    /// Модель диалога настройки режима приёмки платежей из банка.
    /// </summary>
    public class RwFromBankSettingsDlgViewModel : BaseDlgViewModel
    {
        private IDbService repository;
        private RwFromBankSetting[] settings;
        private BankInfo[] banks;

        public RwFromBankSettingsDlgViewModel(IDbService _rep)
        {
            repository = _rep;

            LoadData();

            AddSettingCommand = new DelegateCommand(ExecuteAdd, CanAdd);
            DeleteSettingCommand = new DelegateCommand(ExecuteDelete, CanDelete);
            SaveChangesCommand = new DelegateCommand(ExecuteSaveChanges, CanSaveChanges);
        }

        private ObservableCollection<RwFromBankSettingViewModel> rwFromBankSettings;
        public ObservableCollection<RwFromBankSettingViewModel> RwFromBankSettings
        {
            get { return rwFromBankSettings; }
            set { SetAndNotifyProperty("RwFromBankSettings", ref rwFromBankSettings, value); }
        }

        private int selIndex;
        public int SelIndex
        {
            get { return selIndex; }
            set { selIndex = value; }
        }

        private void LoadData()
        {
            rwUslTypes = Enumerations.GetAllValuesAndDescriptions<RwUslType>();
            banks = Enumerable.Repeat(new BankInfo { Id = 0, BankName = "Любой" }, 1).Union(repository.GetBankGroups()).ToArray();
            LoadSettings();
        }

        private void LoadSettings()
        {
            using (var db = new RealContext())
            {
                settings = db.RwFromBankSettings.ToArray();
            }

            if (settings != null)
            {
                if (rwFromBankSettings != null)
                {
                    rwFromBankSettings.Clear();
                    Array.ForEach(settings, s => rwFromBankSettings.Add(new RwFromBankSettingViewModel(repository, s)));
                }
                else
                    RwFromBankSettings = new ObservableCollection<RwFromBankSettingViewModel>(settings.Select(s => new RwFromBankSettingViewModel(repository, s)));
            }
        }

        private void RefreshData()
        {
            LoadSettings();
        }

        public BankInfo[] Banks
        {
            get { return banks; }
        }

        Dictionary<RwUslType, string> rwUslTypes;
        public Dictionary<RwUslType, string> RwUslTypes
        {
            get { return rwUslTypes; }
        }

        public DelegateCommand AddSettingCommand { get; set; }

        private void ExecuteAdd()
        {
            var newsetting = new RwFromBankSettingViewModel(repository, new RwFromBankSetting()) { TrackingState = TrackingInfo.Created };
            RwFromBankSettings.Add(newsetting);
            var view = CollectionViewSource.GetDefaultView(RwFromBankSettings);
            view.MoveCurrentTo(newsetting);
        }

        private bool CanAdd()
        {
            return true;
        }

        public DelegateCommand DeleteSettingCommand { get; set; }

        private void ExecuteDelete()
        {
            var view = CollectionViewSource.GetDefaultView(RwFromBankSettings);
            var selsetting = view.CurrentItem as RwFromBankSettingViewModel;
            if (selsetting != null)
            {
                if (selsetting.TrackingState == TrackingInfo.Created)
                    RwFromBankSettings.Remove(selsetting);
                else
                    selsetting.TrackingState = TrackingInfo.Deleted;
            }
        }

        private bool CanDelete()
        {
            return true;
        }

        public DelegateCommand SaveChangesCommand { get; set; }

        private void ExecuteSaveChanges()
        {
            var chmodels = RwFromBankSettings.Where(s => s.TrackingState != TrackingInfo.Unchanged);
            using (var db = new RealContext())
            {
                foreach (var s in chmodels)
                    switch (s.TrackingState)
                    {
                        case TrackingInfo.Created: db.Entry(s.Model).State = System.Data.Entity.EntityState.Added; break;
                        case TrackingInfo.Deleted: db.Entry(s.Model).State = System.Data.Entity.EntityState.Deleted; break;
                        default: db.Entry(s.Model).State = System.Data.Entity.EntityState.Modified; break;
                    }
                db.SaveChanges();
            }
            RefreshData();
        }

        private bool CanSaveChanges()
        {
            var ch = RwFromBankSettings.Where(s => s.TrackingState != TrackingInfo.Unchanged);
            return ch.Any() && ch.All(s => s.IsValid);
        }

    }
}
