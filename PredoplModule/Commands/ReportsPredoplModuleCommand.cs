using CommonModule.Composition;
using CommonModule.ViewModels;

namespace PredoplModule.Commands
{
    //[Export("SfModule.ModuleCommand", typeof(ModuleCommand))]
    [ExportModuleCommand("PredoplModule.ModuleCommand", DisplayOrder=98f)]
    public class ReportsPredoplModuleCommand : BaseReportModuleCommand
    {
        public ReportsPredoplModuleCommand()
        {
            Label = "Отчётность по предоплатам";
        }
    }
}
