using System;
using System.ComponentModel.Composition;
using System.Linq;
using CommonModule.Commands;
using CommonModule.ViewModels;
using DataObjects;
using PredoplModule.ViewModels;
using CommonModule.Composition;

namespace PredoplModule.Commands
{
    /// <summary>
    /// Комманда принятия предоплат из банка
    /// </summary>

    //[Export("PredoplModule.ModuleCommand", typeof(ModuleCommand))]
    [ExportModuleCommand("PredoplModule.ModuleCommand", DisplayOrder = 99f)]
    public class ModuleSettingsCommand : ModuleCommand
    {
        public ModuleSettingsCommand()
        {
            Label = "Настройки модуля";
        }

        protected override int MinParentAccess
        {
            get { return 2; }
        }

        public override void Execute(object parameter)
        {
            base.Execute(parameter);

            if (Parent == null) return;
            
            var repository = Parent.Repository;

            var nDlg = new PredoplSchetsDlgViewModel(repository)
            {
                Title = "Настройка приёмки предоплат из подсистемы Финансы"
            };

            Parent.OpenDialog(nDlg);
        }
    }
}
