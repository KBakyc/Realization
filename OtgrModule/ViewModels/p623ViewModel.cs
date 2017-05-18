using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using CommonModule.Commands;
using CommonModule.Interfaces;
using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Collections;
using CommonModule.Helpers;


namespace OtgrModule.ViewModels
{
    /// <summary>
    /// Модель режима приёмки отгрузки в реализацию из общего хранилища первичных документов.
    /// </summary>
    public class p623ViewModel : BasicModuleContent
    {
        public p623ViewModel(IModule _parent)
            :base(_parent)
        {            
            SubmitCmd = new DelegateCommand(ExecSubmitCmd, () => OtgrRows != null && OtgrRows.Any(r=>r.IsChecked));
            FindInRealCmd = new DelegateCommand(ExecFindInRealCmd, CanFindInRealCmd);
            SelectDeselectAllCommand = new DelegateCommand(ExecSelectDeselectAll);
            SelectDeselectDocCommand = new DelegateCommand(ExecSelectDeselectDoc,()=>SelectedOtgr!=null && !SelectedOtgr.HasErrors);
            SelectDeselectMyOtgrCommand = new DelegateCommand(ExecSelectDeselectMyOtgr,()=>MyKodfs!=null && MyKodfs.Length>0);
            OnCheckItemChangeCommand = new DelegateCommand(ExecOnCheckItemChange);
        }

        public p623ViewModel(IModule _parent, PoupModel _poup, PkodModel _pkod, DateTime _datefrom, DateTime _dateto)
            :this(_parent)
        {
            SelectedPoup = _poup;
            SelectedPkod = _pkod;
            DateFrom = _datefrom;
            DateTo = _dateto;
            LoadReestr();
            InitSelection();            
        }      

        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public PoupModel SelectedPoup { get; set; }
        public PkodModel SelectedPkod { get; set; }

        private Selectable<KodfModel>[] kodfs;
        public Selectable<KodfModel>[] Kodfs
        {
            get { return kodfs; }
            set
            {
                SetAndNotifyProperty("Kodfs", ref kodfs, value);
            }
        }

        private void LoadReestr()
        {
            OtgrRows.Tracking = false;
            OtgrRows.Clear();
            var olines = Parent.Repository.GetTempOtgr()
                                          .Select(ol => new OtgrLineViewModel(Parent.Repository, ol, false));
            
            LoadKodfs(olines);

            CollectSelectedKodfs();

            foreach (var o in olines)
                OtgrRows.Add(o);
            OtgrRows.Tracking = true;
            
            // Выгрузка страницы, если нет непринятой отгрузки            
        }

        public string SelectedKodfsLabel
        {
            get
            {
                return GenerateSelectedKodfsLabel();
            }
        }

        private string GenerateSelectedKodfsLabel()
        {
            CollectSelectedKodfs();
            string res = "Все";
            if (!IsAllSelectMode)
            {
                var strarr = selectedKodfs.Select(k => k.ToString()).ToArray();
                res = string.Join(", ", strarr);
            }
            return res;
        }

        private void LoadKodfs(IEnumerable<OtgrLineViewModel> _olines)
        {
            Kodfs = _olines.Select(or => or.Kodf).Distinct().Select(kf => new Selectable<KodfModel>(kf)).ToArray();
            for(int i=0; i< Kodfs.Length; i++)
                Kodfs[i].PropertyChanged += KodfPropertyChanged;
        }

        private void KodfPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var selkodf = sender as Selectable<KodfModel>;
            if (selkodf != null && e.PropertyName == "IsSelected")
            {
                SelectDeselectByKodf(selkodf.Value.Kodf, selkodf.IsSelected);
                //RefreshView();
                IsNeedRefresh = true;
            }
        }

        private void SelectDeselectAllKodfs(bool _select)
        {
            for (int i = 0; i < kodfs.Length; i++)
                kodfs[i].IsSelected = _select;
        }

        private void SelectDeselectMyKodfs(bool _select)
        {
            var mykodfsm = kodfs.Join(MyKodfs, km => km.Value.Kodf, k => k, (km, k) => km);
            foreach (var km in mykodfsm)
                km.IsSelected = IsMySelectMode;
        }

        private void SelectDeselectByKodf(int _kodf, bool _select)
        {
            var correctOtgr = OtgrRows.Where(o => !o.HasErrors && o.Otgr.Kodf == _kodf);
            foreach (var otgr in correctOtgr)
                otgr.IsChecked = _select;
            NotifyAfterKodfsSelection();
        }

        private void NotifyAfterKodfsSelection()
        {
            IsAllSelectMode = Kodfs.All(k => k.IsSelected);
            NotifyPropertyChanged("SelectedKodfsLabel");
            NotifySelectedChanged();
            //NotifyPropertyChanged("IsAllSelectMode");
        }

        /// <summary>
        /// Начальное выделение отобранной отгрузки в соответствии с настройками
        /// </summary>
        private void InitSelection()
        {
            if (OtgrRows.Count == 0) return;
            if (Properties.Settings.Default.KodfsSelMode != 0)
            {
                IsAllSelectMode = false;
                foreach (var o in OtgrRows)
                    o.IsChecked = false;
                //SelectDeselectAllCommand.Execute(null);
                if (Properties.Settings.Default.KodfsSelMode == 1)
                {
                    IsMySelectMode = true;
                    SelectDeselectMyOtgrCommand.Execute(null);
                }
            }
            else
            {
                IsAllSelectMode = true;
                IsMySelectMode = false;
                SelectAllOtgr();
            }
            isShowUnchecked = Properties.Settings.Default.ShowUnchecked;
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

        private void SelectAllOtgr()
        {
            //var correctOtgr = otgrRows.Where(o => !o.HasErrors);
            //foreach (var or in correctOtgr)
            //    or.IsChecked = IsAllSelectMode;
            SelectDeselectAllKodfs(IsAllSelectMode);
            IsMySelectMode = IsAllSelectMode;

            NotifySelectedChanged();
        }

        private bool isMySelectMode;

        /// <summary>
        /// Выбраны мои
        /// </summary>
        public bool IsMySelectMode 
        {
            get { return isMySelectMode; }
            set
            {
                if (value != isMySelectMode)
                {
                    isMySelectMode = value;
                    NotifyPropertyChanged("IsMySelectMode");
                }
            }
        }

        public ICommand FindInRealCmd { get; set; }
        private void ExecFindInRealCmd()
        {
            Action work = () =>
            {
                var otgrArcData = Parent.Repository.GetOtgrArc(new DataObjects.SeachDatas.OtgruzSearchData{IdRnn = selectedOtgr.Otgr.Idrnn});
                if (otgrArcData == null || otgrArcData.Length == 0)
                    Parent.Services.ShowMsg("Результат", "Отгрузка в архиве реализации не найдена.", false);
                else
                {
                    var ncontent = new OtgrArcViewModel(Parent, otgrArcData) { Title = String.Format("Отгрузка по док. № {0}", selectedOtgr.DocumentNumber) };
                    ncontent.TryOpen();
                }
            };

            Parent.Services.DoWaitAction(work, "Подождите", "Приём отгрузки в реализацию");
        }
        private bool CanFindInRealCmd()
        {
            return selectedOtgr != null && selectedOtgr.Otgr.Idrnn > 0;
        }

        /// <summary>
        /// Комманда подтверждения принятия выбранной отгрузки
        /// </summary>
        public ICommand SubmitCmd { get; set; }
        private void ExecSubmitCmd()
        {
            Action work = () =>
            {
                Parent.Repository.AcceptP623(OtgrRows.Select(vm => vm.Otgr).ToArray());
                Parent.Services.ShowMsg("Результат", "Загрузка выбранных данных закончена.", false);
                Parent.ShellModel.UpdateUi(() => LoadReestrAndRestoreSelection(), true, false);
            };

            Parent.Services.DoWaitAction(work, "Подождите", "Приём отгрузки в реализацию");
        }

        private void LoadReestrAndRestoreSelection()
        {
            var prevKodfs = Kodfs.Where(k => k.IsSelected);
            LoadReestr();
            var prevInNewKodfs = prevKodfs.Join(Kodfs,o=>o.Value.Kodf,i=>i.Value.Kodf,(o,i)=>i);
            foreach (var kf in prevInNewKodfs)
                kf.IsSelected = true;
            //ChangeFilter();
            RefreshView();
        }

        /// <summary>
        /// Комманда выделения/снятия выделения всех отгрузок
        /// </summary>
        public ICommand SelectDeselectAllCommand { get; set; }
        private void ExecSelectDeselectAll()
        {
            SelectAllOtgr();
        }

        private bool isMyKodfsReaded;
        private int[] myKodfs;

        public string MyKodfsString
        {
            get { return myKodfs == null ? "" : String.Join(",", myKodfs.Select(k => k.ToString()).ToArray()); }
        }

        /// <summary>
        /// Мои выбранные коды форм отгрузки
        /// </summary>
        public int[] MyKodfs
        {
            get
            {
                if (myKodfs == null && !isMyKodfsReaded)
                {
                    {
                        try
                        {
                            myKodfs = CommonModule.CommonSettings.GetMyKodfs(SelectedPoup.Kod);
                        }
                        catch (Exception e)
                        {
                            WorkFlowHelper.OnCrash(e);
                            //string _mess = e.Message;
                            //string _type = e.GetType().ToString();
                            //Parent.Repository.LogToFile(null, String.Format("{0} : {1}\n{2}", _type, _mess, e.StackTrace));
                        }
                        finally
                        {
                            isMyKodfsReaded = true;
                        }
                    }
                }
                return myKodfs;
            }
        }

        /// <summary>
        /// Комманда выделения/снятия выделения настроенных моих отгрузок
        /// </summary>
        public ICommand SelectDeselectMyOtgrCommand { get; set; }
        private void ExecSelectDeselectMyOtgr()
        {
            if (MyKodfs == null || MyKodfs.Length == 0) return;
            if (MyKodfs.Contains(0))
            {
                IsAllSelectMode = IsMySelectMode;
                SelectAllOtgr();
                return;
            }
            SelectDeselectMyKodfs(IsMySelectMode);
            //var correctOtgr = otgrRows.Where(o => !o.HasErrors);
            //foreach (var or in correctOtgr.Join(MyKodfs, o => o.Kodf, m => m, (o, m) => o))
            //    or.IsChecked = IsMySelectMode;
            NotifySelectedChanged();
        }

        private void NotifySelectedChanged()
        {
            NotifyPropertyChanged("CheckedRows");
            NotifyPropertyChanged("CheckedTn2");
            NotifyPropertyChanged("CheckedRnn");
            NotifyPropertyChanged("SelectedKolf");
        }

        /// <summary>
        /// Комманда выделения/снятия выделения отгрузок документа
        /// </summary>
        public ICommand SelectDeselectDocCommand { get; set; }
        private void ExecSelectDeselectDoc()
        {
            if (SelectedOtgr == null) return;
            bool tostate = SelectedOtgr.IsChecked;
            Func<OtgrLineViewModel,bool> findDoc;
            findDoc = FindDoc(false);

            var correctOtgr = otgrRows.Where(o => !o.HasErrors);

            foreach (var or in correctOtgr.Where(findDoc))
                or.IsChecked = tostate;
            NotifySelectedChanged();
        }

        private Func<OtgrLineViewModel, bool> FindDoc(bool _isbyrwnum)
        {
            if (_isbyrwnum)
                return o => o.RwBillNumber == SelectedOtgr.RwBillNumber;
            else
                return o => o.DocumentNumber == SelectedOtgr.DocumentNumber;
        }

        private int CountDocs(bool? _ischk)
        {
            IEnumerable<OtgrLineViewModel> or = OtgrRows;
            if (_ischk.HasValue)
                or = or.Where(o => o.IsChecked == _ischk);
            return or.Select(o => o.DocumentNumber).Distinct().Count();
        }

        /// <summary>
        /// Комманда выполняется при пометке/снятии элемента коллекции
        /// </summary>
        public ICommand OnCheckItemChangeCommand { get; set; }
        private void ExecOnCheckItemChange()
        {
            NotifySelectedChanged();
        }

        public int TotalRows
        {
            get { return OtgrRows.Count; }
        }

        public int CheckedRows
        {
            get { return OtgrRows.Where(o => o.IsChecked).Count(); }
        }

        public int TotalDocuments
        {
            get { return CountDocs(null); }
        }

        public int CheckedDocuments
        {
            get { return CountDocs(true); }
        }

        public decimal SelectedKolf
        {
            get { return GetSelectedKolf(); }
        }

        private decimal GetSelectedKolf()
        {
            decimal res = 0;
            res = OtgrRows.Where(or => or.IsChecked).Sum(or => or.Kolf);
            return res;
        }


        /// <summary>
        /// Коллекция строк принимаемой отгрузки
        /// </summary>
        private ChangeTrackingCollection<OtgrLineViewModel> otgrRows = new ChangeTrackingCollection<OtgrLineViewModel>();
        public ChangeTrackingCollection<OtgrLineViewModel> OtgrRows
        {
            set
            {
                if (value != otgrRows)
                {
                    otgrRows = value;
                    NotifyPropertyChanged("OtgrRows");
                }
            }
            get
            {
                return otgrRows;
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


        private bool isShowUnchecked;
        /// <summary>
        /// Призкак фильтрации по отметкам
        /// </summary>
        public bool IsShowUnchecked 
        {
            get { return isShowUnchecked; }
            set
            {
                if (value != isShowUnchecked)
                {
                    isShowUnchecked = value;
                    ChangeFilter();
                }
            }
        }

        private IEnumerable<int> selectedKodfs;
        private void CollectSelectedKodfs()
        {
            selectedKodfs = Kodfs.Where(k => k.IsSelected).Select(k => k.Value.Kodf);
        }

        /// <summary>
        /// Комманда фильтрации
        /// </summary>
        //public ICommand FilterCommand { get; set; }
        public void ChangeFilter()
        {
            var cv = CollectionViewSource.GetDefaultView(OtgrRows);
            if (!IsShowUnchecked)
            {
                cv.Filter = r =>
                {
                    var ol = (OtgrLineViewModel)r;
                    bool ret = ol.IsChecked || (selectedKodfs.Contains(ol.Otgr.Kodf) && ol.HasErrors);
                    return ret;
                };
            }
            else
                cv.Filter = null;
            //cv.Refresh();
        }

        public bool IsNeedRefresh { get; set; }

        public void RefreshView()
        {
            if (IsNeedRefresh)
            {
                Parent.ShellModel.UpdateUi(() =>
                {
                    var cv = CollectionViewSource.GetDefaultView(OtgrRows);
                    cv.Refresh();
                }, true, false);
                IsNeedRefresh = false;
            }
        }
    }
}