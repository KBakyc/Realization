using CommonModule.Commands;
using CommonModule.Composition;
using InfoModule.ViewModels;

namespace InfoModule.Commands
{
    /// <summary>
    /// Команда модуля для настройки пользовательских видов реализации и кодов форм.
    /// </summary>
    [ExportModuleCommand("InfoModule.ModuleCommand", DisplayOrder=3f)]
    public class PoupSettingsModuleCommand : ModuleCommand
    {
        public PoupSettingsModuleCommand()
        {
            Label = "Виды реализации и коды форм";
        }

        public override void Execute(object parameter)
        {
            base.Execute(parameter);

            if (Parent == null) return;

            Parent.OpenDialog(new PoupSettingsViewModel(Parent.Repository)
            {
                Title = "Мои виды реализации",
                OnSubmit = (d) => Parent.CloseDialog(d)
            });
        }
    }
}
