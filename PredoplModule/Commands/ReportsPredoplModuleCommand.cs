using CommonModule.Composition;
using CommonModule.ViewModels;

namespace PredoplModule.Commands
{
    /// <summary>
    /// Команда модуля для выбора отчёта.
    /// </summary>
    [ExportModuleCommand("PredoplModule.ModuleCommand", DisplayOrder=98f)]
    public class ReportsPredoplModuleCommand : BaseReportModuleCommand
    {
        public ReportsPredoplModuleCommand()
        {
            Label = "Отчётность по предоплатам";
        }
    }
}
