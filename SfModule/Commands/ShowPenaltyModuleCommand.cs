using System;
using System.Collections.Generic;
using System.Linq;
using CommonModule.Commands;
using CommonModule.Composition;
using CommonModule.Interfaces;
using CommonModule.ViewModels;
using SfModule.ViewModels;
using DataObjects;
using CommonModule.DataViewModels;

namespace SfModule.Commands
{
    /// <summary>
    /// Команда модуля для запуска режима просмотра штрафных санкций.
    /// </summary>
    [ExportModuleCommand("SfModule.ModuleCommand", DisplayOrder = 4f)]
    public class ShowPenaltyModuleCommand : ModuleCommand
    {
        public ShowPenaltyModuleCommand()
        {
            Label = "Штрафные санкции";
        }

        public override void Execute(object parameter)
        {
            base.Execute(parameter);

            if (Parent == null) return;

            var nDialog = MakeFilterDialog();
            if (nDialog != null)
                Parent.OpenDialog(nDialog);
        }

        private BaseDlgViewModel MakeFilterDialog()
        {

            var res = new DateRangeDlgViewModel()
            {
                Title = "Просмотреть штрафные санкции",
                OnSubmit = SubmitDlg
            };

            return res;
        }

        private void SubmitDlg(Object _dlg)
        {
            Parent.CloseDialog(_dlg);

            var dlg = _dlg as DateRangeDlgViewModel;
            if (dlg == null) return;

            ShowByDatesDlg(dlg);
            
            return;
        }

        private void ShowByDatesDlg(DateRangeDlgViewModel _dlg)
        {
            if (_dlg == null) return;

            PoupModel poup = Parent.Repository.Poups.Values.FirstOrDefault(p => p.PayDoc == PayDocTypes.Penalty && p.IsActive);
            var dateFrom = _dlg.DateFrom;
            var dateTo = _dlg.DateTo;

            Action work = () => ShowPenaltyByPoupAndDates(poup, dateFrom, dateTo);
            SeekAndShowSfs(work);
        }

        /// <summary>
        /// Выборка и формирование представления претензий по направлению за интервал дат
        /// </summary>
        /// <param name="_poup"></param>
        /// <param name="_dtFrom"></param>
        /// <param name="_dtTo"></param>
        private void ShowPenaltyByPoupAndDates(PoupModel _poup, DateTime _dtFrom, DateTime _dtTo)
        {
            var penalty = GetPenalty(_poup.Kod, _dtFrom, _dtTo);

            if (penalty.Length == 0)
            {
                Parent.Services.ShowMsg("Результат", "Нет данных, удовлетворяющих указанным критериям.", true);
            }
            var nContent = new PenaltyArcViewModel(Parent, penalty)
                        {
                            Title = "Архив штрафных санкций",
                            SelectedPoup = _poup,
                            DateFrom = _dtFrom,
                            DateTo = _dtTo,
                            RefreshCommand = new DelegateCommand<PenaltyArcViewModel>(vm =>
                            {
                                Action wk = () =>
                                {
                                    var newpens = GetPenalty(_poup.Kod, _dtFrom, _dtTo);
                                    Parent.ShellModel.UpdateUi(() => vm.LoadData(newpens), true, false);
                                };
                                Parent.Services.DoWaitAction(wk, "Ожидание выполнения", "Выборка и обновление списка претензий...");
                            })
                        };
            nContent.TryOpen();
        }

        private PenaltyModel[] GetPenalty(int _poup, DateTime _dtFrom, DateTime _dtTo)
        {
            var l_penalty = Parent.Repository.GetPenaltyList(_poup, _dtFrom, _dtTo);
            return l_penalty;
        }

        private void SeekAndShowSfs(Action work)
        {
            Parent.Services.DoWaitAction(work, "Ожидание выполнения", "Поиск претензий...");
        }
    }
}
