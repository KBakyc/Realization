using System;
using System.ComponentModel.Composition;
using System.Linq;
using CommonModule.Commands;
using CommonModule.ViewModels;
using DataObjects;
using RwModule.ViewModels;
using CommonModule.Composition;

namespace RwModule.Commands
{
    /// <summary>
    /// Комманда принятия предоплат из банка
    /// </summary>

    [ExportModuleCommand("RwModule.ModuleCommand", DisplayOrder = 99f)]
    public class BuhSchetSettingsCommand : ModuleCommand
    {
        public BuhSchetSettingsCommand()
        {
            Label = "Настройка бухгалтерских счетов";
            GroupName = "Настройки модуля";
        }

        protected override int MinParentAccess
        {
            get { return 2; }
        }

        public override void Execute(object parameter)
        {
            base.Execute(parameter);

            if (Parent == null) return;

            Action work = () =>
            {
                var nDlg = new RwBuhSchetsDlgViewModel(Parent.Repository)
                {
                    Title = "Настройка бухгалтерских счетов"
                };
                
                Parent.OpenDialog(nDlg);
            };

            Parent.Services.DoWaitAction(work, "Подождите", "Выборка данных из базы.");           
        }
    }
}
