using System;
using System.Linq;
using CommonModule.Commands;
using CommonModule.Helpers;
using DataObjects;
using CommonModule.DataViewModels;

namespace CommonModule.ViewModels
{
    public class BaseReportModuleCommand : ModuleCommand
    {
        protected virtual ReportModel[] GetReports(string _moduleName)
        {
            ReportModel[] res = null;
            if (!String.IsNullOrEmpty(_moduleName))
                res = Parent.Repository.GetReports(_moduleName);
            return res;
        }

        public override void Execute(object parameter)
        {
            base.Execute(parameter);

            var parentModule = Parent as BasicModuleViewModel;
            if (parentModule != null && !String.IsNullOrEmpty(parentModule.Info.Name))
            {
                ReportModel[] reps = GetReports(parentModule.Info.Name.Trim());
                if (reps != null && reps.Length > 0)
                    DoSelectReport(reps);
                else
                    Parent.Services.ShowMsg("Результат", "Не найдены отчёты, отмеченные как ИЗБРАННЫЕ", true);
            }
        }

        const string favName = "Избранное";
        
        protected virtual IGrouping<string, ReportModel>[] GetCategories(ReportModel[] _reps)
        {
            var favorites = _reps.Where(r => r.IsFavorite).ToArray();           
            var categs = _reps.GroupBy(r => r.CategoryName ?? "Без категории").ToArray();
            if (favorites.Length > 0 && !categs.Any(g => g.Key == favName))
                categs = favorites.GroupBy(f => favName).Union(categs).ToArray();

            return categs;
        }
        
        protected virtual BaseDlgViewModel GetDialog(ReportModel[] _reps)
        {
            BaseDlgViewModel res = null;

            var categs = GetCategories(_reps);
            res = (categs.Length > 1) 
                ? DoMakeDlgFromGroups(categs) 
                : res = new ReportSelectionViewModel(Parent.Repository, _reps)
                {
                    OnSubmit = (d) => 
                    {
                        Parent.CloseDialog(d);
                        this.DoReportDlgCallback(d as ReportSelectionViewModel);
                    }
                };
            return res;
        }

        private void DoSelectReport(ReportModel[] _reps)
        {
            var dlg = GetDialog(_reps);
            if (dlg != null)
                Parent.OpenDialog(dlg);
        }

        private BaseDlgViewModel DoMakeDlgFromGroups(IGrouping<string, ReportModel>[] _categs)
        {
            var rdialog = new SelectedCompositeDlgViewModel()
            {
                Title = "Выберите отчёт",
                OnSubmit = (d) => 
                    {
                        Parent.CloseDialog(d);
                        var rselection = (d as SelectedCompositeDlgViewModel).SelectedDialog as ReportSelectionViewModel;
                        DoReportDlgCallback(rselection);
                    }
            };

            var rvms = _categs.SelectMany(g => g).Distinct().ToDictionary(r => r.ReportId, r => new ReportDataViewModel(Parent.Repository, r));

            var groupVMs = _categs.Select(g => new ReportSelectionViewModel(Parent.Repository, g.Select(r => rvms[r.ReportId]))
            { 
                Title = g.Key, 
                BgColor = g.Key == favName ? "MistyRose" : null,
                SubmitCommand = rdialog.SubmitCommand
            });

            foreach (var dvm in groupVMs)
                rdialog.Add(dvm);

            return rdialog;
        }

        /// <summary>
        /// Метод обратного вызова из диалога
        /// </summary>
        /// <param name="_dlg"></param>
        private void DoReportDlgCallback(ReportSelectionViewModel _dlg)
        {
            var rModel = _dlg.SelectedReport.Model;
            
            ReportService service = null;

            Action work = () => 
            {
                try
                {
                    service = new ReportService(Parent, rModel);
                    if (service != null)
                        service.ExecuteReport(); 
                }
                catch (Exception _e)
                {
                    var emess = _e.InnerException == null ? _e.Message : _e.Message + Environment.NewLine + _e.InnerException.Message;
                    Parent.Services.ShowMsg("Ошибка инициализации отчёта", emess, true);
                }
            };

            Parent.Services.DoWaitAction(work, "Подождите", "Запрос данных о параметрах отчёта");
        }

    }
}
