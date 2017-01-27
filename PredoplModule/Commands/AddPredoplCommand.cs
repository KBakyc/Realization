using System;
using System.ComponentModel.Composition;
using CommonModule.Commands;
using CommonModule.ViewModels;
using PredoplModule.ViewModels;
using DataObjects;
using System.Linq;
using PredoplModule.Helpers;

namespace PredoplModule.Commands
{
    /// <summary>
    /// Комманда добавления предоплаты
    /// </summary>

    [Export("PredoplModule.ModuleCommand", typeof(ModuleCommand))]
    public class AddPredoplCommand : ModuleCommand
    {
        public AddPredoplCommand()
        {
            Label = "Ручной ввод предоплаты";
        }

        protected override int MinParentAccess
        {
            get { return 2; }
        }

        public override void Execute(object parameter)
        {
            base.Execute(parameter);

            if (Parent == null) return;

            Parent.OpenDialog(
                new EditPredoplDlgViewModel(Parent.Repository, null)
                {
                    OnSubmit = DoSubmitAddPredopl
                });
        }

        private void DoSubmitAddPredopl(object _dlg)
        {
            Parent.CloseDialog(_dlg);
            var dlg = _dlg as EditPredoplDlgViewModel;
            if (dlg == null || dlg.NewModel == null) return;
            dlg.NewModel.DatPropl = dlg.NewModel.DatVvod;

            Action work = () =>
            {
                var predoplsService = Parent.Services as PredoplService;
                if (predoplsService != null)
                    predoplsService.DoAddPredopl(dlg.NewModel, PredoplAddKind.New);
            };

            Parent.Services.DoWaitAction(work, "Подождите", "Добавление предоплаты ... ");
        }        
    }
}
