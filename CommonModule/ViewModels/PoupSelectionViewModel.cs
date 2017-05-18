using System.Linq;
using CommonModule.Commands;
using CommonModule.Helpers;
using CommonModule.Interfaces;
using DataObjects;
using DataObjects.Interfaces;


namespace CommonModule.ViewModels
{
    /// <summary>
    /// Модель диалога выбора вида реализации
    /// </summary>
    public class PoupSelectionViewModel : BaseDlgViewModel
    {
        private IDbService repository;
        private bool isSaveSelection = true;

        public PoupSelectionViewModel(IDbService _rep, bool _isMultiPkod, bool _isSaveSelection)
        {
            PoupTitle = "По направлению:";
            PkodsTitle = "Подвид реализации";

            isMultiPkod = _isMultiPkod;
            isSaveSelection = _isSaveSelection;
            repository = _rep;

            LoadPoups();
            LoadPkods();
        }

        public PoupSelectionViewModel(IDbService _rep, bool _isMultiPkod)
            :this(_rep, _isMultiPkod, true)
        {}

        public PoupSelectionViewModel(IDbService _rep)
            :this(_rep, false)
        {}

        public bool IsSaveSelection
        {
            get { return isSaveSelection; }
            set { isSaveSelection = value; }
        }

        public override bool IsValid()
        {
            return base.IsValid() && SelPoup != null 
                && (IsPkodEnabled ? (SelPkods != null) : true );
        }

        //private void ExecuteSubmit()
        //{
        //    if (OnSubmit != null)
        //        OnSubmit(this);
        //}

        private string poupTitle;
        public string PoupTitle 
        { 
            get { return poupTitle; }
            set { poupTitle = value; } 
        } // Надпись рядом с выбором направления

        private string pkodsTitle;
        public string PkodsTitle
        {
            get { return pkodsTitle; }
            set { pkodsTitle = value; }
        } // Надпись рядом с выбором подвида направления

        private PoupModel[] poups;
        public PoupModel[] Poups
        {
            get
            {
                return poups;
            }
        }

        private PoupModel selPoup;
        public PoupModel SelPoup
        {
            get { return selPoup; }
            set
            {
                if (SetAndNotifyProperty("SelPoup", ref selPoup, value))
                {
                    if (value != null)
                    {
                        if (isSaveSelection)
                            Remember.SetValue("SelPoup", selPoup.Kod);
                        LoadPkods();
                    }
                    NotifyPropertyChanged("IsPkodEnabled");
                }
            }
        }

        private void LoadPoups()
        {
            var myPoups = CommonSettings.GetMyPoups();
            poups = repository.Poups.Join(myPoups,kv => kv.Key, m => m, (kv,m) => kv.Value).Where(p => p.IsActive).ToArray();
            if (isSaveSelection)
            {
                int poup = Remember.GetValue<int>("SelPoup");
                selPoup = poups.SingleOrDefault(p => p.Kod == poup);
            }
            //repository.ActivePoups.TryGetValue(poup, out selPoup);
        }

        private Selectable<PkodModel> allPkodsSelection;
        private Selectable<PkodModel> AllPkodsSelection
        {
            get
            {
                if (allPkodsSelection == null)
                {
                    var model = new PkodModel(0) { Name = "Все" };
                    allPkodsSelection = new Selectable<PkodModel>(model);
                }
                return allPkodsSelection;
            }
        }

        public bool IsAllPkods
        {
            get
            {
                return AllPkodsSelection.IsSelected;
            }
            set
            {
                SelectAllPkods(value);
            }
        }

        private void SelectAllPkods(bool _on)
        {
            if (!IsPkodEnabled || Pkods == null || Pkods.Length == 0) return;
            AllPkodsSelection.IsSelected = _on;
            if (IsMultiPkod)
                for (int i = 1; i < Pkods.Length; i++)
                    Pkods[i].IsSelected = _on;
            else
                SingleSelectedPkodItem = AllPkodsSelection;

            NotifyPropertyChanged("IsAllPkods");
        }

        private void LoadPkods()
        {
            if (SelPoup != null && IsCanSelectPkod)
            {
                var poup = SelPoup.Kod;
                var pkWithAll = Enumerable.Repeat(AllPkodsSelection, 1);
                var pkods = repository.GetPkods(poup);
                if (pkods != null)
                {
                    var pkodsm = pkods.Select(p => new Selectable<PkodModel>(p));
                    pkWithAll = pkWithAll.Concat(pkodsm);
                }
                Pkods = pkWithAll.ToArray();

                if (isSaveSelection)
                {
                    short[] savedPkods = GetSavedPkods();                    
                    SelectThisPkods(savedPkods);
                }
                SubscribeToChangeSelection();
            }
            else
                Pkods = null;
        }

        private short[] GetSavedPkods()
        {
            short[] res = null;
            string pkodsString = Remember.GetValue<string>("SelPkods");
            if (!string.IsNullOrEmpty(pkodsString))
                try
                {
                    res = pkodsString.Split(',').Select(s => short.Parse(s)).ToArray();
                }
                catch
                { }
            return res;
        }

        private void SelectThisPkods(params short[] _pkods)
        {
            if (_pkods == null || _pkods.Length == 0) return;
            if (IsMultiPkod)
            {
                SelectMultiplePkods(_pkods);
                NotifyPropertyChanged("SelectedPkodsLabel");
            }
            else
                SelectSinglePkod(_pkods[0]);
        }

        private void SelectMultiplePkods(params short[] _pkods)
        {
            for (int i = 0; i < _pkods.Length; i++)
                SelectSinglePkod(_pkods[i]);
        }

        private void SelectSinglePkod(short _pkod)
        {
            Selectable<PkodModel> selPkod = null;
            if (_pkod == 0)
            {
                SelectAllPkods(true);
                selPkod = AllPkodsSelection;
            }
            else
            {
                selPkod = Pkods.SingleOrDefault(sp => sp.Value.Pkod == _pkod);
                if (selPkod != null)
                    selPkod.IsSelected = true;
            }
            if (!IsMultiPkod)
                SingleSelectedPkodItem = selPkod;
        }

        /// <summary>
        /// Список услуг по процессингу
        /// </summary>
        private Selectable<PkodModel>[] pkods;
        public Selectable<PkodModel>[] Pkods
        {
            get { return pkods; }
            private set { SetAndNotifyProperty("Pkods", ref pkods, value); }
        }

        private bool isMultiPkod;
        public bool IsMultiPkod { get { return isMultiPkod; } }

        private Selectable<PkodModel> singleSelectedPkodItem;
        public Selectable<PkodModel> SingleSelectedPkodItem 
        {
            get { return singleSelectedPkodItem; }
            set { SetAndNotifyProperty("SingleSelectedPkodItem", ref singleSelectedPkodItem, value); }
        }

        //private PkodModel selPkod;
        public PkodModel[] SelPkods
        {
            get 
            {
                return GetSelectedPkods();
            }
        }

        private PkodModel[] GetSelectedPkods()
        {
            if (Pkods == null || !IsPkodEnabled) return null;

            PkodModel[] res = null;
            if (isMultiPkod)
            {
                if (IsAllPkods)
                    res = new PkodModel[] { AllPkodsSelection.Value };
                else
                    res = Pkods.Where(i => i.IsSelected && i != AllPkodsSelection).Select(i => i.Value).ToArray();
            }
            else
                if (singleSelectedPkodItem != null)
                    res = new PkodModel[] { singleSelectedPkodItem.Value };
            return res;
        }

        public string SelectedPkodsLabel
        {
            get
            { 
                return GenerateSelectedPkodsLabel();
            }
        }

        private string GenerateSelectedPkodsLabel()
        {
            string res = "Все";
            if (!IsAllPkods && SelPkods != null && SelPkods.Length > 0)
            {
                var strarr = SelPkods.Select(p => p.Pkod.ToString()).ToArray();
                res = string.Join(", ", strarr);
            }
            return res;
        }

        private void SubscribeToChangeSelection()
        {
            if (!IsPkodEnabled) return;
            foreach (var a in Pkods)
                a.PropertyChanged += SelectedPkodChanged;
        }

        void SelectedPkodChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsSelected")
            {                
                var sel = sender as Selectable<PkodModel>;
                if (sel == AllPkodsSelection)
                    SelectAllPkods(sel.IsSelected);
                else
                    NotifyItemSelectionChanged(sel);
                if (isSaveSelection && sel.IsSelected)
                    SaveSelection();
            }
        }

        private void NotifyItemSelectionChanged(Selectable<PkodModel> _item)
        {
            if (IsMultiPkod)
            {
                if (!_item.IsSelected && IsAllPkods || IsAllPkodsSelected() && !IsAllPkods)
                    ChangeAllPkodsSelection();
                NotifyPropertyChanged("SelectedPkodsLabel");
            }
            else
                if (_item != null && _item.IsSelected)
                {
                    SingleSelectedPkodItem = _item;
                }
        }

        private bool IsAllPkodsSelected()
        {
            return Pkods.Skip(1).All(sp => sp.IsSelected);
        }

        private void ChangeAllPkodsSelection()
        {
            AllPkodsSelection.PropertyChanged -= SelectedPkodChanged;
            AllPkodsSelection.IsSelected = !IsAllPkods;
            AllPkodsSelection.PropertyChanged += SelectedPkodChanged;
        }
        
        private void SaveSelection()
        {
            Remember.SetValue("SelPkods", GenerateStringOfPkods());
        }

        private string GenerateStringOfPkods()
        {
            var arr = SelPkods.Select(pm => pm.Pkod.ToString()).ToArray();
            return string.Join(",", arr);
        }

        private bool isCanSelectPkod = true;
        public bool IsCanSelectPkod
        {
            get { return isCanSelectPkod; }
            set { isCanSelectPkod = value; }
        }

        public bool IsPkodEnabled
        {
            get
            {
                return SelPoup != null && IsCanSelectPkod && SelPoup.IsPkodsEnabled;//.Kod == 33;
            }
        }
    }
}
