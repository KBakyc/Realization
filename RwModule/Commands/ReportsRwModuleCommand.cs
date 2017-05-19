using CommonModule.Commands;
using CommonModule.Composition;
using CommonModule.ViewModels;

namespace RwModule.Commands
{
    /// <summary>
    /// Команда для выбора отчёта.
    /// </summary>
    [ExportModuleCommand("RwModule.ModuleCommand", DisplayOrder = 90f)]
    public class ReportsRwModuleCommand : BaseReportModuleCommand
    {
        public ReportsRwModuleCommand()
        {
            Label = "Отчётность по услугам ЖД";
        }
    }
}
