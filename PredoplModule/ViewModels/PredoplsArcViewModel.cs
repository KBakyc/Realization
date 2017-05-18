using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using CommonModule.Commands;
using CommonModule.Interfaces;
using CommonModule.ViewModels;
using DataObjects;
using CommonModule.ModuleServices;
using PredoplModule.Helpers;
using CommonModule.DataViewModels;
using DataObjects.SeachDatas;
using CommonModule.Composition;


namespace PredoplModule.ViewModels
{
    /// <summary>
    /// Модель режима просмотра архива принятых предоплат.
    /// </summary>
    public class PredoplsArcViewModel : BasicModuleContent
    {
        private LoadMode loadMode;
        private ISfModule sfModule;
        private PredoplModel[] predLst;

        public PredoplsArcViewModel(IModule _parent, IEnumerable<PredoplModel> _predopls)
            :base(_parent)
        {
            predLst = _predopls.ToArray();
            loadMode = LoadMode.ByData;
            if (Parent != null)
            {
                Init();
                LoadData();
            }
        }

        public PredoplsArcViewModel(IModule _parent, Valuta _val, PoupModel _poup, DateTime _dfrom, DateTime _dto, PkodModel _pkod)
            :base(_parent)
        {
            predoplVal = _val;
            selectedPoup = _poup;
            selectedPkod = _pkod;
            dateFrom = _dfrom;
            dateTo = _dto;
            loadMode = LoadMode.ByParams;
            if (Parent != null)
            {
                Init();
                LoadData();
            }
        }       

        private void Init()
        {
            sfModule = Parent.ShellModel.Container.GetExportedValueOrDefault<ISfModule>();
            RefreshCommand = new DelegateCommand(RefreshData);
        }

        public LoadMode PredoplsLoadMode
        {
            get { return loadMode; }
        }

        // Валюта
        private readonly Valuta predoplVal;
        public Valuta PredoplVal
        {
            get
            {
                return predoplVal;
            }
        }

        // Выбранное направление реализации
        private readonly PoupModel selectedPoup;
        public PoupModel SelectedPoup
        {
            get
            {
                return selectedPoup;
            }
        }

        private readonly PkodModel selectedPkod;
        public PkodModel SelectedPkod
        {
            get
            {
                return selectedPkod;
            }
        }

        private DateTime dateFrom;
        public DateTime DateFrom
        {
            get
            {
                return dateFrom;
            }
        }

        private DateTime dateTo;
        public DateTime DateTo
        {
            get
            {
                return dateTo;
            }
        }

        /// <summary>
        /// Загрузка данных
        /// </summary>
        private void LoadData()
        {
            if (loadMode == LoadMode.ByParams)
            {
                var schData = new PredoplSearchData();
                schData.Kodval = predoplVal.Kodval;
                schData.Poup = selectedPoup.Kod;
                schData.Dfrom = dateFrom;
                schData.Dto = dateTo;
                if (SelectedPkod != null)
                    schData.Pkod = SelectedPkod.Pkod;

                predLst = Parent.Repository.GetPredopls(schData);                
            }
            Parent.ShellModel.UpdateUi(() =>
            {
                if (predoplsList == null)
                    PredoplsList = new PredoplsListViewModel(Parent.Repository, predLst);
                else                
                    predoplsList.LoadData(predLst);
                predoplsList.SelectedPredopl = predoplsList.Predopls.FirstOrDefault();
            }, true, false);
        }

        private PredoplsListViewModel predoplsList;
        /// <summary>
        /// Список предоплат
        /// </summary>
        public PredoplsListViewModel PredoplsList 
        {
            get { return predoplsList; }
            set {SetAndNotifyProperty("PredoplsList", ref predoplsList, value);}
        }

        /// <summary>
        /// Комманда редактирования выбраной предоплаты
        /// </summary>
        private ICommand editPredoplCommand;

        private const string EDITPREDOPLCOMMAND_COMPONENTNAME = "PredoplModule.ViewModels.PredoplsArcViewModel.EditPredoplCommand";
        [ExportNamedComponent("PredoplModule.ComponentCommand", EDITPREDOPLCOMMAND_COMPONENTNAME)]
        public ICommand EditPredoplCommand
        {
            get 
            {
                if (editPredoplCommand == null && CheckComponentAccess(EDITPREDOPLCOMMAND_COMPONENTNAME))
                    editPredoplCommand = new DelegateCommand(ExecEditPredoplCommand, CanExecEditPredoplCommand);
                return editPredoplCommand;
            }
        }
        private void ExecEditPredoplCommand()
        {
            EditPredoplDlgViewModel nDlg = null;
            Action work = () =>
            {
                var savedSelectedPredopl = PredoplsList.SelectedPredopl;

                // подгрузка sumotgr из DBF
                var om = Parent.Repository.GetPredoplById(PredoplsList.SelectedPredopl.Idpo);
                PredoplsList.SelectedPredopl.PredoplRef = om;
                // ---------------------------
                var sumotgr = om.SumOtgr;

                nDlg = new EditPredoplDlgViewModel(Parent.Repository, PredoplsList.SelectedPredopl.PredoplRef)
                {
                    IsCanChangeVal = (sumotgr == 0),
                    OnSubmit = EditPredoplSubmit
                };
            };
            Action afterwork = () => Parent.OpenDialog(nDlg);

            Parent.Services.DoWaitAction(work, "Подождите", "Загрузка...", afterwork);

        }
        private bool CanExecEditPredoplCommand()
        {
            return !IsReadOnly && Parent != null && PredoplsList != null &&
                PredoplsList.SelectedPredopl != null;
        }        
        
        /// <summary>
        /// Комманда копирования выбраной предоплаты
        /// </summary>
        private ICommand copyPredoplCommand;

        private const string COPYPREDOPLCOMMAND_COMPONENTNAME = "PredoplModule.ViewModels.PredoplsArcViewModel.CopyPredoplCommand";
        [ExportNamedComponent("PredoplModule.ComponentCommand", COPYPREDOPLCOMMAND_COMPONENTNAME)]
        public ICommand CopyPredoplCommand
        {
            get
            {
                if (copyPredoplCommand == null && CheckComponentAccess(COPYPREDOPLCOMMAND_COMPONENTNAME))
                    copyPredoplCommand = new DelegateCommand(ExecCopyPredoplCommand, CanExecCopyPredoplCommand);
                return copyPredoplCommand;
            }
        }
        
        private void ExecCopyPredoplCommand()
        {
            var ovm = PredoplsList.SelectedPredopl;

            EditPredoplDlgViewModel nDlg = null;

            Action getVm = () =>
            {
                // для получения sumotgr из dbf
                var selpoid = ovm.Idpo;
                var om = Parent.Repository.GetPredoplById(selpoid);
                //            
                ovm.PredoplRef = om;

                nDlg = new EditPredoplDlgViewModel(Parent.Repository, om)
                {
                    Title = String.Format("Копия предоплаты № {0} от {1:dd.MM.yyyy}", ovm.NomDok, ovm.DatPost),
                    Action = PredoplEditActions.Copy,
                    OnSubmit = CopyPredoplSubmit
                };
                var newModel = nDlg.NewModel;
                newModel.TrackingState = TrackingInfo.Created;
                newModel.SumOtgr = 0;
                newModel.IdRegDoc = 0;
                newModel.Idpo = 0;
                newModel.IdSourcePO = selpoid;
            };

            Parent.Services.DoWaitAction(getVm, "Подождите", "Подготовка данных предоплаты", () => Parent.OpenDialog(nDlg));
        }

        private bool CanExecCopyPredoplCommand()
        {
            return !IsReadOnly && Parent != null && PredoplsList != null && PredoplsList.SelectedPredopl != null;
        }

        private void CopyPredoplSubmit(object _dlg)
        {
            Parent.CloseDialog(_dlg);
            var dlg = _dlg as EditPredoplDlgViewModel;
            if (dlg == null || dlg.NewModel == null) return;

            var oldmodel = dlg.OldModel;
            var newmodel = dlg.NewModel;

            if (oldmodel.Ndok == newmodel.Ndok
                && oldmodel.Kgr == newmodel.Kgr
                && oldmodel.Poup == newmodel.Poup
                && oldmodel.Pkod == newmodel.Pkod
                && oldmodel.KodVal == newmodel.KodVal
                && oldmodel.IdAgree == newmodel.IdAgree
                && oldmodel.KursVal == newmodel.KursVal
                )
            {
                Parent.Services.ShowMsg("Ошибка", "У копии необходимо изменить какой-нибудь реквизит.", true);
                RefreshSelectedItem();
                return;
            }

            PredoplModel[] existPreds = null;
            Action checkExist = () =>
            {
                var dataExist = Parent.Repository.GetPredopls(new PredoplSearchData { Dfrom = newmodel.DatVvod, Dto = newmodel.DatVvod, Kodval = newmodel.KodVal, Kpok = newmodel.Kgr, Pkod = newmodel.Pkod, Poup = newmodel.Poup });
                if (dataExist != null && dataExist.Length > 0)
                    existPreds = dataExist.Where(p => p.Idpo != oldmodel.Idpo && p.Ndok == oldmodel.Ndok && p.IdAgree == oldmodel.IdAgree && p.KursVal == oldmodel.KursVal).ToArray();
            };
            Action afterCheck = () =>
            {
                if (existPreds != null && existPreds.Length > 0)
                {
                    Parent.Services.ShowMsg("Ошибка", "Копируемая предоплата уже существует.", true);
                    RefreshSelectedItem();
                    var ncontent = new PredoplsArcViewModel(Parent, existPreds.Union(Enumerable.Repeat(oldmodel, 1)))
                    {
                        Title = "Найденные предоплаты с реквизитами копируемой"
                    };
                    ncontent.TryOpen();
                    return;
                }
                else
                    DoAddPredoplAction(newmodel);
            };

            Parent.Services.DoWaitAction(checkExist, "Проверка добавляемой предоплаты", "Подождите...", afterCheck);      
        }

        private void DoAddPredoplAction(PredoplModel _newPredopl)
        {
            Action work = () =>
            {
                var predoplsService = Parent.Services as PredoplService;
                if (predoplsService != null)
                    predoplsService.DoAddPredopl(_newPredopl, PredoplAddKind.Copy);
            };

            Parent.Services.DoWaitAction(work, "Подождите", "Добавление предоплаты ... ");
        }        

        /// <summary>
        /// Комманда разбиения выбраной предоплаты
        /// </summary>
        private ICommand splitPredoplCommand;

        private const string SPLITPREDOPLCOMMAND_COMPONENTNAME = "PredoplModule.ViewModels.PredoplsArcViewModel.SplitPredoplCommand";
        [ExportNamedComponent("PredoplModule.ComponentCommand", SPLITPREDOPLCOMMAND_COMPONENTNAME)]
        public ICommand SplitPredoplCommand
        {
            get 
            {
                if (splitPredoplCommand == null && CheckComponentAccess(SPLITPREDOPLCOMMAND_COMPONENTNAME))
                    splitPredoplCommand = new DelegateCommand(ExecSplitPredoplCommand, CanExecSplitPredoplCommand);
                return splitPredoplCommand;
            }
        }
        private void ExecSplitPredoplCommand()
        {
            var ovm = PredoplsList.SelectedPredopl;

            EditPredoplDlgViewModel nDlg = null;

            Action getVm = () =>
            {
                // для получения sumotgr из dbf
                var selpoid = ovm.Idpo;
                var om = Parent.Repository.GetPredoplById(selpoid);
                //            
                ovm.PredoplRef = om;

                nDlg = new EditPredoplDlgViewModel(Parent.Repository, om)
                {
                    Title = String.Format("Разделение предоплаты № {0} от {1:dd.MM.yyyy}\nОстаток: {2:### ### ### ###.##} {3}", ovm.NomDok, ovm.DatPost, ovm.Ostatok, ovm.ValPropl.ShortName),
                    Action = PredoplEditActions.Split,
                    //IsCanChangeVal = false,
                    OnSubmit = CutPredoplSubmit
                };
                var newModel = nDlg.NewModel;
                newModel.TrackingState = TrackingInfo.Created;
                newModel.SumPropl = 0;
                newModel.SumOtgr = 0;
                newModel.Prim = null;
                newModel.IdSourcePO = selpoid;
            };

            Parent.Services.DoWaitAction(getVm, "Подождите", "Подготовка данных предоплаты", () => Parent.OpenDialog(nDlg));            
        }
        private bool CanExecSplitPredoplCommand()
        {
            return !IsReadOnly && Parent != null && PredoplsList != null &&
                PredoplsList.SelectedPredopl != null && !PredoplsList.SelectedPredopl.IsPaysExist 
                && (PredoplsList.SelectedPredopl.SumPropl > PredoplsList.SelectedPredopl.SumOtgr);
        }

        private void CutPredoplSubmit(Object _dlg)
        {
            Parent.CloseDialog(_dlg);
            var dlg = _dlg as EditPredoplDlgViewModel;
            if (dlg == null || dlg.NewModel == null) return;

            var oldmodel = dlg.OldModel;
            var newmodel = dlg.NewModel;
            newmodel.Idpo = 0;

            if (oldmodel.Ndok == newmodel.Ndok
                && oldmodel.Kgr == newmodel.Kgr
                && oldmodel.Poup == newmodel.Poup
                && oldmodel.Pkod == newmodel.Pkod
                && oldmodel.KodVal == newmodel.KodVal
                && oldmodel.IdAgree == newmodel.IdAgree
                && oldmodel.KursVal == newmodel.KursVal
                )
            {
                Parent.Services.ShowMsg("Ошибка", "У отделённой части необходимо изменить какой-нибудь реквизит.", true);
                RefreshSelectedItem();
                return;
            }

            //var oldsumdelta = CalcOldModelDelta(newmodel.SumPropl, newmodel.KodVal, oldmodel.KodVal, newmodel.DatVvod);
            var oldsumdelta = CalcOldModelDelta(newmodel.SumPropl, newmodel.KodVal, newmodel.KursVal, oldmodel.KodVal, oldmodel.KursVal);

            PredoplModel[] existPreds = null;
            Action checkExist = () =>
            {
                var dataExist = Parent.Repository.GetPredopls(new PredoplSearchData { Dfrom = newmodel.DatVvod, Dto = newmodel.DatVvod, Kodval = newmodel.KodVal, Kpok = newmodel.Kgr, Pkod = newmodel.Pkod, Poup = newmodel.Poup });
                if (dataExist != null && dataExist.Length > 0)
                    existPreds = dataExist.Where(p => p.Idpo != oldmodel.Idpo && p.Ndok == oldmodel.Ndok && p.IdAgree == oldmodel.IdAgree && p.KursVal == oldmodel.KursVal ).ToArray();
            };
            Action afterCheck = () => 
            {
                if (existPreds != null && existPreds.Length > 0)
                {
                    if (existPreds.Length == 1)
                        AskForIncreaseAndSplitPredopl(oldmodel, existPreds[0], oldsumdelta);
                    else
                    {
                        Parent.Services.ShowMsg("Ошибка", "Отделяемая предоплата уже существует.\nДля дополнительного перемещения средств воспользуйтесь функциями корректировки.", true);
                        RefreshSelectedItem();
                        var ncontent = new PredoplsArcViewModel(Parent, existPreds.Union(Enumerable.Repeat(oldmodel, 1)))
                        {
                            Title = "Изменяемые предоплаты"
                        };
                        ncontent.TryOpen();
                        return;
                    }
                }
                else
                    CheckForCloseAndSplit(oldmodel, newmodel, oldsumdelta);
            };

            Parent.Services.DoWaitAction(checkExist, "Проверка добавляемой предоплаты", "Подождите...", afterCheck);            
        }

        private void AskForIncreaseAndSplitPredopl(PredoplModel _oldmodel, PredoplModel _pred2increase, decimal _oldsumdelta)
        {
            var askDlg = new BaseCompositeDlgViewModel()
            {
                Title = "Внимание",
                OnSubmit = (d) =>
                {
                    var dlg = d as BaseCompositeDlgViewModel;
                    Parent.CloseDialog(d);
                    if (dlg == null) return;

                    var choices = (dlg.DialogViewModels[1] as ChoicesDlgViewModel).Groups["1"];
                    if (choices[0].IsChecked ?? false)
                        CheckForCloseAndSplit(_oldmodel, _pred2increase, _oldsumdelta);
                    else
                        if (choices[2].IsChecked ?? false)
                        {
                            var ncontent =new PredoplsArcViewModel(Parent, new PredoplModel[] {_oldmodel, _pred2increase})
                            {
                                Title = "Изменяемые предоплаты"
                            };
                            ncontent.TryOpen();
                        }
                }
            };
            
            var messDlg = new MsgDlgViewModel
            {
                Message = "Отделяемая предоплата уже существует.\n"
            };

            var chDlg = new ChoicesDlgViewModel(new Choice { GroupName = "1", Header = "Да      ", IsChecked = true, IsSingleInGroup = true },
                                                new Choice { GroupName = "1", Header = "Нет     ", IsChecked = false, IsSingleInGroup = true },
                                                new Choice { GroupName = "1", Header = "Показать", IsChecked = false, IsSingleInGroup = true }) 
                                                {
                                                    Title = "Перенести отделяемую сумму на неё?"
                                                };

            askDlg.Add(messDlg);
            askDlg.Add(chDlg);

            Parent.OpenDialog(askDlg);
        }

        private void CheckForCloseAndSplit(PredoplModel _oldmodel, PredoplModel _newmodel, decimal _oldsumdelta)
        {
            if (_oldsumdelta <= 0)
            {
                Parent.Services.ShowMsg("Ошибка", "Сумма отделения должна быть больше 0.", true);
                RefreshSelectedItem();
                return;
            }
            var ostaftersplit = _oldmodel.SumPropl - _oldmodel.SumOtgr - _oldsumdelta;
            if (ostaftersplit < 0)
            {
                Parent.Services.ShowMsg("Ошибка", "Остаток предоплаты после отделения будет меньше 0.", true);
                RefreshSelectedItem();
                return;
            }

            if (ostaftersplit == 0)
                AskForCloseAndSplitPredopl(_oldmodel, _newmodel, _oldsumdelta);
            else
                DoSplitPredoplAction(_oldmodel, _newmodel, _oldsumdelta);
        }

        private decimal CalcOldModelDelta(decimal _newsum, string _nkodval, decimal _nkursval, string _okodval, decimal _okursval)
        {
            decimal res = 0;

            if (_newsum != 0)
            {
                if (_nkodval == _okodval)
                    res = _newsum;
                else
                {
                    res = Parent.Repository.ConvertSumToVal(_newsum, _nkodval, _okodval, null, _nkursval, _okursval);
                }
            }

            return res;
        }

        private void AskForCloseAndSplitPredopl(PredoplModel _oldm, PredoplModel _newm, decimal _oldsumdelta)
        {
            var datedlg = new DateDlgViewModel
            {
                Title = "Укажите дату закрытия разделяемой предоплаты",
                SelDate = DateTime.Now,
                OnSubmit = d =>
                    {
                        Parent.CloseDialog(d);
                        var dlg = d as DateDlgViewModel;
                        _oldm.DatZakr = dlg.SelDate;
                        DoSplitPredoplAction(_oldm, _newm, _oldsumdelta);                        
                    }
            };
            Parent.OpenDialog(datedlg);
        }

        private void DoSplitPredoplAction(PredoplModel _oldm, PredoplModel _newm, decimal _oldsumdelta)
        {
            bool result = false;
            string resultMes = null;

            Action splitwork = () =>
            {
                var oldPred = _oldm;
                oldPred.SumPropl -= _oldsumdelta;//_newm.SumPropl;
                string newPrim = String.Format("\nРазделена. Новая предоплата: №{0} от {1:dd.MM.yyyy}. Сумма {2:### ### ### ###.##} {3}. Пользователь: {4}", _newm.Ndok, _newm.DatVvod, _oldsumdelta, _newm.KodVal, Parent.ShellModel.CurrentUserInfo.Title);
                oldPred.Prim += newPrim;
                var updated = Parent.Repository.PredoplUpdate(oldPred, out result);

                
                if (result && updated != null)
                {
                    resultMes = String.Format("Разделена предоплата {0} от {1:dd.MM.yyyy}\n", updated.Ndok, updated.DatVvod);
                    PredoplModel newinDb = null;
                    bool isnew = _newm.Idpo == 0;
                    if (isnew)
                        newinDb = Parent.Repository.PredoplInsert(_newm, (int)PredoplAddKind.Cut, out result);
                    else
                    {
                        _newm.DatZakr = null;
                        _newm.SumPropl += _oldsumdelta;
                        newinDb = Parent.Repository.PredoplUpdate(_newm, out result);
                    }
                    if (result && newinDb != null)
                    {
                        resultMes += String.Format("{0} предоплата {1} на сумму {2} {3}", isnew ? "Добавлена" : "Увеличена", newinDb.Ndok, _oldsumdelta, newinDb.KodVal);
                        var newPredVm = new PredoplViewModel(Parent.Repository, newinDb);

                        Parent.ShellModel.UpdateUi(
                            () => RefreshSelectedItemByModel(updated), true, false);
                        
                        var ncontent = new PredoplsArcViewModel(Parent, new PredoplModel[] {oldPred, newinDb})
                        {
                            Title = "Изменённые предоплаты"
                        };
                        ncontent.TryOpen();
                    }
                    else
                        resultMes = isnew ? "Ошибка при добавлении предоплаты!" : "Ошибка при увеличении предоплаты!";
                }
                else
                    resultMes = "Ошибка при разделении предоплаты!";
            };

            Action afterwork = () =>
            {
                if (!String.IsNullOrEmpty(resultMes))
                    Parent.Services.ShowMsg("Информация", resultMes, !result);
            };

            Parent.Services.DoWaitAction(splitwork, "Подождите", "Разделение предоплаты", afterwork);          
        }

        /// <summary>
        /// Комманда удаления выбраной предоплаты
        /// </summary>
        private ICommand deletePredoplCommand;
        public ICommand DeletePredoplCommand
        {
            get 
            {
                if (deletePredoplCommand == null)
                    deletePredoplCommand = new DelegateCommand(ExecDeletePredopl, CanExecDeletePredopl);
                return deletePredoplCommand;
            }
        }
        private void ExecDeletePredopl()
        {
            var nDlg = new MsgDlgViewModel()
            {
                Title = "Подтверждение",
                Message = "Удалить выбранную предоплату?",
                OnSubmit = DeletePredopl
            };
            Parent.OpenDialog(nDlg);
        }
        private bool CanExecDeletePredopl()
        {
            return !IsReadOnly && Parent != null && PredoplsList != null 
                && PredoplsList.SelectedPredopl != null 
                && !PredoplsList.SelectedPredopl.IsPaysExist
                && PredoplsList.SelectedPredopl.SumOtgr == 0;
        }

        private ICommand showSfsCommand;

        /// <summary>
        /// Показывает оплаченные счета
        /// </summary>
        public ICommand ShowSfsCommand
        {
            get
            {
                if (showSfsCommand == null)
                    showSfsCommand = new DelegateCommand(ExecShowSfs, CanExecShowSfs);
                return showSfsCommand;
            }
        }
        private void ExecShowSfs()
        {
            if (sfModule == null) return;
            SfInListInfo[] sfs = PredoplsList.SelectedPredopl.PayedSfs
                .Select(s => Parent.Repository.GetSfInListInfo(s.IdSf)).ToArray();
            if (sfs.Length > 0)
            {
                sfModule.ListSfs(sfs, "Счета-фактуры, оплаченные документом № " + PredoplsList.SelectedPredopl.NomDok);
                Parent.ShellModel.LoadModule(sfModule);
            }
        }
        private bool CanExecShowSfs()
        {
            return sfModule != null &&
                Parent != null && PredoplsList != null &&
                PredoplsList.SelectedPredopl != null && PredoplsList.SelectedPredopl.CountSfs > 0;
        }


        /// <summary>
        /// Сохранение изменённой предоплаты
        /// </summary>
        /// <param name="obj"></param>
        private void EditPredoplSubmit(Object _dlg)
        {
            Parent.CloseDialog(_dlg);
            var dlg = _dlg as EditPredoplDlgViewModel;
            if (dlg == null || dlg.NewModel == null) return;

            if (dlg.NewModel.TrackingState == TrackingInfo.Updated)
            {
                var nmodel = dlg.NewModel;
                if (nmodel.DatZakr == null && nmodel.SumPropl == nmodel.SumOtgr)
                    AskForCloseAndUpdatePredopl(nmodel);
                else
                {
                    if (nmodel.SumPropl < nmodel.SumOtgr)
                        Parent.Services.ShowMsg("Ошибка", "Сумма погашений превышает сумму предоплаты", true);
                    else 
                    {
                        if (nmodel.SumPropl > nmodel.SumOtgr)
                            nmodel.DatZakr = null;
                        DoPredoplUpdate(nmodel);
                    }
                }               
            }
        }

        private void AskForCloseAndUpdatePredopl(PredoplModel _newm)
        {
            var datedlg = new DateDlgViewModel
            {
                Title = "Укажите дату закрытия предоплаты",
                SelDate = DateTime.Now,
                OnSubmit = d =>
                {
                    Parent.CloseDialog(d);
                    var dlg = d as DateDlgViewModel;
                    _newm.DatZakr = dlg.SelDate;
                    DoPredoplUpdate(_newm);
                }
            };
            Parent.OpenDialog(datedlg);
        }

        private void DoPredoplUpdate(PredoplModel _p)
        {
            bool isSuccess;
            var newPrModel = Parent.Repository.PredoplUpdate(_p, out isSuccess);
            if (!isSuccess)
            {
                Parent.Services.ShowMsg("Ошибка", "Ошибка при сохранении изменений", true);
                newPrModel = Parent.Repository.GetPredoplById(_p.Idpo);
            }

            RefreshSelectedItemByModel(newPrModel);
        }

        private void RefreshSelectedItem()
        {
            var selpred = PredoplsList.SelectedPredopl;
            if (selpred == null || selpred.Idpo == 0) return;

            Parent.ShellModel.UpdateUi(() => PredoplsList.RefreshItem(selpred), true, false);
        }

        private void RefreshSelectedItemByModel(PredoplModel _newPrModel)
        {
            int indOfSelected = PredoplsList.IndexOf(PredoplsList.SelectedPredopl);
            PredoplsList.RemoveAt(indOfSelected);
            if (_newPrModel != null)
            {
                var newPrViewModel = new PredoplViewModel(Parent.Repository, _newPrModel);
                PredoplsList.Insert(indOfSelected, newPrViewModel);
                PredoplsList.SelectedPredopl = newPrViewModel;
            }

        }

        /// <summary>
        /// Удаление предоплаты
        /// </summary>
        /// <param name="obj"></param>
        private void DeletePredopl(Object obj)
        {
            Parent.CloseDialog(obj);

            var selPredopl = PredoplsList.SelectedPredopl;
            if (selPredopl == null) return;

            bool res = Parent.Repository.DeletePredopl(selPredopl.Idpo);
            if (res)
            {
                PredoplsList.Predopls.Remove(selPredopl);
                PredoplsList.SelectedPredopl = null;
            }
            else
            {
                PredoplsList.RefreshItem(selPredopl);
                Parent.Services.ShowMsg("Ошибка", "Не удалось удалить предоплату", true);
            }
        }

        

        /// <summary>
        /// Комманда отмены оплаты по выбраной предоплате
        /// </summary>
        private ICommand undoPaysCommand;
        public ICommand UndoPaysCommand
        {
            get
            {
                if (undoPaysCommand == null)
                    undoPaysCommand = new DelegateCommand(SelectPaysForUndo, CanUndoPays);
                return undoPaysCommand;
            }
        }

        private void SelectPaysForUndo()
        {
            var selpredopl = PredoplsList.SelectedPredopl;
            PayAction[] pacts = null;
            
            pacts = Parent.Repository.GetPayActions(selpredopl.Idpo, 0);

            Parent.OpenDialog(new SelectPaysForUndoDlgViewModel(Parent.Repository, pacts)
            {
                Title = String.Format("Выберите отменяемые погашения предоплаты № {0}", PredoplsList.SelectedPredopl.NomDok),
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
                Message = String.Format("Внимание!\nВыбрано {0} погашений на сумму {1}\nВы уверены, что хотите отменить выбранные погашения?", pacs.Length, pacs.Sum(p => p.SumOpl)),
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
            if (_pacs == null || _pacs.Length == 0) return;

            WaitDlgViewModel wdlg = new WaitDlgViewModel
            {
                Title = "Выполнение операции"
            };

            int selidpo = _pacs[0].IdPo;
            bool undone = false;
            List<string> errors = new List<string>();
            Action<WaitDlgViewModel> work;
            
            work = (d) =>
            {
                DateTime atime = DateTime.Now;                
                for (int i = 0; i < _pacs.Length; i++)
                {
                    var pa = _pacs[i];
                    string pasf = pa.Idsf == 0 ? "" 
                                               : pa.PayActionType == PayActionTypes.Sf ? String.Format("С/ф № {0}", pa.Numsf)
                                                                                       : String.Format("Возврат № {0}", pa.Numsf);
                    string papo = pa.IdPo == 0 ? "" : String.Format("Платёжка № {0}", pa.Ndoc);
                    d.Message = String.Format("Отмена погашения {0}/{1}:\n{2} {3} Сумма:{4}", i, _pacs.Length, pasf, papo, pa.SumOpl);
                    bool undores = Parent.Repository.UndoPayAction(pa, atime);
                    if (!undores)
                        errors.Add(d.Message);
                    else
                        if (pa.Idsf != 0)
                            Parent.Repository.SetSfCurPayStatus(pa.Idsf, PayActions.UndoPays, atime);
                    undone |= undores;
                }
                if (undone)
                    Parent.Repository.SetPredolpStatus(selidpo, PredoplStatuses.UndoPays, atime);
            };

            Action afterwork = () =>
            {
                Parent.ShellModel.UpdateUi(() => PredoplsList.RefreshItem(PredoplsList.SelectedPredopl), true, true);
                if (errors.Count > 0)
                    Parent.Services.ShowMsg("Ошибки отмены погашений", String.Join("\n", errors.ToArray()), true);
                if (undone)
                    ShowUndoPaysReport();
            };

            Parent.Services.DoWaitAction(work, "Подождите", "Аннулирование погашений предоплаты", afterwork);
        }

        private void ShowUndoPaysReport()
        {
            ReportModel repm = new ReportModel
            {
                Title = String.Format("Протокол аннулирования погашений предоплаты"),
                Path = @"/real/Reports/ClearPaymentsProtokol",
                Parameters = new Dictionary<string, string> { { "ConnString", Parent.Repository.ConnectionString } }
            };
            (new ReportViewModel(Parent, repm)).TryOpen();
        }

        private bool CanUndoPays()
        {
            return !IsReadOnly && PredoplsList.SelectedPredopl != null && PredoplsList.SelectedPredopl.SumOtgr > 0;
        }

        private const string SHOWHISTORYCOMMAND_COMPONENTNAME = "PredoplModule.ViewModels.PredoplsArcViewModel.ShowHistoryCommand";
        [ExportNamedComponent("PredoplModule.ComponentCommand", SHOWHISTORYCOMMAND_COMPONENTNAME)]
        private ICommand showHistoryCommand;
        public ICommand ShowHistoryCommand
        {
            get
            {
                if (showHistoryCommand == null && CheckComponentAccess(SHOWHISTORYCOMMAND_COMPONENTNAME))
                    showHistoryCommand = new DelegateCommand(ExecShowHistory, () => predoplsList != null && predoplsList.SelectedPredopl != null);
                return showHistoryCommand;
            }
        }
        private void ExecShowHistory()
        {
            Action work = () =>
            {
                var pHistory = Parent.Repository.GetPredoplHistory(predoplsList.SelectedPredopl.Idpo);
                Parent.OpenDialog(new HistoryDlgViewModel(pHistory)
                {
                    Title = "История предоплаты № " + predoplsList.SelectedPredopl.NomDok.ToString()
                });
            };
            Parent.Services.DoWaitAction(work, "Подождите", "Запрос истории предоплаты...");
        }

        /// <summary>
        /// Комманда обновления
        /// </summary>
        public ICommand RefreshCommand { get; set; }

        private void RefreshData()
        {
            Action work = () =>
            {
                if (loadMode == LoadMode.ByParams)
                    LoadData();
                else
                {
                    var selPredopl = predoplsList.SelectedPredopl;
                    var idpos = predoplsList.Predopls.Select(pvm => pvm.PredoplRef.Idpo);
                    var pos = idpos.Select(i => Parent.Repository.GetPredoplById(i)).ToArray();
                    Parent.ShellModel.UpdateUi(() =>
                    {
                        predoplsList.LoadData(pos);
                        predoplsList.SelectedPredopl = selPredopl == null ? predoplsList.Predopls.FirstOrDefault()
                                                                          : predoplsList.Predopls.FirstOrDefault(p => p.Idpo == selPredopl.Idpo);
                    }, true, false);
                }
            };

            Parent.Services.DoWaitAction(work, "Подождите", "Обновление данных");
        }
    }
}