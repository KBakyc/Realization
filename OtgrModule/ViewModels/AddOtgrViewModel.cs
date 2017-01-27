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
using System.Collections.ObjectModel;


namespace OtgrModule.ViewModels
{
    public class AddOtgrViewModel : BasicModuleContent
    {
        public AddOtgrViewModel(IModule _parent)
            : this(_parent, null)
        {           
        }
        
        public AddOtgrViewModel(IModule _parent, IEnumerable<OtgrLine> _otgrs)
            : base(_parent)
        {
            Title = "Добавление отгрузки/услуг в реализацию";
            if (_otgrs == null)
                OtgrRows = new ObservableCollection<OtgrLineViewModel>();
            else
                OtgrRows = new ObservableCollection<OtgrLineViewModel>(
                                                                       _otgrs.Select(m => new OtgrLineViewModel(Parent.Repository, m))
                                                                      );
        }

        private OtgrLineViewModel selectedOtgr;
        /// <summary>
        /// Выбранная отгрузка
        /// </summary>
        public OtgrLineViewModel SelectedOtgr
        {
            get { return selectedOtgr; }
            set
            {
                if (value != selectedOtgr)
                {
                    selectedOtgr = value;
                    if (selectedOtgr != null)// && selectedOtgr.Totals == null)
                        selectedOtgr.Totals = OtgrHelper.GetOtgrTotals(selectedOtgr, otgrRows);
                    NotifyPropertyChanged("SelectedOtgr");
                }
            }
        }

        /// <summary>
        /// Строки отгрузок
        /// </summary>
        private ObservableCollection<OtgrLineViewModel> otgrRows;
        public ObservableCollection<OtgrLineViewModel> OtgrRows
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
            }
        }

        private ICommand deleteCommand;

        /// <summary>
        /// Комманда удаления выбранной отгрузки
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
            return OtgrRows.Any(r => r.IsChecked);
        }

        private void ExecDeleteCommand()
        {
            var selOtgr = OtgrRows.Where(r => r.IsChecked).ToArray();
            DeleteOtgruz(selOtgr);
        }

        private string GetOtgrString(OtgrLineViewModel _o)
        {
            return string.Format("Накладная №{0} на \"{1}\"", _o.Otgr.DocumentNumber, _o.Product.Name)                             
                             + (_o.TransportId == 3 ? string.Format(" вагон №{0}", _o.Otgr.Nv) : "");
        }

        private void DeleteOtgruz(OtgrLineViewModel[] _selOtgr)
        {
            for (int i = 0; i < _selOtgr.Length; i++)
                OtgrRows.Remove(_selOtgr[i]);
        }

        private ICommand editCommand;

        /// <summary>
        /// Комманда дублирования отгрузки
        /// </summary>
        public ICommand EditCommand
        {
            get
            {
                if (editCommand == null)
                    editCommand = new DelegateCommand(ExecEditCommand, CanEdit);
                return editCommand;
            }
        }
        private bool CanEdit()
        {
            return selectedOtgr != null;
        }
        private void ExecEditCommand()
        {
            var ndlg = new EditOtgrDlgViewModel(Parent.Repository, SelectedOtgr.Otgr)
            {
                OnSubmit = DoEditOtgr
            };

            Parent.OpenDialog(ndlg);
        }

        private void DoEditOtgr(Object _d)
        {
            var dlg = _d as EditOtgrDlgViewModel;
            if (dlg == null) return;
            Parent.CloseDialog(_d);

            var newotgr = dlg.NewModel;
            var newOtgrViewModel = new OtgrLineViewModel(Parent.Repository, newotgr);

            var selIndex = otgrRows.IndexOf(selectedOtgr);
            otgrRows.RemoveAt(selIndex);
            otgrRows.Insert(selIndex, newOtgrViewModel);
        }

        private ICommand addCopyCommand;

        /// <summary>
        /// Комманда дублирования отгрузки
        /// </summary>
        public ICommand AddCopyCommand
        {
            get
            {
                if (addCopyCommand == null)
                    addCopyCommand = new DelegateCommand(ExecAddCopyCommand, CanAddCopy);
                return addCopyCommand;
            }
        }
        private bool CanAddCopy()
        {
            return SelectedOtgr != null;
        }
        private void ExecAddCopyCommand()
        {
            var ndlg = new EditOtgrDlgViewModel(Parent.Repository, SelectedOtgr.Otgr)
            {
                OnSubmit = DoAddNewOtgr
            };
            ndlg.NewModel.IsChecked = false;

            Parent.OpenDialog(ndlg);
        }

        private ICommand addCommand;

        /// <summary>
        /// Комманда добавления отгрузки
        /// </summary>
        public ICommand AddCommand
        {
            get
            {
                if (addCommand == null)
                    addCommand = new DelegateCommand(ExecAddCommand);
                return addCommand;
            }
        }

        private void ExecAddCommand()
        {
            var ndlg = new EditOtgrDlgViewModel(Parent.Repository, null)
            {
                OnSubmit = DoAddNewOtgr
            };

            Parent.OpenDialog(ndlg);
        }

        private void DoAddNewOtgr(Object _d)
        {
            var dlg = _d as EditOtgrDlgViewModel;
            if (dlg == null) return;
            Parent.CloseDialog(_d);

            var newotgr = dlg.NewModel;
            var newOtgrViewModel = new OtgrLineViewModel(Parent.Repository, newotgr);
            OtgrRows.Add(newOtgrViewModel);
        }

        private ICommand submitCommand;

        /// <summary>
        /// Комманда добавления отгрузки
        /// </summary>
        public ICommand SubmitCommand
        {
            get
            {
                if (submitCommand == null)
                    submitCommand = new DelegateCommand(ExecSubmitCommand, CanExecSubmitCommand);
                return submitCommand;
            }
        }

        private bool CanExecSubmitCommand()
        {
            return otgrRows != null && otgrRows.Count > 0;
        }

        private void ExecSubmitCommand()
        {
            var ndlg = new MsgDlgViewModel()
            {
                Title = "Подтверждени",
                Message = "Сохранить введённую отгрузку?",
                OnSubmit = DoSubmitNewOtgr
            };

            Parent.OpenDialog(ndlg);
        }

        private void DoSubmitNewOtgr(Object _d)
        {
            Parent.CloseDialog(_d);
            SubmitOtgruz();
        }

        private bool SubmitSingleOtgr(OtgrLine _ol)
        {
            bool res = true;
            res = Parent.Repository.AddOtgruz(_ol, null);
            return res;
        }

        private void SubmitOtgruz()
        {
            string retmess = null;
            Dictionary<OtgrLineViewModel, bool> res = new Dictionary<OtgrLineViewModel, bool>();

            Action<WaitDlgViewModel> work = (w) =>
            {
                foreach(var or in otgrRows)
                {
                    string curOtgrStr = GetOtgrString(or);
                    w.Message = curOtgrStr;
                    bool sres = SubmitSingleOtgr(or.Otgr);
                    res[or] = sres;
                    retmess += curOtgrStr + (sres ? " : Сохранена\n" : " : Не сохранена\n");
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
                    }
                });
            };

            Parent.Services.DoWaitAction(work, "Ожидание выполнения", "Сохранение отгрузки...", afterwork);

        }
    }
}