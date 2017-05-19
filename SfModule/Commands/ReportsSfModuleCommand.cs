using CommonModule.Commands;
using CommonModule.Composition;
using CommonModule.ViewModels;

namespace SfModule.Commands
{
    /// <summary>
    /// Команда модуля для выбора отчёта.
    /// </summary>
    [ExportModuleCommand("SfModule.ModuleCommand", DisplayOrder = 4f)]
    public class ReportsSfModuleCommand : BaseReportModuleCommand
    {
        public ReportsSfModuleCommand()
        {
            Label = "Отчётность по выписанным счетам-фактурам";
        }
    }
}
