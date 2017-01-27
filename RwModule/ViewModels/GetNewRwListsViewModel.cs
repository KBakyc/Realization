using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.ViewModels;
using CommonModule.Interfaces;
using RwModule.Models;
using System.Windows.Input;
using CommonModule.Commands;
using DAL;
using System.Collections.ObjectModel;
using CommonModule.Helpers;
using DataObjects;

namespace RwModule.ViewModels
{
    public class GetNewRwListsViewModel : BasicModuleContent
    {
        public GetNewRwListsViewModel(IModule _parent, IEnumerable<RwListViewModel> _rwlst)
            : base(_parent)
        {
            if (_rwlst != null && _rwlst.Any())
                rwListCollection = new ObservableCollection<Selectable<RwListViewModel>>(_rwlst.Select(l => new Selectable<RwListViewModel>(l, true)));

            AcceptRwListsCommand = new DelegateCommand(ExecuteAccept, CanExecuteAccept);
            RwListCheckReportCommand = new DelegateCommand(ExecuteCheckReport, CanCheckReport);
            MarkRwListReceivedCommand = new DelegateCommand<Selectable<RwListViewModel>>(ExecMarkReceived, CanExecMarkReceived);
        }

        private ObservableCollection<Selectable<RwListViewModel>> rwListCollection;
        public ObservableCollection<Selectable<RwListViewModel>> RwListCollection
        {
            get { return rwListCollection; }
        }

        private Selectable<RwListViewModel> selectedRwList;
        public Selectable<RwListViewModel> SelectedRwList
        {
            get { return selectedRwList; }
            set { SetAndNotifyProperty("SelectedRwList", ref selectedRwList, value); }
        }

        public ICommand RwListCheckReportCommand { get; set; }

        private bool CanCheckReport()
        {
            return selectedRwList != null;
        }

        private void ExecuteCheckReport()
        {
            var report = new ReportModel
            { 
                Mode = ReportModes.Server, 
                Title = "Протокол по перечню", 
                Path = @"/real/Reports/RwDiffReport",
                Parameters = new Dictionary<string,string>{{"keykrt", selectedRwList.Value.Keykrt.ToString()}}
            };
            (new ReportViewModel(Parent, report)).TryOpen();
        }

        public ICommand AcceptRwListsCommand { get; set; }

        private bool CanExecuteAccept()
        {
            return rwListCollection != null && rwListCollection.Count > 0;
        }

        private void ExecuteAccept()
        {
            bool res = true;

            RwListViewModel lastRwl = null;

            List<long> keys = new List<long>();

            Action<ProgressDlgViewModel> work = (dlg) =>
            {
                dlg.FinishValue = rwListCollection.Count;
                using (var db = new RealContext())
                {
                    foreach (var rwl in rwListCollection.Where(sl => sl.IsSelected))
                    {
                        dlg.Message = String.Format("Принимается перечень № {0}\n{1} из {2}", rwl.Value.Num_rwlist, dlg.CurrentValue + 1, dlg.FinishValue);
                        lastRwl = rwl.Value;
                        res = db.AcceptNewRwList(rwl.Value.Keykrt);
                        if (!res) break;
                        keys.Add(rwl.Value.Keykrt);
                        dlg.CurrentValue++;                        
                        //System.Threading.Thread.Sleep(1000);
                    }
                    Parent.ShellModel.UpdateUi(() =>
                    {
                        foreach (var k in keys)
                        {
                            var rwl = rwListCollection.SingleOrDefault(l => l.Value.Keykrt == k);
                            if (rwl != null)
                                rwListCollection.Remove(rwl);
                        }
                    }, true, false);
                }
            };

            Action after = () => 
            {
                if (!res) Parent.Services.ShowMsg("Ошибка", "Сбой при приёмке перечня №" + lastRwl.Num_rwlist.ToString(), true);
                else
                {
                    Parent.UnLoadContent(this);
                    Parent.Services.ShowMsg("Результат", "Новые ЖД перечни успешно приняты", false);
                    RwList[] newRwls = null;
                    using (var db = new RealContext())
                    {
                        newRwls = db.RwLists.Where(l => keys.Contains(l.Keykrt)).ToArray();
                    }
                    if (newRwls != null && newRwls.Length > 0)
                        (new RwListsArcViewModel(Parent, newRwls) { Title = "Новые ЖД перечни"}).TryOpen();
                }
            };

            Parent.Services.DoWaitAction(work, "Подождите", "Приём новых ЖД перечней...", after);
        }

        private bool isCheckAll = true;
        public bool IsCheckAll
        {
            get { return isCheckAll; }
            set 
            {
                isCheckAll = value;
                foreach (var rwl in rwListCollection)
                    rwl.IsSelected = isCheckAll;
                NotifyPropertyChanged("IsCheckAll");
            }
        }

        public ICommand MarkRwListReceivedCommand { get; set; }

        private void ExecMarkReceived(Selectable<RwListViewModel> _rwlist)
        {
            var dlg = new MsgDlgViewModel
            {
                Title = "Внимание!",
                Message = String.Format("Перечень № {0}\nбудет отмечен, как принятый.\nПоследующая приёмка его будет невозможна.", _rwlist.Value.Num_rwlist),
                IsCancelable = true,
                OnSubmit = d =>
                {
                    Parent.CloseDialog(d);
                    SubmitExecMarkReceived(_rwlist);
                }
            };
            Parent.OpenDialog(dlg);
            
        }

        private void SubmitExecMarkReceived(Selectable<RwListViewModel> _rwlist)
        {            
            Action work = () => 
            {
                if (MarkRwListReceived(_rwlist.Value.ModelRef))
                {
                    Parent.ShellModel.UpdateUi(() =>
                    {
                        SelectedRwList = null;
                        rwListCollection.Remove(_rwlist);
                    }, true, true);                    
                }
            };
            Parent.Services.DoWaitAction(work);
        }

        private bool MarkRwListReceived(RwList _rwl)
        {
            bool res = false;
            using (var db = new RealContext())
            {
                res = db.MarkRwListReceived(_rwl.Keykrt);
            }
            return res;
        }

        private bool CanExecMarkReceived(Selectable<RwListViewModel> _rwlist)
        {
            return _rwlist != null && _rwlist.IsSelected && !_rwlist.Value.IsNew && _rwlist.Value.Keykrt > 0;
        }
    }
}
