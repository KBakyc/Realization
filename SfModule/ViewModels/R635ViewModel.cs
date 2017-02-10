using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommonModule.Commands;
using CommonModule.Interfaces;
using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Helpers;
using SfModule.Reports;
using System.Windows.Data;
using CommonModule.DataViewModels;
using CommonModule.Composition;
using DataObjects.ESFN;
using EsfnHelper.ViewModels;

namespace SfModule.ViewModels
{
    public class R635ViewModel : BasicModuleContent
    {

        private IOtgruzModule otgruzModule;
        private IPredoplModule predoplModule;
        private ISfModule sfModule;

        public R635ViewModel(IModule _parent, IEnumerable<SfInListViewModel> _lst)
            : base(_parent)
        {
            if (Parent != null)
            {
                sfModule = Parent as ISfModule;
                otgruzModule = Parent.ShellModel.Container.GetExportedValueOrDefault<IOtgruzModule>();
                predoplModule = Parent.ShellModel.Container.GetExportedValueOrDefault<IPredoplModule>();
                LoadData(_lst);
            }
        }

        public void LoadData(IEnumerable<SfInListViewModel> _lst)
        {
            Parent.ShellModel.UpdateUi(() =>
                {
                    SfItogList.Clear();
                    foreach (var sf in _lst)
                        SfItogList.Add(sf);
                    if (SelectedSf != null)
                        SelectedSf = SfItogList.SingleOrDefault(s => s.SfRef.IdSf == SelectedSf.SfRef.IdSf);
                    else
                        SelectedSf = SfItogList.FirstOrDefault();
                    CollectYears();
                    CalcValItogs();
                }, false, false);
        }

        /// <summary>
        /// Список счетов-фактур
        /// </summary>
        private ObservableCollection<SfInListViewModel> sfItogList = new ObservableCollection<SfInListViewModel>();
        public ObservableCollection<SfInListViewModel> SfItogList
        {
            get { return sfItogList; }
            set
            {
                if (value != sfItogList)
                {
                    sfItogList = value;
                    NotifyPropertyChanged("SfItogList");
                }
            }
        }

        /// <summary>
        /// Выбранный счёт
        /// </summary>
        private SfInListViewModel selectedSf;
        public SfInListViewModel SelectedSf
        {
            get { return selectedSf; }
            set
            {
                if (value != selectedSf && value != null)
                {
                    if (sfItogList.Any(s => s.IsSelected))
                        foreach (var sf in sfItogList.Where(s => s.IsSelected && s != value)) sf.IsSelected = false;
                    selectedSf = value;
                    LoadSelectedSfDataAsync();
                    NotifyPropertyChanged("SelectedSf");
                }
            }
        }

        private void LoadSelectedSfDataAsync()
        {            
            System.Threading.Tasks.Task.Factory.StartNew(LoadSelectedSfData);
        }
        
        private void LoadSelectedSfData()
        {
            if (selectedSf == null) return;
            selectedSf.LoadViewModel(false);
            Parent.ShellModel.UpdateUi(() => System.Windows.Input.CommandManager.InvalidateRequerySuggested(), true, false);
        }

        private ICommand showEsfnCommand;
        public ICommand ShowEsfnCommand
        {
            get 
            {
                if (showEsfnCommand == null)
                    showEsfnCommand = new DelegateCommand<int?>(ExecShowEsfnCommand, CanShowEsfnCommand);
                return showEsfnCommand; 
            }
        }

        private bool CanShowEsfnCommand(int? _idinvoice)
        {
            return _idinvoice.GetValueOrDefault() > 0;
        }

        private void ExecShowEsfnCommand(int? _idinvoice)
        {
            Action work = () =>
            {
                var vi = VatInvoiceViewModel.FromId(_idinvoice.GetValueOrDefault());
                if (vi != null)
                {
                    var dlg = new VatInvoiceDlgViewModel(vi);
                    Parent.OpenDialog(dlg);
                }
            };

            Parent.Services.DoWaitAction(work);
        }

        /// <summary>
        /// Комманда для вызова диалога редактирования счёта
        /// </summary>

        private const string EDITSFCOMMAND_COMPONENTNAME = "SfModule.ViewModels.R635ViewModel.EditSfCommand";
        [ExportNamedComponent("SfModule.ComponentCommand", EDITSFCOMMAND_COMPONENTNAME)]
        private ICommand editSfCommand;
        public ICommand EditSfCommand
        {
            get
            {
                if (editSfCommand == null && CheckComponentAccess(EDITSFCOMMAND_COMPONENTNAME))
                    editSfCommand = new DelegateCommand(ExecEdit, CanShowEditDlg);
                return editSfCommand;
            }
        }
        private bool CanShowEditDlg()
        {
            return SelectedSf != null
                && sfModule != null
                && !IsReadOnly
                && SelectedSf.SfStatus != LifetimeStatuses.Deleted
                //&& SelectedSf.SumOpl == 0
                ;
        }

        private void ExecEdit()
        {
            if (sfModule == null) return;
            SfModel mod = Parent.Repository.GetSfModel(SelectedSf.SfRef.IdSf);
            sfModule.EditSf(mod, nm =>
            {
                if (nm != null)
                {
                    UpdateSelectedItem();
                }
            });
        }

        private const string PURGESFCOMMAND_COMPONENTNAME = "SfModule.ViewModels.R635ViewModel.PurgeSfCommand";
        [ExportNamedComponent("SfModule.ComponentCommand", PURGESFCOMMAND_COMPONENTNAME)]
        private ICommand purgeSfCommand;
        public ICommand PurgeSfCommand
        {
            get
            {
                if (purgeSfCommand == null && CheckComponentAccess(PURGESFCOMMAND_COMPONENTNAME))
                    purgeSfCommand = new DelegateCommand(ExecPurge, CanPurge);
                return purgeSfCommand;
            }
        }

        private bool CanPurge()
        {
            var sel = sfItogList.Where(i => i.IsSelected);
            return sfModule != null 
                   && !IsReadOnly && isShowDeleted 
                   && sel.Any() && sel.All(i => i.SfStatus == LifetimeStatuses.Deleted);
        }

        private void ExecPurge()
        {
            if (sfModule == null) return;

            var sfs = GetSelectedSfsForPurge();
            if (sfs.Any())
            {
                String sfnums = String.Join(",", sfs.Select(s => s.NumSf.ToString()).ToArray());
                var nDialog = new MsgDlgViewModel()
                {
                    Title = "Подтверждение!!!",
                    BgColor = "Crimson",
                    Message = String.Format("ВНИМАНИЕ!!!\n\nБудут удалены записи об аннулированных,\nошибочно-сформированных счётах-фактурах\n№ {0}.\n\nВ журналах по перевыставлениям они учитываться не будут.", sfnums),
                    OnSubmit = (d) =>
                    {
                        Parent.CloseDialog(d);
                        DoPurgeSfs(sfs);
                    },
                    OnCancel = (d) => Parent.CloseDialog(d)
                };
                Parent.OpenDialog(nDialog);
            }
        }

        private void DoPurgeSfs(IEnumerable<SfInListViewModel> _sfs)
        {
            string errors = "";

            Action work = () =>
            {
                List<SfInListViewModel> purged = new List<SfInListViewModel>();
                foreach (var sf in _sfs)
                {
                    if (!Parent.Repository.PurgeSf(sf.SfRef.IdSf))
                        errors += String.Format("Не удалось удалить счёт № {0}\n", sf.NumSf);
                    else
                        purged.Add(sf);                    
                }
                Parent.ShellModel.UpdateUi(() => { foreach (var sf in purged) sfItogList.Remove(sf); }, false, true);
            };

            Action after = () =>
            {
                //UpdateSelectedItem();
                if (RefreshCommand != null)
                    RefreshCommand.Execute(this);

                if (!String.IsNullOrEmpty(errors))
                    Parent.Services.ShowMsg("Ошибка", errors, true);

                //ChangeFilter();
            };

            Parent.Services.DoWaitAction(work, "Подождите", "Удаление выбранных аннулированных счетов", after);
        }

        /// <summary>
        /// Комманда для удаления счёта
        /// </summary>

        private const string DELETESFCOMMAND_COMPONENTNAME = "SfModule.ViewModels.R635ViewModel.DeleteSfCommand";
        [ExportNamedComponent("SfModule.ComponentCommand", DELETESFCOMMAND_COMPONENTNAME)]
        private ICommand deleteSfCommand;
        public ICommand DeleteSfCommand
        {
            get
            {
                if (deleteSfCommand == null && CheckComponentAccess(DELETESFCOMMAND_COMPONENTNAME))
                    deleteSfCommand = new DelegateCommand(ExecDelete, CanDelete);
                return deleteSfCommand;
            }
        }
        private bool CanDelete()
        {            
            return sfModule != null &&
                   !IsReadOnly && sfItogList.Any(SfsForDelSelector);
        }
        private void ExecDelete()
        {
            if (sfModule == null) return;

            var sfs = GetSelectedSfsForDelete();
            if (sfs.Any())
            {
                LoadViewModels(sfs);
                if (sfs.Any(s => s.View.Esfn != null && s.View.Esfn.Length > 0))
                    Parent.Services.ShowMsg("Внимание","Имеются сформированые электронные с/ф по НДС:\n" 
                                                      + String.Join("\n", sfs.Where(s => s.View.Esfn != null && s.View.Esfn.Length > 0)
                                                                             .Select(s => String.Format("С/ф № {0} - ЕСФН № {1}", s.NumSf, s.View.Esfn[0].VatInvoiceNumber))
                                                                             .ToArray()) 
                                                      + "\nАННУЛИРОВАНИЕ НЕВОЗМОЖНО!", true);
                else
                    DoAskForDelete(sfs);
            }
        }

        private void DoAskForDelete(IEnumerable<SfInListViewModel> _sfs)
        {
            String sfnums = String.Join(",", _sfs.Select(s => s.NumSf.ToString()).ToArray());
            var nDialog = new MsgDlgViewModel()
            {
                Title = "Подтверждение",
                Message = String.Format("Аннулируются счёта-фактуры № {0}.", sfnums),
                OnSubmit = (d) =>
                {
                    Parent.CloseDialog(d);
                    DoDeleteSfs(_sfs);
                },
                OnCancel = (d) => Parent.CloseDialog(d)
            };
            Parent.OpenDialog(nDialog);
        }

        private void DoDeleteSfs(IEnumerable<SfInListViewModel> _sfs)
        {
            string errors = "";

            Action work = () =>
            {
                foreach (var sf in _sfs)
                {
                    if (!Parent.Repository.DeleteSf(sf.SfRef.IdSf))
                        errors += String.Format("Не удалось аннулировать счёт № {0}\n", sf.NumSf);
                    else
                        sf.SfRef.Status = LifetimeStatuses.Deleted;
                }
            };

            Action after = () =>
            {
                //UpdateSelectedItem();
                if (RefreshCommand != null)
                    RefreshCommand.Execute(this);

                if (!String.IsNullOrEmpty(errors))
                    Parent.Services.ShowMsg("Ошибка", errors, true);

                //ChangeFilter();
            };

            Parent.Services.DoWaitAction(work, "Подождите", "Аннулирование выбранных счетов", after);
        }

        private Func<SfInListViewModel, bool> SfsForDelSelector = li => li.IsSelected && li.SumOpl == 0 && li.SfStatus != LifetimeStatuses.Deleted && !li.IsESFN;
        private Func<SfInListViewModel, bool> SfsForPurgeSelector = li => li.IsSelected && li.SfStatus == LifetimeStatuses.Deleted;

        private IEnumerable<SfInListViewModel> GetSelectedSfsForDelete()
        {
            return sfItogList.Where(SfsForDelSelector);
        }

        private void LoadViewModels(IEnumerable<SfInListViewModel> _sfs)
        {
            foreach (var s in _sfs)
                if (!s.IsViewLoaded) s.LoadViewModel(false);
        }
        
        private IEnumerable<SfInListViewModel> GetSelectedSfsForPurge()
        {
            IEnumerable<SfInListViewModel> sfs;
            sfs = sfItogList.Where(SfsForPurgeSelector);
            return sfs;
        }

        /// <summary>
        /// Показывает окно предварительного просмотра счёта
        /// </summary>
        private ICommand showSfPreviewCommand;
        public ICommand ShowSfPreviewCommand
        {
            get
            {
                if (showSfPreviewCommand == null)
                    showSfPreviewCommand = new DelegateCommand(ExecShowSfPreview, () => SelectedSf != null);
                return showSfPreviewCommand;
            }
        }
        private void ExecShowSfPreview()
        {
            if (SelectedSf.IsViewLoaded)
                ShowPreview();
            else
            {
                //SfModel mod = Parent.Repository.GetSfModel(SelectedSf.SfRef.IdSf);
                //sfModule.ShowSf(mod, SfViewForm.Default);
                LoadSfViewModelAndShow();
            }
        }

        private void LoadSfViewModelAndShow()
        {
            Action work = () => SelectedSf.LoadViewModel(false);
            Parent.Services.DoWaitAction(work, "Подождите", "Загрузка данных счёта...", ShowPreview);
        }

        private void ShowPreview()
        {
            var serv = sfModule.Services as SfModule.Helpers.SfService;
            serv.ShowSf(SelectedSf.View);
        }

        /// <summary>
        /// Показывает историю по счёту
        /// </summary>
        private ICommand showSfHistoryCommand;
        public ICommand ShowSfHistoryCommand
        {
            get
            {
                if (showSfHistoryCommand == null)
                    showSfHistoryCommand = new DelegateCommand(ExecShowSfHistory, () => SelectedSf != null);
                return showSfHistoryCommand;
            }
        }
        private void ExecShowSfHistory()
        {
            Action work = () =>
            {
                var sfhs = Parent.Repository.GetSfHistory(SelectedSf.SfRef.IdSf);
                Parent.OpenDialog(new HistoryDlgViewModel(sfhs)
                {
                    Title = "История счёта № " + SelectedSf.NumSf.ToString()
                });
            };
            Parent.Services.DoWaitAction(work, "Подождите", "Запрос истории счёта...");
        }

        /// <summary>
        /// Комманда для вызова окна просмотра отгрузки
        /// </summary>
        private ICommand showSfOtgrCommand;
        public ICommand ShowSfOtgrCommand
        {
            get
            {
                if (showSfOtgrCommand == null)
                    showSfOtgrCommand = new DelegateCommand(ExecShowSfOtgr, CanShowSfOtgr);
                return showSfOtgrCommand;
            }
        }
        private bool CanShowSfOtgr()
        {
            return SelectedSf != null && SelectedSf.SfStatus != LifetimeStatuses.Deleted && otgruzModule != null;
        }
        private void ExecShowSfOtgr()
        {
            if (Parent.Repository.GetSfModel(SelectedSf.SfRef.IdSf) != null)
            {
                otgruzModule.ShowOtgrArc(SelectedSf.SfRef.IdSf);
                sfModule.ShellModel.LoadModule(otgruzModule);
            }
            else
            {
                sfItogList.Remove(SelectedSf);
                SelectedSf = null;
            }
        }

        /// <summary>
        /// Комманда для вызова окна просмотра предоплат
        /// </summary>
        private ICommand showSfPredoplsCommand;
        public ICommand ShowSfPredoplsCommand
        {
            get
            {
                if (showSfPredoplsCommand == null)
                    showSfPredoplsCommand = new DelegateCommand(ExecShowSfPredopls, CanShowSfPredopls);
                return showSfPredoplsCommand;
            }
        }
        private bool CanShowSfPredopls()
        {
            return SelectedSf != null
               && (SelectedSf.PayStatus == PayStatuses.Payed || SelectedSf.PayStatus == PayStatuses.TotallyPayed)
               && predoplModule != null;
        }
        private void ExecShowSfPredopls()
        {
            var predopls = Parent.Repository.GetPredoplsByPaydoc(SelectedSf.SfRef.IdSf, PayDocTypes.Sf);
            string title = String.Format("Предоплаты по счёту №{0}", SelectedSf.NumSf);
            predoplModule.ListPredopls(predopls, title);
            Parent.ShellModel.LoadModule(predoplModule);
        }

        /// <summary>
        /// Комманда обновления
        /// </summary>
        public ICommand RefreshCommand { get; set; }

        /// <summary>
        /// Запуск выбранного отчёта по счёту
        /// </summary>
        private ICommand startSfReportCommand;
        public ICommand StartSfReportCommand 
        { 
            get
            {
                if (startSfReportCommand == null)
                    startSfReportCommand = new DelegateCommand<ReportModel>(ExecStartSfReport);
                return startSfReportCommand;
            }
        }
        private void ExecStartSfReport(ReportModel _rep)
        {
            if (_rep == null) return;

            if (String.IsNullOrEmpty(_rep.ParamsGetterName))
                (new ReportViewModel(Parent, _rep)).TryOpen();
            else
                RunSfReport(_rep);
        }

        private void RunSfReport(ReportModel _rep)
        {
            var rServ = new CommonModule.Helpers.ReportService(Parent, _rep);
            if (rServ != null)
            {
                Action work = () => rServ.ExecuteReport();
                Parent.Services.DoWaitAction(work, "Подождите", "Запрос данных о параметрах отчёта");
            }

        }

        /// <summary>
        /// Обновляет текущий элемент списка
        /// </summary>
        private void UpdateSelectedItem()
        {
            var nSelSf = Parent.Repository.GetSfInListInfo(SelectedSf.SfRef.IdSf);
            SfInListViewModel nSelSfVm = nSelSf == null ? null : new SfInListViewModel(Parent.Repository, nSelSf);
            var oldyear = SelectedSf.DatUch.Year;
            var oldnum = SelectedSf.NumSf;
            SelectedSf = sfItogList.UpdateItem(SelectedSf, nSelSfVm);            
            if (oldyear != SelectedSf.DatUch.Year)
            {
                CollectYears();
                var newnum = SelectedSf.NumSf;
                if (newnum != oldnum)
                    Parent.Services.ShowMsg("Внимание!", String.Format("Изменен год бухучёта счёта-фактуры.\nНомер счёта изменён с {0} на {1}", oldnum, newnum), true);
            }
            if (selectedSf != null)
            {
                var view = CollectionViewSource.GetDefaultView(sfItogList);
                view.MoveCurrentTo(selectedSf);
                view.Refresh();
            }
        }

        private const string APPROVEESFNFCOMMAND_COMPONENTNAME = "SfModule.ViewModels.R635ViewModel.ApproveESFNCommand";
        [ExportNamedComponent("SfModule.ComponentCommand", APPROVEESFNFCOMMAND_COMPONENTNAME)]
        private ICommand approveESFNCommand;
        public ICommand ApproveESFNCommand
        {
            get
            {
                if (approveESFNCommand == null && CheckComponentAccess(APPROVEESFNFCOMMAND_COMPONENTNAME))
                    approveESFNCommand = new DelegateCommand(ExecApproveESFNCommand, CanApproveESFN);
                return approveESFNCommand;
            }
        }

        private bool CanApproveESFN()
        {
            return selectedSf != null && selectedSf.IsESFN && selectedSf.View != null && selectedSf.View.Esfn != null;
        }

        private void ExecApproveESFNCommand()
        {            
            var dlg = new MsgDlgViewModel
            {
                Title = "Подтверждение",
                Message = "Подтвердить ЕСФН по выбранным с/ф?",
                IsCancelable = true,
                OnSubmit = DoApproveESFN
            };
            Parent.OpenDialog(dlg);
        }

        private void DoApproveESFN(Object _d)
        {
            Parent.CloseDialog(_d);
            Action work = () =>
            {
                List<Result<bool>> ares = new List<Result<bool>>();
                foreach (var sf in sfItogList.Where(s => s.IsSelected && s.IsESFN))
                    ares.Add(ApproveSingleESFN(sf));
                var totmess = String.Join("\n", ares.Select(r => r.Description).ToArray());
                var totres = ares.All(r => r.Value);
                Parent.Services.ShowMsg("Результат", totmess, !totres);
            };
            Parent.Services.DoWaitAction(work, "Подождите", "Подтверждение электронного с/ф по НДС...", () => Parent.ShellModel.UpdateUi(UpdateSelectedItem, true, false));
        }

        private Result<bool> ApproveSingleESFN(SfInListViewModel _sf)
        {
            Result<bool> res = new Result<bool>(false, "С/ф № " + _sf.NumSf.ToString() + Environment.NewLine);
            _sf.LoadViewModel(false);
            var esfn = _sf.View != null ? _sf.View.Esfn : null;
            if (esfn != null && esfn.Length > 0)
            {
                Array.ForEach(esfn, e => e.GetStatus());
                if (esfn.All(e => e.StatusId < InvoiceStatuses.STATUS3))
                {
                    res.Value = Parent.Repository.Approve_ESFN(_sf.SfRef.IdSf, true);
                    res.Description += res.Value ? "Данные по ЕСФН подтверждены" : "Ошибка при подтверждении электронных с/ф по НДС";
                }
                else
                {
                    res.Value = false;
                    res.Description += "Нельзя подтвердить электронные с/ф по НДС";
                }
            }
            return res;
        }
        
        private ICommand cancelApproveESFNCommand;
        public ICommand CancelApproveESFNCommand
        {
            get
            {
                if (cancelApproveESFNCommand == null && CheckComponentAccess(APPROVEESFNFCOMMAND_COMPONENTNAME))
                    cancelApproveESFNCommand = new DelegateCommand(ExecCancelApproveESFNCommand, CanCancelApproveESFN);
                return cancelApproveESFNCommand;
            }
        }

        private bool CanCancelApproveESFN()
        {
            return selectedSf != null && selectedSf.IsESFN && selectedSf.View != null && selectedSf.View.Esfn != null && selectedSf.View.Esfn.All(e => e.ApprovedByUserFIO == Parent.ShellModel.CurrentUserInfo.FullName);                
        }

        private void ExecCancelApproveESFNCommand()
        {
            var esfn = selectedSf.View.Esfn;
            var dlg = new MsgDlgViewModel
            {
                Title = "Подтверждение",
                Message = "Отменить подтверждение ЕСФН?",
                IsCancelable = true,
                OnSubmit = DoCancelApproveESFN
            };
            Parent.OpenDialog(dlg);
        }

        private void DoCancelApproveESFN(Object _d)
        {
            Parent.CloseDialog(_d);
            Action work = () =>
            {
                selectedSf.LoadViewModel(false);
                var esfn = selectedSf != null && selectedSf.View != null ? selectedSf.View.Esfn : null;
                if (esfn != null && esfn.Length > 0)
                {
                    Array.ForEach(esfn, e => e.GetStatus());
                    if (esfn.All(e => e.StatusId < InvoiceStatuses.STATUS3))
                    {
                        bool res = Parent.Repository.Approve_ESFN(selectedSf.SfRef.IdSf, false);
                        var resmess = res ? "Подтверждение данных по ЕСФН отменено" : "Ошибка при отмене подтверждения электронных с/ф по НДС";
                        Parent.Services.ShowMsg("Результат", resmess, !res);
                    }
                    else
                        Parent.Services.ShowMsg("Ошибка", "Нельзя отменить подтверждение электронных с/ф по НДС", true);
                }
            };
            Parent.Services.DoWaitAction(work, "Подождите", "Отмена подтверждения электронного с/ф по НДС...", () => Parent.ShellModel.UpdateUi(UpdateSelectedItem, true, false));
        }

        private const string LINKESFNFCOMMAND_COMPONENTNAME = "SfModule.ViewModels.R635ViewModel.LinkESFNCommand";
        [ExportNamedComponent("SfModule.ComponentCommand", LINKESFNFCOMMAND_COMPONENTNAME)]
        private ICommand linkESFNCommand;
        public ICommand LinkESFNCommand
        {
            get
            {
                if (linkESFNCommand == null && CheckComponentAccess(LINKESFNFCOMMAND_COMPONENTNAME))
                    linkESFNCommand = new DelegateCommand(ExecLinkESFNCommand, CanLinkESFNCommand);
                return linkESFNCommand;
            }
        }

        private bool CanLinkESFNCommand()
        {
            return selectedSf != null && selectedSf.SfType.SfTypeId == 0;
        }

        private void ExecLinkESFNCommand()
        {
            if (!selectedSf.IsESFN)
                AskLinkESFN();
            else
                AskUnLinkESFN();
        }

        private void AskLinkESFN()
        {
            Action work = () =>
            {
                var esfns = Parent.Repository.Get_ESFNs_ToLink(selectedSf.SfRef.IdSf);
                if (esfns == null || esfns.Length == 0)
                    Parent.Services.ShowMsg("Результат", "ЭСФН для привязки не найдены", true);
                else
                {
                    var selDlg = new SelectESFNDlgViewModel(esfns)
                    {
                        Title = "Укажите электронный счёт-фактуру",
                        OnSubmit = d =>
                        {
                            Parent.CloseDialog(d);
                            var dlg = d as SelectESFNDlgViewModel;
                            var res = SetLinkedESFN(dlg.SelectedInvoiceId);
                            selectedSf.View.CheckESFN();
                            Parent.Services.ShowMsg("Результат", res ? "Привязка завершена успешно" : "Не удалось привязать ЭСФН к счёту-фактуре", res);
                        }
                    };
                    Parent.OpenDialog(selDlg);
                }
            };
            Parent.Services.DoWaitAction(work);
        }

        private void AskUnLinkESFN()
        {
            var selDlg = new MsgDlgViewModel
            {
                Title = "Требуется подтверждение",
                Message = "Отменить привязку выбранного счёта-фактуры к сформированному ЭСФН ?",
                IsCancelable = true,
                OnSubmit = d =>
                {
                    Parent.CloseDialog(d);
                    var res = SetLinkedESFN(0);
                    UpdateSelectedItem();
                    //selectedSf.View.CheckESFN();
                    Parent.Services.ShowMsg("Результат", res ? "Привязка счёта-фактуры к ЭСФН удалена." : "Не удалось удалить привязку ЭСФН к счёту-фактуре", res);
                }
            };
            Parent.OpenDialog(selDlg);
        }

        private bool SetLinkedESFN(int _invoiceId)
        {
            bool res = false;
            res = Parent.Repository.Set_ESFN_Link(selectedSf.SfRef.IdSf, null, _invoiceId);
            if (!res)
                Parent.Services.ShowMsg("Ошибка", "Не удалось изменить привязку к ЭСФН.", true);
            return res;
        }

        private const string DELETEESFNFCOMMAND_COMPONENTNAME = "SfModule.ViewModels.R635ViewModel.DeleteESFNCommand";
        [ExportNamedComponent("SfModule.ComponentCommand", DELETEESFNFCOMMAND_COMPONENTNAME)]
        private ICommand deleteESFNCommand;
        public ICommand DeleteESFNCommand
        {
            get
            {
                if (deleteESFNCommand == null && CheckComponentAccess(DELETEESFNFCOMMAND_COMPONENTNAME))
                    deleteESFNCommand = new DelegateCommand(ExecDeleteESFNCommand, CanDeleteESFN);
                return deleteESFNCommand;
            }
        }

        private void ExecDeleteESFNCommand()
        {
            var esfn = selectedSf.View.Esfn;
            var dlg = new MsgDlgViewModel 
            {
                Title = "Подтверждение",
                Message = "Очистить данные по ЕСФН ?",
                IsCancelable = true,
                OnSubmit = DoDeleteESFN
            };
            Parent.OpenDialog(dlg);
        }

        private void DoDeleteESFN(Object _d)
        {
            Parent.CloseDialog(_d);
            Action work = () => 
            {
                selectedSf.LoadViewModel(false);
                var esfn = selectedSf != null && selectedSf.View != null ? selectedSf.View.Esfn : null;
                if (esfn != null && esfn.Length > 0)
                {
                    Array.ForEach(esfn, e => e.GetStatus());
                    if (esfn.All(e => (e.StatusId < InvoiceStatuses.STATUS3) && String.IsNullOrWhiteSpace(e.ApprovedByUserFIO) || e.StatusId == InvoiceStatuses.STATUS7 || e.StatusId == InvoiceStatuses.STATUS8)) // 7 - аннулирован; 8 - ошибка портала
                    {
                        bool res = Parent.Repository.Delete_ESFN(selectedSf.SfRef.IdSf);
                        var resmess = res ? "Данные по ЭСФН очищены"
                                          : "Ошибка при удалении данных по ЭСФН";
                        Parent.Services.ShowMsg("Результат", resmess, !res);
                    }
                    else
                        Parent.Services.ShowMsg("Ошибка", "Нельзя удалить данные по электронным с/ф по НДС"
                            + (esfn != null ? String.Join("\n№ ", esfn.Where(e => e.StatusId > InvoiceStatuses.STATUS2).Select(e => e.VatInvoiceNumber 
                                                                                                            + " Статус: " + e.StatusName 
                                                                                                            + (String.IsNullOrWhiteSpace(e.ApprovedByUserFIO) ? "" : ("Подтверждён: " + e.ApprovedByUserFIO))).ToArray()) 
                                                                          : "-пусто-"), true);
                }
            };
            Parent.Services.DoWaitAction(work, "Подождите", "Удаление электронного с/ф по НДС...", () => Parent.ShellModel.UpdateUi(UpdateSelectedItem, true, false));
        }

        private bool CanDeleteESFN()
        {
            return selectedSf != null && selectedSf.IsESFN && selectedSf.View != null && selectedSf.View.Esfn != null;
        }

        private const string CREATEESFNFCOMMAND_COMPONENTNAME = "SfModule.ViewModels.R635ViewModel.CreateESFNCommand";
        [ExportNamedComponent("SfModule.ComponentCommand", CREATEESFNFCOMMAND_COMPONENTNAME)]
        private ICommand createESFNCommand;
        public ICommand CreateESFNCommand
        {
            get
            {
                if (createESFNCommand == null && CheckComponentAccess(CREATEESFNFCOMMAND_COMPONENTNAME))
                    createESFNCommand = new DelegateCommand(ExecCreateESFNCommand, CanCreateESFN);
                return createESFNCommand;
            }
        }

        private Result<bool> GetCanMakeESFN(EsfnDataViewModel[] _surESFN)
        {
            string resultMsg = "";
            bool isEsnfPossible = true;
            bool isNeedFixed = false;
            if (_surESFN != null && _surESFN.Length > 0 && _surESFN.Any(e => e.VatInvoiceId != null))
            {
                Array.ForEach(_surESFN, e => e.GetStatus());
                resultMsg = "Уже сформирован электронный счёт-фактура по НДС\n№ "
                           + String.Join("\n№ ", _surESFN.Select(e => e.VatInvoiceNumber
                                                                + " : Статус: " + e.StatusName
                                                                + " : Подтверждён: " + (String.IsNullOrWhiteSpace(e.ApprovedByUserFIO) ? "НЕТ" : e.ApprovedByUserFIO))
                                                                .ToArray());
                if (_surESFN.Any(e => e.StatusId != InvoiceStatuses.STATUS7 && (e.StatusId > InvoiceStatuses.STATUS2 || !String.IsNullOrWhiteSpace(e.ApprovedByUserFIO)))) // статус 7 - аннулирован
                {
                    if (selectedSf.SfType.SfTypeId == 0)
                    {
                        isEsnfPossible = true;
                        isNeedFixed = true;
                    }
                    else
                    {
                        isEsnfPossible = false;
                        resultMsg += "\nВЫСТАВЛЕН КОРРЕКТИРОВОЧНЫЙ СЧЁТ-ФАКТУРА";
                    }
                }                                                                                 
            }
            if (isEsnfPossible == false)
                resultMsg += "\nФОРМИРОВАНИЕ НЕВОЗМОЖНО!";
            else
                resultMsg += ("\nСформировать " + (isNeedFixed ? "ИСПРАВЛЕННЫЙ " : "") + "электронный счёт-фактуру по НДС?");

            return new Result<bool>(isEsnfPossible, resultMsg);
        }

        private void ExecCreateESFNCommand()
        {            
            if (selectedSf.View == null) selectedSf.LoadViewModel(false);

            var sfview = selectedSf.View;
            var esfn = sfview.Esfn;

            var isCanMakeNewEsfn = GetCanMakeESFN(esfn);
            if (!isCanMakeNewEsfn.Value)
            {
                Parent.Services.ShowMsg("Внимание", isCanMakeNewEsfn.Description, true);
                return;
            }

            var options = Parent.Repository.GetESFNCreateOptions(sfview.SfRef.IdSf);
            if (options == null)
            {
                Parent.Services.ShowMsg("Внимание", "Ошибка при получении настроек режима формирования.", true);
                return;
            }

            bool isVozv = options.IsVozvrat; //sfview.SfType.SfTypeId == 0 && sfview.SumPltr < 0; // для возвратов формируем дополнительные ЭСФН

            bool isVozmUsl = options.IsVozmUsl; //sfview.SfProductLines.Any(p => p.ProdRef.IsService && !p.ProdRef.IsInReal 
                               //                          && p.ProdRef.IdProdType.GetValueOrDefault() != 11);    // энергоносители, вода и т.п.  
            //if (!isVozmUsl)
            //{
            //    short[] rwpays = { 5, 6, 7, 8 };
            //    isVozmUsl = Parent.Repository.GetSfPays(sfview.SfRef.IdSf).Any(p => p.Isaddtosum && rwpays.Contains(p.PayType));
            //}

            Action askAndCreate = () =>
            {
                var askDlg = new MsgDlgViewModel
                    {
                        Title = "Подтверждение",
                        Message = isCanMakeNewEsfn.Description,
                        IsCancelable = true,
                        OnSubmit = d =>
                        {
                            Parent.CloseDialog(d);
                            DoCreateESFNCommand();
                        }
                    };
                Parent.OpenDialog(askDlg);
            };

            Action setEsfnInfoOrRun = () =>
                {
                    if (isVozv)
                    {
                        var selInDlg = new LinkSfToPrimaryDlgViewModel(Parent.Repository, sfview)
                        {
                            Title = "Укажите исходный счёт-фактуру",
                            OnSubmit = d =>
                                {
                                    Parent.CloseDialog(d);
                                    var dlg = d as LinkSfToPrimaryDlgViewModel;
                                    if (SetPrimaryESFN(dlg.SelectedPrimarySf.IdSf))
                                    {
                                        selectedSf.View.CheckESFN();
                                        askAndCreate();
                                    }
                                }
                        };
                        if (esfn != null && esfn.Length > 0 && esfn[0].PrimaryIdsf.GetValueOrDefault() != 0)
                            selInDlg.SelectedPrimarySf = selInDlg.PrimarySfs.SingleOrDefault(i => i.IdSf == esfn[0].PrimaryIdsf.Value);
                        Parent.OpenDialog(selInDlg);
                    }

                    else
                        if (isVozmUsl)
                        {
                            var selInDlg = new LinkSfToIncomeDlgViewModel(Parent.Repository, sfview)
                            {
                                Title = "Укажите входящий электронный счёт-фактуру",
                                OnSubmit = d =>
                                    {
                                        Parent.CloseDialog(d);
                                        var dlg = d as LinkSfToIncomeDlgViewModel;
                                        if (SetIncomeESFN(dlg.SelectedIncomeInvoiceId))
                                        {
                                            selectedSf.View.CheckESFN();
                                            askAndCreate();
                                        }
                                    }
                            };
                            if (esfn != null && esfn.Length > 0 && esfn[0].InVatInvoiceId != null)
                                selInDlg.SelectESFN.SelectedESFN = selInDlg.IncomeESFNs.SingleOrDefault(i => i.Item1 == esfn[0].InVatInvoiceId);
                            Parent.OpenDialog(selInDlg);
                        }
                        else
                            askAndCreate();
                };
            Parent.Services.DoWaitAction(setEsfnInfoOrRun);
        }

        private bool SetPrimaryESFN(int _primaryIdsf)
        {
            bool res = false;
            res = Parent.Repository.Set_Primary_ESFN(selectedSf.SfRef.IdSf, _primaryIdsf);
            if (!res)
                Parent.Services.ShowMsg("Ошибка", "Не удалось привязать исходный счёт-фактуру.", true);
            return res;
        }

        private bool SetIncomeESFN(int _incomeInvoiceId)
        {
            bool res = false;
            res = Parent.Repository.Set_Income_ESFN(selectedSf.SfRef.IdSf, _incomeInvoiceId);
            if (!res)
                Parent.Services.ShowMsg("Ошибка", "Не удалось привязать входящий ЭСФН.", true);
            return res;
        }

        private void DoCreateESFNCommand()
        {
            EsfnData[] res = null;
            Action work = () =>
            {
                res = Parent.Repository.Make_ESFN(selectedSf.SfRef.IdSf);
            };
            Action after = () =>
            {
                if (res != null)
                {
                    var mess = res.Length == 1 ? ("Сформирован электронный счёт-фактура по НДС № " + res[0].VatInvoiceNumber) 
                                               : ("Сформированы электронные счёта-фактуры по НДС\n№ " + String.Join("\n№ ", res.Select(e => e.VatInvoiceNumber).ToArray()));
                    Parent.Services.ShowMsg("Результат", mess, false);                    
                    selectedSf.View.Esfn = res.Select(e => new EsfnDataViewModel(Parent.Repository, e)).ToArray();
                    selectedSf.IsESFN = res != null && res.Length > 0;
                }
                else
                    Parent.Services.ShowMsg("Результат", "Электронный счёт-фактура по НДС не сформирован", true);
            };
            Parent.Services.DoWaitAction(work, "Подождите", "Формирование электронного счёта-фактуры по НДС", after);
        }

        private bool CanCreateESFN()
        {
            return selectedSf != null && selectedSf.SumPltr != 0;
        }

        private EsfnData[] GetEsfnDataAction(int _idsf)
        {
            return Parent.Repository.Get_ESFN(_idsf);
        }

        /// <summary>
        /// Печать счетов
        /// </summary>
        private ICommand printAllCommand;
        public ICommand PrintAllCommand
        {
            get
            {
                if (printAllCommand == null)
                    printAllCommand = new DelegateCommand(ExecPrintAllCommand, () => sfItogList != null && sfItogList.Count > 0);
                return printAllCommand;
            }
        }

        private Choice printAllChoice = new Choice() { GroupName = "Печатать", Header = "Все", IsChecked = true, IsSingleInGroup = true };
        private Choice printSelectedChoice = new Choice() { GroupName = "Печатать", Header = "Выбранные", IsChecked = false, IsSingleInGroup = true };

        private void ExecPrintAllCommand()
        {
            Parent.OpenDialog(new ChoicesDlgViewModel(printAllChoice, printSelectedChoice)
            {
                Title = "Настройка печати",
                OnSubmit = ExecPrint
            });
        }

        private void ExecPrint(Object _dlg)
        {
            Parent.CloseDialog(_dlg);
            IEnumerable<SfInListViewModel> sfs = GetAllSfsInView();
            if (!(printAllChoice.IsChecked ?? false))
                sfs = GetAllSfsInView().Where(li => li.IsSelected);//.ToArray();
            if (sfs.Any())
                sfModule.PrintSfs(sfs.Select(s => Parent.Repository.GetSfModel(s.SfRef.IdSf)));
        }

        IEnumerable<SfInListViewModel> GetAllSfsInView()
        {
            IEnumerable<SfInListViewModel> res = null;
            var view = CollectionViewSource.GetDefaultView(SfItogList);
            res = view.OfType<SfInListViewModel>();
            return res;
        }

        /// <summary>
        /// Печать выборки счетов
        /// </summary>
        private ICommand printListCommand;
        public ICommand PrintListCommand
        {
            get
            {
                if (printListCommand == null)
                    printListCommand = new DelegateCommand(ExecPrintListCommand, () => sfItogList != null && sfItogList.Count > 0);
                return printListCommand;
            }
        }
        private void ExecPrintListCommand()
        {
            var sfs = GetAllSfsInView().ToArray();
            Action work = () => MakeAndShowSfsListReport(sfs);

            Parent.Services.DoWaitAction(work, "Подождите", "Формирование отчёта");
        }

        private void MakeAndShowSfsListReport(SfInListViewModel[] _sfs)
        {
            var ds = _sfs
                .Select(s => new SfsListReportData()
                {
                    NumSf = s.NumSf,
                    DatePltr = s.DatePltr,
                    Kpok = s.Platelschik.Kgr,
                    KpokName = s.Platelschik.Name,
                    SumPltr = s.SumPltr,
                    ValName = s.Valuta.ShortName,
                    IsDeleted = s.SfStatus == LifetimeStatuses.Deleted
                }).ToArray();

            ReportModel rep = new ReportModel()
            {
                Title = this.Title,
                Description = "Печать выборки счетов-фактур",
                Mode = ReportModes.Local,
                DataSources = new Dictionary<string, IEnumerable<object>> { { "DS1", ds } },
                Parameters = new Dictionary<string, string> { { "Title", this.Title }, { "Description", "Печать выборки счетов-фактур" } },
                Path = @"Reports/SfsListReport.rdlc"
            };
            var repDC = new ReportViewModel(Parent, rep);
            if (repDC.IsValid)
                repDC.TryOpen();
            else
                Parent.Services.ShowMsg("Ошибка", repDC.GetErrMsg(), true);
        }

        private bool isShowDeleted;
        public bool IsShowDeleted
        {
            get { return isShowDeleted; }
            set
            {
                if (value != isShowDeleted)
                {
                    isShowDeleted = value;
                    NotifyPropertyChanged("IsShowDeleted");
                    ChangeFilter();
                }
            }
        }

        private bool isESFN;
        public bool IsESFN
        {
            get { return isESFN; }
            set
            {
                if (value != isESFN)
                {
                    isESFN = value;
                    NotifyPropertyChanged("IsESFN");
                    ChangeFilter();
                    //ApplyOnlyESFN();
                }
            }
        }

        private bool isNotESFN;
        public bool IsNotESFN
        {
            get { return isNotESFN; }
            set
            {
                if (value != isNotESFN)
                {
                    isNotESFN = value;
                    NotifyPropertyChanged("IsNotESFN");
                    ChangeFilter();
                    //ApplyOnlyESFN();
                }
            }
        }

        //private void ApplyOnlyESFN()
        //{
        //    if (!isOnlyESFN) 
        //        ChangeFilter();
        //    else
        //        Parent.Services.DoWaitAction(GetAllESFN, "Подождите", "Запрос данных о ЭСФН...", () => Parent.ShellModel.UpdateUi(ChangeFilter, true, false));

        //}

        //private void GetAllESFN()
        //{
        //    foreach(var sf in sfItogList.Where(s => s.SfStatus != LifetimeStatuses.Deleted && s.Esfn == null));
        //}

        private Func<SfInListViewModel, bool> filter;

        public void ChangeFilter()
        {
            filter = null;

            Predicate<SfInListViewModel> dfilter = null;
            if (!IsShowDeleted)
                dfilter = o => o.SfStatus != LifetimeStatuses.Deleted;

            Predicate<SfInListViewModel> esfnFilter = null;
            if (isESFN != isNotESFN )
                esfnFilter = o => o.IsESFN == isESFN; //o.Esfn != null && o.Esfn.Length > 0;

            Predicate<SfInListViewModel> yfilter = null;
            if (SelYear != null && SelYear.Value > 0)
            {
                int year = SelYear.Value;
                yfilter = o => o.DatUch.Year == year;
            }

            if (yfilter != null || dfilter != null || esfnFilter != null)
                filter = o => (yfilter == null ? true : yfilter(o))
                            & (dfilter == null ? true : dfilter(o))
                            & (esfnFilter == null ? true : esfnFilter(o));

            SetFilter(filter);
        }

        private void SetFilter(Func<SfInListViewModel, bool> _filter)
        {
            var view = CollectionViewSource.GetDefaultView(SfItogList);
            view.Filter = o => filter(o as SfInListViewModel);
            view.Refresh();            
            CalcValItogs();
        }

        public PoupModel SelectedPoup { get; set; }
        public PkodModel SelectedPkod { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }

        private List<KeyValueObj<string, int>> years;// = new List<KeyValuePair<string, int>>() { new KeyValuePair<string, int>("2011", 2011) };
        public List<KeyValueObj<string, int>> Years
        {
            get { return years; }
            set { SetAndNotifyProperty("Years", ref years, value); }
        }

        private KeyValueObj<string, int> selYear;// = new KeyValuePair<string, int>("2011", 2011);
        public KeyValueObj<string, int> SelYear
        {
            get { return selYear; }
            set
            {
                SetAndNotifyProperty("SelYear", ref selYear, value);
                ChangeFilter();
            }
        }

        private void CollectYears()
        {
            List<KeyValueObj<string, int>> res = null;

            if (sfItogList != null && sfItogList.Count > 0)
            {
                res = sfItogList.Select(s => s.DatUch.Year).Distinct().OrderBy(y => -y).Select(y => new KeyValueObj<string, int>(y.ToString(), y)).ToList();
                if (res.Count > 1)
                    res.Add(new KeyValueObj<string, int>("Все", 0));
            }

            if (res != null)
                SelYear = res[0];
            Years = res;
        }

        //
        /// <summary>
        /// Комманда отмены оплаты по выбраной предоплате
        /// </summary>

        private const string UNDOPAYMENTSCOMMAND_COMPONENTNAME = "SfModule.ViewModels.R635ViewModel.UndoPaymentsCommand";
        [ExportNamedComponent("SfModule.ComponentCommand", UNDOPAYMENTSCOMMAND_COMPONENTNAME)]
        private ICommand undoPaymentsCommand;
        public ICommand UndoPaymentsCommand
        {
            get
            {
                if (undoPaymentsCommand == null && CheckComponentAccess(UNDOPAYMENTSCOMMAND_COMPONENTNAME))
                    undoPaymentsCommand = new DelegateCommand(SelectPaysForUndo, CanUndoPayments);
                return undoPaymentsCommand;
            }
        }

        private void SelectPaysForUndo()
        {
            PayAction[] pacts = null;

            pacts = Parent.Repository.GetPayActions(0, selectedSf.SfRef.IdSf);

            Parent.OpenDialog(new SelectPaysForUndoDlgViewModel(Parent.Repository, pacts)
            {
                Title = String.Format("Выберите отменяемые оплаты счёта № {0}", selectedSf.NumSf),
                OnSubmit = ExecUndoSelectedPays
            });
        }

        private void ExecUndoSelectedPays(Object _dlg)
        {
            Parent.CloseDialog(_dlg);

            var dlg = _dlg as SelectPaysForUndoDlgViewModel;

            PayAction[] pacs = dlg.SelectedPayActions;

            if (pacs == null || pacs.Length == 0) return;

            var askDlg = new MsgDlgViewModel
            {
                Title = "Подтверждение",
                Message = String.Format("Внимание!\nВыбрано {0} оплат на сумму {1}\nВы уверены, что хотите отменить выбранные оплаты?", pacs.Length, pacs.Sum(p => p.SumOpl)),
                OnSubmit = d =>
                {
                    Parent.CloseDialog(d);
                    DoUndoSelectedPayActions(pacs);
                }
            };

            Parent.OpenDialog(askDlg);

        }

        private void DoUndoSelectedPayActions(PayAction[] _pacs)
        {
            WaitDlgViewModel wdlg = new WaitDlgViewModel
            {
                Title = "Выполнение операции"
            };
           
            bool undone = false;
            List<string> errors = new List<string>();
            Action<WaitDlgViewModel> work;
            work = (d) =>
            {
                DateTime atime = DateTime.Now;
                for (int i = 0; i < _pacs.Length; i++)
                {
                    var pa = _pacs[i];
                    int selidpo = pa.IdPo;
                    string pasf = pa.Idsf == 0 ? ""
                                               : pa.PayActionType == PayActionTypes.Sf ? String.Format("С/ф № {0}", pa.Numsf)
                                                                                       : String.Format("Возврат № {0}", pa.Numsf);
                    string papo = pa.IdPo == 0 ? "" : String.Format("Платёжка № {0}", pa.Ndoc);
                    d.Message = String.Format("Отмена оплаты {0}/{1}:\n{2} {3} Сумма:{4}", i, _pacs.Length, pasf, papo, pa.SumOpl);
                    bool undores = Parent.Repository.UndoPayAction(pa, atime);
                    if (!undores)
                        errors.Add(d.Message);
                    else
                        if (selidpo != 0)
                            Parent.Repository.SetPredolpStatus(selidpo, PredoplStatuses.UndoPays, atime);
                    undone |= undores;
                }
                if (undone)
                    Parent.Repository.SetSfCurPayStatus(selectedSf.SfRef.IdSf, PayActions.UndoPays, atime);
            };

            Action afterwork = () =>
            {
                Parent.ShellModel.UpdateUi(() => UpdateSelectedItem(), true, true);
                if (errors.Count > 0)
                    Parent.Services.ShowMsg("Ошибки отмены оплат", String.Join("\n", errors.ToArray()), true);
                if (undone)
                    ShowUndoPaymentsReport();
            };

            Parent.Services.DoWaitAction(work, "Подождите", "Аннулирование погашений предоплаты", afterwork);
        }

        private void ShowUndoPaymentsReport()
        {
            ReportModel repm = new ReportModel
            {
                Title = String.Format("Протокол аннулирования оплат"),
                Path = @"/real/Reports/ClearPaymentsProtokol",
                Parameters = new Dictionary<string, string> { { "ConnString", Parent.Repository.ConnectionString } }
            };
            (new ReportViewModel(Parent, repm)).TryOpen();
        }

        private bool CanUndoPayments()
        {
            return !IsReadOnly && selectedSf != null && selectedSf.SumOpl != 0;
        }
        //

        //private bool isShowItogi;
        //public bool IsShowItogi
        //{
        //    get { return isShowItogi; }
        //    set { SetAndNotifyProperty("IsShowItogi", ref isShowItogi, value); }
        //}

        private Dictionary<string, decimal[]> sfsItogs;
        public Dictionary<string, decimal[]> SfsItogs
        {
            get { return sfsItogs; }
            set { SetAndNotifyProperty("SfsItogs", ref sfsItogs, value); }
        }

        private void CalcValItogs()
        {
            IEnumerable<SfInListViewModel> sfs = filter == null ? sfItogList : sfItogList.Where(filter);
            SfsItogs = sfs.Where(s => s.SfStatus != LifetimeStatuses.Deleted).GroupBy(s => s.Valuta == null ? "неизв." : s.Valuta.ShortName)
                          .ToDictionary(g => g.Key,
                                             g => new decimal[] { g.Count(), g.Sum(i => i.SumPltr), g.Sum(i => i.SumOpl), 0 });
            foreach (var kv in SfsItogs)
                kv.Value[3] = kv.Value[1] - kv.Value[2];            
        }
    }
}