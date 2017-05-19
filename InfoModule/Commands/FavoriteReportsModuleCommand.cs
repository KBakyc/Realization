using CommonModule.Composition;
using CommonModule.ViewModels;
using DataObjects;
using System.Linq;

namespace InfoModule.Commands
{
    /// <summary>
    /// Команда модуля для запуска режима отображения и выбора отображения помеченных пользователем отчётов.
    /// </summary>
    [ExportModuleCommand("InfoModule.ModuleCommand", DisplayOrder = 98.1f)]
    public class FavoriteReportsModuleCommand : BaseReportModuleCommand
    {
        public FavoriteReportsModuleCommand()
        {
            GroupName = "Отчётность";
            Label = "Все избранные отчёты АРМа РеПКа";
        }

        protected override ReportModel[] GetReports(string _moduleName)
        {
            ReportModel[] res = null;
            res = Parent.Repository.GetReports(null).Where(r => r.IsFavorite).OrderBy(r => r.ReportId).ToArray();
            return res;
        }

        protected override IGrouping<string, ReportModel>[] GetCategories(ReportModel[] _reps)
        {
            const string favName = "Избранное";
            var categs = _reps.Where(r => r.IsFavorite).GroupBy(f => favName).ToArray();
            return categs;
        }

        protected override BaseDlgViewModel GetDialog(ReportModel[] _reps)
        {
            var dlg = base.GetDialog(_reps);
            if (dlg != null)
                dlg.Title = "Избранные отчёты АРМа РеПКа";
            return dlg;
        }
    }
}
