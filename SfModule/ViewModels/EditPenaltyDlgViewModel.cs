using System;
using System.Linq;
using CommonModule.Commands;
using CommonModule.Helpers;
using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Helpers;
using DataObjects.Interfaces;
using System.Windows.Input;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using CommonModule.Interfaces;


namespace SfModule.ViewModels
{
    public class EditPenaltyDlgViewModel : BaseDlgViewModel
    {
        private PenaltyModel oldModel;
        private PenaltyModel newModel;
        private ValSelectionViewModel valDlg;
        private KaSelectionViewModel platDlg;
        private PDogListViewModel dogDlg;

        private IDbService repository;
        private IPersister persister;

        private bool isNew;

        public EditPenaltyDlgViewModel(IDbService _rep, PenaltyModel _pm)
        {
            repository = _rep;
            persister = CommonModule.CommonSettings.Persister;
            oldModel = _pm;
            
            if (_pm != null)
            {
                newModel = DeepCopy.Make(_pm); //.DataClone();
                Title = "Изменение штрафной санкции";
            }
            else
            {
                var now = DateTime.Now;
                newModel = new PenaltyModel()
                {
                    TrackingState = TrackingInfo.Created
                };
                Title = "Добавление штрафной санкции";
                isNew = true;
            }
            
            LoadData();            
        }

        private void LoadData()
        {
            if (dogDlg != null)
                dogDlg.PropertyChanged -= dogDlg_PropertyChanged;
            dogDlg = new PDogListViewModel(repository);
            valDlg = new ValSelectionViewModel(repository);
            if (platDlg != null)
                platDlg.PropertyChanged -= platDlg_PropertyChanged;
            platDlg = new KaSelectionViewModel(repository);

            if (!isNew)
                RefreshVMs(oldModel);
            else
                InitNew();                            

            //agreeDlg = GetNewAgreeSelectionViewModel();

            platDlg.PropertyChanged += platDlg_PropertyChanged;
            dogDlg.PropertyChanged += dogDlg_PropertyChanged;
            //prodDlg.PropertyChanged += prodDlg_PropertyChanged;
        }

        private void InitNew()
        {
            newModel.Poup = repository.Poups.Where(p => p.Value.PayDoc == PayDocTypes.Penalty && p.Value.IsActive).Select(p => p.Value.Kod).FirstOrDefault();
            DateTime now = DateTime.Now;
            newModel.Datgr = now;
            newModel.Datkro = now;
            newModel.DateAdd = now;
            newModel.UserAdd = repository.UserToken;
            newModel.Nomish = GetNomishPrefix(persister.GetValue<String>("LastPenaltyNomish"));
        }

        private string GetNomishPrefix(string _nom)
        {
            if (String.IsNullOrEmpty(_nom)) return String.Empty;
            int lastSlashInd = _nom.LastIndexOfAny(@"\/".ToCharArray());
            return lastSlashInd == -1 ? String.Empty : _nom.Substring(0, lastSlashInd + 1);
        }


        void dogDlg_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelPDogInfo")
            {
                var newd = dogDlg.SelPDogInfo;
                if (newd != null)
                    SelectVal(newd.ValutaOpl.Kodval);
            }
        }

        private void SelectVal(string _kv)
        {
            valDlg.SelVal = valDlg.ValList.FirstOrDefault(v => v.Kodval == _kv);
        }

        void platDlg_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedKA")
            {
                var newpok = platDlg.SelectedKA;
                if (newpok != null)
                    CollectDogs();
            }
        }
        
        private void CollectDogs()
        {
            if (platDlg != null && platDlg.SelectedKA != null)
            {
                var poup = newModel.Poup;

                var kpok = platDlg.SelectedKA.Kgr;
                var pdogs = repository.GetPDogInfosByKaPoup(kpok, poup, (short)Datgr.Year).OrderBy(d => d.Osn);
                dogDlg.LoadData(pdogs);
                if (dogDlg.PDogInfos.Length == 1)
                    dogDlg.SelPDogInfo = dogDlg.PDogInfos[0];
            }
        }

        private void RefreshVMs(PenaltyModel _pm)
        {
            valDlg.SelVal = valDlg.ValList.FirstOrDefault(v => v.Kodval == _pm.Kodval);
            var pok = repository.GetKontrAgent(_pm.Kpok);
            platDlg.PopulateKaList(new KontrAgent[] { pok });
            platDlg.SelectedKA = pok;
            CollectDogs();
            dogDlg.SelPDogInfo = dogDlg.PDogInfos.SingleOrDefault(d => d.ModelRef.Kdog == _pm.Kdog);
        }

        private void SaveData()
        {
            if (newModel == null) return;

            newModel.Kpok = platDlg.SelectedKA.Kgr;
            newModel.Kdog = dogDlg.SelPDogInfo.ModelRef.Kdog;
            newModel.Kodval = valDlg.SelVal.Kodval;
            newModel.Rnpl = MakeRnpl();
            newModel.Kursval = GetNewKurs();

            if (!isNew && !oldModel.DataEqualsTo(newModel))
            {
                newModel.TrackingState = TrackingInfo.Updated;
                newModel.DateKor = DateTime.Now;
                newModel.UserKor = repository.UserToken;
            }

            if (isNew)
                persister.SetValue("LastPenaltyNomish", newModel.Nomish);
        }

        private decimal GetNewKurs()
        {
            decimal res = 1;
            return res;
        }

        private int MakeRnpl()
        {
            if (newModel == null) return 0;
            
            string ish = newModel.Nomish.Trim();
            if (String.IsNullOrEmpty(ish)) return 0;

            Regex rx = new Regex(@"\d{1,5}$");
            string lastnumstr = rx.Match(ish).Value;
            int res = 0;
            int.TryParse(lastnumstr, out res);

            return res;
        }

        public ValSelectionViewModel ValVM
        {
            get { return valDlg; }
        }

        public KaSelectionViewModel PlatVM
        {
            get { return platDlg; }
        }

        public PDogListViewModel DogVM
        {
            get { return dogDlg; }
        }


        protected override void ExecuteSubmit()
        {
            SaveData();
            base.ExecuteSubmit();
        }

        public DateTime Datgr
        {
            get
            {
                return newModel.Datgr;
            }
            set
            {
                if (value != newModel.Datgr)
                {
                    newModel.Datgr = value;
                    NotifyPropertyChanged("Datgr");
                }
            }
        }

        public int Nomkro 
        {
            get { return newModel.Nomkro; }
            set
            {
                if (value != newModel.Nomkro)
                {
                    newModel.Nomkro = value;
                    NotifyPropertyChanged("Nomkro");
                }
            }
        }

        public DateTime Datkro 
        {
            get { return newModel.Datkro; }
            set
            {
                if (value != newModel.Datkro)
                {
                    newModel.Datkro = value;
                    NotifyPropertyChanged("Datkro");
                }
            }
        }

        public string Nomish
        {
            get { return newModel.Nomish; }
            set
            {
                if (value != newModel.Nomish)
                {
                    newModel.Nomish = value;
                    NotifyPropertyChanged("Nomish");
                }
            }
        }

        public decimal Sumpenalty
        {
            get { return newModel.Sumpenalty; }
            set
            {
                if (value != newModel.Sumpenalty)
                {
                    newModel.Sumpenalty = value;
                    NotifyPropertyChanged("Sumpenalty");
                }
            }
        }

        public override bool IsValid()
        {
            return base.IsValid()
                && ValVM.IsValid()
                && PlatVM.IsValid()
                && DogVM.IsValid()
                && Datgr.Year > 2000
                && Datkro.Year > 2000
                && Sumpenalty > 0
                && Datgr <= Datkro
                && Nomkro > 0
                && !String.IsNullOrEmpty(Nomish);
        }

        /// <summary>
        /// Результат
        /// </summary>
        public PenaltyModel NewModel
        {
            get { return newModel; }
        }

        public PenaltyModel OldModel
        {
            get { return oldModel; }
        }

    }
}
