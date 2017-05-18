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
    /// Модель диалога настроек видов реализации пользователя.
    /// </summary>
    public class PoupSettingsViewModel : BaseDlgViewModel
    {
        private IDbService repository;

        private Dictionary<int, int[]> myPoupsWithKodfs;

        public PoupSettingsViewModel(IDbService _rep)
        {
            repository = _rep;
            LoadData();            
        }
        /// <summary>
        /// Загрузка данных для редактирования
        /// </summary>
        private void LoadData()
        {
            Kodfs = repository.GetKodfs().Select(k => new Selectable<KodfModel>(k)).ToArray();
            Poups = repository.Poups.Values.Where(p => p.IsActive).Select(p => new Selectable<PoupModel>(p)).ToArray();
            SubscribeToPoupsChanges();
            GetMyPoupsWithKodfs();
            CheckAndNotifyKodfs();
            CheckMyPoups();
        }

        protected override void ExecuteSubmit()
        {
            SaveData();
            base.ExecuteSubmit();
        }

        private void SubscribeToPoupsChanges()
        {
            foreach(var p in Poups)
                p.PropertyChanged += PoupSelectionChanged;
        }

        void PoupSelectionChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var obj = sender as Selectable<PoupModel>;
            if (obj != null && e.PropertyName == "IsSelected" && obj.IsSelected && !myPoupsWithKodfs.ContainsKey(obj.Value.Kod))
                myPoupsWithKodfs[obj.Value.Kod] = new int[] { 0 };
            NotifyPropertyChanged("MySelectedPoupsString");
        }

        private void CheckMyPoups()
        {
            IEnumerable<Selectable<PoupModel>> poupsToCheck = Poups;
            if (!IsAllPoups)
            {
                poupsToCheck = myPoupsWithKodfs.Select(kv => Poups.Single(p => p.Value.Kod == kv.Key));
                CheckThisPoups(poupsToCheck);
            }
        }

        private void CheckThisPoups(IEnumerable<Selectable<PoupModel>> _poupsToCheck)
        {
            foreach (var pm in _poupsToCheck)
                pm.IsSelected = true;
            NotifyPropertyChanged("Poups");
        }

        private bool isAllPoups;
        public bool IsAllPoups 
        {
            get { return isAllPoups; }
            set 
            {
                SetAndNotifyProperty("IsAllPoups", ref isAllPoups, value);
                CheckAllPoups(value);
                NotifyPropertyChanged("IsCanSelectPoups");
                NotifyPropertyChanged("MySelectedPoupsString");
            }
        }

        private void CheckAllPoups(bool _val)
        {
            if (_val)
            {
                int[] allkf = null;
                if (!myPoupsWithKodfs.ContainsKey(0))
                {
                    allkf = new int[] { 0 };
                    myPoupsWithKodfs.Add(0, allkf);
                    IsAllKodfs = true;
                }
            }
            else
                myPoupsWithKodfs.Remove(0);
        }

        public bool IsCanSelectKodfs 
        {
            get
            {
                return !IsAllKodfs && IsValidPoupSelection;
            }
        }

        public bool IsCanSelectPoups 
        {
            get
            {
                return !IsAllPoups;
            }
        }


        public bool IsValidPoupSelection
        {
            get
            {
                return (SelectedPoup != null && SelectedPoup.IsSelected) || IsAllPoups;
            }
        }

        private bool isAllKodfs;
        public bool IsAllKodfs
        {
            get { return isAllKodfs; }
            set { SetAndNotifyProperty("IsCanSelectKodfs", ref isAllKodfs, value); }
        }

        private void GetMyPoupsWithKodfs()
        {
            myPoupsWithKodfs = CommonModule.CommonSettings.MyPoupsWithKodfs.ToDictionary(kv => kv.Key, kv => kv.Value);
            IsAllPoups = myPoupsWithKodfs.Count == 0 || myPoupsWithKodfs.ContainsKey(0);
        }

        /// <summary>
        /// Сохранение изменённых данных
        /// </summary>
        private void SaveData()
        {
            SavePoupKodfs();
            CommonModule.CommonSettings.MyPoupsWithKodfs = myPoupsWithKodfs.Where(kv => IsPoupChecked(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        private bool IsPoupChecked(int _poup)
        {
            bool res = false;
            if (_poup == 0)
                res = IsAllPoups;
            else
                if (!IsAllPoups)
                {
                    Selectable<PoupModel> spm = Poups.SingleOrDefault(sp => sp.Value.Kod == _poup);
                    res = spm != null && spm.IsSelected;
                }
            return res;
        }

        /// <summary>
        /// Все коды форм
        /// </summary>
        public Selectable<KodfModel>[] Kodfs { get; set; }

        /// <summary>
        /// Все виды реализации
        /// </summary>
        public Selectable<PoupModel>[] Poups { get; set; }

        private Selectable<PoupModel> selectedPoup;
        public Selectable<PoupModel> SelectedPoup
        {
            get { return selectedPoup; }
            set 
            {
                SavePoupKodfs();
                SetAndNotifyProperty("SelectedPoup", ref selectedPoup, value);
                CheckAndNotifyKodfs();
            }
        }

        private void CheckAndNotifyKodfs()
        {
            NotifyPropertyChanged("IsValidPoupSelection");
            if (IsValidPoupSelection)
                {
                    CheckMyKodfs();
                    NotifyPropertyChanged("Kodfs");
                    NotifyPropertyChanged("IsAllKodfs");
                }
            NotifyPropertyChanged("IsCanSelectKodfs");
        }

        private void SavePoupKodfs()
        {
            if (!IsValidPoupSelection) return;

            int key = 0;
            if (!IsAllPoups && SelectedPoup != null)
                key = SelectedPoup.Value.Kod;
            int[] value = new int[]{0};
            if (!IsAllKodfs && Kodfs.Any(k => !k.IsSelected))
                value = Kodfs.Where(k => k.IsSelected).Select(k => k.Value.Kodf).ToArray();
            myPoupsWithKodfs[key] = value;
            //IsAllPoups = false;
        }

        private void CheckMyKodfs()
        {
            var myPoupKodfs = GetMyPoupKodfs();
            if (!IsAllKodfs)
                CheckThisKodfs(myPoupKodfs);
        }

        private void CheckThisKodfs(IEnumerable<Selectable<KodfModel>> _myPoupKodfs)
        {
            foreach(var k in Kodfs)
                k.IsSelected = false;
            
            if (_myPoupKodfs == null) return;
            foreach(var k in _myPoupKodfs)
                k.IsSelected = true;
        }

        private bool IsAnyPoupSelected { get { return Poups.Any(p => p.IsSelected); } }

        private IEnumerable<Selectable<KodfModel>> GetMyPoupKodfs()
        {
            IEnumerable<Selectable<KodfModel>> res = null;
            int poup = 0;
            if (SelectedPoup != null)
                poup = SelectedPoup.Value.Kod;
            int[] mykodfs = null;
            if (myPoupsWithKodfs.ContainsKey(poup))
                mykodfs = myPoupsWithKodfs[poup];
            IsAllKodfs = true;
            if (mykodfs != null && !mykodfs.Contains(0))
            {
                res = mykodfs.Select(k => Kodfs.SingleOrDefault(skf => skf.Value.Kodf == k));
                IsAllKodfs = false;
            }
            else
                res = Kodfs;
            return res;
        }

        public string MySelectedPoupsString
        {
            get
            {
                return GetMySelectedPoupsString();
            }
        }

        private string GetMySelectedPoupsString()
        {
            string res = String.Empty;
            if (!IsAllPoups && IsAnyPoupSelected)
            {
                var pstrings = myPoupsWithKodfs.Keys.Where(k => k != 0).Select(k => k.ToString()).ToArray();
                res = String.Join(", ", pstrings);
            }
            return res;
        }
    }
}
