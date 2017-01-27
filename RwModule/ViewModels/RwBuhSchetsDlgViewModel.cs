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
    public class RwBuhSchetsDlgViewModel : BaseDlgViewModel
    {
        private PoupModel[] poups;
        private IDbService repository;
        private RwBuhSchet[] schets;
        private Predicate<object> filter;

        public RwBuhSchetsDlgViewModel(IDbService _rep)
        {
            repository = _rep;

            LoadData();
            
            AddSchetCommand = new DelegateCommand(ExecuteAdd, CanAdd);
            DeleteSchetCommand = new DelegateCommand(ExecuteDelete, CanDelete);
            SaveChangesCommand = new DelegateCommand(ExecuteSaveChanges, CanSaveChanges);
        }

        private ObservableCollection<RwBuhSchetViewModel> rwBuhSchets;
        public ObservableCollection<RwBuhSchetViewModel> RwBuhSchets 
        {
            get { return rwBuhSchets; }
            set { SetAndNotifyProperty("RwBuhSchets", ref rwBuhSchets, value); } 
        }

        private PoupModel selectedPoup;
        public PoupModel SelectedPoup
        {
            get { return selectedPoup; }
            set
            {
                if (value != selectedPoup)
                {
                    selectedPoup = value;
                    if (selectedPoup != null)
                        filter = o => (o as RwBuhSchetViewModel).Poup == selectedPoup.Kod;
                    else
                        filter = null;
                    DoApplyFilter();
                }
            }
        }

        private void DoApplyFilter()
        {
            var view = CollectionViewSource.GetDefaultView(RwBuhSchets);
            view.Filter = filter;
            view.Refresh();
            if (view.CurrentItem != null)
                view.MoveCurrentToFirst();
        }


        private int selIndex;
        public int SelIndex
        {
            get { return selIndex; }
            set { selIndex = value; }
        }

        private void LoadData()
        {
            poups = repository.Poups.Values.Where(p => p.IsActive).ToArray();       
            rTypes = Enumerations.GetAllValuesAndDescriptions<RefundTypes>();
            LoadSchets();
        }

        private void LoadSchets()
        {
            using (var db = new RealContext())
            {
                schets = db.RwBuhSchets.ToArray();
                sTypes = db.GetSumTypes();
            }

            if (schets != null)
            {
                if (rwBuhSchets != null)
                {
                    rwBuhSchets.Clear();
                    Array.ForEach(schets, s => rwBuhSchets.Add(new RwBuhSchetViewModel(repository, s)));
                }
                else
                    RwBuhSchets = new ObservableCollection<RwBuhSchetViewModel>(schets.Select(s => new RwBuhSchetViewModel(repository, s)));                
            }
        }

        private void RefreshData()
        {
            LoadSchets();            
            DoApplyFilter();
        }

        public PoupModel[] Poups
        {
            get { return poups; }
        }

        SumType[] sTypes;
        public SumType[] STypes
        {
            get { return sTypes; }
        }

        Dictionary<RefundTypes, string> rTypes;
        public Dictionary<RefundTypes, string> RefundTypes
        {
            get { return rTypes; }
        }

        public DelegateCommand AddSchetCommand { get; set; }

        private void ExecuteAdd()
        {
            var newschet = new RwBuhSchetViewModel(repository, new RwBuhSchet()) { TrackingState = TrackingInfo.Created };
            if (selectedPoup != null)
                newschet.Poup = selectedPoup.Kod;
            RwBuhSchets.Add(newschet);
            var view = CollectionViewSource.GetDefaultView(RwBuhSchets);
            view.MoveCurrentTo(newschet);
        }

        private bool CanAdd()
        {
            return true;
        }

        public DelegateCommand DeleteSchetCommand { get; set; }

        private void ExecuteDelete()
        {
            var view = CollectionViewSource.GetDefaultView(RwBuhSchets);
            var selschet = view.CurrentItem as RwBuhSchetViewModel;
            if (selschet != null)
            {
                if (selschet.TrackingState == TrackingInfo.Created)
                    RwBuhSchets.Remove(selschet);
                else
                    selschet.TrackingState = TrackingInfo.Deleted;
            }
        }

        private bool CanDelete()
        {
            return true;
        }

        public DelegateCommand SaveChangesCommand { get; set; }

        private void ExecuteSaveChanges()
        {
            var chmodels = RwBuhSchets.Where(s => s.TrackingState != TrackingInfo.Unchanged);
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
            var ch = RwBuhSchets.Where(s => s.TrackingState != TrackingInfo.Unchanged);
            return ch.Any() && ch.All(s => s.IsValid);
        }

    }   
}
