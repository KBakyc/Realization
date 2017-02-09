using System;
using System.Linq;
using System.Windows.Input;
using CommonModule.Commands;
using CommonModule.Interfaces;
using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Collections;
using System.Collections.Generic;
using OtgrModule.Helpers;
using DataObjects.SeachDatas;
using CommonModule.Helpers;
using DataObjects.Helpers;
using OtgrModule.Reports;
using DotNetHelper;
using System.ComponentModel.Composition;
using CommonModule.Composition;


namespace OtgrModule.ViewModels
{
    public class OtgrArcViewModel : BasicModuleContent
    {
        private ISfModule sfModule;
        private OtgrLine[] otgrs;

        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public PoupModel SelectedPoup { get; set; }
        public PkodModel SelectedPkod { get; set; }
        public SfModel SfRef { get; set; }

        private bool isInRealiz = true;

        public OtgrArcViewModel(IModule _parent, IEnumerable<OtgrLine> _otgrs)
            :base(_parent)
        {
            otgrs = _otgrs.ToArray();
            CommonInit();
        }

        public OtgrArcViewModel(IModule _parent, PoupModel _poup, PkodModel _pkod, DateTime _datefrom, DateTime _dateto, bool _isInRealiz)
            :base(_parent)
        {
            SelectedPoup = _poup;
            SelectedPkod = _pkod;
            DateFrom = _datefrom;
            DateTo = _dateto;            
            Title = "Архив отгрузки";
            isInRealiz = _isInRealiz;
            CommonInit();
        }

        public OtgrArcViewModel(IModule _parent, SfModel _sfm)
            :base(_parent)
        {
            if (_sfm == null)
                throw(new Exception("Ссылка на модель счёта = NULL"));
            SfRef = _sfm;
            Title = String.Format("Отгрузка по счёту № {0}", SfRef.NumSf);
            CommonInit();
        }

        private void CommonInit()
        { 
            LoadOtgrArc();
            sfModule = Parent.ShellModel.Container.GetExportedValueOrDefault<ISfModule>();
        }

        private void LoadSfs()
        {
            if (selectedOtgr == null) return;
            var oLine = selectedOtgr;
            Action work = () =>
            {
                oLine.LoadSfs();
                Parent.ShellModel.UpdateUi(() => CommandManager.InvalidateRequerySuggested(), false, true);
            };
            work.BeginInvoke(null, null);
        }      

        private void LoadOtgrArc()
        {
            if (otgrs == null)
            {
                if (SfRef != null)
                    otgrs = Parent.Repository.GetOtgrArc(SfRef.IdSf);
                else
                {
                    var pkod = SelectedPkod == null ? (short)0 : SelectedPkod.Pkod;
                    otgrs = Parent.Repository.GetOtgrArc(new OtgruzSearchData(isInRealiz) { Poup = SelectedPoup.Kod, Pkod = pkod, Dfrom = DateFrom, Dto = DateTo });
                }
            }
            otgrs = ReorderOtgrLines(otgrs).ToArray();
            FillOtgrCollection();            
        }

        private void FillOtgrCollection()
        {
            OtgrRows = new ChangeTrackingCollection<OtgrLineViewModel>(otgrs.Select(m => new OtgrLineViewModel(Parent.Repository, m)),
                true
            );
        }

        private IEnumerable<OtgrLine> ReorderOtgrLines(IEnumerable<OtgrLine> _otgrs)
        {
            return _otgrs.OrderBy(o => o.Datgr).ThenBy(o => o.DocumentNumber).ThenBy(o => o.RwBillNumber);
        }

        public void LoadOtgrArc(IEnumerable<OtgrLine> _otgrs, bool _clear)
        {
            if (_otgrs == null) return;

            var newotgrs = ReorderOtgrLines(_otgrs);

            if (otgrs != null && !_clear)
            {
                var olddata = otgrs.Where(od => od.Idp623 == 0 && !_otgrs.Any(n => n.Idrnn == od.Idrnn) || !_otgrs.Any(n => n.Idp623 == od.Idp623));
                otgrs = olddata.Union(newotgrs).ToArray();
            }
            else
                otgrs = newotgrs.ToArray();

            FillOtgrCollection();            
        }

        private ChangeTrackingCollection<OtgrLineViewModel> otgrRows;
        public ChangeTrackingCollection<OtgrLineViewModel> OtgrRows
        {
            get
            {
                return otgrRows;
            }
            set
            {
                if (value != otgrRows)
                    otgrRows = value;
                NotifyPropertyChanged("OtgrRows");
                NotifyPropertyChanged(() => TotalKolf);
            }
        }

        private OtgrLineViewModel selectedOtgr;
        public OtgrLineViewModel SelectedOtgr
        {
            get { return selectedOtgr; }
            set
            {
                if (value != selectedOtgr)
                {
                    selectedOtgr = value;
                    if (selectedOtgr != null)
                    {
                        if (selectedOtgr.Totals == null)
                            //selectedOtgr.Totals = OtgrHelper.GetOtgrTotals(selectedOtgr, otgrRows);
                            OtgrHelper.SetNaklTotals(selectedOtgr, otgrRows);
                        if (selectedOtgr.IsFindSfs && !selectedOtgr.IsSfsLoaded)
                            LoadSfs();
                    }
                    NotifyPropertyChanged("SelectedOtgr");
                }
            }
        }

        private ICommand showSfsCommand;
        private ICommand deleteCommand;       
        private ICommand editCommand;
        private ICommand showHistoryCommand;

        private const string SHOWHISTORYCOMMAND_COMPONENTNAME = "OtgrModule.ViewModels.OtgrArcViewModel.ShowHistoryCommand";
        [ExportNamedComponent("OtgrModule.ComponentCommand", SHOWHISTORYCOMMAND_COMPONENTNAME)]
        public ICommand ShowHistoryCommand
        {
            get
            {
                if (showHistoryCommand == null && CheckComponentAccess(SHOWHISTORYCOMMAND_COMPONENTNAME))
                    showHistoryCommand = new DelegateCommand(ExecShowHistory, () => selectedOtgr != null);
                return showHistoryCommand;
            }
        }
        private void ExecShowHistory()
        {
            Action work = () =>
            {
                var oHistory = Parent.Repository.GetOtgrHistory(selectedOtgr.Otgr.Idp623);
                Parent.OpenDialog(new HistoryDlgViewModel(oHistory)
                {
                    Title = "История отгрузки № " + selectedOtgr.DocumentNumber
                });
            };
            Parent.Services.DoWaitAction(work, "Подождите", "Запрос истории отгрузки...");
        }      

        private Result<bool> DoUpdateSelOtgr(OtgrLine _newOtgr, string _logDescr)
        {
            bool res = Parent.Repository.UpdateOtgruz(_newOtgr, _logDescr);
            var tRes = res ? new Result<bool> { Value = true }
                           : new Result<bool>(false, "Ошибка при изменении данных по выбранной отгрузке");
            return tRes;
        }

        /// <summary>
        /// Комманда показа списка счетов, выписанных на выбранную отгрузку
        /// </summary>
        public ICommand ShowSfsCommand
        {
            get
            {
                if (showSfsCommand == null)
                    showSfsCommand = new DelegateCommand(ExecShowSfsCommand, CanShowSfsCommand);
                return showSfsCommand;
            }
        }
        private bool CanShowSfsCommand()
        {
            return otgrRows.Any(o => o.IsChecked); //SelectedOtgr != null && sfModule != null && (SelectedOtgr.IsSfsLoaded && SelectedOtgr.OtgrAllSfs != null && SelectedOtgr.OtgrAllSfs.Length != 0);
        }
        private void ExecShowSfsCommand()
        {
            var selOtgr = otgrRows.Where(o => o.IsChecked);
            foreach(var o in selOtgr.Where(o => !o.IsSfsLoaded))
                o.LoadSfs();
            
            var sfs = selOtgr.Where(o => o.OtgrAllSfs != null && o.OtgrAllSfs.Length != 0).SelectMany(o => o.OtgrAllSfs).DistinctBy(s => s.IdSf).ToArray();

            if (sfs == null) return;

            var crsfsstring = String.Join(", ", sfs.Where(s => s.Status == LifetimeStatuses.Created)
                                       .Select(s => s.NumSf.ToString()).ToArray());

            if (!String.IsNullOrEmpty(crsfsstring))
                Parent.Services.ShowMsg("Неподтверждённые счета по данной отгрузке: ", crsfsstring, false);
            else
            {
                var oldsfs = sfs.Where(s => s.Status != LifetimeStatuses.Created).ToArray();

                if (oldsfs.Length > 0)
                {
                    string doc = String.Join(", ", selOtgr.Select(o => o.DocumentNumber));
                    if (!String.IsNullOrWhiteSpace(SelectedOtgr.RwBillNumber))
                        doc += String.Format(", ЖД накл.№ {0}", SelectedOtgr.RwBillNumber);
                    if (SelectedOtgr.Nv != 0)
                        doc += String.Format(", ваг.№ {0}", SelectedOtgr.Nv);
                    sfModule.ListSfs(oldsfs, "Счета-фактуры на док.№ " + doc);
                    Parent.ShellModel.LoadModule(sfModule);
                }
            }
        }
        
        /// <summary>
        /// Комманда копирования / разделения отгрузки
        /// </summary>

        private const string COPYREALCOMMAND_COMPONENTNAME = "OtgrModule.ViewModels.OtgrArcViewModel.CopyRealCommand";
        [ExportNamedComponent("OtgrModule.ComponentCommand", COPYREALCOMMAND_COMPONENTNAME)]
        private ICommand copyRealCommand;
        public ICommand CopyRealCommand
        {
            get
            {
                if (copyRealCommand == null && CheckComponentAccess(COPYREALCOMMAND_COMPONENTNAME))
                    copyRealCommand = new DelegateCommand(ExecCopyRealCommand, CanCopyRealCommand);
                return copyRealCommand;
            }
        }
        private bool CanCopyRealCommand()
        {
            return !IsReadOnly && selectedOtgr != null;
                //&& selectedOtgr.IsInRealiz;
                // && (SelectedOtgr.Otgr.SourceId == 1 || SelectedOtgr.IsEditEnabled);
        }
        private void ExecCopyRealCommand()
        {
            //var selOtgr = SelectedOtgr;
            //var allsfs = Parent.Repository.GetSfsByOtgruz(selOtgr.Otgr.Idp623).ToArray();
            //string otgrtxt = GetOtgrString(selOtgr);

            //if (allsfs.Any(s => s.Status != LifetimeStatuses.Deleted))
            //{
            //    string exSfs = String.Join(",", allsfs.Where(s => s.Status != LifetimeStatuses.Deleted).Select(s => s.NumSf.ToString()).ToArray());
            //    Parent.Services.ShowMsg("Ошибка!", String.Format("Имеется неаннулированные счета:\n {0}\n Изменение невозможно.", exSfs), true);
            //}
            //else
            //{
                DoCopySelectedOtgr(selectedOtgr.Otgr);
            //}
        }

        private void DoCopySelectedOtgr(OtgrLine _selOtgr)
        {
            if (_selOtgr == null) throw new InvalidOperationException("Не выбрана отгрузка/услуга!");

            var otgrCopy = DeepCopy.Make(_selOtgr);
            otgrCopy.Idp623 = 0;
            otgrCopy.SourceId = 3;
            
            AddOtgrViewModel addotgrContent = Parent.GetLoadedContent<AddOtgrViewModel>(null) as AddOtgrViewModel;
            if (addotgrContent != null)
            {
                var newItem = new OtgrLineViewModel(Parent.Repository, otgrCopy);
                addotgrContent.OtgrRows.Add(newItem);
                addotgrContent.SelectedOtgr = newItem;
                Parent.SelectContent<AddOtgrViewModel>(c => c == addotgrContent);
            }
            else
            {
                addotgrContent = new AddOtgrViewModel(Parent, Enumerable.Repeat(otgrCopy,1));
                addotgrContent.TryOpen();
            }            
        }

        /// <summary>
        /// Комманда изменения выбранной отгрузки.
        /// (если нет неаннулированных счетов)
        /// </summary>
        public ICommand EditCommand
        {
            get
            {
                if (editCommand == null)
                    editCommand = new DelegateCommand(ExecEditCommand, CanExecEditCommand);
                return editCommand;
            }
        }
        private bool CanExecEditCommand()
        {
            return !IsReadOnly && SelectedOtgr != null && (SelectedOtgr.Otgr.SourceId == 1 || SelectedOtgr.IsEditEnabled);// || SelectedOtgr.Otgr.TransportId == 3);
        }
        private void ExecEditCommand()
        {
            var selOtgr = SelectedOtgr;
            var allsfs = Parent.Repository.GetSfsByOtgruz(selOtgr.Otgr.Idp623).ToArray();
            string otgrtxt = GetOtgrString(selOtgr);

            if (allsfs.Any(s => s.Status != LifetimeStatuses.Deleted))
            {
                string exSfs = String.Join(",", allsfs.Where(s => s.Status != LifetimeStatuses.Deleted).Select(s => s.NumSf.ToString()).ToArray());
                Parent.Services.ShowMsg("Ошибка!", String.Format("Имеется неаннулированные счета:\n {0}\n Изменение невозможно.", exSfs), true);
            }
            else
            {
                DoEditSelectedOtgr();
            }
        }

        private void DoEditSelectedOtgr()
        {
            var selOtgr = SelectedOtgr;
            if (selOtgr == null) return;

            var eDlg = new EditOtgrDlgViewModel(Parent.Repository, selOtgr.Otgr)
            {
                Title = "Изменение данных",
                OnSubmit = SubmitEditOtgr
            };

            Parent.OpenDialog(eDlg);
        }

        private void SubmitEditOtgr(Object _d)
        {
            var eDlg = _d as EditOtgrDlgViewModel;
            if (_d == null) return;
            Parent.CloseDialog(_d);

            var updatedOtgruz = eDlg.NewModel;

            Action work = () =>
            {
                var res = Parent.Repository.UpdateOtgruz(updatedOtgruz, null);
                if (!res)
                    Parent.Services.ShowMsg("Ошибка", "Произошла ошибка при изменении отгрузки", true);
                else
                    Parent.ShellModel.UpdateUi(() => RefreshOtgruzItem(selectedOtgr, true), false, true);
            };

            Parent.Services.DoWaitAction(work, "Подождите", "Изменение отгрузки/услуги...");
        }

        private void RefreshOtgruzItems(IEnumerable<OtgrLineViewModel> _items)
        {
            var iArr = _items.ToArray();
            for(int i=0; i< iArr.Length; i++)
                RefreshOtgruzItem(iArr[i], selectedOtgr != null && iArr[i] == selectedOtgr);
        }

        private void RefreshOtgruzItem(OtgrLineViewModel _ovm, bool _selectRefreshed)
        {
            var repository = Parent.Repository;
            if (_ovm == null || otgrRows == null || otgrRows.Count == 0) return;
            var itemIndex = otgrRows.IndexOf(_ovm);
            if (itemIndex < 0) return;
            bool inRealiz = _ovm.Otgr.Idp623 > 0;
            var newOtgruzData = repository.GetOtgrLine(inRealiz ? _ovm.Otgr.Idp623 : _ovm.Otgr.Idrnn, inRealiz);
            OtgrLineViewModel newOvm = null;
            if (newOtgruzData != null)
                newOvm = new OtgrLineViewModel(repository, newOtgruzData);

            Parent.ShellModel.UpdateUi(() =>
            {
                otgrRows.RemoveAt(itemIndex);
                if (newOvm != null)
                    otgrRows.Insert(itemIndex, newOvm);
                if (_selectRefreshed)
                    SelectedOtgr = newOvm;
                NotifyPropertyChanged(() => TotalKolf);
            }, false, false);
        
        }
        
        private void InsertOtgruzItemAfter(OtgrLineViewModel _oToInsert, OtgrLineViewModel _oAfter, bool _selectInserted)
        {
            var repository = Parent.Repository;
            if (_oToInsert == null || _oAfter == null || otgrRows == null || otgrRows.Count == 0) return;
            var itemIndex = otgrRows.IndexOf(_oAfter);
            if (itemIndex < 0) return;
            itemIndex++;
            Parent.ShellModel.UpdateUi(() =>
            {
                otgrRows.Insert(itemIndex, _oToInsert);
                if (_selectInserted)
                    SelectedOtgr = _oToInsert;
            }, true, false);
        }
        
        /// <summary>
        /// Комманда удаления выбранной отгрузки и связанных аннулированных счетов.
        /// (если нет неаннулированных)
        /// </summary>
        public ICommand DeleteCommand 
        {
            get
            {
                if (deleteCommand == null)
                    deleteCommand = new DelegateCommand(ExecDeleteCommand, CanExecDeleteCommand);
                return deleteCommand;
            }
        }
        private bool CanExecDeleteCommand()
        {
            return !IsReadOnly && OtgrRows.Any(r => r.IsChecked && r.Otgr.Idp623 > 0);
        }
        private void ExecDeleteCommand()
        {
            var selOtgr = OtgrRows.Where(r => r.IsChecked && r.Otgr.Idp623 > 0).ToArray();

            var allsfs = selOtgr.SelectMany(o => Parent.Repository.GetSfsByOtgruz(o.Otgr.Idp623))
                                .GroupBy(s => s.IdSf)
                                .Select(g => g.First()).ToArray();

            string msgtxt = "";
            string otgrtxt = String.Join("\n", selOtgr.Select(r => GetOtgrString(r)).Distinct().ToArray());

            if (allsfs.Any(s => s.Status != LifetimeStatuses.Deleted))
            {
                string exSfs = String.Join(",", allsfs.Where(s => s.Status != LifetimeStatuses.Deleted).Select(s => s.NumSf.ToString()).ToArray());
                Parent.Services.ShowMsg("Ошибка!", String.Format("Имеется неаннулированные счета:\n {0}\n Удаление отгрузки невозможно.", exSfs), true);
            }
            else
            {
                string dlgTitle = null;
                var vozvrats = selOtgr.Where(o => o.Otgr.IdVozv.GetValueOrDefault() > 0 && o.Otgr.SourceId == 1 && o.Kolf < 0);
                if (vozvrats.Any())
                {
                    if (vozvrats.Count() > 1)
                    {
                        Parent.Services.ShowMsg("Ошибка!", "Среди выбранной отгрузки имеются возвраты\nДля отмены возврата выберите только его.", true);
                        return;
                    }
                    else
                        dlgTitle = "Внимание, Отменяется возврат!";
                }
                else
                    dlgTitle = "Внимание, Удаляется отгрузка!";


                    Parent.OpenDialog(new MsgDlgViewModel()
                    {
                        Title = dlgTitle,
                        Message = otgrtxt + "\n" + msgtxt,
                        OnSubmit = (d) =>
                        {
                            Parent.CloseDialog(d);
                            SubmitDeleteOtgruz(selOtgr);
                        },
                        OnCancel = (d) => Parent.CloseDialog(d)
                    });
            }
        }

        private bool DeleteSingleOtgr(OtgrLine _ol)
        {
            bool res = true;
            if (_ol.IdVozv.GetValueOrDefault() > 0 && _ol.SourceId == 1 && _ol.Kolf < 0)
                res = Parent.Repository.UnDoOtgrVozvrat(_ol);
            else
                res = Parent.Repository.DeleteOtgruz(_ol);
            return res;
        }

        private string GetOtgrString(OtgrLineViewModel _o)
        {
            return string.Format("Накладная №{0}", _o.Otgr.DocumentNumber)
                             + (_o.TransportId == 3 ? string.Format(" вагон №{0}", _o.Otgr.Nv) : "");
        }

        private void SubmitDeleteOtgruz(OtgrLineViewModel[] _selOtgr)
        {
            string retmess = null;
            Dictionary<OtgrLineViewModel, bool> res = new Dictionary<OtgrLineViewModel, bool>();

            Action<WaitDlgViewModel> work = (w) =>
            {                
                for (int i = 0; i < _selOtgr.Length; i++)
                {
                    var curOtgr = _selOtgr[i];
                    string curOtgrStr = GetOtgrString(curOtgr);
                    w.Message = curOtgrStr;
                    bool delres = DeleteSingleOtgr(curOtgr.Otgr);
                    res[curOtgr] = delres;
                    retmess += curOtgrStr + (delres ? " : Удалена\n" : " : Не удалена\n");
                }
            };

            Action afterwork = () => 
            {
                Parent.OpenDialog(new MsgDlgViewModel()
                {
                    Title = "Результат",
                    Message = retmess,
                    OnSubmit = d =>
                    {
                        Parent.CloseDialog(d);

                        foreach (var kv in res.Where(kv => kv.Value))
                            OtgrRows.Remove(kv.Key);
                        SelectedOtgr = OtgrRows.FirstOrDefault(r => r.IsChecked);
                        NotifyPropertyChanged(() => TotalKolf);
                    }
                });
            };
            
            Parent.Services.DoWaitAction(work, "Ожидание выполнения", "Удаление отгрузки...", afterwork);

        }

        private const string VOZVRATCOMMAND_COMPONENTNAME = "OtgrModule.ViewModels.OtgrArcViewModel.VozvratCommand";
        [ExportNamedComponent("OtgrModule.ComponentCommand", VOZVRATCOMMAND_COMPONENTNAME)]
        private ICommand vozvratCommand;
        public ICommand VozvratCommand
        {
            get
            {
                if (vozvratCommand == null && CheckComponentAccess(VOZVRATCOMMAND_COMPONENTNAME))
                    vozvratCommand = new DelegateCommand(ExecVozvratCommand, CanExecVozvratCommand);
                return vozvratCommand;
            }
        }
        private bool CanExecVozvratCommand()
        {
            //if (Parent.Repository.UserToken != 1) return false;

            if (IsReadOnly || selectedOtgr == null) return false;
            var otgr = selectedOtgr.Otgr;
            return otgr != null && otgr.IdVozv.GetValueOrDefault() == 0 && otgr.TransportId > 0;
        }
        private void ExecVozvratCommand()
        {
            var selOtgr = SelectedOtgr;
            var allsfs = Parent.Repository.GetSfsByOtgruz(selOtgr.Otgr.Idp623).ToArray();
            string otgrtxt = GetOtgrString(selOtgr);

            if (allsfs.Any(s => s.Status != LifetimeStatuses.Deleted))
            {
                string exSfs = String.Join(",", allsfs.Where(s => s.Status != LifetimeStatuses.Deleted).Select(s => s.NumSf.ToString()).ToArray());
                Parent.Services.ShowMsg("Ошибка!", String.Format("Имеется неаннулированные счета:\n {0}\n Возврат запрещен.", exSfs), true);
            }
            else
            {
                DoVozvratSelectedOtgr();
            }
        }
        private void DoVozvratSelectedOtgr()
        {
            var selOtgr = SelectedOtgr.Otgr;
            if (selOtgr == null) return;

            var vozvOtgr = DeepCopy.Make(selOtgr);
            vozvOtgr.Idrnn = 0;
            vozvOtgr.Idp623 = 0;
            vozvOtgr.DocumentNumber = null;
            vozvOtgr.RwBillNumber = null;
            vozvOtgr.Series = null;
            vozvOtgr.Dataccept = vozvOtgr.Datdrain = vozvOtgr.Datarrival = null;
            vozvOtgr.Datgr = vozvOtgr.Datnakl = DateTime.Now;
            vozvOtgr.Sper = vozvOtgr.Ndssper = vozvOtgr.Dopusl = vozvOtgr.Ndsdopusl = 0;
            vozvOtgr.KodRaznar = 0;
            vozvOtgr.Stgr = selOtgr.Stotpr;
            vozvOtgr.Stotpr = selOtgr.Stgr;
            vozvOtgr.Kolf = -vozvOtgr.Kolf;
            vozvOtgr.IdVozv = selOtgr.Idrnn;
            vozvOtgr.SourceId = 1;


            var eDlg = new EditOtgrDlgViewModel(Parent.Repository, vozvOtgr)
            {
                Title = "Введите данные по накладной на возврат",
                OnSubmit = SubmitEditVozvratOtgr
            };

            eDlg.SetAllRegionsEnable(false);
            eDlg.IsDocumentNumberEdEnabled = eDlg.IsDatgrEdEnabled = eDlg.IsDatnaklEdEnabled = eDlg.IsKolfEdEnabled = true;
            eDlg.GrpolVM.Title = "Отправитель";

            Parent.OpenDialog(eDlg);
        }

        private void SubmitEditVozvratOtgr(Object _d)
        {
            var eDlg = _d as EditOtgrDlgViewModel;
            if (_d == null) return;
            Parent.CloseDialog(_d);

            var vozvrat = eDlg.NewModel;

            Action work = () =>
            {
                var res = Parent.Repository.DoOtgrVozvrat(selectedOtgr.Otgr, vozvrat);
                if (!res)
                    Parent.Services.ShowMsg("Ошибка", "Произошла ошибка при обработке возврата", true);
                else
                    Parent.ShellModel.UpdateUi(() => RefreshOtgruzItem(selectedOtgr, true), false, true);
            };

            Parent.Services.DoWaitAction(work, "Подождите", "Обработка возврата отгрузки...");
        }

        private ICommandInterface[] reports;
        public ICommandInterface[] Reports
        {
            get
            {
                if (reports == null)
                    reports = new ICommandInterface[] { new VagListReport(this) };
                return reports;
            }
        }

        public KeyValueObj<string, decimal> TotalKolf
        {
            get 
            {
                var muid = otgrRows[0].Otgr.MeasureUnitId;
                return otgrRows.Skip(1).Any(r => r.Otgr.MeasureUnitId != muid) ? null : new KeyValueObj<string, decimal>(Parent.Repository.GetMeasureUnits(muid).Select(mu => mu.FullName).FirstOrDefault(), otgrRows.Sum(r => r.Kolf)); 
            }
        }

        
    }
}