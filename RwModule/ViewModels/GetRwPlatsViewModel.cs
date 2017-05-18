using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommonModule.Commands;
using CommonModule.Interfaces;
using CommonModule.ViewModels;
using DataObjects;
using System.Collections.Generic;
using RwModule.Models;
using CommonModule.Helpers;
using DAL;
using DotNetHelper;


namespace RwModule.ViewModels
{
    /// <summary>
    /// Модель режима приёмки новых ЖД платежей из банка.
    /// </summary>
    public class GetRwPlatsViewModel : BasicModuleContent
    {
        public GetRwPlatsViewModel(IModule _parent, RwPlat[] _newPlats)
            : base(_parent)
        {
            AcceptRwPlatsCommand = new DelegateCommand(ExecAccept, CanExecAccept);
            Title = "Новые платежи";            
            loadData(_newPlats);
            //OnCheckItemChangeCommand = new DelegateCommand(ExecOnCheckItemChange);            
        }

        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }

        private bool isCheckAll = false;
        public bool IsCheckAll
        {
            get { return isCheckAll; }
            set
            {
                isCheckAll = value;
                foreach (var rwl in newRwPlats)
                    rwl.IsSelected = isCheckAll;
                NotifyPropertyChanged("IsCheckAll");
            }
        }

        // принятие отмеченных предоплат
        public ICommand AcceptRwPlatsCommand { get; set; }

        private void ExecAccept()
        {
            Action<ProgressDlgViewModel> work = ExecAcceptAction;
            Parent.Services.DoWaitAction(work, "Подождите", "Приём и синхронизация предоплат");
        }

        private void ExecAcceptAction(ProgressDlgViewModel _dlg)
        {
            var plats = newRwPlats.Where(p => p.IsSelected).ToArray();
            _dlg.StartValue = 1;
            _dlg.FinishValue = plats.Length;
            List<RwPlat> savedPlats = new List<RwPlat>();
            foreach (var pl in plats)
            {
                try 
                {
                    var curVM = pl.Value;
                    _dlg.Message = "Платёжка № {0} от {1:dd.MM.yyyy}".Format(curVM.Numplat, curVM.Datplat);
                    savedPlats.Add(TrySavePlat(curVM.GetModel()));
                }
                catch (Exception e)
                {
                    CommonModule.Helpers.WorkFlowHelper.OnCrash(e);
                    break;
                }
                Action updateui = () => newRwPlats.Remove(pl);
                Parent.ShellModel.UpdateUi(updateui, false, false);
            }
            if (newRwPlats.Count(p => p.IsSelected) == 0)
            {
                Parent.Services.ShowMsg("Результат", "Выбранные платежи успешно приняты", false);
                Parent.UnLoadContent(this);
                var ncontent = new RwPlatsArcViewModel(Parent, savedPlats) { Title = "Принятые платежи из подсистемы Финансы"};
                ncontent.TryOpen();
            }
            else
                Parent.Services.ShowMsg("Результат", "Ошибка при сохранении выбранных платежей", true);
        }

        private RwPlat TrySavePlat(RwPlat _pl)
        {
            using (var db = new RealContext())
            {
                db.RwPlats.Add(_pl);
                db.SaveChanges();
            }
            return _pl;
        }

        private ObservableCollection<Selectable<RwPlatViewModel>> newRwPlats;

        public ObservableCollection<Selectable<RwPlatViewModel>> NewRwPlats
        {
            get { return newRwPlats; }
            set { SetAndNotifyProperty("NewRwPlats", ref newRwPlats, value); }
        }

        private void loadData(RwPlat[] _newplats)
        {
            if (newRwPlats == null) NewRwPlats = new ObservableCollection<Selectable<RwPlatViewModel>>();
            else
                newRwPlats.Clear();

            if (_newplats != null)
                newRwPlats.AddRange(_newplats.Select(p => new Selectable<RwPlatViewModel>(new RwPlatViewModel(p), false)));
        }

        private bool CanExecAccept()
        {
            return newRwPlats.Any(p => p.IsSelected);
        }
        
        /// <summary>
        /// Комманда выполняется при пометке/снятии элемента коллекции
        /// </summary>
        //public ICommand OnCheckItemChangeCommand { get; set; }
        //private void ExecOnCheckItemChange()
        //{
        //    NotifySelectedChanged();
        //}

        //private void NotifySelectedChanged()
        //{
        //    NotifyPropertyChanged("CheckedRows");
        //    NotifyPropertyChanged("SelectedSum");
        //}

        //public int TotalRows
        //{
        //    get { return newRwPlats.Count; }
        //}

        //public int CheckedRows
        //{
        //    get
        //    {
        //        return newRwPlats.Where(p => p.IsSelected).Count();
        //    }
        //}

        //public decimal SelectedSum
        //{
        //    get { return GetSelectedSum(); }
        //}

        //private decimal GetSelectedSum()
        //{
        //    decimal res = 0;
        //    res = newRwPlats.Where(p => p.IsSelected).Sum(p => p.Value.Sumplat);
        //    return res;
        //}
    }
}