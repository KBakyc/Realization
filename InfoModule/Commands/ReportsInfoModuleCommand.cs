using CommonModule.Composition;
using CommonModule.ViewModels;

namespace InfoModule.Commands
{
    /// <summary>
    /// Команда модуля для выбора отчёта.
    /// </summary>
    [ExportModuleCommand("InfoModule.ModuleCommand", DisplayOrder = 98f)]
    public class ReportsInfoModuleCommand : BaseReportModuleCommand
    {
        public ReportsInfoModuleCommand()
        {
            GroupName = "Отчётность";
            Label = "Отчёты модуля";
        }
    }
}
