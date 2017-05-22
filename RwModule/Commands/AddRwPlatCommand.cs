using System;
using System.ComponentModel.Composition;
using CommonModule.Commands;
using CommonModule.ViewModels;
using RwModule.ViewModels;
using DataObjects;
using System.Linq;
using RwModule.Helpers;

namespace RwModule.Commands
{
    /// <summary>
    /// Комманда модуля для запуска механизма ручного добавления платежа за услуги БелЖД
    /// </summary>

    [Export("RwModule.ModuleCommand", typeof(ModuleCommand))]
    public class AddRwPlatCommand : ModuleCommand
    {
        public AddRwPlatCommand()
        {
            Label = "Ручной ввод оплаты";
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
                new EditRwPlatDlgViewModel(Parent.Repository, null)
                {
                    OnSubmit = DoSubmitAddRwPlat
                });
        }

        private void DoSubmitAddRwPlat(object _dlg)
        {
            Parent.CloseDialog(_dlg);
            //var dlg = _dlg as EditPredoplDlgViewModel;
            //if (dlg == null || dlg.NewModel == null) return;
            //dlg.NewModel.DatPropl = dlg.NewModel.DatVvod;

            //Action work = () =>
            //{
            //    var predoplsService = Parent.Services as PredoplService;
            //    if (predoplsService != null)
            //        predoplsService.DoAddPredopl(dlg.NewModel, PredoplAddKind.New);
            //};

            //Parent.Services.DoWaitAction(work, "Подождите", "Добавление предоплаты ... ");
        }        
    }
}
