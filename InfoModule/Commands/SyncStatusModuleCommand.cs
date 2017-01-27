using CommonModule.Commands;
using CommonModule.Composition;
using InfoModule.ViewModels;

namespace InfoModule.Commands
{
    [ExportModuleCommand("InfoModule.ModuleCommand", DisplayOrder=10f)]
    public class SyncStatusModuleCommand : ModuleCommand
    {
        public SyncStatusModuleCommand()
        {
            Label = "Состояние системы подкачки данных";
        }

        public override void Execute(object parameter)
        {
            base.Execute(parameter);

            if (Parent == null) return;

            Parent.OpenDialog(new SyncStatusViewModel(Parent.Repository)
            {
                Title = "Состояние системы подкачки данных",
                OnSubmit = (d) => Parent.CloseDialog(d)
            });
        }
    }
}
