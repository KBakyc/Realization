using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using CommonModule.Commands;
using CommonModule.Interfaces;
using CommonModule.ViewModels;
using DataObjects;
using CommonModule.ModuleServices;
using CommonModule.DataViewModels;
using DataObjects.SeachDatas;
using System.Collections.ObjectModel;
using RwModule.Models;
using CommonModule.Helpers;
using DAL;
using DotNetHelper;
using CommonModule.Composition;


namespace RwModule.ViewModels
{
    /// <summary>
    /// Модель режима просмотра архива принятых банковских платежей по ЖД услугам.
    /// </summary>
    public class RwPlatsArcViewModel : BasicModuleContent
    {        
        public RwPlatsArcViewModel(IModule _parent, IEnumerable<RwPlat> _rwPlats)
            :base(_parent)
        {            
            loadMode = LoadMode.ByData;
            rwPlatsList = new ObservableCollection<Selectable<RwPlatViewModel>>(_rwPlats.Select(p => new Selectable<RwPlatViewModel>(new RwPlatViewModel(p))));
            Init();
        }

        private string[] paramInfos;
        public string[] ParamInfos
        {
            get { return paramInfos; }
            set { paramInfos = value; }
        }


        private void Init()
        {            
            RefreshCommand = new DelegateCommand(RefreshData);
        }

        public void LoadData(IEnumerable<RwPlat> _rwp)
        {
            var selRwp = selectedRwPlat;

            if (rwPlatsList == null)
                rwPlatsList = new ObservableCollection<Selectable<RwPlatViewModel>>();
            else
                rwPlatsList.Clear();

            if (_rwp != null && _rwp.Any())
                foreach (var p in _rwp)
                    rwPlatsList.Add(new Selectable<RwPlatViewModel>(new RwPlatViewModel(p)));
            if (selRwp != null)
                SelectedRwPlat = rwPlatsList.FirstOrDefault(p => p.Value.Idrwplat == selRwp.Value.Idrwplat);
        }

        private LoadMode loadMode;
        public LoadMode RwPlatsLoadMode
        {
            get { return loadMode; }
        }

        private Selectable<RwPlatViewModel> selectedRwPlat;
        public Selectable<RwPlatViewModel> SelectedRwPlat
        {
            get { return selectedRwPlat; }
            set { SetAndNotifyProperty("SelectedRwPlat", ref selectedRwPlat, value); }
        }

        private ObservableCollection<Selectable<RwPlatViewModel>> rwPlatsList;
        public ObservableCollection<Selectable<RwPlatViewModel>> RwPlatsList
        {
            get { return rwPlatsList; }
            set { SetAndNotifyProperty("RwPlatsList", ref rwPlatsList, value); }
        }

        private const string UNDOPAYSCOMMAND_COMPONENTNAME = "RwModule.ViewModels.RwPlatsArcViewModel.UndoPaysCommand";
        [ExportNamedComponent("RwModule.ComponentCommand", UNDOPAYSCOMMAND_COMPONENTNAME)]
        private ICommand undoPaysCommand;
        public ICommand UndoPaysCommand
        {
            get 
            {
                if (undoPaysCommand == null && CheckComponentAccess(UNDOPAYSCOMMAND_COMPONENTNAME))
                    undoPaysCommand = new DelegateCommand(ExecUndoPaysCommand, CanUndoPaysCommand);
                return undoPaysCommand;
            }
        }

        private bool CanUndoPaysCommand()
        {
            return selectedRwPlat != null && selectedRwPlat.Value.Ostatok != selectedRwPlat.Value.Sumplat;
        }

        private void ExecUndoPaysCommand()
        {
            Action load = () =>
            {
                try
                {
                    var pas = LoadPaysArcForSelectedRwPlat();
                    if (pas != null && pas.Length > 0)
                    {
                        var dlg = new SelectRwPayActionsDlgViewModel(pas)
                        {
                            Title = "Погашения выбранной оплаты\nВыберите отменяемые погашения",
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
                    string title = "Ошибка при загрузке погашений оплаты №" + selectedRwPlat.Value.Numplat.ToString();
                    Parent.Services.ShowMsg(title, _e.Message, true);
                    CommonModule.Helpers.WorkFlowHelper.OnCrash(_e, title + " : " + _e.Message, true);
                }
            };

            Parent.Services.DoWaitAction(load);
        }

        private RwPaysArc[] LoadPaysArcForSelectedRwPlat()
        {
            RwPaysArc[] res = null;

            if (selectedRwPlat != null)
            {
                var selPlat = selectedRwPlat.Value;
                using (var db = new RealContext())
                {
                    if (selPlat.Direction == RwPlatDirection.In) // возврат
                        res = db.RwPaysArcs.Where(pa => pa.Iddoc == selPlat.Idrwplat && (pa.Payaction == RwPayActionType.DoVozvrat || pa.Payaction == RwPayActionType.CloseVozvrat)).ToArray();
                    else
                        res = db.RwPaysArcs.Where(pa => pa.Idrwplat == selPlat.Idrwplat && pa.Payaction != RwPayActionType.DoVozvrat && pa.Payaction != RwPayActionType.CloseVozvrat).ToArray();
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

            Parent.ShellModel.UpdateUi(RefreshSelectedItemAction, true, false);
        }

        private const string EDITRWPLATCOMMAND_COMPONENTNAME = "RwModule.ViewModels.RwPlatsArcViewModel.EditRwPlatCommand";
        [ExportNamedComponent("RwModule.ComponentCommand", EDITRWPLATCOMMAND_COMPONENTNAME)]         
        private ICommand editRwPlatCommand;
        public ICommand EditRwPlatCommand
        {
            get 
            {
                if (editRwPlatCommand == null && CheckComponentAccess(EDITRWPLATCOMMAND_COMPONENTNAME))
                    editRwPlatCommand = new DelegateCommand(ExecEditRwPlatCommand, CanEditRwPlatCommand);
                return editRwPlatCommand;
            }
        }

        private void ExecEditRwPlatCommand()
        {
            
            Action work = () =>
            {
                RwPlat updSel = null;
                var selRwplat = selectedRwPlat.Value.GetModel();
                using(var db = new RealContext())
                {
                    updSel = db.RwPlats.SingleOrDefault(p => p.Idrwplat == selRwplat.Idrwplat);
                }
                Parent.ShellModel.UpdateUi(() => 
                    {
                        RefreshSelectedItemByModel(updSel);
                        if (selectedRwPlat != null)
                        {
                            var nDlg = new EditRwPlatDlgViewModel(Parent.Repository, selectedRwPlat.Value)
                            {
                                Title = "Изменение данных платежа",
                                OnSubmit = EditRwPlatSubmit
                            };
                            nDlg.SwitchAllTo(false);
                            nDlg.IsWhatforEdEnabled = nDlg.IsNotesEdEnabled = true;
                            Parent.OpenDialog(nDlg);
                        }
                    
                    }, true, false);
            };
            Parent.Services.DoWaitAction(work, "Подождите", "Загрузка...");
        }

        private bool CanEditRwPlatCommand()
        {
            return !IsReadOnly && Parent != null && rwPlatsList != null && SelectedRwPlat != null;
        }

        private const string DELETERWPLATCOMMAND_COMPONENTNAME = "RwModule.ViewModels.RwPlatsArcViewModel.DeleteRwPlatCommand";
        [ExportNamedComponent("RwModule.ComponentCommand", DELETERWPLATCOMMAND_COMPONENTNAME)]         
        private ICommand deleteRwPlatCommand;
        public ICommand DeleteRwPlatCommand
        {
            get 
            {
                if (deleteRwPlatCommand == null && CheckComponentAccess(DELETERWPLATCOMMAND_COMPONENTNAME))
                    deleteRwPlatCommand = new DelegateCommand(ExecDeleteRwPlat, CanExecDeleteRwPlat);
                return deleteRwPlatCommand;
            }
        }
        private void ExecDeleteRwPlat()
        {
            var nDlg = new MsgDlgViewModel()
            {
                Title = "Подтверждение",
                Message = "Удалить выбранный платёж?",
                OnSubmit = DeleteRwPlat
            };
            Parent.OpenDialog(nDlg);
        }
        private bool CanExecDeleteRwPlat()
        {
            return !IsReadOnly && Parent != null && rwPlatsList != null 
                && selectedRwPlat != null                 
                && selectedRwPlat.Value.Sumplat == selectedRwPlat.Value.Ostatok;
        }

        private void DeleteRwPlat(Object obj)
        {
            Parent.CloseDialog(obj);

            var selPlat = selectedRwPlat.Value.GetModel();

            using (var db = new RealContext())
            {
                var plat = db.RwPlats.Attach(selPlat);//.FirstOrDefault(l => l.Id_rwlist == _rwListToDelVm.Id_rwlist);
                bool res = false;
                try
                {
                    db.RwPlats.Remove(plat);
                    db.SaveChanges();
                    res = true;
                }
                catch (Exception _e)
                {
                    string title = "Ошибка при удалении платежа №" + selPlat.Numplat.ToString();
                    Parent.Services.ShowMsg(title, _e.Message, true);
                    CommonModule.Helpers.WorkFlowHelper.OnCrash(_e, title + " : " + _e.Message, true);
                }
                if (res)
                {
                    rwPlatsList.Remove(selectedRwPlat);
                    if (rwPlatsList.Count == 0)
                        Parent.UnLoadContent(this);
                    selectedRwPlat = null;
                }
            }            
        }

        private void EditRwPlatSubmit(Object _dlg)
        {
            Parent.CloseDialog(_dlg);
            var dlg = _dlg as EditRwPlatDlgViewModel;
            if (dlg == null || !dlg.IsValid()) return;

            var model = selectedRwPlat.Value.GetModel();      
            string nWhatfor = dlg.IsWhatforEdEnabled ? dlg.Whatfor : model.Whatfor;
            string nNotes = dlg.IsNotesEdEnabled ? dlg.Notes : model.Notes;

            Action work = () =>
            {
                bool isdirty = false;
                if (model.Whatfor != nWhatfor)
                {
                    model.Whatfor = nWhatfor;
                    isdirty = true;
                }
                if (model.Notes != nNotes)
                {
                    model.Notes = nNotes;
                    isdirty = true;
                }
                if (isdirty)
                {
                    DoRwPlatUpdate(model);
                }               
            };
            Parent.Services.DoWaitAction(work, "Подождите", "Сохранение изменений");
        }       

        private void DoRwPlatUpdate(RwPlat _p)
        {
            bool res = false;
            using (var db = new RealContext())
            {
                
                try
                {
                    db.Entry(_p).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                    res = true;
                }
                catch (Exception _e)
                {
                    string title = "Ошибка при обновлении платежа №" + _p.Numplat.ToString();
                    Parent.Services.ShowMsg(title, _e.Message, true);
                    CommonModule.Helpers.WorkFlowHelper.OnCrash(_e, title + " : " + _e.Message, true);
                }                               
            }
            if (res)
                Parent.ShellModel.UpdateUi(() => RefreshSelectedItemByModel(_p), true, false);
        }

        private void RefreshSelectedItemAction()
        {
            var sel = selectedRwPlat;
            if (sel == null || sel.Value.Idrwplat == 0) return;

            RwPlat fromDb = null;

            try
            {
                using (var db = new RealContext())
                {
                    fromDb = db.RwPlats.FirstOrDefault(p => p.Idrwplat == sel.Value.Idrwplat);
                }
            }
            catch (Exception e)
            {
                Parent.Services.ShowMsg("Ошибка (" + e.GetType().ToString() + ")", e.Message, true);
                CommonModule.Helpers.WorkFlowHelper.OnCrash(e, null, true);
            }

            RefreshSelectedItemByModel(fromDb);
        }

        private void RefreshSelectedItemByModel(RwPlat _newModel)
        {
            int indOfSelected = rwPlatsList.IndexOf(selectedRwPlat);
            rwPlatsList.RemoveAt(indOfSelected);
            selectedRwPlat = null;
            if (_newModel != null)
            {
                var newPlViewModel = new Selectable<RwPlatViewModel>(new RwPlatViewModel(_newModel), true);
                rwPlatsList.Insert(indOfSelected, newPlViewModel);
                SelectedRwPlat = newPlViewModel;
            }
        }

        private const string SHOWHISTORYCOMMAND_COMPONENTNAME = "RwModule.ViewModels.RwPlatsArcViewModel.ShowHistoryCommand";
        [ExportNamedComponent("RwModule.ComponentCommand", SHOWHISTORYCOMMAND_COMPONENTNAME)]
        private ICommand showHistoryCommand;
        public ICommand ShowHistoryCommand
        {
            get
            { 
                if (showHistoryCommand == null && CheckComponentAccess(SHOWHISTORYCOMMAND_COMPONENTNAME))
                    showHistoryCommand = new DelegateCommand(ExecShowHistory, () => rwPlatsList != null && selectedRwPlat != null);
                return showHistoryCommand;
            }
        }
        private void ExecShowHistory()
        {
            Action work = () =>
            {
                var selrwplat = selectedRwPlat.Value;
                var pnumstr = selrwplat.Numplat.ToString();
                var pHistory = GetRwPlatHistory(selrwplat.Idrwplat);
                if (pHistory != null && pHistory.Length > 0)
                    Parent.OpenDialog(new HistoryDlgViewModel(pHistory)
                    {
                        Title = "История платежа № " + pnumstr
                    });
                else
                    Parent.Services.ShowMsg("Результат", "История по платежу № " + pnumstr + "не найдена.", true);
            };
            Parent.Services.DoWaitAction(work, "Подождите", "Запрос истории платежа...");
        }

        private HistoryInfo[] GetRwPlatHistory(int _idrwplat)
        {
            HistoryInfo[] res = null;

            try
            {
                using (var db = new RealContext())
                {
                    res = db.RwModuleLogs.Where(l => l.Resource == "dbo.RwPlats" && l.Idres == _idrwplat).OrderBy(h => h.Id)
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

        public bool IsShowPlatItogs
        {
            get { return rwPlatsList != null && rwPlatsList.Any(p => p.Value.Direction == RwPlatDirection.Out) && PlatItogs != null; }
        }

        private Tuple<decimal,decimal> platItogs;

        /// <summary>
        /// Итоги платежей
        /// </summary>        
        public Tuple<decimal,decimal> PlatItogs
        {
            get
            {
                if (platItogs == null)
                    platItogs = CalcItogs(RwPlatDirection.Out);
                return platItogs;
            }
        }

        private Tuple<decimal,decimal> CalcItogs(RwPlatDirection _dir)
        {
            Tuple<decimal,decimal> res = null;
            var items = this.RwPlatsList.Where(p => p.Value.Direction == _dir);
            res = new Tuple<decimal, decimal>(items.Sum(i => i.Value.Sumplat), items.Sum(i => i.Value.Ostatok));
            return res;
        }

        public bool IsShowVozvrItogs
        {
            get { return rwPlatsList != null && rwPlatsList.Any(p => p.Value.Direction == RwPlatDirection.In) && VozvrItogs != null; }
        }

        private Tuple<decimal, decimal> vozvrItogs;

        /// <summary>
        /// Итоги возвратов по валютам
        /// </summary>
        public Tuple<decimal, decimal> VozvrItogs
        {
            get
            {
                if (vozvrItogs == null)
                    vozvrItogs = CalcItogs(RwPlatDirection.In);
                return vozvrItogs;
            }
        }

        /// <summary>
        /// Комманда обновления
        /// </summary>
        public ICommand RefreshCommand { get; set; }
        private void RefreshData()
        {
            //Action work = () =>
            //{
            //    if (loadMode == LoadMode.ByParams)
            //        LoadData();
            //    else
            //    {
            //        var selPredopl = predoplsList.SelectedPredopl;
            //        var idpos = predoplsList.Predopls.Select(pvm => pvm.PredoplRef.Idpo);
            //        predoplsList.LoadData(idpos.Select(i => Parent.Repository.GetPredoplById(i)));
            //        predoplsList.SelectedPredopl = selPredopl == null ? predoplsList.Predopls.FirstOrDefault()
            //                                                          : predoplsList.Predopls.FirstOrDefault(p => p.Idpo == selPredopl.Idpo);
            //    }
            //};

            //Parent.Services.DoWaitAction(work, "Подождите", "Обновление данных");
        }
    }
}