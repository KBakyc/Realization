using System;
using System.Linq;
using CommonModule.Commands;
using CommonModule.Helpers;
using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Helpers;
using DataObjects.Interfaces;
using System.Windows.Input;

namespace PredoplModule.ViewModels
{
    public enum PredoplEditActions {Add, Edit, Split, Copy}

    public class EditPredoplDlgViewModel : BaseDlgViewModel
    {
        private PredoplModel oldModel;
        private PredoplModel newModel;
        private PoupSelectionViewModel poupDld;
        private ValSelectionViewModel pValDlg;
        private KaSelectionViewModel platDlg;
        private AgreeSelectionViewModel agreeDlg;
        private IDbService repository;

        public EditPredoplDlgViewModel(IDbService _rep, PredoplModel _pm)
        {
            repository = _rep;
            oldModel = _pm;           

            if (_pm != null)
            {
                newModel = DeepCopy.Make(_pm); //.DataClone();
                Title = "Изменение предоплаты";
                Action = PredoplEditActions.Edit;
            }
            else
            {
                newModel = new PredoplModel(0, new byte[] {0})
                               {
                                   //DatPropl = DateTime.Now,                                   
                                   Direction = 0,
                                   TrackingState = TrackingInfo.Created
                               };
                Title = "Добавление предоплаты";
                Action = PredoplEditActions.Add;                
            }

            LoadData();
            
            LinkToFinanceCommand = new DelegateCommand(ExecuteLink, CanLink);
            ChangeDogKontrAgentCommand = new DelegateCommand(ChangeDogKontrAgent, CanChangeDogKontrAgent);
        }

        private void LoadData()
        {
            poupDld = new PoupSelectionViewModel(repository, false, false);
            if (pValDlg != null)
                pValDlg.PropertyChanged -= pValDlg_PropertyChanged;
            pValDlg = new ValSelectionViewModel(repository);
            if (platDlg != null)
                platDlg.PropertyChanged -= platDlg_PropertyChanged;
            platDlg = new KaSelectionViewModel(repository);

            TypeDocs = repository.GetTypePlatDocs();

            if (oldModel != null)
                RefreshVMs(oldModel);            

            agreeDlg = GetNewAgreeSelectionViewModel();
            //CheckKursValIfChanged();
            if (Action == PredoplEditActions.Add) DatVvod = DateTime.Now;

            pValDlg.PropertyChanged += pValDlg_PropertyChanged;
            platDlg.PropertyChanged += platDlg_PropertyChanged;
        }

        public TypePlatDoc[] TypeDocs { get; set; }
        public byte IdTypeDoc
        {
            get { return newModel.IdTypeDoc; }
            set
            {
                if (newModel.IdTypeDoc != value)
                    newModel.IdTypeDoc = value;
            }
        }

        void pValDlg_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelVal" && DatVvod.HasValue)
            {
                string oldkodval = NewModel.KodVal;
                string newkodval = pValDlg.SelVal.Kodval;                
                if (oldkodval != newkodval)
                    ChangeKursAndSum(newkodval, DatVvod.Value);
                
                NotifyPropertyChanged("IsShowKurs");
            }
        }

        private void ChangeKurs(string _kodval, DateTime _date)
        {
            Action work = () => { KursVal = _kodval == "RB" ? 1 : repository.GetKursVal(_date, _kodval); };
            if (Parent == null)
                work();
            else
                Parent.Services.DoWaitAction(work, "Подождите", "Обновление курса валюты");
        }

        private void ChangeKursAndSum(string _kodval, DateTime _date)
        {
            decimal oldKurs = KursVal;
            Action work = () => ChangeKurs(_kodval, _date);
            Action after = () =>
            {
                SumPropl = SumPropl == 0 ? 0 
                                         : (KursVal == 1 && (oldKurs == 1 || oldKurs == 0) ? SumPropl 
                                                                                           : repository.ConvertSumToVal(SumPropl, null, _kodval, null, oldKurs, KursVal));
            };
            if (Parent == null)
            {
                work();
                after();
            }
            else
                Parent.Services.DoWaitAction(work, "Подождите", "Обновление курса валюты", after);
        }

        void platDlg_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedKA")
                DogKontrAgent = platDlg.SelectedKA;
        }

        private void RefreshVMs(PredoplModel _pm)
        {
            poupDld.SelPoup = poupDld.Poups.FirstOrDefault(p => p.Kod == _pm.Poup);
            if (poupDld.IsPkodEnabled)
            {
                var selpkod = poupDld.Pkods.FirstOrDefault(p => p.Value.Pkod == _pm.Pkod);
                if (selpkod != null)
                {
                    selpkod.IsSelected = true;
                }
            }
            pValDlg.SelVal = pValDlg.ValList.FirstOrDefault(v => v.Kodval == _pm.KodVal);
            var ka = repository.GetKontrAgent(_pm.Kgr);
            platDlg.PopulateKaList(new KontrAgent[] { ka });
            platDlg.SelectedKA = ka;
            
            dogKontrAgent = ka;
            if (_pm.IdAgree != 0)
            {
                var selAgreement = repository.GetAgreementById(_pm.IdAgree);
                if (selAgreement != null && selAgreement.IdCounteragent != ka.Kgr)
                {
                    var newdogka = repository.GetKontrAgent(selAgreement.IdCounteragent);
                    if (newdogka != null)
                        dogKontrAgent = newdogka;
                }
            }
            
        }

        private void SaveData()
        {
            if (newModel == null) return;
            newModel.Poup = PoupVM.SelPoup.Kod;
            newModel.Pkod = PoupVM.IsPkodEnabled ? PoupVM.SelPkods[0].Pkod : default(short);
            //newModel.Ndok = Ndok;
            newModel.KodVal = PredoplValVM.SelVal.Kodval;
            newModel.Kgr = PlatVM.SelectedKA.Kgr;
            //newModel.DatVvod = DatVvod.Value;
            newModel.IdAgree = AgreeSelection.SelectedAgreeId;
            //newModel.SumPropl = SumPropl;
            //newModel.Whatfor = Whatfor;
            if (oldModel != null && !oldModel.DataEqualsTo(newModel))
                newModel.TrackingState = TrackingInfo.Updated;
        }

        //private void NotifyVMsChanged

        public PoupSelectionViewModel PoupVM
        {
            get { return poupDld; }
        }

        public ValSelectionViewModel PredoplValVM
        {
            get { return pValDlg; }
        }

        public KaSelectionViewModel PlatVM
        {
            get { return platDlg; }
        }

        public AgreeSelectionViewModel AgreeSelection
        {
            get
            {
                if (agreeDlg == null)
                    GetNewAgreeSelectionViewModel();
                return agreeDlg;
            }
        }

        protected override void ExecuteSubmit()
        {
            SaveData();
            base.ExecuteSubmit();
        }

        //private DateTime? datVvod;
        public DateTime? DatVvod
        {
            get
            {
                if (newModel.DatVvod == null)
                    newModel.DatVvod = DateTime.Now;
                return newModel.DatVvod;
            }
            set
            {
                if (value != null && value != newModel.DatVvod)
                {
                    newModel.DatVvod = value.Value;                    
                    Remember.SetValue("SelDate", value.Value);
                    NotifyPropertyChanged("DatVvod");
                    DatKurs = value.Value;
                }
            }
        }

        public bool IsShowKurs
        {
            get { return IsKursValChanged || pValDlg != null && pValDlg.SelVal != null && pValDlg.SelVal.Kodval != "RB"; }
        }

        //private DateTime? datKurs;
        public DateTime? DatKurs
        {
            get
            {
                return newModel.DatKurs;
            }
            set
            {
                if (value != null && value != newModel.DatKurs)
                {
                    newModel.DatKurs = value;
                    if (pValDlg != null && pValDlg.SelVal != null)
                        ChangeKurs(pValDlg.SelVal.Kodval, value.Value);
                    NotifyPropertyChanged("DatKurs");
                    NotifyPropertyChanged("IsKursValChanged");
                }
            }
        }

        public decimal KursVal
        {
            get { return newModel.KursVal; }
            set 
            { 
                newModel.KursVal = value;
                NotifyPropertyChanged("KursVal");
            }
        }


        public bool IsKursValChanged
        {
            get { return DatKurs != null && DatKurs != DatVvod; }
        }

        public int Ndok
        {
            get { return newModel.Ndok; }
            set 
            {
                if (newModel.Ndok != value)
                    newModel.Ndok = value;
            }
        }

        public decimal SumPropl
        {
            get { return newModel.SumPropl; }
            set 
            {
                if (value != newModel.SumPropl)
                {
                    newModel.SumPropl = value;
                    NotifyPropertyChanged("SumPropl");
                }
            }
        }

        public string Whatfor
        {
            get { return newModel.Whatfor; }
            set 
            {
                if (value != newModel.Whatfor)
                {
                    newModel.Whatfor = value;
                    NotifyPropertyChanged("Whatfor");
                }
            }
        }

        public string Prim
        {
            get { return newModel.Prim; }
            set
            {
                if (value != newModel.Prim)
                    newModel.Prim = value;
            }
        }


        public override bool IsValid()
        {
            return base.IsValid()
                //&& NewModel.TrackingState != TrackingInfo.Unchanged
                && DatVvod <= DateTime.Now && DatVvod != null
                && PoupVM.IsValid()
                && PredoplValVM.IsValid()
                && SumPropl != 0
                && Ndok != 0
                && AgreeSelection != null && AgreeSelection.IsValid();
        }

        /// <summary>
        /// Результат
        /// </summary>
        public PredoplModel NewModel
        {
            get { return newModel; }
        }

        public PredoplModel OldModel
        {
            get { return oldModel; }
        }

        /// <summary>
        /// 0 - предоплата, 1 - возврат
        /// </summary>
        public short Direction
        {
            get { return newModel==null ? (short)0 : newModel.Direction; }
            set
            {
                if (newModel != null && value != newModel.Direction)
                    newModel.Direction = value;
            }
        }

        public bool IsPredopl
        {
            get { return Direction == 0; }
            set 
            {
                if (value && Direction != 0)
                    Direction = 0;
            }
        }

        public bool IsVozvrat
        {
            get { return Direction == 1; }
            set
            {
                if (value && Direction != 1)
                    Direction = 1;
            }
        }

        /// <summary>
        /// Команда привязки предоплаты к финансовому поступлению
        /// </summary>
        public ICommand LinkToFinanceCommand { get; set; }

        private void ExecuteLink()
        {
            var poup = PoupVM.SelPoup.Kod;
            var kpok = PlatVM.SelectedKA.Kgr;
            //var kodval = PredoplValVM.SelVal.Kodval;
            var data = repository.GetPredopsFromFinance(poup, kpok, Ndok, DatVvod.Value, "", (byte)Direction);
            if (data.Length > 0)
                SelectFinanceDoc(data);
        }
        private bool CanLink()
        {
            return //(newModel.IdRegDoc == 0 || Action == PredoplEditActions.Split) && 
                DatVvod != null 
                && PoupVM.SelPoup != null
                && PlatVM.SelectedKA != null
                && Ndok != 0
                && PredoplValVM.SelVal != null;
        }

        //BaseDlgViewModel edtdlg;

        private void SelectFinanceDoc(PredoplModel[] _docs)
        {

            var ndlg = new SelectPredoplDlgViewModel(repository, _docs) 
            {
                Title = "Выберите финансовый документ",
                OnSubmit = DoLinkPredopl
            };
            if (Parent != null)
                Parent.OpenDialog(ndlg);
        }

        private void DoLinkPredopl(Object _dlg)
        {
            Parent.CloseDialog(_dlg);
            var dlg = _dlg as SelectPredoplDlgViewModel;
            var predFromFin = dlg.PredoplsList.SelectedPredopl.PredoplRef;
            newModel.IdRegDoc = predFromFin.IdRegDoc;
            newModel.DatPropl = predFromFin.DatPropl;
            if (AgreeSelection.SelectedAgreeId == 0)
            {
                newModel.IdAgree = predFromFin.IdAgree;
                AgreeSelection.SelectedAgreeId = newModel.IdAgree;
            }
            newModel.KodValB = predFromFin.KodValB;
            newModel.Kpokreal = predFromFin.Kpokreal;
            newModel.SumBank = predFromFin.SumBank;
            newModel.Nop = predFromFin.Nop;
            if (SumPropl == 0)
                SumPropl = predFromFin.SumPropl;
            //if (String.IsNullOrEmpty(Whatfor))
            Whatfor = predFromFin.Whatfor;
            if (oldModel != null)
                newModel.TrackingState = TrackingInfo.Updated;
        }

        /// <summary>
        /// Можно ли менять валюту
        /// </summary>
        private bool isCanChangeVal = true;
        public bool IsCanChangeVal
        {
            get { return isCanChangeVal && IsEditable; }
            set { SetAndNotifyProperty("IsCanChangeVal", ref isCanChangeVal, value); } 
        }

        //public bool IsCanSetOff
        //{
        //    get { return isCanChangeVal && IsEditable && pValDlg != null && pValDlg.SelVal != null && pValDlg.SelVal.Kodval != "RB"
        //                 && (Action == PredoplEditActions.Split || Action == PredoplEditActions.Edit); }// && oldModel.IdSourcePO > 0); }
        //}

        /// <summary>
        /// Можно ли менять тип (Предоплата/возврат)
        /// </summary>
        public bool IsCanChangeType
        {
            get { return IsEditable && (oldModel == null || Action == PredoplEditActions.Copy); }
        }

        private KontrAgent dogKontrAgent;
        public KontrAgent DogKontrAgent 
        {
            get { return dogKontrAgent; }
            set 
            {
                SetAndNotifyProperty("DogKontrAgent", ref dogKontrAgent, value); 
                ChangeAgreements();
            }
        }

        private void ChangeAgreements()
        {
            if (dogKontrAgent == null) return;
            Action work = () => { agreeDlg = GetNewAgreeSelectionViewModel(); };
            Action after = () => { if (agreeDlg != null) NotifyPropertyChanged("AgreeSelection"); };

            Parent.Services.DoWaitAction(work, "Подождите", "Обновление информации", after);
        }

        private AgreeSelectionViewModel GetNewAgreeSelectionViewModel()
        {
            AgreeSelectionViewModel res = null;
            if (dogKontrAgent != null)
                if (oldModel == null)
                    res = new AgreeSelectionViewModel(repository, dogKontrAgent.Kgr);
                else
                    res = new AgreeSelectionViewModel(repository, dogKontrAgent.Kgr, oldModel.IdAgree);
            return res;
        }

        public ICommand ChangeDogKontrAgentCommand { get; set; }

        private void ChangeDogKontrAgent()
        {
            var dlg = new KaSelectionViewModel(repository)
            {
                Title = "Выберите контрагента по договору",
                OnSubmit = OnSubmitDogKontrAgent
            };
            Parent.OpenDialog(dlg);
        }
        
        private bool CanChangeDogKontrAgent()
        {
            return true;
        }

        private void OnSubmitDogKontrAgent(Object _dlg)
        {
            var dlg = _dlg as KaSelectionViewModel;
            Parent.CloseDialog(_dlg);
            DogKontrAgent = dlg.SelectedKA;
        }
        

        public PredoplEditActions Action { get; set; }

        public bool IsEditable { get { return oldModel == null || oldModel.DatZakr == null || Action == PredoplEditActions.Copy; } }
    }   
}
