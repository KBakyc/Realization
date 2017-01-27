using System;
using System.Linq;
using CommonModule.Commands;
using CommonModule.Composition;
using CommonModule.ViewModels;
using DataObjects;
using InfoModule.ViewModels;
using DataObjects.Interfaces;
using CommonModule.Helpers;

namespace InfoModule.Commands
{
    [ExportModuleCommand("InfoModule.ModuleCommand", DisplayOrder=1.1f)]
    public class FinSostModuleCommand : ModuleCommand
    {
        private IDbService repository;

        public FinSostModuleCommand()
        {
            Label = "Финансовое состояние контрагента";

            if (Parent != null)
                repository = Parent.Repository;
            else
                repository = CommonModule.CommonSettings.Repository;
        }

        public override void Execute(object parameter)
        {
            base.Execute(parameter);

            if (Parent == null) return;

            Parent.OpenDialog(new KaSelectionViewModel(repository)
            {
                OnSubmit = OnKaSelect
            });
        }

        /// <summary>
        /// Вызов после выбора контрагента
        /// </summary>
        /// <param name="obj"></param>
        private void OnKaSelect(object _dlg)
        {
            KaSelectionViewModel dlg = _dlg as KaSelectionViewModel;
            Parent.CloseDialog(_dlg);
            if (dlg == null) return;
            KontrAgent ka = dlg.SelectedKA;

            var parDlg = MakeDlg();
            parDlg.OnSubmit = (d) => DoShowFinSost(d, ka);

            Parent.OpenDialog(parDlg);

        }

        private BaseDlgViewModel MakeDlg()
        {
            var curdate = DateTime.Now.Date;
            var dfrom = curdate.AddMonths(-1).AddDays(-curdate.Day + 1);

            var poupSelection = new MultiPoupSelectionViewModel(repository, true, true)
            {
                Title = "Направления реализации",
                PoupTitle = null,
                Name = "PoupSelection"
            };
            var datesSelection = new DateRangeDlgViewModel(false)
            {
                Title = "За период",
                DatesLabel = null,
                DateFrom = dfrom,
                DateTo = curdate,
                Name = "DatesSelection"
            };

            var dlg = new BaseCompositeDlgViewModel() 
            {
                Title = "Укажите параметры отображения",
            };

            dlg.Add(poupSelection);
            dlg.Add(datesSelection);

            return dlg;
        }

        private void DoShowFinSost(Object _dlg, KontrAgent _ka)
        {
            var dlg = _dlg as BaseCompositeDlgViewModel;
            Parent.CloseDialog(_dlg);
            if (dlg == null) return;

            var poupSelection = dlg.DialogViewModels[0] as MultiPoupSelectionViewModel;
            var datesSelection = dlg.DialogViewModels[1] as DateRangeDlgViewModel;
            if (poupSelection == null || datesSelection == null) return;

            poupSelection.SaveSelection();

            var selPoupModel = poupSelection.GetSelectedPoupsWithPkodsModels();
            var dateFrom = datesSelection.DateFrom;
            var dateTo = datesSelection.DateTo;

            Action work = () =>
            {
                var nContent = new KaFinHistoryViewModel(Parent, _ka, selPoupModel, dateFrom, dateTo)
                {
                    Title = String.Format("Фин.сост.-{0}", _ka.Kgr)
                };
                nContent.TryOpen();
            };

            Parent.Services.DoWaitAction(work, "Ожидание выполнения", "Запрос данных о финансовом состоянии...");
        }
    }
}
