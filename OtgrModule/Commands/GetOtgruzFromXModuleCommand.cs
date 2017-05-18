using System;
using CommonModule.Commands;
using CommonModule.Composition;
using CommonModule.ViewModels;
using OtgrModule.ViewModels;

namespace OtgrModule.Commands
{
    /// <summary>
    /// Команда модуля для запуска режима приёмки отгрузки из внешних источников.
    /// </summary>
    [ExportModuleCommand("OtgrModule.ModuleCommand", DisplayOrder = 1.6f)]
    public class GetOtgruzFromXModuleCommand : ModuleCommand
    {
        public GetOtgruzFromXModuleCommand()
        {
            Label = "Приём отгрузки из внешних источников";
        }

        protected override int MinParentAccess
        {
            get { return 2; }
        }

        public override void Execute(object parameter)
        {
            base.Execute(parameter);

            if (Parent == null) return;
            
            if (Parent.SelectContent<GetOtgrViewModel>(null)) return;

            (new GetOtgrViewModel(Parent)).TryOpen();
        }
    }
}
