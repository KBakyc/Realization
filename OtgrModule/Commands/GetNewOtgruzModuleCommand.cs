using System;
using System.Linq;
using CommonModule.Commands;
using CommonModule.Composition;
using CommonModule.ViewModels;
using OtgrModule.ViewModels;

namespace OtgrModule.Commands
{
    //[Export("SfModule.ModuleCommand", typeof(ModuleCommand))]
    [ExportModuleCommand("OtgrModule.ModuleCommand", DisplayOrder = 1f)]
    public class GetNewOtgruzModuleCommand : ModuleCommand
    {
        public GetNewOtgruzModuleCommand()
        {
            Label = "Принять новую отгрузку";
        }

        protected override int MinParentAccess
        {
            get { return 2; }
        }

        public override void Execute(object parameter)
        {
            base.Execute(parameter);

            if (Parent == null) return;

            if (Parent.SelectContent<p623ViewModel>(null)) return;

            Parent.OpenDialog(new PoupAndDatesDlgViewModel(Parent.Repository)
            {
                Title = "Принять новую отгрузку:",
                OnSubmit = SubmitMakeP623Dlg
            });
        }

        /// <summary>
        /// Метод обратного вызова из диалога
        /// </summary>
        /// <param name="_dlg"></param>
        private void SubmitMakeP623Dlg(object _dlg)
        {
            Parent.CloseDialog(_dlg);
            var dlg = _dlg as PoupAndDatesDlgViewModel;
            if (dlg == null) return;
            int poup = dlg.SelPoup.Kod;
            short pkod = 0;
            if (dlg.PoupSelection.IsPkodEnabled)
                 pkod = dlg.SelPkods[0].Pkod;
            var dateFrom = dlg.DateFrom;
            var dateTo = dlg.DateTo;
            var remember = CommonModule.CommonSettings.Persister;
            remember.SetValue("P623.Poup", poup);
            remember.SetValue("P623.Pkod", pkod);
            remember.SetValue("P623.DateFrom", dateFrom);
            remember.SetValue("P623.DateTo", dateTo);

            Action work = () =>
            {
                Parent.Repository.MakeTempP623(poup, pkod, dateFrom, dateTo);
                var otgMod = Parent as OtgrModuleViewModel;
                if (otgMod != null)
                    otgMod.ExecShowReestr();
            };

            Parent.Services.DoWaitAction(work, "Ожидание выполнения", "Формирование реестра отгрузки...");
        }
    }
}
