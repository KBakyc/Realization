using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Input;
using CommonModule.Commands;
using CommonModule.Helpers;
using CommonModule.Interfaces;
using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Helpers;
using CommonModule.DataViewModels;

namespace SfModule.ViewModels
{
    public class AcceptSfsViewModel : BasicModuleContent
    {
        /// <summary>
        /// Ссылка на модуль
        /// </summary>
        private ISfModule sfModule;
        private IOtgruzModule otgruzModule;
        private IEnumerable<SfModel> sfsLst;

        public AcceptSfsViewModel(IModule _parent, IEnumerable<SfModel> _lst)
            :base(_parent)
        {
            sfsLst = _lst;
            SelectDeselectAllCommand = new DelegateCommand<bool>(ExecSelectDeselectAll);
            sfModule = _parent as ISfModule;
            otgruzModule = _parent.ShellModel.Container.GetExportedValueOrDefault<IOtgruzModule>();
            sfItogList = new ObservableCollection<Selectable<SfViewModel>>(
                    sfsLst.Select(m => new Selectable<SfViewModel>
                                                    (new SfViewModel(_parent.Repository, m, true),
                                                    true)));
        }       

        /// <summary>
        /// Список счетов-фактур
        /// </summary>
        private ObservableCollection<Selectable<SfViewModel>> sfItogList;
        public ObservableCollection<Selectable<SfViewModel>> SfItogList 
        { 
            get { return sfItogList; }
        }

        /// <summary>
        /// Выбранный счёт
        /// </summary>
        private Selectable<SfViewModel> selectedSf;
        public Selectable<SfViewModel> SelectedSf
        {
            get { return selectedSf; }
            set
            {
                if (IsLoaded && value != selectedSf)
                {
                    selectedSf = value;
                    NotifyPropertyChanged("SelectedSf");
                }
            }
        }

        /// <summary>
        /// Комманда выделения/снятия выделения всех счетов
        /// </summary>
        public ICommand SelectDeselectAllCommand { get; set; }
        private void ExecSelectDeselectAll(bool _select)
        {
            foreach (var s in sfItogList)
                s.IsSelected = _select;
        }


        /// <summary>
        /// Комманда принятия выбранных сформированных счетов и отмены невыбранных
        /// </summary>
        private ICommand acceptSfsCommand;
        public ICommand AcceptSfsCommand
        {
            get
            {
                if (acceptSfsCommand == null)
                    acceptSfsCommand = new DelegateCommand(ExecAcceptSfs, () => SfItogList.Count > 0);
                return acceptSfsCommand;
            }
        }
        private void ExecAcceptSfs()
        {
            sfModule.OpenDialog(new MsgDlgViewModel()
            {
                Title = "Приём",
                Message = "Принять выбранные счета и удалить невыбранные?",
                OnSubmit = DoAcceptSfs
            });
        }

        private void DoAcceptSfs(Object _dlg)
        {
            Parent.CloseDialog(_dlg);
            
            string accmsg = "Приём выбранных счетов завершён успешно";
            var sid = SfItogList.Where(m => m.IsSelected).Select(m => m.Value.SfRef.IdSf).ToArray();
            
            SfItogList.Clear();

            bool isErr = false;
            
            Action work = () =>
            {
                if (sid.Length > 0)
                {
                    var uid = Parent.Repository.AcceptSfs(sid);
                    
                    if (uid != null && uid.Length > 0)
                    {
                        isErr = true;
                        StringBuilder lsb = new StringBuilder("Не приняты следующие счета:");
                        
                        for (int i = 0; i < uid.Length; i++)
                        {
                            var unacceptedModel = Parent.Repository.GetSfModel(uid[i]);
                            var nvm = new SfViewModel(Parent.Repository, unacceptedModel);
                            
                            var newItem =  new Selectable<SfViewModel>(nvm) { IsSelected = false };
                            Parent.ShellModel.UpdateUi(()=>sfItogList.Add(newItem), true, false);
                            
                            lsb.AppendFormat(" {0},", nvm.NumSf);
                        }
                        lsb.Remove(lsb.Length - 1, 1).Append('.');
                        accmsg = lsb.ToString();
                    }
                    else
                        Parent.Repository.DeleteUnacceptedSfs();
                    
                    if (sfModule != null && (uid == null || sid.Length > uid.Length))
                    {
                        var aid = sid.Except(uid);
                        var arcVM = new R635ViewModel(Parent, aid.Select(id => new SfInListViewModel(Parent.Repository, id)))
                                    {
                                        Title = String.Format("Счета: Напр. {0}, с {1:dd/MM/yy} по {2:dd/MM/yy}", SelectedPoup.Kod, DateFrom, DateTo),
                                        SelectedPoup = SelectedPoup,
                                        DateFrom = DateFrom,
                                        DateTo = DateTo,
                                        RefreshCommand = new DelegateCommand<R635ViewModel>(vm =>
                                        {
                                            Action wk = () =>
                                            {
                                                var newsfs = vm.SfItogList.ToArray();
                                                Parent.ShellModel.UpdateUi(() => vm.LoadData(newsfs), true, false);
                                            };
                                            Parent.Services.DoWaitAction(wk, "Ожидание выполнения", "Выборка и обновление списка счетов...");
                                        })
                                    };
                        arcVM.TryOpen();
                    }
                }
                else
                    Parent.Repository.DeleteUnacceptedSfs();
            }; // -- action
            
            Action afterwork = () => 
            {
                if (!isErr)
                    Parent.UnLoadContent(this);
                Parent.Services.ShowMsg("Приём", accmsg, isErr);
            };
            
            Parent.Services.DoWaitAction(work, "Обработка", "Подтверждение счетов и синхронизация", afterwork);
        }

        private ICommand deleteUnacceptedSfsCommand;
        /// <summary>
        /// Комманда удаления сформированных счетов
        /// </summary>
        public ICommand DeleteUnacceptedSfsCommand
        {
            get
            {
                if (deleteUnacceptedSfsCommand == null)
                    deleteUnacceptedSfsCommand = new DelegateCommand(ExecDeleteUnacceptedSfs, () => SfItogList.Count > 0);
                return deleteUnacceptedSfsCommand;
            }
        }
        private void ExecDeleteUnacceptedSfs()
        {
            sfModule.OpenDialog(new MsgDlgViewModel()
            {
                Title = "Отмена сформированных счетов",
                Message = "Удалить сформированные счета?",
                OnSubmit = DoDeleteUnacceptedSfs
            });
        }

        private void DoDeleteUnacceptedSfs(Object _dlg)
        {
            Parent.CloseDialog(_dlg);

            Action work = () => Parent.Repository.DeleteUnacceptedSfs();
            Action afterwork = () => Parent.UnLoadContent(this);

            Parent.Services.DoWaitAction(work, "Обработка", "Удаление неподтверждённых счетов", afterwork);
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
            return SelectedSf != null && otgruzModule != null;
        }
        private void ExecShowSfOtgr()
        {
            otgruzModule.ShowOtgrArc(SelectedSf.Value.SfRef.IdSf);
            sfModule.ShellModel.LoadModule(otgruzModule);
        }


        /// <summary>
        /// Комманда просмотра счёта
        /// </summary>
        private ICommand showSfCommand;
        public ICommand ShowSfCommand 
        {
            get 
            { 
                if (showSfCommand==null)
                    showSfCommand = new DelegateCommand(ExecShowSelectedSf, ()=>SelectedSf!=null);
                return showSfCommand;
            }
        }

        private void ExecShowSelectedSf()
        {
            sfModule.ShowSf(SelectedSf.Value.SfRef);
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

            IEnumerable<Selectable<SfViewModel>> sfs;
            if (printAllChoice.IsChecked ?? false)
                sfs = sfItogList;
            else
                sfs = sfItogList.Where(li => li.IsSelected);

            if (sfs.Any())
                sfModule.PrintSfs(sfs.Select(s => s.Value.SfRef));
        }

        public PoupModel SelectedPoup { get; set; }
        public PkodModel SelectedPkod { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }

     }
}