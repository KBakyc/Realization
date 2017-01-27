using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using CommonModule;
using CommonModule.Composition;
using CommonModule.Helpers;
using CommonModule.Interfaces;
using CommonModule.ViewModels;
using DataObjects;
//using DAL;
using CommonModule.ModuleServices;
using SfModule.Helpers;
using CommonModule.Commands;
using CommonModule.DataViewModels;

namespace SfModule.ViewModels
{
    [ExportModule(DisplayOrder = 4f)]
    [Export(typeof(ISfModule))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class SfModuleViewModel : PagesModuleViewModel, ISfModule, IPartImportsSatisfiedNotification
    {
        public SfModuleViewModel()
        {
            //Xceed.Wpf.DataGrid.Licenser.LicenseKey = "DGP36-DZ3KR-0HPZ8-5YKA";

            Info = new ModuleDescription()
            {
                Name = Properties.Settings.Default.Name,
                Description = Properties.Settings.Default.Description,
                Version = Properties.Settings.Default.Version,
                Header = Properties.Settings.Default.Header,
                IconUri = @"/SfModule;component/Resources/invoice.png"
            };
        }

        //[ImportMany("SfModule.ModuleCommand")]
        //private Lazy<ModuleCommand, IDisplayOrderMetaData>[] moduleCommands;
        
        //public ModuleCommand[] ModuleCommands { get; set; }

        /// <summary>
        /// Показывает предварительный просмотр модели счёта
        /// </summary>
        /// <param name="m"></param>
        public void ShowSf(SfModel _sm)
        {
            var serv = Services as SfService;
            if (serv == null) return;
            serv.ShowSf(_sm);
        }

        /// <summary>
        /// Отображение списка счетов
        /// </summary>
        /// <param name="ms"></param>
        /// <param name="t"></param>
        public void ListSfs(IEnumerable<SfInListInfo> ms, string t)
        {
            if (ms != null)
            {
                var sfsVm = ms.Select(h => new SfInListViewModel(Repository, h));
                var nContent = new R635ViewModel(this, sfsVm)
                { 
                    Title = t,
                    RefreshCommand = new DelegateCommand<R635ViewModel>(vm =>
                    {
                        Action wk = () =>
                        {
                            var newSfVms = vm.SfItogList.Select(li => new SfInListViewModel(Repository, li.SfRef.IdSf)).ToArray();
                            ShellModel.UpdateUi(() => vm.LoadData(newSfVms), true, false);
                        };
                        Services.DoWaitAction(wk, "Ожидание выполнения", "Выборка и обновление списка счетов...");
                    })
                };
                nContent.TryOpen();
            }
        }
        
        public void ListPenalties(IEnumerable<PenaltyModel> ms, string t)
        {
            if (ms != null)
            {
                var nContent = new PenaltyArcViewModel(this, ms)
                { 
                    Title = t,
                    RefreshCommand = new DelegateCommand<PenaltyArcViewModel>(vm =>
                    {
                        Action wk = () =>
                        {
                            var newmodels = vm.PenaltyList.Select(li => vm.Parent.Repository.GetPenaltyById(li.PenRef.Id)).ToArray();
                            ShellModel.UpdateUi(() => vm.LoadData(newmodels), true, false);
                        };
                        Services.DoWaitAction(wk, "Ожидание выполнения", "Выборка и обновление списка штрафных санкций...");
                    })
                };
                nContent.TryOpen();
            }
        }

        /// <summary>
        /// Изменение данных счёта
        /// </summary>
        /// <param name="m"></param>
        public void EditSf(SfModel m, Action<SfModel> callback)
        {
            Action work = () =>
            {
                var editedVM = new SfViewModel(Repository, m, false);
                var editDlg = new SfEditDlgViewModel(Repository, editedVM)
                {
                    Title = "Изменение данных счёта",
                    OnSubmit = d =>
                    {
                        var res = SubmitEditSf(d);
                        if (callback != null)
                            callback(res);
                    }
                };
                OpenDialog(editDlg);
            };
            Services.DoWaitAction(work, "Подождите", "Загрузка данных счёта...");
        }

        /// <summary>
        /// Обратный вызов из диалога редактирования
        /// </summary>
        /// <param name="obj"></param>
        private SfModel SubmitEditSf(object _dlg)
        {
            CloseDialog(_dlg);
            
            SfEditDlgViewModel dlg = _dlg as SfEditDlgViewModel;
            if (dlg == null) return null;
            
            SfViewModel SelectedSf = dlg.SfVMRef;
            SfModel oldSfModel = Repository.GetSfModel(SelectedSf.SfRef.IdSf);
            SfModel newSfModel = SelectedSf.SfRef; ;
            bool isSuccess = true;
            bool isSfChanged = false;
            bool isAdvChanged = false;
            string errmsg = "";

            // сохранение платежей
            var payschanges = dlg.SfProductPays.GetChanges();
            if (payschanges.Length > 0)
            {
                isSfChanged = true;
                
                SfProductPayModel newPModel = null;
                for (int i = 0; i < payschanges.Length; i++)
                {
                    newPModel = Repository.SfProductPayUpdate(payschanges[i].ModelRef, out isSuccess);
                    if (!isSuccess)
                    {
                        errmsg = string.Format("Ошибка изменения : [{0}]",payschanges[i]);
                        break;
                    }
                }
            }

            // сохранение шапки счёта
            if ((isSfChanged || SelectedSf.TrackingState == TrackingInfo.Updated) && isSuccess)
            {
                isSfChanged = true;
                newSfModel = Repository.SfHeaderUpdate(SelectedSf.SfRef, out isSuccess);
                if (!isSuccess)
                    errmsg = string.Format("Ошибка изменения данных счёта: [{0}]", SelectedSf.NumSf);
            }

            // сохранение сроков оплаты
            if (SelectedSf.SfPeriod != null && isSuccess)
            {
                if (SelectedSf.SfPeriod.TrackingState == TrackingInfo.Updated)
                {
                    isAdvChanged = true;
                    SelectedSf.SfPeriod = Repository.SfPeriodUpdate(SelectedSf.SfPeriod, out isSuccess);
                } 
                else
                if (SelectedSf.SfPeriod.TrackingState == TrackingInfo.Created)
                {
                    isAdvChanged = true;
                    SelectedSf.SfPeriod = Repository.SfPeriodInsert(SelectedSf.SfPeriod, out isSuccess);
                }

                if (!isSuccess)
                    errmsg = "Ошибка изменения сроков оплаты";
            }

            if (dlg.IsKroInfoUpdated && isSuccess)
            {
                bool krores = Repository.SfKroInfoUpdate(SelectedSf.SfRef.IdSf, dlg.KroDate);
                if (!krores)
                {
                    var msg = string.Format("Ошибка изменения информации по КРО счёта: [{0}]", oldSfModel.NumSf);
                    Services.ShowMsg("Ошибка", msg, true);
                }
            }            

            if (!isSuccess)
                Services.ShowMsg("Ошибка", errmsg, true);

            return isSuccess && newSfModel != null 
                    ? newSfModel 
                    : oldSfModel;
 
        }

        /// <summary>
        /// Печать счетов (асинхронно)
        /// </summary>
        /// <param name="_sfs"></param>
        public void PrintSfs(IEnumerable<SfModel> _sfs)
        {
            int poup = _sfs.First().Poup;
            ApplyFeature withSigns = CommonSettings.GetNeedSignsModeForPoup(poup);
            if (withSigns == ApplyFeature.Ask)
                PrintSfsWithAsk(_sfs);
            else
            {
                bool isPrintSigns = withSigns == ApplyFeature.Yes ? true : false;
                ExecPrintSfs(_sfs, isPrintSigns);
            }
        }

        private void PrintSfsWithAsk(IEnumerable<SfModel> _sfs)
        {
            Choice isPrintSigns = new Choice { GroupName = "Печатать", Header = "Подписи", IsChecked = true, IsSingleInGroup = false };
            var optDlg = new ChoicesDlgViewModel(isPrintSigns)
            {
                Title = @"Опции печати",
                OnSubmit = d =>
                {
                    CloseDialog(d);
                    ExecPrintSfs(_sfs, isPrintSigns.IsChecked ?? false);
                }
            };
            OpenDialog(optDlg);
        }

        private void ExecPrintSfs(IEnumerable<SfModel> _sfs, bool _prnSigns)
        {
            var phelper = new PrintHelper() { IsAsync = false };
            phelper.GetPrintSettings();
            if (!phelper.IsSettingsOk) return;

            Action work = () =>
            {
                var repSfsForms = _sfs.Select(sm => Repository.GetSfPrintForm(sm.IdSf));
                if (repSfsForms.Any())
                {
                    foreach (var r in repSfsForms)
                    {
                        r.Parameters["issign"] = _prnSigns.ToString();
                        ReportHelper.PrintReport(this, phelper, r);
                    }
                }
            };

            Services.DoWaitAction(work, "Подождите", "Формирование документов для печати");
        }

        protected override IModuleService GetModuleService()
        {
            return new SfService(this); ;
        }

        #region IPartImportsSatisfiedNotification Members

        public void OnImportsSatisfied()
        {
            LoadCommands("SfModule.ModuleCommand");
        }

        #endregion

    }
}
