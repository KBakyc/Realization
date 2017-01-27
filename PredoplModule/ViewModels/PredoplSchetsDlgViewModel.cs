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

namespace PredoplModule.ViewModels
{
    public class PredoplSchetsDlgViewModel : BaseDlgViewModel
    {
        private PoupModel[] poups;
        private Valuta[] vals;
        private BankInfo[] banks;
        private Dictionary<byte, string> rtypes;
        private IDbService repository;
        private PredoplSchetModel[] schets;
        private Predicate<object> filter;

        public PredoplSchetsDlgViewModel(IDbService _rep)
        {
            repository = _rep;

            LoadData();
            
            AddSchetCommand = new DelegateCommand(ExecuteAdd, CanAdd);
            DeleteSchetCommand = new DelegateCommand(ExecuteDelete, CanDelete);
            SaveChangesCommand = new DelegateCommand(ExecuteSaveChanges, CanSaveChanges);
            CloseCommand = new DelegateCommand(DoClose);
        }

        private ObservableCollection<PredoplSchetViewModel> predoplSchets;
        public ObservableCollection<PredoplSchetViewModel> PredoplSchets 
        {
            get { return predoplSchets; }
            set { SetAndNotifyProperty("PredoplSchets", ref predoplSchets, value); } 
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
                        filter = o => (o as PredoplSchetViewModel).Poup == selectedPoup.Kod;
                    else
                        filter = null;
                    //if (predoplSchets != null && selectedPoup != null && selIndex >= 0 && predoplSchets[selIndex].Poup != selectedPoup.Kod)
                    //    SelIndex = -1;
                    DoApplyFilter();
                }
            }
        }

        private void DoApplyFilter()
        {
            var view = CollectionViewSource.GetDefaultView(PredoplSchets);
            view.Filter = filter;
            view.Refresh();
            //if (view.CurrentItem != null)
            //    view.MoveCurrentToFirst();
        }


        private int selIndex;
        public int SelIndex
        {
            get { return selIndex; }
            set { SetAndNotifyProperty(()=>SelIndex, ref selIndex, value); }
        }

        private void LoadData()
        {
            poups = repository.Poups.Values.Where(p => p.IsActive).ToArray();
            vals = repository.GetValutes();
            banks = Enumerable.Repeat(new BankInfo { Id = 0, BankName = "Любой" }, 1).Union(repository.GetBankGroups()).ToArray();
            GetRecTypes();

            LoadSchets();
        }

        private void GetRecTypes()
        {
            rtypes = repository.GetBuhSchetRecTypes();//new Dictionary<byte, string>() {{0,""}, {1, "Возмещаемые"}, {2, "Плательщик из ТС"}, {2, "Плательщик не из ТС"} };
        }

        private void LoadSchets()
        {
            schets = repository.GetPredoplSchets();

            if (schets != null)
            {
                if (predoplSchets != null)
                {
                    predoplSchets.Clear();
                    Array.ForEach(schets, s => predoplSchets.Add(new PredoplSchetViewModel(repository, s)));
                }
                else
                    PredoplSchets = new ObservableCollection<PredoplSchetViewModel>(schets.Select(s => new PredoplSchetViewModel(repository, s)));                
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

        public Valuta[] Vals
        {
            get { return vals; }
        }

        public BankInfo[] Banks
        {
            get { return banks; }
        }

        public Dictionary<byte, string> RecTypes
        {
            get { return rtypes; }
        }

        public DelegateCommand AddSchetCommand { get; set; }

        private void ExecuteAdd()
        {
            var newschet = new PredoplSchetViewModel(repository, new PredoplSchetModel() { TrackingState = TrackingInfo.Created });
            if (selectedPoup != null)
                newschet.Poup = selectedPoup.Kod;
            predoplSchets.Add(newschet);
            var view = CollectionViewSource.GetDefaultView(PredoplSchets);
            view.MoveCurrentTo(newschet);
        }

        private bool CanAdd()
        {
            return true;
        }

        public DelegateCommand DeleteSchetCommand { get; set; }

        private void ExecuteDelete()
        {
            var view = CollectionViewSource.GetDefaultView(PredoplSchets);
            var selschet = view.CurrentItem as PredoplSchetViewModel;
            if (selschet != null)
            {
                if (selschet.TrackingState == TrackingInfo.Created)
                    PredoplSchets.Remove(selschet);
                else
                    selschet.TrackingState = TrackingInfo.Deleted;
            }
        }

        private bool CanDelete()
        {
            return true;
            //var view = CollectionViewSource.GetDefaultView(PredoplSchets) as ListCollectionView;
            //return selIndex != view.Count - 1;
        }

        public DelegateCommand SaveChangesCommand { get; set; }

        private void ExecuteSaveChanges()
        {
            var chmodels = PredoplSchets.Where(s => s.TrackingState != TrackingInfo.Unchanged).Select(vm => vm.SchetModel);
            repository.SavePredoplSchets(chmodels);
            RefreshData();
        }

        private bool CanSaveChanges()
        {
            var ch = PredoplSchets.Where(s => s.TrackingState != TrackingInfo.Unchanged);
            return ch.Any() && ch.All(s => s.IsValid);
        }

        private void DoClose()
        {
            if (CanSaveChanges())
            {
                Parent.OpenDialog(new MsgDlgViewModel
                {
                    Title = "Подтверждение",
                    Message = "Имеются несохранённые изменения.\nЗакрытие приведёт к их отмене.\nЗакрыть?",
                    IsCancelable = true,
                    OnSubmit = d => 
                    {
                        Parent.CloseDialog(d);
                        Parent.CloseDialog(this);
                    }
                });
            }
            else
                Parent.CloseDialog(this);               
        }
    }   
}
