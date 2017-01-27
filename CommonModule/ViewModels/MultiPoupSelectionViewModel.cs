using System.Linq;
using CommonModule.Commands;
using CommonModule.Helpers;
using CommonModule.Interfaces;
using DataObjects;
using DataObjects.Interfaces;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using System.Collections.ObjectModel;


namespace CommonModule.ViewModels
{
    public class MultiPoupSelectionViewModel : BaseDlgViewModel
    {
        private IDbService repository;
        private PoupModel[] allPoups;
        //private PkodModel[] allPkods;
        private Dictionary<Selectable<PoupModel>, Selectable<PkodModel>[]> allPoupData = new Dictionary<Selectable<PoupModel>, Selectable<PkodModel>[]>();

        public MultiPoupSelectionViewModel(IDbService _rep, bool _isMultiPoup, bool _isMultiPkod, bool _isSave)
        {
            PoupTitle = "По направлению:";

            isMultiPoup = _isMultiPoup;
            isMultiPkod = _isMultiPkod;
            isSaveSelection = _isSave;
            repository = _rep;
            LoadPoups();
        }

        public MultiPoupSelectionViewModel(IDbService _rep, bool _isMultiPoup, bool _isMultiPkod)
            : this(_rep, _isMultiPoup, _isMultiPkod, true)
        {

        }

        public MultiPoupSelectionViewModel(IDbService _rep, bool _isMultiPkod)
            : this(_rep, false, _isMultiPkod)
        {
        }

        public MultiPoupSelectionViewModel(IDbService _rep)
            : this(_rep, false)
        { }

        public override bool IsValid()
        {
            return base.IsValid()
                && isAnyPoupSelected
                && (allPoupData.Where(pd => pd.Key.IsSelected && pd.Value != null).All(spd => spd.Value.Any(pk => pk.IsSelected)));
        }

        private string poupTitle;
        public string PoupTitle
        {
            get { return poupTitle; }
            set { poupTitle = value; }
        } // Надпись рядом с выбором направления

        private bool isAnyPoupSelected
        {
            get { return allPoupData.Keys.Any(sp => sp.IsSelected); }
        }

        public PoupModel SelPoup
        {
            get { return CurPoupSelection.Key.Value; }
            set
            {
                DeselectAllPoups();
                if (value != null)
                {
                    int poup = value.Kod;
                    //SetSelectedPoups(poup);
                    CurPoupSelection = allPoupData.SingleOrDefault(kv => kv.Key.Value.Kod == poup);
                    if (isSaveSelection)
                        Remember.SetValue("SelPoup", poup);
                    LoadPkods();
                }
                    
                NotifyPropertyChanged("SelPoup");
                NotifyPropertyChanged("IsPkodEnabled");
            }
        }

        private void DeselectAllPoups()
        {
            foreach (var spm in allPoupData.Keys)
                spm.IsSelected = false;
        }

        private void SetSelectedPoupsWithPkods(Dictionary<short,short[]> _poups)
        {
            if (_poups == null) return;

            foreach (var p in _poups)
            {
                if (p.Key == 0) continue;
                var spforsel = allPoupData.SingleOrDefault(sp => sp.Key.Value.Kod == p.Key);
                if (spforsel.Key != null)
                {
                    spforsel.Key.IsSelected = true;
                    if (p.Value != null && p.Value.Length > 0)
                        SetSelectedPkods(p.Key, p.Value);
                }
            }
        }        

        private void SetSelectedPkods(short _poup, params short[] _pkods)
        {
            if (_poup == 0 || _pkods == null || _pkods.Length == 0)  return;
            var poupDataItem = allPoupData.SingleOrDefault(pd => pd.Key.Value.Kod == _poup);
            if (poupDataItem.Value == null) return;
            IEnumerable<Selectable<PkodModel>> selectedPkodsItems = poupDataItem.Value;
            if (!_pkods.Any(pk => pk == 0))
            {
                selectedPkodsItems = poupDataItem.Value.Join(_pkods, pkdi => pkdi.Value.Pkod, pk => pk, (pkdi, pk) => pkdi);
            }            
            foreach (var spki in selectedPkodsItems)
                spki.IsSelected = true;
            
            // если помечены все пкоды, то помечаем пункт "Все"
            var allpkdSelection = poupDataItem.Value.SingleOrDefault(pkd => pkd.Value.Pkod == 0);
            if (poupDataItem.Value
                            .Where(pkd => pkd != allpkdSelection)
                            .All(pkd => pkd.IsSelected))
                allpkdSelection.IsSelected = true;
            else
                allpkdSelection.IsSelected = false;
        }

        public Dictionary<PoupModel, PkodModel[]> GetSelectedPoupsWithPkodsModels()
        {
            return allPoupData.Where(pd => pd.Key.IsSelected)
                              .ToDictionary(pd => pd.Key.Value,
                                            pd => pd.Value == null ? null
                                                                   : pd.Value.Where(pkd => pkd.IsSelected && pkd.Value.Pkod != 0)
                                                                             .Select(pkd => pkd.Value)
                                                                             .ToArray());
        }

        public Dictionary<int, short[]> GetSelectedPoupsWithPkodsCodes()
        {
            return allPoupData.Where(pd => pd.Key.IsSelected)
                              .ToDictionary(pd => pd.Key.Value.Kod,
                                            pd => pd.Value == null ? null
                                                                   :  pd.Value.Where(pkd => pkd.IsSelected && pkd.Value.Pkod != 0)
                                                                              .Select(pkd => pkd.Value.Pkod)
                                                                              .ToArray());
        }

        private void LoadPoups()
        {
            var myPoups = CommonSettings.GetMyPoups();
            allPoups = repository.Poups.Join(myPoups, kv => kv.Key, m => m, (kv, m) => kv.Value).Where(p => p.IsActive).ToArray();
            //allPkods = repository.GetPkods(0);
            foreach (var pm in allPoups)
            {
                var spm = new Selectable<PoupModel>(pm, false);
                Selectable<PkodModel>[] spkma = null;
                if (IsCanSelectPkod)
                {
                    var poupPkods = repository.GetPkods(pm.Kod);
                    if (poupPkods != null && poupPkods.Length > 0)
                    {
                        var tspkma = poupPkods.Select(pkm => new Selectable<PkodModel>(pkm, false));
                        spkma = Enumerable.Repeat(AllPkodsSelection, 1).Concat(tspkma).ToArray();
                    }
                }
                allPoupData[spm] = spkma;
            }

            if (isSaveSelection)
                LoadSavedData();

            if (allPoupData.Any(pd => pd.Key.IsSelected))
                curPoupSelection = allPoupData.First(pd => pd.Key.IsSelected);

            foreach (var pd in allPoupData)
            {
                SubscribeToPoupsSelection();
            }
        }

        private ObservableCollection<KeyValuePair<Selectable<PoupModel>,Selectable<PkodModel>[]>> selectedData;

        public ObservableCollection<KeyValuePair<Selectable<PoupModel>,Selectable<PkodModel>[]>> SelectedData
        {
            get 
            {
                if (selectedData == null) CollectSelectedData();
                return selectedData;
            }
        }
        
        private void CollectSelectedData()        
        {
            var selected = allPoupData.Where(kv => kv.Key.IsSelected);
            selectedData = new ObservableCollection<KeyValuePair<Selectable<PoupModel>, Selectable<PkodModel>[]>>(selected);
        }

        private void LoadSavedData()
        {
            string poupData = Remember.GetValue<string>("SelPoupData");
            if (!string.IsNullOrEmpty(poupData))
            {
                Dictionary<short, short[]> pda = null;
                try
                {
                    pda = ParseMyPoupData(poupData);
                }
                catch{}
                
                SetSelectedPoupsWithPkods(pda);
            }
        }

        private Dictionary<short, short[]> ParseMyPoupData(string _data)
        {
            Dictionary<short, short[]> res = null;

            string pattern = @"^(\d+:(\d+,?)+;?)+$";
            if (Regex.IsMatch(_data, pattern))
            {
                res = _data.Split(';').Select(s => s.Trim().Split(':'))
                                           .ToDictionary(
                                                sa => short.Parse(sa[0]),
                                                sa => sa[1].Split(',').Select(s => short.Parse(s)).ToArray());
            }

            return res;
        }

        public Dictionary<Selectable<PoupModel>, Selectable<PkodModel>[]> AllPoupData
        {
            get { return allPoupData; }
        }

        private KeyValuePair<Selectable<PoupModel>, Selectable<PkodModel>[]> curPoupSelection;
        public KeyValuePair<Selectable<PoupModel>, Selectable<PkodModel>[]> CurPoupSelection
        {
            get { return curPoupSelection; }
            set { ChangeCurPoupSelection(value); }
        }

        private void ChangeCurPoupSelection(KeyValuePair<Selectable<PoupModel>, Selectable<PkodModel>[]> _value)
        {
            UnsubscribeToPkodsSelection();            
            if (!isMultiPoup)
            {
                DeselectAllPoups();
                if (_value.Key != null)
                    _value.Key.IsSelected = true;
            }
            SetAndNotifyProperty("CurPoupSelection", ref curPoupSelection, _value);
            SubscribeToPkodsSelection();
            NotifyPropertyChanged("IsPkodEnabled");
            if (IsPkodEnabled)
                if (isMultiPkod)
                    NotifyPropertyChanged("SelectedPkodsLabel");
                else
                    SingleSelectedPkodItem = Pkods.FirstOrDefault(spkm => spkm.IsSelected) ;
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
            if (!IsPkodEnabled || Pkods == null) return;
            AllPkodsSelection.IsSelected = _on;
            if (IsMultiPkod)
                foreach(var pkm in Pkods)
                    pkm.IsSelected = _on;
            NotifyPropertyChanged("IsAllPkods");
        }

        private void LoadPkods()
        {
            NotifyPropertyChanged("Pkods");
            //if (SelPoup != null && IsPkodEnabled)
            //{
            //    var poup = SelPoup.Kod;
            //    var pkodsm = allPkods.Where(k => k.Poup == poup).Select(p => new Selectable<PkodModel>(p));
            //    var pkWithAll = Enumerable.Repeat(AllPkodsSelection, 1).Concat(pkodsm);
            //    short[] savedPkods = GetSavedPkods(); ;
            //    Pkods = pkWithAll.ToArray();
            //    SelectThisPkods(savedPkods);
            //    SubscribeToChangeSelection();
            //}
            //else
            //    Pkods = null;
        }

        //private short[] GetSavedPkods()
        //{
        //    short[] res = null;
        //    string pkodsString = Remember.GetValue<string>("SelPkods");
        //    if (!string.IsNullOrEmpty(pkodsString))
        //        try
        //        {
        //            res = pkodsString.Split(',').Select(s => short.Parse(s)).ToArray();
        //        }
        //        catch
        //        { }
        //    return res;
        //}

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
        public IEnumerable<Selectable<PkodModel>> Pkods
        {
            get { return GetCurPoupPkods(); }
            //private set { SetAndNotifyProperty("Pkods", ref pkods, value); }
        }

        private IEnumerable<Selectable<PkodModel>> GetCurPoupPkods()
        {
            IEnumerable<Selectable<PkodModel>> res = null;
            if (curPoupSelection.Value != null && isCanSelectPkod)
            {
                res = curPoupSelection.Value;
            }
            return res;
        }

        private bool isSaveSelection;
        public bool IsSaveSelection { get { return isSaveSelection; } }

        private bool isMultiPoup;
        public bool IsMultiPoup { get { return isMultiPoup; } }

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
            if (Pkods == null) return null;

            PkodModel[] res = null;
            if (IsAllPkods)
                res = new PkodModel[] { AllPkodsSelection.Value };
            else
                res = Pkods.Where(i => i.IsSelected && i != AllPkodsSelection).Select(i => i.Value).ToArray();
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
            if (!IsAllPkods && SelPkods != null)
            {
                var strarr = SelPkods.Select(p => p.Pkod.ToString()).ToArray();
                res = string.Join(", ", strarr);
            }
            return res;
        }

        private void SubscribeToPoupsSelection()
        {
            if (!isMultiPoup) return;
            foreach (var sp in allPoupData.Keys)
                sp.PropertyChanged += SelectedPoupChanged;
        }

        void SelectedPoupChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsSelected")
            {
                if (isSaveSelection)
                    SaveSelection();
                var sel = sender as Selectable<PoupModel>;
                if (sel != null)
                {
                    var selPoupData = allPoupData.FirstOrDefault(kv => kv.Key == sel);
                    if (sel.IsSelected)
                    {
                        if (!SelectedData.Contains(selPoupData))
                        {
                            var minSelPoup = selectedData.Where(d => d.Key.Value.Kod < sel.Value.Kod).OrderByDescending(d => d.Key.Value.Kod).FirstOrDefault();
                            var minSelPoupInd = selectedData.IndexOf(minSelPoup);
                            selectedData.Insert(minSelPoupInd + 1, selPoupData);
                        }
                    }
                    else
                    {
                        if (SelectedData.Contains(selPoupData))
                            selectedData.Remove(selPoupData);
                    }
                    //var selDataItem = SelectedData.
                }
            }
        }

        private void SubscribeToPkodsSelection()
        {
            if (!IsPkodEnabled) return;
            foreach (var a in Pkods)
                a.PropertyChanged += SelectedPkodChanged;
        }

        private void UnsubscribeToPkodsSelection()
        {
            if (!IsPkodEnabled) return;
            foreach (var a in Pkods)
                a.PropertyChanged -= SelectedPkodChanged;
        }


        void SelectedPkodChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsSelected")
            {
                if (isSaveSelection)
                    SaveSelection();
                var sel = sender as Selectable<PkodModel>;
                if (sel == AllPkodsSelection)
                    SelectAllPkods(sel.IsSelected);
                else
                    NotifyItemSelectionChanged(sel);
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
                    SingleSelectedPkodItem = _item;
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

        public void SaveSelection()
        {
            Remember.SetValue("SelPoupData", SelectedPoupDataString);
        }

        public string SelectedPoupDataString
        {
            get { return GenerateStringOfPoupData(); }
        }


        private string GenerateStringOfPoupData()
        {
            var selPoupData = GetSelectedPoupsWithPkodsCodes();
            string res = String.Join(";",
                                selPoupData.Select(pd =>
                                                 String.Join(":",
                                                        new string[] { pd.Key.ToString(), 
                                                                       (pd.Value == null || pd.Value.Length == 0) 
                                                                         ? "0"
                                                                         : String.Join(",", pd.Value.Select(i => i.ToString()).ToArray())
                                                                     }
                                                            )
                                                       ).ToArray()
                                    );
            return res;
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
                return CurPoupSelection.Value != null && IsCanSelectPkod;
            }
        }
    }
}
