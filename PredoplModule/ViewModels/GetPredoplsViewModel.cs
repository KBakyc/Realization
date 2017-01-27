using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommonModule.Commands;
using CommonModule.Interfaces;
using CommonModule.ViewModels;
using DataObjects;


namespace PredoplModule.ViewModels
{
    public class GetPredoplsViewModel : BasicModuleContent
    {
        public GetPredoplsViewModel(IModule _parent)
            :base(_parent)
        {
            AcceptPredoplsCommand = new DelegateCommand(ExecAcceptPredopls, CanExecAcceptPredopls);
            Title = "Новые предоплаты";            
            OnCheckItemChangeCommand = new DelegateCommand(ExecOnCheckItemChange);
            SelectDeselectAllCommand = new DelegateCommand<bool>(ExecSelectDeselectAll);
            loadData();
        }     

        public PoupModel SelectedPoup { get; set; }
        public PkodModel SelectedPkod { get; set; }
        public Valuta PredoplVal { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }

        /// <summary>
        /// Комманда выделения/снятия выделения всех предоплат
        /// </summary>
        public ICommand SelectDeselectAllCommand { get; set; }
        private void ExecSelectDeselectAll(bool _select)
        {
            var pca = PredoplList.Where(p => p.CanAccept).ToArray();
            foreach (var p in pca)//PredoplList.Where(p => p.CanAccept))
                p.IsAccepted = _select;
            NotifySelectedChanged();
        }

        // принятие отмеченных предоплат
        public ICommand AcceptPredoplsCommand { get; set; }

        private void ExecAcceptPredopls()
        {
            Action work = ExecAcceptPredoplsAction;
            Parent.Services.DoWaitAction(work, "Подождите", "Приём и синхронизация предоплат");
        }

        private void ExecAcceptPredoplsAction()
        {
            Parent.Repository.SaveAndAcceptTmpPredopls(
                PredoplList.Where(p => p.IsChanged).ToDictionary(i=>i.PredoplRef,i=>i.Info));

            Action updateui = () => 
            {
                loadData();
                NotifyPropertyChanged("TotalRows");
                NotifySelectedChanged();
            };

            Parent.ShellModel.UpdateUi(updateui, false, false);
            if (PredoplList.Count == 0)
            {
                Parent.Services.ShowMsg("Результат", "Выбранные предоплаты успешно приняты", false);
                Parent.UnLoadContent(this);
            }
        }

        private void loadData()
        {
            if (PredoplList.Count > 0) 
                PredoplList.Clear();
            foreach (var item in Parent.Repository.GetTmpPredopls())
                PredoplList.Add(new TmpPredoplViewModel(Parent.Repository) { PredoplRef = item.Key, Info = item.Value });
        }

        private bool CanExecAcceptPredopls()
        {
            return PredoplList.Any(p => p.IsAccepted);
        }

        private ObservableCollection<TmpPredoplViewModel> predoplList;
        public ObservableCollection<TmpPredoplViewModel> PredoplList
        {
            get
            {
                if (predoplList == null)
                    predoplList = new ObservableCollection<TmpPredoplViewModel>();
                return predoplList;
            }
        }

        /// <summary>
        /// Комманда выполняется при пометке/снятии элемента коллекции
        /// </summary>
        public ICommand OnCheckItemChangeCommand { get; set; }
        private void ExecOnCheckItemChange()
        {
            NotifySelectedChanged();
        }

        private void NotifySelectedChanged()
        {
            NotifyPropertyChanged("CheckedRows");
            NotifyPropertyChanged("SelectedSum");
        }

        public int TotalRows
        {
            get { return PredoplList.Count; }
        }

        public int CheckedRows
        {
            get 
            {
                return PredoplList.Where(p => p.IsAccepted).Count(); 
            }
        }

        public decimal SelectedSum
        {
            get { return GetSelectedSum(); }
        }

        private decimal GetSelectedSum()
        {
            decimal res = 0;
            res = PredoplList.Where(p => p.IsAccepted).Sum(p => p.SumPropl);
            return res;
        }


    }
}