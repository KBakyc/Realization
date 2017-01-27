using CommonModule.Composition;
using CommonModule.ViewModels;

namespace InfoModule.Commands
{
    //[Export("SfModule.ModuleCommand", typeof(ModuleCommand))]
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
