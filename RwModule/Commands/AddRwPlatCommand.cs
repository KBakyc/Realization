using System;
using System.ComponentModel.Composition;
using CommonModule.Commands;
using CommonModule.ViewModels;
using RwModule.ViewModels;
using DataObjects;
using System.Linq;
using RwModule.Helpers;
using CommonModule.Composition;
using RwModule.Models;

namespace RwModule.Commands
{
    /// <summary>
    /// Комманда модуля для запуска механизма ручного добавления платежа за услуги БелЖД
    /// </summary>

    [ExportModuleCommand("RwModule.ModuleCommand", DisplayOrder = 19f)]
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

            var today = DateTime.Today;

            Parent.OpenDialog(
                new EditRwPlatDlgViewModel(Parent.Repository, null)
                {
                    Title = "Ввод нового платежа",
                    DatPlat = today,
                    DatBank = today, 
                    OnSubmit = DoSubmitAddRwPlat
                });
        }

        private void DoSubmitAddRwPlat(object _dlg)
        {
            Parent.CloseDialog(_dlg);
            var dlg = _dlg as EditRwPlatDlgViewModel;
            if (dlg == null || !dlg.IsValid()) return;

            var newModel = dlg.GetRwPlat();

            Action work = () =>
            {
                var rwHelper = new BusinessHelper(Parent.Repository, null);
                RwPlat res = null;

                try
                {
                    res = rwHelper.AddRwPlat(newModel);
                }
                catch (Exception e)
                {
                    Parent.Services.ShowMsg("Ошибка (" + e.GetType().ToString() + ")", e.Message, true);
                    CommonModule.Helpers.WorkFlowHelper.OnCrash(e, null, true);
                }

                if (res != null)
                {
                    var newContent = new RwPlatsArcViewModel(Parent, Enumerable.Repeat(res, 1))
                    {
                        Title = "Новый платёж"
                    };
                    newContent.TryOpen();
                }
                else
                    Parent.Services.ShowMsg("Ошибка", "Не удалось сохранить введённый платёж", true);
            };

            Parent.Services.DoWaitAction(work, "Подождите", "Добавление платежа ... ");
        }    
        
        
    }
}
