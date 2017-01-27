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
using System.Collections.ObjectModel;

namespace OtgrModule.ViewModels
{
    public class ChangeOtgrForReDoneViewModel : BaseDlgViewModel
    {
        IDbService repository;

        public ChangeOtgrForReDoneViewModel(IDbService _repository, IEnumerable<OtgrLine> _otgrData)
        {
            if (_otgrData == null) throw (new ArgumentNullException("_otgrData", "Нет данных для отображения"));

            repository = _repository;
            otgrData = new ObservableCollection<OtgrLineViewModel>(_otgrData.OrderBy(d => d.Datgr).ThenBy(d => d.DocumentNumber).ThenBy(d => d.RwBillNumber)
                                                            .Select(o => new OtgrLineViewModel(repository, o)));

            CheckOtgr();

            SelectDeselectAllCommand = new DelegateCommand(ExecSelectDeselectAll);
            SelectDeselectDocCommand = new DelegateCommand(ExecSelectDeselectDoc, () => selectedOtgr != null && !selectedOtgr.HasErrors);
            SelectDeselectRwCommand = new DelegateCommand(ExecSelectDeselectRw, () => selectedOtgr != null && !selectedOtgr.HasErrors);
            SelectDeselectOtgrCommand = new DelegateCommand(ExecSelectDeselectOtgr);
            SetNewPriceCommand = new DelegateCommand(ExecSetNewPrice, () => otgrData.Any(d => d.IsChecked));
            SplitOtgrCommand = new DelegateCommand(ExecSplitOtgr, CanSplitOtgr);            
        }

        private void CheckOtgr()
        {
            if (otgrData == null) return;
            foreach (var otgr in otgrData.Where(o => o.OtgrAllSfs.Any(s => s.Status != LifetimeStatuses.Deleted)))
            {
                otgr.StatusType = 100;
                otgr.StatusMsgs = new string[] { "Сформирован счёт № " + otgr.OtgrAllSfs.First(s => s.Status != LifetimeStatuses.Deleted).NumSf.ToString() };
            }
        }

        /// <summary>
        /// Коллекция строк принимаемой отгрузки
        /// </summary>
        private ObservableCollection<OtgrLineViewModel> otgrData = new ObservableCollection<OtgrLineViewModel>();
        public ObservableCollection<OtgrLineViewModel> OtgrData
        {
            set
            {
                if (value != otgrData)
                {
                    otgrData = value;
                    NotifyPropertyChanged("OtgrData");
                }
            }
            get
            {
                return otgrData;
            }
        }

        private OtgrLineViewModel selectedOtgr;
        /// <summary>
        /// Выбранная отгрузка
        /// </summary>
        public OtgrLineViewModel SelectedOtgr
        {
            get { return selectedOtgr; }
            set
            {
                if (value != selectedOtgr)
                {
                    selectedOtgr = value;
                    NotifyPropertyChanged("SelectedOtgr");
                }
            }
        }

        //public DelegateCommand SubmitChangesCommand { get; set; }

        public override bool IsValid()
        {
            var vdata = otgrData.Where(o => o.TrackingState != TrackingInfo.Unchanged || o.IsChecked).ToArray();
            return base.IsValid()
                && vdata.Length > 0
                && vdata.All(o => o.IsChecked && o.StatusType == 1);
        }

        protected override void ExecuteSubmit()
        {
            if (!DoFinalCheck())
            {
                //ShowError();
                Parent.Services.ShowMsg("Ошибка", "Операция не может быть завершена.\nПопробуйте повторить все этапы.", true);
                return;
            }
            base.ExecuteSubmit();
        }

        //private void ShowError()
        //{
        //    var dlg = new MsgDlgViewModel 
        //    {
        //        Title = "Ошибка",
        //        Message = "Операция не может быть завершена.\nПопробуйте повторить все этапы."
        //    };
        //    Parent.OpenDialog(dlg);
        //}

        private bool DoFinalCheck()
        {
            var otgr = otgrData.Where(o => o.IsChecked && o.Otgr.Idp623 > 0);
            foreach (var o in otgr)
            {
                var sfs = repository.GetSfsByOtgruz(o.Otgr.Idp623);
                if (sfs != null && sfs.Length > 0 && sfs.Any(s => s.Status != LifetimeStatuses.Deleted))
                    return false;                
            }
            return true;
        }


        private bool isAllSelectMode;
        /// <summary>
        /// Выбраны все
        /// </summary>
        public bool IsAllSelectMode
        {
            get
            {
                return isAllSelectMode;
            }
            set
            {
                SetAndNotifyProperty("IsAllSelectMode", ref isAllSelectMode, value);
                //SelectAllOtgr();
            }
        }

        /// <summary>
        /// Комманда выделения/снятия выделения всех отгрузок
        /// </summary>
        public ICommand SelectDeselectAllCommand { get; set; }
        private void ExecSelectDeselectAll()
        {
            SelectAllOtgr();
            NotifySelectedChanged();
        }

        private void SelectAllOtgr()
        {
            if (otgrData == null) return;

            foreach (var o in otgrData)
                o.IsChecked = o.HasErrors ? false : IsAllSelectMode;
        }
        
        public ICommand SelectDeselectOtgrCommand { get; set; }
        private void ExecSelectDeselectOtgr()
        {
            NotifySelectedChanged();
        }

        public ICommand SelectDeselectRwCommand { get; set; }
        private void ExecSelectDeselectRw()
        {
            if (selectedOtgr == null) return;
            bool tostate = selectedOtgr.IsChecked;

            foreach (var o in otgrData.Where(o => o.DocumentNumber == selectedOtgr.DocumentNumber && o.RwBillNumber == selectedOtgr.RwBillNumber && o.Datgr == selectedOtgr.Datgr))
                o.IsChecked = o.HasErrors ? false : tostate;

            NotifySelectedChanged();
        }

        /// <summary>
        /// Комманда выделения/снятия выделения отгрузок ТН2
        /// </summary>
        public ICommand SelectDeselectDocCommand { get; set; }
        private void ExecSelectDeselectDoc()
        {
            if (SelectedOtgr == null) return;
            bool tostate = selectedOtgr.IsChecked;

            foreach (var o in otgrData.Where(o => o.DocumentNumber == selectedOtgr.DocumentNumber && o.Datgr == selectedOtgr.Datgr))
                o.IsChecked = o.HasErrors ? false : tostate;

            NotifySelectedChanged();
        }

        private bool isShowErrors;
        public bool IsShowErrors
        {
            get { return isShowErrors; }
            set
            {
                if (value != isShowErrors)
                {
                    isShowErrors = value;
                    ChangeFilter();
                }
            }
        }

        public void ChangeFilter()
        {
            var cv = System.Windows.Data.CollectionViewSource.GetDefaultView(otgrData);
            if (!IsShowErrors)
                cv.Filter = r => !((OtgrLineViewModel)r).HasErrors;
            else
                cv.Filter = null;
            cv.Refresh();
        }

        private int CountDocs(int _idInvoiceType, bool? _ischk)
        {
            IEnumerable<OtgrLineViewModel> or = OtgrData;
            if (_ischk.HasValue)
                or = or.Where(o => o.IsChecked == _ischk);
            return or.Where(o => o.DocInvoiceType != null && o.DocInvoiceType.IdInvoiceType == _idInvoiceType).Select(o => o.DocumentNumber).Distinct().Count();
        }

        private void NotifySelectedChanged()
        {
            NotifyPropertyChanged("CheckedRows");
            NotifyPropertyChanged("CheckedTn2");
            NotifyPropertyChanged("CheckedRnn");
            NotifyPropertyChanged("SelectedKolf");
        }
        
        ///// <summary>
        ///// Комманда выполняется при пометке/снятии элемента коллекции
        ///// </summary>
        //public ICommand OnCheckItemChangeCommand { get; set; }
        //private void ExecOnCheckItemChange()
        //{
        //    NotifySelectedChanged();
        //}

        public int TotalRows
        {
            get { return OtgrData.Count; }
        }

        public int CheckedRows
        {
            get { return OtgrData.Where(o => o.IsChecked).Count(); }
        }

        //public int TotalTn2
        //{
        //    get { return CountDocs(DocTypes.Tn2, null); }
        //}

        //public int CheckedTn2
        //{
        //    get { return CountDocs(DocTypes.Tn2, true); }
        //}

        //public int TotalRnn
        //{
        //    get { return CountDocs(DocTypes.Rnn, null); }
        //}

        //public int CheckedRnn
        //{
        //    get { return CountDocs(DocTypes.Rnn, true); }
        //}

        public decimal SelectedKolf
        {
            get { return GetSelectedKolf(); }
        }

        private decimal GetSelectedKolf()
        {
            decimal res = 0;
            res = OtgrData.Where(or => or.IsChecked).Sum(or => or.Kolf);
            return res;
        }

        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public PDogInfoViewModel InPdogInfo { get; set; }

        private PDogInfoViewModel outPdogInfo;
        public PDogInfoViewModel OutPdogInfo 
        {
            get { return outPdogInfo; }
            set 
            {
                SetAndNotifyProperty("OutPdogInfo", ref outPdogInfo, value);
                if (value != null)
                    NewPrice = value.ModelRef.Cenaprod;
            }
        }

        private decimal newPrice;
        public decimal NewPrice
        {
            get { return newPrice; }
            set { SetAndNotifyProperty("NewPrice", ref newPrice, value); }
        }

        public ICommand SetNewPriceCommand { get; set; }

        private void ExecSetNewPrice()
        {
            if (otgrData == null || !otgrData.Any(d => d.IsChecked)) return;

            foreach (var otgr in otgrData.Where(d => d.IsChecked))
            {
                if (newPrice != 0)
                    otgr.Cena = newPrice;
                otgr.Otgr.Kdog = outPdogInfo.ModelRef.Kdog;
                otgr.Otgr.Poup = outPdogInfo.ModelRef.Poup;
                if (otgr.TrackingState == TrackingInfo.Unchanged) 
                    otgr.TrackingState = TrackingInfo.Updated;
                otgr.IsChecked = false;
                otgr.StatusType = 1;
                otgr.StatusMsgs = new string[] { "Перенесена на новый договор" };
            }

            NewPrice = 0;
        }

        private decimal newKolf;
        public decimal NewKolf
        {
            get { return newKolf; }
            set { SetAndNotifyProperty("NewKolf", ref newKolf, value); }
        }

        public ICommand SplitOtgrCommand { get; set; }

        private void ExecSplitOtgr()
        {
            if (otgrData == null || selectedOtgr == null || newKolf <= 0 || newKolf >= selectedOtgr.Kolf) return;

            var oldOtgr = selectedOtgr.Otgr;
            var newOtgr = DeepCopy.Make(oldOtgr);
            //newOtgr.Idp623 = 0;
            newOtgr.Idrnn = 0;
            newOtgr.Kolf = newKolf;
            newOtgr.Sper = 0;
            newOtgr.Ndssper = 0;
            newOtgr.Dopusl = 0;
            newOtgr.Ndsdopusl = 0;
            newOtgr.TrackingState = TrackingInfo.Created;

            oldOtgr.SourceId = 1; // Новый источник - Репка, для возможности последующего редактирования
            newOtgr.SourceId = 1;

            selectedOtgr.StatusType = 1;
            selectedOtgr.StatusMsgs = new string[] { "Разделена" };

            selectedOtgr.Kolf -= newKolf;
            if (selectedOtgr.TrackingState == TrackingInfo.Unchanged) selectedOtgr.TrackingState = TrackingInfo.Updated;
            var newOtgrVM = new OtgrLineViewModel(repository, newOtgr);

            var oldIndex = otgrData.IndexOf(selectedOtgr);
            otgrData.Insert(oldIndex + 1, newOtgrVM);
            NewKolf = 0;
            selectedOtgr.IsChecked = newOtgrVM.IsChecked = false;
        }

        private bool CanSplitOtgr()
        {
            return otgrData != null && selectedOtgr != null && newKolf > 0 && newKolf < selectedOtgr.Kolf;
        }

        private bool isAllChangedSelected;
        public bool IsAllChangedSelected 
        {
            get { return isAllChangedSelected; } 
            set 
            {
                SelectAllChanged(value);
                SetAndNotifyProperty("IsAllChangedSelected", ref isAllChangedSelected, value);
            }
        }

        private void SelectAllChanged(bool _newstat)
        {
            if (otgrData == null || otgrData.Count == 0 || otgrData.All(o => o.TrackingState == TrackingInfo.Unchanged)) return;

            foreach (var o in otgrData)
                o.IsChecked = o.StatusType != 1 ? false : _newstat;
        }
    }
}
