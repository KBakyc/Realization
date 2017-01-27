using CommonModule.Commands;
using CommonModule.Composition;
using CommonModule.ViewModels;

namespace OtgrModule.Commands
{
    //[Export("SfModule.ModuleCommand", typeof(ModuleCommand))]
    [ExportModuleCommand("OtgrModule.ModuleCommand", DisplayOrder = 5f)]
    public class ReportsOtgrModuleCommand : BaseReportModuleCommand
    {
        public ReportsOtgrModuleCommand()
        {
            Label = "Отчётность по отгрузке / услугам";
        }
    }
}
