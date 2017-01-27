using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Text;
using CommonModule.ViewModels;
using CommonModule.Interfaces;
using RwModule.Models;
using System.Windows.Input;
using CommonModule.Commands;
using System.Collections.ObjectModel;
using RwModule.Reports;
using RwModule.Interfaces;
using CommonModule.Helpers;
using DataObjects;
using DotNetHelper;
using DAL;
using CommonModule.Composition;

namespace RwModule.ViewModels
{
    public class RwListsArcViewModel : BasicModuleContent
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        private IOtgruzModule otgruzModule;

        public RwListsArcViewModel(IModule _parent, IEnumerable<RwList> _rwlst, IEnumerable<IModelFilter<RwDoc>> _rwDocFilters)
            :base(_parent)
        {            
            LoadData(_rwlst);
            if (_rwDocFilters != null && _rwDocFilters.Any())
                PrepareFilters(_rwDocFilters);
            if (Parent != null)
            {
                otgruzModule = Parent.ShellModel.Container.GetExportedValueOrDefault<IOtgruzModule>();
                if (rwListCollection != null && rwListCollection.Count == 1)
                {
                    selectedRwList = rwListCollection[0];
                    LoadRwDocsAsync(false);
                }
            }                        
        }

        public RwListsArcViewModel(IModule _parent, IEnumerable<RwList> _rwlst)
            :this(_parent, _rwlst, null)
        {}
       
        public void LoadData(IEnumerable<RwList> _rwlst)
        {
            var selRwl = selectedRwList;

            if (rwListCollection == null)
                rwListCollection = new ObservableCollection<RwListViewModel>();
            else
                rwListCollection.Clear();
            if (rwDocsCollection == null)
                rwDocsCollection = new ObservableCollection<Selectable<RwDocViewModel>>();
            else
                rwDocsCollection.Clear();

            if (_rwlst != null && _rwlst.Any())
                foreach(var l in _rwlst)
                    rwListCollection.Add(new RwListViewModel(Parent.Repository, l));
            if (selRwl != null)
                SelectedRwList = rwListCollection.FirstOrDefault(l => l.Id_rwlist == selRwl.Id_rwlist);           
        }

        public ICommand RefreshCommand { get; set; }

        private ObservableCollection<RwListViewModel> rwListCollection;
        public ObservableCollection<RwListViewModel> RwListCollection
        {
            get { return rwListCollection; }
        }

        private RwListViewModel selectedRwList;
        public RwListViewModel SelectedRwList
        {
            get { return selectedRwList; }
            set 
            {
                if (selectedRwList != value)
                {
                    selectedRwList = value;
                    NotifyPropertyChanged("SelectedRwList");
                    NotifyPropertyChanged("SelectedRwListReportsMenuItems");

                    LoadRwDocsAsync(true);
                }                
            }
        }

        private void LoadRwDocsAsync(bool _async)
        {
            Action work = () =>
            {
                //RwDocsCollection = null;
                if (selectedRwList != null)
                    Parent.ShellModel.UpdateUi(() => 
                    {
                        rwDocsCollection.Clear();
                        foreach (var d in selectedRwList.RwDocsCollection)
                            rwDocsCollection.Add(new Selectable<RwDocViewModel>(d));
                        //RwDocsCollection = selectedRwList.RwDocsCollection;   
                        CalcRwDocsTotals();
                    }, true, true);
            };
            if (_async)
                Parent.Services.DoAsyncAction(work, null);
            else
                work();
        }

        private ObservableCollection<Selectable<RwDocViewModel>> rwDocsCollection;
        public ObservableCollection<Selectable<RwDocViewModel>> RwDocsCollection
        {
            get { return rwDocsCollection; }
            set { SetAndNotifyProperty("RwDocsCollection", ref rwDocsCollection, value); }
        }

        private Selectable<RwDocViewModel> selectedRwDoc;
        public Selectable<RwDocViewModel> SelectedRwDoc
        {
            get { return selectedRwDoc; }
            set 
            { 
                if (value != null)
                    foreach (var sd in rwDocsCollection.Where(d => d.IsSelected && d != value))
                        sd.IsSelected = false;
                SetAndNotifyProperty("SelectedRwDoc", ref selectedRwDoc, value);
            }
        }

        private ICommand selectAllDocsCommand;

        public ICommand SelectAllDocsCommand
        {
            get 
            {
                if (selectAllDocsCommand == null)
                    selectAllDocsCommand = new DelegateCommand<bool>(ExecuteSelectAllDocsCommand, CanSelectAllDocsCommand); 
                return selectAllDocsCommand; 
            }
        }

        private bool CanSelectAllDocsCommand(bool _param)
        {
            var items = GetAllRwdocsItemsInView();
            return items != null && items.Length > 0;// && items.Any(i => i.IsSelected != !_param);
        }

        private void ExecuteSelectAllDocsCommand(bool _param)
        {
            var items = GetAllRwdocsItemsInView();
            if (items != null && items.Length > 0)
                foreach (var i in items.Where(i => i.IsSelected != _param))
                    i.IsSelected = _param;
        }
        
        private const string UNDOPAYSCOMMAND_COMPONENTNAME = "RwModule.ViewModels.RwListsArcViewModel.UndoPaysCommand";
        [ExportNamedComponent("RwModule.ComponentCommand", UNDOPAYSCOMMAND_COMPONENTNAME)]
        private ICommand undoPaysCommand;
        public ICommand UndoPaysCommand
        {
            get 
            {
                if (undoPaysCommand == null && CheckComponentAccess(UNDOPAYSCOMMAND_COMPONENTNAME))
                    undoPaysCommand = new DelegateCommand(ExecuteUndoPaysCommand, CanUndoPaysCommand);
                return undoPaysCommand; 
            }
        }

        private bool CanUndoPaysCommand()
        {
            return selectedRwList != null && selectedRwList.PayStatus != PayStatuses.Unpayed;
        }

        private void ExecuteUndoPaysCommand()
        {
            Action load = () => 
            {
                try 
                {
                    var pas = LoadPaysArcForSelectedRwList();
                    if (pas != null && pas.Length > 0)
                    {
                        var dlg = new SelectRwPayActionsDlgViewModel(pas)
                        {
                            Title = "Погашения по выбранному перечню\nВыберите отменяемые погашения",
                            IsCancelable = true,
                            OnSubmit = OnSubmitUndoPays
                        };
                        Parent.OpenDialog(dlg);
                    }
                    else
                        Parent.Services.ShowMsg("Результат", "Погашения не найдены", false);

                }
                catch (Exception _e)
                {
                    string title = "Ошибка при загрузке погашений перечня №" + selectedRwList.Num_rwlist.ToString();
                    Parent.Services.ShowMsg(title, _e.Message, true);
                    CommonModule.Helpers.WorkFlowHelper.OnCrash(_e, title + " : " + _e.Message, true);
                }
            };

            Parent.Services.DoWaitAction(load);
        }

        private RwPaysArc[] LoadPaysArcForSelectedRwList()
        {
            RwPaysArc[] res = null;
            
            if (selectedRwList != null)
            {
                using (var db = new RealContext())
                {
                    res = db.RwDocs.Where(d => d.Id_rwlist == selectedRwList.Id_rwlist).SelectMany(d => db.RwPaysArcs.Where(pa => pa.Iddoc == d.Id_rwdoc && pa.Payaction != RwPayActionType.DoVozvrat && pa.Payaction != RwPayActionType.CloseVozvrat)).ToArray();
                }
            }          
            return res;
        }

        private void OnSubmitUndoPays(Object _d)
        {
            Parent.CloseDialog(_d);
            var dlg = _d as SelectRwPayActionsDlgViewModel;
            if (dlg == null) return;
            var sel_pa = dlg.SelectedPayActions;
            Action<WaitDlgViewModel> uAction = wd => UndoSelPayActions(sel_pa, wd);
            Parent.Services.DoWaitAction(uAction, "Подождите", "Отмена выбранных погашений...");
        }

        private void UndoSelPayActions(RwPayActionViewModel[] _sel_pa, WaitDlgViewModel _wd)
        {
            var bh = new RwModule.Helpers.BusinessHelper(Parent.Repository, _wd);

            bool res = false;

            try
            {
                res = bh.UndoSelPayActions(_sel_pa);
            }
            catch (Exception e)
            {
                _wd.Message += "Ошибка";
                Parent.Services.ShowMsg("Ошибка (" + e.GetType().ToString() + ")", e.Message, true);
                CommonModule.Helpers.WorkFlowHelper.OnCrash(e, null, true);
            }

            if (!res)
                Parent.OpenDialog(new SubmitRwSinksDlgViewModel
                {
                    Title = "Ошибка при отмене погашений",
                    PayActions = _sel_pa.ToList()
                });
            else
                Parent.Services.ShowMsg("Результат", "Отмена погашений оплат завершены успешно", false);

            Parent.ShellModel.UpdateUi(RefreshSelectedRwListItem, true, false);
        }

        private void RefreshSelectedRwListItem()
        {
            rwDocsCollection.Clear();
            if (selectedRwList == null) return;

            var selrwl = selectedRwList;

            var lIndex = rwListCollection.IndexOf(selrwl);
            if (lIndex >= 0)
                rwListCollection.RemoveAt(lIndex);
            RwList lFromDb = null;
            
            try
            {
                using (var db = new RealContext())
                {
                    lFromDb = db.RwLists.FirstOrDefault(l => l.Id_rwlist == selrwl.Id_rwlist);
                }
            }
            catch (Exception e)
            {
                Parent.Services.ShowMsg("Ошибка (" + e.GetType().ToString() + ")", e.Message, true);
                CommonModule.Helpers.WorkFlowHelper.OnCrash(e, null, true);
            }

            if (lFromDb != null)
            {
                var newSelRwl = new RwListViewModel(Parent.Repository, lFromDb);
                rwListCollection.Insert(lIndex, newSelRwl);
                SelectedRwList = newSelRwl;
            }

            RefreshAllRwDocs();
            LoadRwDocsAsync(true);
        }        
       
        private const string DELETERWLISTCOMMAND_COMPONENTNAME = "RwModule.ViewModels.RwListsArcViewModel.DeleteRwListCommand";
        [ExportNamedComponent("RwModule.ComponentCommand", DELETERWLISTCOMMAND_COMPONENTNAME)]
        private ICommand deleteRwListCommand;
        public ICommand DeleteRwListCommand
        {
            get 
            {
                if (deleteRwListCommand == null && CheckComponentAccess(DELETERWLISTCOMMAND_COMPONENTNAME))
                    deleteRwListCommand = new DelegateCommand(ExecuteDeleteRwList, CanExecuteDeleteRwList);
                return deleteRwListCommand; 
            }
        }

        private bool CanExecuteDeleteRwList()
        {
            return selectedRwList != null;
        }
        private void ExecuteDeleteRwList()
        {
            var dlg = new MsgDlgViewModel
            {
                Title = "Удаление перечня № " + selectedRwList.Num_rwlist.ToString(),
                Message = "Удалить перечень № " + selectedRwList.Num_rwlist.ToString() + " ?",
                OnSubmit = (d) => 
                {
                    Parent.CloseDialog(d);
                    DoDeleteRwList(selectedRwList);
                },
                IsCancelable = true
            };
            Parent.OpenDialog(dlg);
        }
        private void DoDeleteRwList(RwListViewModel _rwListToDelVm)
        {
            using (var db = new RealContext())
            {
                var rwListToDel = db.RwLists.FirstOrDefault(l => l.Id_rwlist == _rwListToDelVm.Id_rwlist);
                if (rwListToDel != null)
                {
                    bool res = false;
                    try
                    {
                        db.RwLists.Remove(rwListToDel);
                        db.SaveChanges();
                        res = true;
                    }
                    catch (Exception _e)
                    {
                        string title = "Ошибка при удалении перечня №" + selectedRwList.Num_rwlist.ToString();
                        Parent.Services.ShowMsg(title, _e.Message, true);
                        CommonModule.Helpers.WorkFlowHelper.OnCrash(_e, title + " : " + _e.Message, true);
                    }
                    if (res)
                    {
                        rwListCollection.Remove(selectedRwList);
                        rwDocsCollection.Clear();
                        if (rwListCollection.Count == 0)
                            Parent.UnLoadContent(this);
                        selectedRwList = null;
                    }
                }
            }
        }

        private const string SHOWHISTORYCOMMAND_COMPONENTNAME = "RwModule.ViewModels.RwListsArcViewModel.ShowHistoryCommand";
        [ExportNamedComponent("RwModule.ComponentCommand", SHOWHISTORYCOMMAND_COMPONENTNAME)]
        private ICommand showHistoryCommand;
        public ICommand ShowHistoryCommand
        {
            get
            {
                if (showHistoryCommand == null && CheckComponentAccess(SHOWHISTORYCOMMAND_COMPONENTNAME))
                    showHistoryCommand = new DelegateCommand(ExecShowHistoryCommand, () => rwListCollection != null && selectedRwList != null);
                return showHistoryCommand;
            }
        }
        private void ExecShowHistoryCommand()
        {
            Action work = () =>
            {
                var sel = selectedRwList;
                var pnumstr = sel.Num_rwlist.ToString();
                var pHistory = GetRwListHistory(sel.Id_rwlist);
                if (pHistory != null && pHistory.Length > 0)
                    Parent.OpenDialog(new HistoryDlgViewModel(pHistory)
                    {
                        Title = "История перечня № " + pnumstr
                    });
                else
                    Parent.Services.ShowMsg("Результат", "История по перечню № " + pnumstr + "не найдена.", true);
            };
            Parent.Services.DoWaitAction(work, "Подождите", "Запрос истории перечня...");
        }

        private HistoryInfo[] GetRwListHistory(int _idrwlist)
        {
            HistoryInfo[] res = null;

            try
            {
                using (var db = new RealContext())
                {
                    res = db.RwModuleLogs.Where(l => l.Resource == "dbo.RwList" && l.Idres == _idrwlist).OrderBy(h => h.Id)
                                         .Select(h => new HistoryInfo
                                         {
                                             logId = h.Id,
                                             UserId = h.Userid,
                                             StatusDateTime = h.Adatetime,
                                             StatusDescription = h.Description ?? (h.Action == "I" ? "Добавлено" : (h.Action == "U" ? "Изменено" : (h.Action == "D" ? "Удалено" : "Неизвестная операция")))
                                         }).ToArray();
                    foreach (var hr in res)
                    {
                        var user = Parent.Repository.GetUserInfo(hr.UserId);
                        hr.UserName = user != null ? user.Name : hr.UserId.ToString("{0} не найден");
                        hr.FullName = user != null ? user.FullName : hr.UserId.ToString("{0} не найден");
                    }
                }
            }
            catch (Exception e)
            {
                Parent.Services.ShowMsg("Ошибка (" + e.GetType().ToString() + ")", e.Message, true);
                CommonModule.Helpers.WorkFlowHelper.OnCrash(e, null, true);
            }

            return res;
        }        

        private const string LINKESFNCOMMAND_COMPONENTNAME = "RwModule.ViewModels.RwListsArcViewModel.LinkESFNCommand";
        [ExportNamedComponent("RwModule.ComponentCommand", LINKESFNCOMMAND_COMPONENTNAME)]
        private ICommand linkESFNCommand;
        public ICommand LinkESFNCommand
        {
            get
            {
                if (linkESFNCommand == null && CheckComponentAccess(LINKESFNCOMMAND_COMPONENTNAME))
                    linkESFNCommand = new DelegateCommand(ExecuteLinkESFN, CanExecuteLinkESFN);
                return linkESFNCommand;
            }
        }

        private bool CanExecuteLinkESFN()
        {
            return rwDocsCollection != null && rwDocsCollection.Count > 0;
        }

        private void ExecuteLinkESFN()
        {
            var docsInView = GetAllRwdocsItemsInView();
            var sdocs = (docsInView.Any(d => d.IsSelected) ? docsInView.Where(d => d.IsSelected) : docsInView).ToArray();
            if (sdocs.Length == 0) return;            

            if (sdocs.Length > 1)
            {
                var askDlg = new MsgDlgViewModel
                {
                    Title = "Внимание",
                    Message = String.Format("Выбрано позиций {0} из {1}\nПродолжить?", sdocs.Length, docsInView.Length),
                    IsCancelable = true,
                    OnSubmit = d =>
                    {
                        Parent.CloseDialog(d);
                        OpenLinkDialog(sdocs);
                    }
                };
                Parent.OpenDialog(askDlg);
            }
            else
                OpenLinkDialog(sdocs);            
        }

        private void OpenLinkDialog(Selectable<RwDocViewModel>[] _docs)
        {
            if (_docs == null || _docs.Length == 0) return;

            Action<WaitDlgViewModel> work = (wd) =>
            {
                var ndlg = new LinkRwDocsToEsfnDlgViewModel(Parent)
                {
                    OnClosed = d =>
                    {
                        if (((LinkRwDocsToEsfnDlgViewModel)d).IsWorkMade) RefreshRwDocItems(_docs);
                    }
                };
                ndlg.LoadData(_docs.Select(d => d.Value).ToArray(), wd);
                Parent.OpenDialog(ndlg);
            };
            
            Parent.Services.DoWaitAction(work, "Подождите", "Проверка и настройка привязок сборов к ЭСФН...");
        }

        private Selectable<RwDocViewModel>[] GetAllRwdocsItemsInView()
        {
            Selectable<RwDocViewModel>[] res = new Selectable<RwDocViewModel>[0];
            var view = System.Windows.Data.CollectionViewSource.GetDefaultView(rwDocsCollection);
            if (view != null)
                res = view.OfType<Selectable<RwDocViewModel>>().ToArray();
            return res;
        }

        private const string EDITRWLISTCOMMAND_COMPONENTNAME = "RwModule.ViewModels.RwListsArcViewModel.EditRwListCommand";
        [ExportNamedComponent("RwModule.ComponentCommand", EDITRWLISTCOMMAND_COMPONENTNAME)]
        private ICommand editRwListCommand;
        public ICommand EditRwListCommand
        {
            get
            {
                if (editRwListCommand == null && CheckComponentAccess(EDITRWLISTCOMMAND_COMPONENTNAME))
                    editRwListCommand = new DelegateCommand(ExecuteEditRwList, CanExecuteEditRwList);
                return editRwListCommand;
            }
        }
        private bool CanExecuteEditRwList()
        {
            return selectedRwList != null;
        }
        private void ExecuteEditRwList()
        {
            var dlg = new EditRwListInfoDlgViewModel(selectedRwList)
            {
                Title = "Изменение данных перечня № " + selectedRwList.Num_rwlist.ToString(),
                OnSubmit = DoEditRwList
            };            
            Parent.OpenDialog(dlg);
        }

        private const string EDITRWDOCCOMMAND_COMPONENTNAME = "RwModule.ViewModels.RwListsArcViewModel.EditRwDocCommand";
        [ExportNamedComponent("RwModule.ComponentCommand", EDITRWDOCCOMMAND_COMPONENTNAME)]
        private ICommand editRwDocCommand;
        public ICommand EditRwDocCommand
        {
            get 
            {
                if (editRwDocCommand == null && CheckComponentAccess(EDITRWDOCCOMMAND_COMPONENTNAME))
                    editRwDocCommand = new DelegateCommand(ExecuteEditRwDoc, CanExecuteEditRwDoc);
                return editRwDocCommand; 
            }
        }
        private bool CanExecuteEditRwDoc()
        {
            return selectedRwDoc != null;
        }
        private void ExecuteEditRwDoc()
        {
            var selDocsA = rwDocsCollection.Where(d => d.IsSelected).Select(sd => sd.Value).ToArray();            
            var eDlg = new EditRwDocInfoDlgViewModel(selDocsA)
            {
                Title = "Изменение данных документа № " + selDocsA[0].Num_doc,
                OnSubmit = DoEditRwDoc
            };
            
            Parent.OpenDialog(eDlg);
        }        
       
        private void DoEditRwList(Object _dlg)
        {
            var dlg = _dlg as EditRwListInfoDlgViewModel;
            Parent.CloseDialog(_dlg);
            if (dlg == null) return;
            DateTime? nRep_Date = dlg.RepDate;
            bool isTrans = dlg.CanBeTransition && dlg.IsTransition;
            int idDog = dlg.SelDogovor != null ? dlg.SelDogovor.IdDog : 0;
            DateTime? nAccept_Date = dlg.AcceptDate;
            DateTime? nOplTo_Date = dlg.OplToDate;
            DateTime nOrc_Date = dlg.OrcDate;

            Action work = () =>
            {
                using (var db = new RealContext())
                {
                    var entry = db.Entry(selectedRwList.ModelRef);
                    if (selectedRwList.Transition != isTrans)
                    {
                        selectedRwList.Transition = isTrans;
                        entry.State = System.Data.Entity.EntityState.Modified;
                    }
                    if (selectedRwList.ModelRef.Iddog != idDog)
                    {
                        selectedRwList.ModelRef.Iddog = idDog;
                        entry.State = System.Data.Entity.EntityState.Modified;
                    }
                    if ((nAccept_Date.HasValue || selectedRwList.Dat_accept.HasValue) && nAccept_Date != selectedRwList.Dat_accept)
                    {
                        selectedRwList.Dat_accept = nAccept_Date;
                        selectedRwList.UserAccept = Parent.ShellModel.CurrentUserInfo;
                        entry.State = System.Data.Entity.EntityState.Modified;
                    }
                    if ((nOplTo_Date.HasValue || selectedRwList.Dat_oplto.HasValue) && nOplTo_Date != selectedRwList.Dat_oplto)
                    {
                        selectedRwList.Dat_oplto = nOplTo_Date;
                        entry.State = System.Data.Entity.EntityState.Modified;
                    }
                    if (nOrc_Date != selectedRwList.Dat_orc)
                    {
                        selectedRwList.Dat_orc = nOrc_Date;
                        entry.State = System.Data.Entity.EntityState.Modified;
                    }
                    if (nRep_Date.HasValue)
                        foreach (var doc in selectedRwList.RwDocsCollection)
                        {
                            doc.Rep_date = nRep_Date;
                            db.Entry(doc.ModelRef).State = System.Data.Entity.EntityState.Modified;
                            //var oldRwDoc = db.RwDocs.FirstOrDefault(d => d.Id_rwdoc == doc.Id_rwdoc);
                            //oldRwDoc.Rep_date = nRep_Date;
                        }
                    db.SaveChanges();
                }
            };
            Action after = () =>
            {
                RefreshAllRwDocs();
                LoadRwDocsAsync(true);
            };
            Parent.Services.DoWaitAction(work, "Подождите", "Сохранение изменений", after);
        }

        private void RefreshAllRwDocs()
        {
            if (selectedRwList != null)
                selectedRwList.LoadRwDocs(true);
            //RwDocsCollection = selectedRwList == null ? null : selectedRwList.RwDocsCollection;
        }

        private void DoEditRwDoc(Object _dlg)
        {
            var dlg = _dlg as EditRwDocInfoDlgViewModel;
            Parent.CloseDialog(_dlg);
            if (dlg == null) return;
            
            var selRwDoc = selectedRwDoc.Value;  
            var isRepDate = dlg.IsRepDateEdEnabled;
            DateTime? nRep_Date = dlg.RepDate;
            var isDatDoc = dlg.IsDatDocEdEnabled;
            DateTime? nDoc_Date = dlg.DatDoc;
            var isDatZkrt = dlg.IsDatZKrtEdEnabled;
            DateTime? nDzkrt = dlg.DatZKrt;
            var isNum_doc = dlg.IsNum_docEdEnabled;
            string nDoc_Num = String.IsNullOrWhiteSpace(dlg.Num_doc) ? null : dlg.Num_doc.Trim();
            var isNkrt = dlg.IsNkrtEdEnabled;
            string nNkrt = String.IsNullOrWhiteSpace(dlg.Nkrt) ? null : dlg.Nkrt.Trim();
            bool isSumDoc = dlg.IsSumDocEdEnabled;
            decimal nSumDoc = dlg.Sum_doc;
            bool isSumNds = dlg.IsSumNdsEdEnabled;
            decimal nSumNds = dlg.Sum_nds;
            var isIsExclude = dlg.IsExcludeEdEnabled;
            bool nExclude = dlg.IsExclude;
            //var isSum_excl = dlg.IsSumExclEdEnabled;
            decimal nSumExcl = dlg.Sum_excl;
            var isExcl_info = dlg.IsExclInfoEdEnabled;
            string nExclInfo = String.IsNullOrWhiteSpace(dlg.Excl_info) ? null : dlg.Excl_info.Trim();
            var isComments = dlg.IsCommentsEdEnabled;
            string nComments = String.IsNullOrWhiteSpace(dlg.Comments) ? null : dlg.Comments.Trim();

            List<Selectable<RwDocViewModel>> updatedDocs = new List<Selectable<RwDocViewModel>>();

            bool isMulti = dlg.IsMultipleEdit;

            if (dlg.IsChanged)
            {
                Action work = () => 
                {
                    bool update = false;
                    using (var db = new RealContext())
                    {
                        var selrwds = rwDocsCollection.Where(d => d.IsSelected).ToArray();
                        var oDocSum_excl = selrwds.Sum(sd => sd.Value.Sum_excl);
                        foreach (var ssrwd in selrwds)
                        {
                            var srwd = ssrwd.Value;
                            bool uRwDoc = false;
                            if (isSumDoc && srwd.Sum_doc != nSumDoc)
                            {
                                srwd.Sum_doc = nSumDoc;
                                uRwDoc = true;

                            }
                            if (isSumNds && srwd.Sum_nds != nSumNds)
                            {
                                srwd.Sum_nds = nSumNds;
                                uRwDoc = true;

                            }
                            if (isIsExclude && srwd.Exclude != nExclude)
                            {
                                srwd.Exclude = nExclude;
                                uRwDoc = true;
                                if (isMulti)
                                    srwd.Sum_excl = nExclude ? srwd.Sum_itog : 0;
                            }
                            if (isRepDate && srwd.Rep_date != nRep_Date && nRep_Date != null)
                            {
                                srwd.Rep_date = nRep_Date;
                                uRwDoc = true;
                            }
                            if (isDatDoc && nDoc_Date != null && srwd.Dat_doc != nDoc_Date.Value)
                            {
                                srwd.Dat_doc = nDoc_Date.Value;
                                uRwDoc = true;
                            }
                            if (isDatZkrt && nDzkrt != null && srwd.Dzkrt != nDzkrt.Value)
                            {
                                srwd.Dzkrt = nDzkrt.Value;
                                uRwDoc = true;
                            }

                            if (!isMulti)
                            {
                                if (srwd.Sum_excl != nSumExcl)
                                {
                                    srwd.Sum_excl = nSumExcl;
                                    uRwDoc = true;
                                }
                            }
                            
                            if (isExcl_info && srwd.Excl_info != nExclInfo)
                            {
                                srwd.Excl_info = nExclInfo;
                                uRwDoc = true;
                            }
                            if (isNum_doc && srwd.Num_doc != nDoc_Num)
                            {
                                srwd.Num_doc = nDoc_Num;
                                uRwDoc = true;
                            }
                            if (isNkrt && srwd.Nkrt != nNkrt)
                            {
                                srwd.Nkrt = nNkrt;
                                uRwDoc = true;
                            }
                            if (isComments && srwd.Comments != nComments)
                            {
                                srwd.Comments = nComments;
                                uRwDoc = true;
                            }
                            if (uRwDoc)
                            {
                                db.Entry(srwd.ModelRef).State = System.Data.Entity.EntityState.Modified;
                                update = true;
                                updatedDocs.Add(ssrwd);
                            }
                        }

                        if (isIsExclude && oDocSum_excl != nSumExcl)
                        {
                            var xdocs = db.RwDocs.Where(d => d.Id_rwlist == selectedRwList.Id_rwlist && d.Exclude);
                            var lSumExcl = xdocs.Any() ? xdocs.Sum(d => d.Sum_excl) : 0;
                            var nlSumExcl = lSumExcl - oDocSum_excl + nSumExcl;
                            if (selectedRwList.Sum_itog > 0)
                            {
                                if (nlSumExcl > selectedRwList.Sum_itog) nlSumExcl = selectedRwList.Sum_itog;
                                else
                                    if (nlSumExcl < 0) nlSumExcl = 0;
                            }
                            else
                            {
                                if (nlSumExcl < selectedRwList.Sum_itog) nlSumExcl = selectedRwList.Sum_itog;
                                else
                                    if (nlSumExcl > 0) nlSumExcl = 0;
                            }
                            selectedRwList.Sum_excl = nlSumExcl;
                            db.Entry(selectedRwList.ModelRef).State = System.Data.Entity.EntityState.Modified;
                            update = true;
                        }
                        if (update)
                            db.SaveChanges();                        
                    }
                };
                Action after = () =>
                {
                    RefreshRwDocItems(updatedDocs);
                    //if (refreshRwList)
                        //RefreshRwDocItem
                };
                Parent.Services.DoWaitAction(work, "Подождите", "Сохранение изменений", after);
            }
        }

        private void RefreshRwDocItems(IEnumerable<Selectable<RwDocViewModel>> _items)
        {
            foreach (var i in _items)
                RefreshRwDocItem(i);
        }   

        private void RefreshRwDocItem(Selectable<RwDocViewModel> _item)
        {
            RwDoc newRwDoc = null;
            using (var db = new DAL.RealContext())
            {
                newRwDoc = db.RwDocs.Include(d => d.Esfn).FirstOrDefault(d => d.Id_rwdoc == _item.Value.Id_rwdoc);
            }
            if (newRwDoc == null)
            {
                Parent.Services.ShowMsg("Ошибка", "Документ не найден в базе данных.", true);
                return;
            }
            
            Parent.ShellModel.UpdateUi(()=>
            {
                //var view = System.Windows.Data.CollectionViewSource.GetDefaultView(rwDocsCollection) as System.Windows.Data.ListCollectionView;
                //System.ComponentModel.SortDescription[] sorts = null;
                //if (view.SortDescriptions != null && view.SortDescriptions.Count > 0)
                //{
                //    sorts = view.SortDescriptions.ToArray();
                //    view.SortDescriptions.Clear();
                //}
                
                newRwDoc.RwList = selectedRwList.ModelRef;
                var newItem = new Selectable<RwDocViewModel>(new RwDocViewModel(newRwDoc));                
                var oldInd = rwDocsCollection.IndexOf(_item);
                selectedRwList.ModelRef.RwDocs[oldInd] = newRwDoc;
                rwDocsCollection.RemoveAt(oldInd);
                rwDocsCollection.Insert(oldInd, newItem);
                if (SelectedRwDoc == _item)
                    SelectedRwDoc = newItem;

                //if (sorts != null)
                //    view.SortDescriptions.AddRange(sorts);
                //view.Refresh();
            }, true, false);
            
        }

        private List<ICommandInterface> reports;
        public List<ICommandInterface> Reports
        {
            get
            {
                if (reports == null)
                {
                    reports = new List<ICommandInterface>();
                    ICommandInterface curReport = null;
                    if((curReport = ChkTransitionReport.TryCreate(this)) != null)
                        reports.Add(curReport);
                    if ((curReport = ExclDocsReport.TryCreate(this)) != null)
                        reports.Add(curReport);
                }
                return reports;
            }
        }

        private List<Selectable<IModelFilter<RwDoc>>> rwDocFilters;
        public List<Selectable<IModelFilter<RwDoc>>> RwDocFilters { get { return rwDocFilters; } }

        private void PrepareFilters(IEnumerable<IModelFilter<RwDoc>> _rwDocFilters)
        {
            rwDocFilters = new List<Selectable<IModelFilter<RwDoc>>>(_rwDocFilters.Select(f => new Selectable<IModelFilter<RwDoc>>(f, true)));
            foreach(var sf in rwDocFilters)
                sf.PropertyChanged += FilterChanged;
        }

        void FilterChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            SetFilter();
        }

        private Predicate<Object> RwDocFilter 
        { 
            get 
            { 
                if (rwDocFilters == null || !rwDocFilters.Any(sf => sf.IsSelected))
                    return  null;
                else
                    return dvm => rwDocFilters.Where(sf => sf.IsSelected).All(sf => sf.Value.Filter(((Selectable<RwDocViewModel>)dvm).Value.ModelRef));
            } 
        }

        public void SetFilter()
        {
            var rwdView = System.Windows.Data.CollectionViewSource.GetDefaultView(RwDocsCollection);
            if (rwdView.CanFilter)
            {
                rwdView.Filter = RwDocFilter;
                rwdView.Refresh();
                CalcRwDocsTotals();
            }
        }

        private bool isNeedTotals = false;
        public bool IsNeedTotals
        { 
            get { return isNeedTotals; }
            set 
            {
                isNeedTotals = value;
                NotifyPropertyChanged("IsNeedTotals");
                CalcRwDocsTotals();
            }
        }

        private bool isAdvTotals = false;
        public bool IsAdvTotals
        {
            get { return isAdvTotals; }
            set
            {
                isAdvTotals = value;
                NotifyPropertyChanged("IsAdvTotals");
                CalcRwDocsTotals();
            }
        }

        private List<RwDocViewModel> rwDocsTotals;
        public List<RwDocViewModel> RwDocsTotals
        {
            get { return rwDocsTotals; }
            set { SetAndNotifyProperty("RwDocsTotals", ref rwDocsTotals, value); }            
        }
        
        private void CalcRwDocsTotalsAsync()
        {
           Parent.Services.DoAsyncAction(CalcRwDocsTotals, null);
        }

        private void CalcRwDocsTotals()
        {
            List<RwDocViewModel> res = null;
            if (isNeedTotals)
            {
                var rwdView = System.Windows.Data.CollectionViewSource.GetDefaultView(RwDocsCollection).OfType<Selectable<RwDocViewModel>>().Select(d => d.Value).ToArray();
                if (isAdvTotals)
                {
                    var krtsbrTotals = rwdView.GroupBy(d => new { d.Nkrt, d.RwPay.Paycode }).Select(g => new RwDoc { Nkrt = g.Key.Nkrt, Paycode = g.Key.Paycode, Sum_doc = g.Sum(i => i.Sum_doc), Sum_nds = g.Sum(i => i.Sum_nds) });
                    var krtTotals = krtsbrTotals.GroupBy(t => t.Nkrt).Select(g => new RwDoc { Nkrt = g.Key, Paycode = 0, Sum_doc = g.Sum(i => i.Sum_doc), Sum_nds = g.Sum(i => i.Sum_nds) });
                    res = krtTotals.Union(krtsbrTotals).OrderBy(t => Parser.GetIntFromString(t.Nkrt)).ThenBy(t => t.Paycode).Select(t => new RwDocViewModel(t)).ToList();
                    res.ForEach(i => { if (i.ModelRef.Paycode != 0) i.ModelRef.Nkrt = ""; });
                }
                else
                    res = rwdView.GroupBy(t => t.Nkrt)
                                 .Select(g => new RwDocViewModel(new RwDoc { Nkrt = g.Key, Paycode = 0, Sum_doc = g.Sum(i => i.Sum_doc), Sum_nds = g.Sum(i => i.Sum_nds) }))
                                 .OrderBy(i => Parser.GetIntFromString(i.Nkrt))
                                 .ToList();
                Parent.ShellModel.UpdateUi(() => RwDocsTotals = res, true, true);
            }
            else
            {
                if (rwDocsTotals != null) Parent.ShellModel.UpdateUi(() => RwDocsTotals = null, true, true);
            }
        }                

        private const string SHOWOTGRCOMMAND_COMPONENTNAME = "RwModule.ViewModels.RwListsArcViewModel.ShowOtgrCommand";
        [ExportNamedComponent("RwModule.ComponentCommand", SHOWOTGRCOMMAND_COMPONENTNAME)]
        private ICommand showOtgrCommand;
        public ICommand ShowOtgrCommand
        {
            get
            {
                if (showOtgrCommand == null && CheckComponentAccess(SHOWOTGRCOMMAND_COMPONENTNAME))
                    showOtgrCommand = new DelegateCommand(ExecShowOtgr, CanShowOtgr);
                return showOtgrCommand;
            }
        }
        private bool CanShowOtgr()
        {
            return selectedRwList != null && selectedRwList.RwlType == RwUslType.Provoz && otgruzModule != null;
        }
        private void ExecShowOtgr()
        {
            List<DataObjects.OtgrLine> otgrs = new List<DataObjects.OtgrLine>();
            Action work = () => 
            {                
                IEnumerable<RwDocViewModel> docs = ((rwDocsCollection == null || rwDocsCollection.All(d => !d.IsSelected)) ? selectedRwList.RwDocsCollection : rwDocsCollection.Where(d => d.IsSelected).Select(d => d.Value)) //Enumerable.Repeat(selectedRwDoc.Value,1))
                                                   .Where(d => d.RwPay.IdUslType == RwUslType.Provoz).DistinctBy(d => d.Num_doc);
                foreach (var d in docs)
                {
                    var odata = Parent.Repository.GetOtgrArc(new DataObjects.SeachDatas.OtgruzSearchData { RwBillNumber = d.Num_doc, Transportid = 3, Dfrom = d.Dat_doc, Dto = d.Dat_doc});
                    if (odata != null && odata.Length > 0)
                        otgrs.AddRange(odata);
                }                    
            };
            Action afterwork = () =>
            {
                if (otgrs.Count > 0)
                {
                    otgruzModule.ShowOtgrArc(otgrs);
                    Parent.ShellModel.LoadModule(otgruzModule);
                }
                else
                    Parent.Services.ShowMsg("Результат", "Данные по отгрузке не найдены.", true);
            };

            Parent.Services.DoWaitAction(work, "Подождите", "Выборка из архива принятой отгрузки.", afterwork);
        }        

        public MenuItemViewModel[] SelectedRwListReportsMenuItems
        {
            get { return GetSelectedRwListReportsMenuItems(); }
        }

        private MenuItemViewModel[] GetSelectedRwListReportsMenuItems()
        {
            if (selectedRwList == null) return null;
            MenuItemViewModel[] res = null;
            var reps = GetRwListReports(selectedRwList.Id_rwlist);
            if (reps != null && reps.Length > 0)
                res = reps.Select(r => new MenuItemViewModel(r.Title, r.Description, new LabelCommand(() => OpenReport(r)))).ToArray();
            return res;
        }

        private void OpenReport(ReportModel _rep)
        {
            var newcontent = new ReportViewModel(Parent, _rep);
            newcontent.TryOpen();
        }

        private Dictionary<int, ReportModel[]> rwListReports = new Dictionary<int, ReportModel[]>();

        private ReportModel[] GetRwListReports(int _id_rwlist)
        {
            if (!rwListReports.ContainsKey(_id_rwlist))
            {
                ReportModel[] res = null;
                using (var db = new DAL.RealContext())
                {
                    res = db.GetRwListReports(selectedRwList.Id_rwlist);
                    if (res != null && res.Length > 0)
                        foreach (var r in res)
                            r.Parameters["idrwlist"] = selectedRwList.Id_rwlist.ToString();
                }
                rwListReports[_id_rwlist] = res;
            }
            return rwListReports[_id_rwlist];
        }
    }
}
