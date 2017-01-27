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

namespace SfModule.ViewModels
{
    // SfModule.ViewModels.PenaltyArcViewModel
    public class PenaltyArcViewModel : BasicModuleContent
    {

        private IPredoplModule predoplModule;

        public PenaltyArcViewModel(IModule _parent, IEnumerable<PenaltyModel> _lst)
            : base(_parent)
        {            
            LoadData(_lst);
            if (Parent != null)
                predoplModule = Parent.ShellModel.Container.GetExportedValueOrDefault<IPredoplModule>();
        }
        
        public void LoadData(IEnumerable<PenaltyModel> _lst)
        {
            penaltyList.Clear();
            foreach (var p in _lst)
                PenaltyList.Add(new PenaltyViewModel(Parent.Repository, p));
            if (SelectedPenalty != null)
                SelectedPenalty = PenaltyList.SingleOrDefault(s => s.PenRef.Id == SelectedPenalty.PenRef.Id);
        }

        private ObservableCollection<PenaltyViewModel> penaltyList = new ObservableCollection<PenaltyViewModel>();
        public ObservableCollection<PenaltyViewModel> PenaltyList
        {
            get { return penaltyList; }
            set
            {
                if (value != penaltyList)
                {
                    penaltyList = value;
                    NotifyPropertyChanged("PenaltyList");
                }
            }
        }

        private PenaltyViewModel selectedPenalty;
        public PenaltyViewModel SelectedPenalty
        {
            get { return selectedPenalty; }
            set
            {
                if (value != selectedPenalty && value != null)
                {
                    selectedPenalty = value;
                    NotifyPropertyChanged("SelectedPenalty");
                }
            }
        }

        private ICommand editPenaltyCommand;
        public ICommand EditPenaltyCommand
        {
            get
            {
                if (editPenaltyCommand == null)
                    editPenaltyCommand = new DelegateCommand(ExecEdit, CanShowEditDlg);
                return editPenaltyCommand;
            }
        }
        private bool CanShowEditDlg()
        {
            return SelectedPenalty != null
                && !IsReadOnly
                && SelectedPenalty.SumOpl == 0
                ;
        }
        private void ExecEdit()
        {
            var oldm = SelectedPenalty.PenRef;
            var edtdlg = new EditPenaltyDlgViewModel(Parent.Repository, oldm)
            {
                OnSubmit = SubmitEdit
            };
            
            Parent.OpenDialog(edtdlg);
        }
        private void SubmitEdit(Object _dlg)
        {
            var dlg = _dlg as EditPenaltyDlgViewModel;
            Parent.CloseDialog(_dlg);
            if (dlg == null) return;

            SaveEditedPenalty(dlg.NewModel);

            UpdateSelectedItem();
        }

        private void SaveEditedPenalty(PenaltyModel _pm)
        {
            bool res = Parent.Repository.UpdatePenalty(_pm);
            if (!res)
                Parent.Services.ShowMsg("Ошибка", "Не удалось сохранить данные", true);
        }

        private void SaveAddedPenalty(PenaltyModel _pm)
        {
            PenaltyModel res = Parent.Repository.InsertPenalty(_pm);
            if (res == null)
                Parent.Services.ShowMsg("Ошибка", "Не удалось сохранить данные", true);
            else
            {
                var nvm = new PenaltyViewModel(Parent.Repository, res);
                penaltyList.Add(nvm);
                SelectedPenalty = nvm;
            }
        }

        private ICommand addPenaltyCommand;
        public ICommand AddPenaltyCommand
        {
            get
            {
                if (addPenaltyCommand == null)
                    addPenaltyCommand = new DelegateCommand(ExecAdd, CanAdd);
                return addPenaltyCommand;
            }
        }

        private bool CanAdd()
        {
            return !IsReadOnly;
        }

        private void ExecAdd()
        {
            Parent.OpenDialog(
                new EditPenaltyDlgViewModel(Parent.Repository, null)
                {
                    OnSubmit = DoSubmitAdd
                });
        }
        
        private void DoSubmitAdd(Object _dlg)
        {
            var dlg = _dlg as EditPenaltyDlgViewModel;
            Parent.CloseDialog(_dlg);
            if (dlg == null) return;

            SaveAddedPenalty(dlg.NewModel);
        }

        private ICommand deletePenaltyCommand;
        public ICommand DeletePenaltyCommand
        {
            get
            {
                if (deletePenaltyCommand == null)
                    deletePenaltyCommand = new DelegateCommand(ExecDelete, CanDelete);
                return deletePenaltyCommand;
            }
        }
        private bool CanDelete()
        {
            return !IsReadOnly &&
                   penaltyList.Any(PenaltyForDelSelector);
        }
        private void ExecDelete()
        {
            var penalties = GetSelectedPenaltyForDelete();
            if (penalties.Any())
            {
                String pennums = String.Join(",", penalties.Select(s => s.NomKRO.ToString()).ToArray());
                var nDialog = new MsgDlgViewModel()
                {
                    Title = "Подтверждение",
                    Message = String.Format("Аннулируются претензии КРО № {0}.", pennums),
                    OnSubmit = (d) =>
                    {
                        Parent.CloseDialog(d);
                        DoDeletePenalties(penalties);
                    },
                    OnCancel = (d) => Parent.CloseDialog(d)
                };
                Parent.OpenDialog(nDialog);
            }
        }

        private void DoDeletePenalties(IEnumerable<PenaltyViewModel> _pens)
        {
            string errors = "";

            Action work = () =>
            {
                foreach (var p in _pens)
                {
                    if (!Parent.Repository.DeletePenalty(p.PenRef.Id))
                        errors += String.Format("Не удалось удалить претензию № {0}\n", p.NomKRO);
                }
            };

            Action after = () =>
            {
                if (RefreshCommand != null)
                    RefreshCommand.Execute(this);

                if (!String.IsNullOrEmpty(errors))
                    Parent.Services.ShowMsg("Ошибка", errors, true);
            };

            Parent.Services.DoWaitAction(work, "Подождите", "Аннулирование выбранных претензий", after);
        }

        private Func<PenaltyViewModel, bool> PenaltyForDelSelector = li => li.IsSelected && li.SumOpl == 0;

        private IEnumerable<PenaltyViewModel> GetSelectedPenaltyForDelete()
        {
            return penaltyList.Where(PenaltyForDelSelector);
        }

        /// <summary>
        /// Комманда отмены оплаты претензии
        /// </summary>
        private ICommand undoPaysCommand;
        public ICommand UndoPaysCommand
        {
            get
            {
                if (undoPaysCommand == null)
                    undoPaysCommand = new DelegateCommand(ExecUndoPays, CanUndoPays);
                return undoPaysCommand;
            }
        }
        private void ExecUndoPays()
        {
            Parent.OpenDialog(new MsgDlgViewModel()
            {
                Title = "Предупреждение",
                Message = String.Format("Аннулировать оплату претензии № {0} ?", SelectedPenalty.Nomish),
                OnSubmit = UndoSelectedPenaltyPays
            });
        }
        
        private void UndoSelectedPenaltyPays(Object _dlg)
        {
            Parent.CloseDialog(_dlg);
            Action work = () =>
            {
                Parent.Repository.PenaltyUndoPays(SelectedPenalty.PenRef.Id);
                Parent.ShellModel.UpdateUi(() =>
                {
                    UpdateSelectedItem();
                    //ShowUndoPaysReport();
                }, true, false);
            };
            Parent.Services.DoWaitAction(work, "Подождите", "Аннулирование оплаты претензии №" + SelectedPenalty.Nomish);
        }
        
        private bool CanUndoPays()
        {
            return SelectedPenalty != null && SelectedPenalty.SumOpl > 0 && !IsReadOnly;
        }
        
        //private void ShowUndoPaysReport()
        //{
        //    ReportModel repm = new ReportModel
        //    {
        //        Title = String.Format("Протокол аннулирования счёта: {0}", SelectedSf.NumSf),
        //        Path = @"/real/Reports/ClearPaysProtokol",
        //        Parameters = new Dictionary<string, string> { { "ConnString", Parent.Repository.ConnectionString } }
        //    };
        //    Parent.LoadContent(new ReportViewModel(Parent, repm));
        //}

        /// <summary>
        /// Комманда для вызова окна просмотра предоплат
        /// </summary>
        private ICommand showPredoplsCommand;
        public ICommand ShowPredoplsCommand
        {
            get
            {
                if (showPredoplsCommand == null)
                    showPredoplsCommand = new DelegateCommand(ExecShowPredopls, CanShowPredopls);
                return showPredoplsCommand;
            }
        }
        private bool CanShowPredopls()
        {
            return SelectedPenalty != null && SelectedPenalty.SumOpl != 0
                && predoplModule != null;
        }
        private void ExecShowPredopls()
        {
            var predopls = Parent.Repository.GetPredoplsByPaydoc(SelectedPenalty.PenRef.Id, PayDocTypes.Penalty);
            string title = String.Format("Предоплаты по претензии №{0}", SelectedPenalty.Nomish);
            predoplModule.ListPredopls(predopls, title);
            Parent.ShellModel.LoadModule(predoplModule);
        }

        /// <summary>
        /// Комманда обновления
        /// </summary>
        public ICommand RefreshCommand { get; set; }

        /// <summary>
        /// Обновляет текущий элемент списка
        /// </summary>
        private void UpdateSelectedItem()
        {
            var nSelPen = Parent.Repository.GetPenaltyById(selectedPenalty.PenRef.Id);
            PenaltyViewModel nSelPenVm = nSelPen == null ? null : new PenaltyViewModel(Parent.Repository, nSelPen);
            SelectedPenalty = PenaltyList.UpdateItem(SelectedPenalty, nSelPenVm);
        }

        public PoupModel SelectedPoup { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }

    }
}