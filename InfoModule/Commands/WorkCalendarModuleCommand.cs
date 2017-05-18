using CommonModule.Commands;
using CommonModule.ViewModels;
using CommonModule.Composition;
using DataObjects.Interfaces;
using InfoModule.ViewModels;

namespace InfoModule.Commands
{
    /// <summary>
    /// Команда модуля для управления производственным календарём.
    /// </summary>
    [ExportModuleCommand("InfoModule.ModuleCommand", DisplayOrder = 3.1f)]
    public class WorkCalendarModuleCommand : ModuleCommand
    {
        public WorkCalendarModuleCommand()
        {
            Label = "Производственный календарь";
        }

        public override void Execute(object parameter)
        {
            base.Execute(parameter);

            if (Parent == null) return;            

            var cDlg = new WorkCalendarViewModel(Parent.Repository, isReadOnly)
            {
                Title = "Производственный календарь",
                IsCanClose = true
            };
            Parent.OpenDialog(cDlg);
        }        
        
    }
}
