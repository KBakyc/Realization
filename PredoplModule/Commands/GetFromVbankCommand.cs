using System;
using System.ComponentModel.Composition;
using System.Linq;
using CommonModule.Commands;
using CommonModule.ViewModels;
using DataObjects;
using PredoplModule.ViewModels;

namespace PredoplModule.Commands
{
    /// <summary>
    /// Комманда принятия предоплат из банка
    /// </summary>

    [Export("PredoplModule.ModuleCommand", typeof(ModuleCommand))]
    public class GetFromVbankCommand : ModuleCommand
    {
        public GetFromVbankCommand()
        {
            Label = "Загрузка поступлений из банка";
        }

        protected override int MinParentAccess
        {
            get { return 2; }
        }

        public override void Execute(object parameter)
        {
            base.Execute(parameter);

            if (Parent == null) return;

            if (Parent.SelectContent<GetPredoplsViewModel>(null)) return;
            
            var nDlg = new GetPredoplsDlgViewModel(Parent.Repository)
            {
                Title = "Принять предоплаты из VBANK",
                OnSubmit = SubmitGetFromVbank
            };

            Parent.OpenDialog(nDlg);
        }

        private void SubmitGetFromVbank(object _dlg)
        {
            Parent.CloseDialog(_dlg);
            var dlg = _dlg as GetPredoplsDlgViewModel;
            if (dlg == null) return;

            PoupModel poupm = dlg.PoupDatesSelection.SelPoup;
            short[] pkods = null;
            if (dlg.PoupDatesSelection.IsPkodEnabled && !dlg.PoupDatesSelection.PoupSelection.IsAllPkods)
                pkods = dlg.PoupDatesSelection.SelPkods.Select(p => p.Pkod).ToArray();
            var idbank = dlg.SelectedBank.Id;
            var dateFrom = dlg.PoupDatesSelection.DateFrom;
            var dateTo = dlg.PoupDatesSelection.DateTo;
            Valuta bv = dlg.BankVal;
            Valuta pv = dlg.PredoplVal;

            //var pm = Parent as PagesModuleViewModel;
            //if (pm != null)
            //    pm.RemoveSimilarContents<GetPredoplsViewModel>();

            Action work = () =>
            {
                try
                {
                    Parent.Repository.GetPredoplFromBank(dateFrom, dateTo, bv.Kodval, pv.Kodval, poupm.Kod, pkods, idbank);
                    var nContent = new GetPredoplsViewModel(Parent) 
                    {
                        SelectedPoup = poupm,
                        DateFrom = dateFrom,
                        DateTo = dateTo,
                        PredoplVal = pv
                    };
                    nContent.TryOpen();
                }
                catch (Exception e)
                {
                    CommonModule.Helpers.WorkFlowHelper.OnCrash(e);
                }
            };

            Parent.Services.DoWaitAction(work, "Ожидание выполнения", "Выборка поступлений из банка...");
        }
    }
}
