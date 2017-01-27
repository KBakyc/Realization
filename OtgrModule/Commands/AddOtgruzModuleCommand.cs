using System;
using CommonModule.Commands;
using CommonModule.Composition;
using CommonModule.ViewModels;
using OtgrModule.ViewModels;

namespace OtgrModule.Commands
{
    //[Export("SfModule.ModuleCommand", typeof(ModuleCommand))]
    [ExportModuleCommand("OtgrModule.ModuleCommand", DisplayOrder = 1.5f)]
    public class AddOtgruzModuleCommand : ModuleCommand
    {
        public AddOtgruzModuleCommand()
        {
            Label = "Ввод новой отгрузки/услуг";
        }

        protected override int MinParentAccess
        {
            get { return 2; }
        }

        public override void Execute(object parameter)
        {
            base.Execute(parameter);

            if (Parent == null) return;

            if (Parent.SelectContent<AddOtgrViewModel>(null)) return;

            (new AddOtgrViewModel(Parent)).TryOpen();
        }
    }
}
