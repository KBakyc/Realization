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

namespace InfoModule.ViewModels
{
    public class SalesJournalTypeDlgViewModel : BaseDlgViewModel
    {
        private PoupModel[] poups;
        private Valuta[] vals;
        private Country[] countries;
        private Dictionary<byte, string> rUnionTypes;
        private List<KeyValueObj<decimal?, string>> ndsTypes;
        private IDbService repository;
        private JournalTypeModel[] jrns;
        private Predicate<object> filter;

        public SalesJournalTypeDlgViewModel(IDbService _rep)
        {
            repository = _rep;
            LoadData();
            AddJrnCommand = new DelegateCommand(ExecuteAdd, CanAdd);
            DeleteJrnCommand = new DelegateCommand(ExecuteDelete, CanDelete);
            SaveChangesCommand = new DelegateCommand(ExecuteSaveChanges, CanSaveChanges);
            CloseCommand = new DelegateCommand(DoClose);
        }

        private ObservableCollection<SalesJournalTypeViewModel> saleJrns;
        public ObservableCollection<SalesJournalTypeViewModel> SaleJrns
        {
            get { return saleJrns; }
            set { SetAndNotifyProperty("SaleJrns", ref saleJrns, value); }
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
                        filter = o => (o as SalesJournalTypeViewModel).Poup == selectedPoup.Kod;
                    else
                        filter = null;
                    DoApplyFilter();
                }
            }
        }

        private void DoApplyFilter()
        {
            var view = CollectionViewSource.GetDefaultView(SaleJrns);
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
            vals = Enumerable.Repeat(new Valuta("", "Любая", ""), 1).Union(repository.GetValutes()).ToArray();
            ndsTypes = new List<KeyValueObj<decimal?, string>>() 
            { 
                new KeyValueObj<decimal?, string>(null, "Любой"), 
                //new KeyValueObj<decimal?, string>(0, "Ставка 0%"),
                //new KeyValueObj<decimal?, string>(10, "Ставка 10%"),
                //new KeyValueObj<decimal?, string>(20, "Ставка 20%"),
                //new KeyValueObj<decimal?, string>(25, "Ставка 25%"),
                new KeyValueObj<decimal?, string>(-1, "Без НДС")
            };
            ndsTypes.AddRange(repository.GetNdsRatesOnDate(DateTime.Today).Values.Distinct().Select(v => new KeyValueObj<decimal?, string>(v, String.Format("Ставка {0}%", v))));

            countries = Enumerable.Repeat(new Country { Kstr = 0, Name = "Любая" }, 1).Union(repository.GetCountries(0)).ToArray();
            GetUnionsRecTypes();           
            LoadJrns();
        }

        private void GetUnionsRecTypes()
        {
            rUnionTypes = repository.GetJournalUnionRecTypes();
        }

        private void LoadJrns()
        {
            jrns = repository.GetJournalTypes(JournalKind.Sell);            
            if (jrns != null)
            {
                if (saleJrns != null)
                {
                    saleJrns.Clear();
                    Array.ForEach(jrns, s => saleJrns.Add(new SalesJournalTypeViewModel(repository, s)));
                }
                else
                    SaleJrns = new ObservableCollection<SalesJournalTypeViewModel>(jrns.Select(s => new SalesJournalTypeViewModel(repository, s)));
            }
        }

        private void RefreshData()
        {
            LoadJrns();
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

        public List<KeyValueObj<decimal?, string>> NdsTypes
        {
            get { return ndsTypes; }
        }

        public Country[] Countries
        {
            get { return countries; }
        }

        public Dictionary<byte, string> UnionRecTypes
        {
            get { return rUnionTypes; }
        }

        public DelegateCommand AddJrnCommand { get; set; }

        private void ExecuteAdd()
        {
            var newjrn = new SalesJournalTypeViewModel(repository, new JournalTypeModel() { TrackingState = TrackingInfo.Created });
            if (selectedPoup != null)
                newjrn.Poup = selectedPoup.Kod;
            saleJrns.Add(newjrn);
            var view = CollectionViewSource.GetDefaultView(SaleJrns);
            view.MoveCurrentTo(newjrn);
        }

        private bool CanAdd()
        {
            return true;
        }

        public DelegateCommand DeleteJrnCommand { get; set; }

        private void ExecuteDelete()
        {
            var view = CollectionViewSource.GetDefaultView(SaleJrns);
            var seljrn = view.CurrentItem as SalesJournalTypeViewModel;
            if (seljrn != null)
            {
                if (seljrn.TrackingState == TrackingInfo.Created)
                    SaleJrns.Remove(seljrn);
                else
                    seljrn.TrackingState = TrackingInfo.Deleted;
            }
        }

        private bool CanDelete()
        {
            return true;
        }

        public DelegateCommand SaveChangesCommand { get; set; }

        private void ExecuteSaveChanges()
        {
            var chmodels = SaleJrns.Where(s => s.TrackingState != TrackingInfo.Unchanged).Select(vm => vm.JrnModel);
            repository.SaveSaleJournalTypes(chmodels);
            RefreshData();
        }

        private bool CanSaveChanges()
        {
            var ch = SaleJrns.Where(s => s.TrackingState != TrackingInfo.Unchanged);
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
