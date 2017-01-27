using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using CommonModule.Commands;
using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Collections;
using DataObjects.Interfaces;
using CommonModule.DataViewModels;

namespace SfModule.ViewModels
{
    public class SfEditDlgViewModel : BaseDlgViewModel
    {
        private IDbService repository;

        public SfEditDlgViewModel(IDbService _rep, SfViewModel _vm)
        {
            repository = _rep;
            sfVmRef = _vm;
            LoadData();            
        }

        /// <summary>
        /// Ссылка на представление счёта
        /// </summary>
        private SfViewModel sfVmRef;
        public SfViewModel SfVMRef
        {
            get { return sfVmRef; }
        }

        /// <summary>
        /// Загрузка данных для редактирования
        /// </summary>
        private void LoadData()
        {
            if (sfVmRef == null)
                return;

            prodlines = SfVMRef.SfProductLines;
            LoadSfProductPays();

            NumSf = SfVMRef.NumSf;
            DatPltr = SfVMRef.DatePltr;
            PrintableNotes = sfVmRef.PrintableNotes;
            if (SfVMRef.DateBuch.HasValue)
                DateBuch = SfVMRef.DateBuch.Value;
            var per = SfVMRef.SfPeriod;
            if (per != null)
                SfPeriodVm = new SfPeriodViewModel(per.Clone() as SfPayPeriodModel);
            kroDate = repository.GetSfKroInfo(SfVMRef.SfRef.IdSf);

            vzamenSfsList = repository.GetOldSfs(SfVMRef.SfRef.IdSf).ToList();
            if (vzamenSfsList.Count > 0)
                vzamenSfsList.Insert(0, new SfModel(0, null));
            if (SfVMRef.VzamenSf.HasValue)
            {
                var onum = SfVMRef.VzamenSf.Value.Key;
                var odat = SfVMRef.VzamenSf.Value.Value;
                var vzsfinlist = vzamenSfsList.Find(s => s.NumSf == onum && s.DatPltr == odat);
                if (vzsfinlist == null)
                {
                    var newVS = new SfModel(0, null) { NumSf = onum, DatPltr = odat };
                    vzamenSfsList.Add(newVS);
                    vzamenSf = newVS;
                }
                else
                    vzamenSf = vzsfinlist;
            }
            else
                vzamenSf = (vzamenSfsList.Count > 0) ? vzamenSfsList[0] : null;

        }

        public bool IsTotallyPayed 
        {
            get { return SfVMRef != null && SfVMRef.SfRef.PayStatus == 2; }
        }

        /// <summary>
        /// Сохранение изменённых данных
        /// </summary>
        private void SaveData()
        {
            if (sfVmRef == null)
                return;
            SfVMRef.NumSf = NumSf;
            SfVMRef.DatePltr = DatPltr;
            SfVMRef.DateBuch = DateBuch;
            SfVMRef.PrintableNotes = PrintableNotes;
            if (vzamenSfUpdated)
                SfVMRef.VzamenSf = vzamenSf.NumSf != 0 ? new KeyValuePair<int, DateTime>? (new KeyValuePair<int, DateTime>(vzamenSf.NumSf, vzamenSf.DatPltr)) 
                                                       : null;

            if (SfPeriodVm != null)
            {
                if (SfPeriodVm.TrackingState == TrackingInfo.Created
                    && (SfPeriodVm.DatStart.HasValue || SfPeriodVm.LastDatOpl.HasValue)
                    || SfPeriodVm.TrackingState == TrackingInfo.Updated)
                    
                    SfVMRef.SfPeriod = SfPeriodVm.SfPeriodModel;
            }
            SfProductPays.Tracking = false;
        }

        public int NumSf { get; set; }
        public DateTime DatPltr { get; set; }
        public DateTime? DateBuch { get; set; }

        public string PrintableNotes { get; set; }

        private SfLineViewModel[] prodlines;
        public SfLineViewModel[] Prodlines 
        {
            get
            {
                return prodlines;
            }
        }

        private SfLineViewModel selectedPril;
        /// <summary>
        /// Выбранное приложение
        /// </summary>
        public SfLineViewModel SelectedPril 
        {
            get { return selectedPril; }
            set
            {
                if (value != selectedPril)
                {
                    selectedPril = value;
                    var paysView = System.Windows.Data.CollectionViewSource.GetDefaultView(SfProductPays);
                    if (selectedPril != null)
                        paysView.Filter = p => ((SfProductPayViewModel)p).ModelRef.Idprilsf == SelectedPril.IdprilSf;
                    else
                        paysView.Filter = null;
                    paysView.Refresh();                    
                }
            }
        }      

        /// <summary>
        /// Составляющие суммы по счёту
        /// </summary>
        private ChangeTrackingCollection<SfProductPayViewModel> sfProductPays;
        public ChangeTrackingCollection<SfProductPayViewModel> SfProductPays
        {
            get
            {
                return sfProductPays;
            }
        }

        private void LoadSfProductPays()
        {
            var allpays = repository.GetSfPays(SfVMRef.SfRef.IdSf).Select(p => new SfProductPayViewModel(repository, p));
            sfProductPays = new ChangeTrackingCollection<SfProductPayViewModel>(allpays, true);
        }

        private SfPeriodViewModel sfPeriodVm;
        public SfPeriodViewModel SfPeriodVm
        {
            get
            {
                return sfPeriodVm;
            }
            set
            {
                if (value != sfPeriodVm)
                {
                    sfPeriodVm = value;
                    NotifyPropertyChanged("SfPeriodVm");
                    NotifyPropertyChanged("IsAddPeriodEnabled");
                }
            }
        }

        public bool IsKroInfoUpdated;
        private DateTime? kroDate;
        public DateTime? KroDate
        {
            get { return kroDate; }
            set
            {
                if (value != kroDate)
                {
                    kroDate = value;
                    IsKroInfoUpdated = true;
                    NotifyPropertyChanged("KroDate");
                }
            }
        }

        private List<SfModel> vzamenSfsList;
        public List<SfModel> VzamenSfsList
        {
            get { return vzamenSfsList; }
        }

        private bool vzamenSfUpdated;

        private SfModel vzamenSf;
        public SfModel VzamenSf
        {
            get { return vzamenSf; }
            set
            {
                if (vzamenSf != value)
                {
                    vzamenSf = value;
                    vzamenSfUpdated = true;
                    NotifyPropertyChanged("VzamenSf");
                }
            }
        }

        /// <summary>
        /// Комманда добавления данных о сроках оплаты
        /// </summary>
        private ICommand addPeriodCommand;
        public ICommand AddPeriodCommand
        {
            get
            {
                if (addPeriodCommand == null)
                    addPeriodCommand = new DelegateCommand(ExecAddPeriodCommand, () => SfPeriodVm == null);

                return addPeriodCommand;
            }
        }
        private void ExecAddPeriodCommand()
        {
            SfPeriodVm = new SfPeriodViewModel( 
                                        new SfPayPeriodModel()
                                                    {
                                                        IdSf = SfVMRef.SfRef.IdSf,
                                                        TrackingState = TrackingInfo.Created
                                                    }
                                              );
        }

        protected override void ExecuteSubmit()
        {
            SaveData();
            base.ExecuteSubmit();
        }
    }
}