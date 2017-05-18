using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.ViewModels;
using CommonModule.Interfaces;
using DataObjects.Interfaces;
using System.Collections.ObjectModel;
using CommonModule.Commands;
using System.Windows.Data;
using System.Threading.Tasks;
using System.ServiceModel;
using System.Threading;
using System.Windows.Input;
using System.Xml.Linq;
using DataObjects;
using CommonModule.Helpers;
using DotNetHelper;
using ServiceModule.Helpers;
using ServiceModule.DAL.Models;
using CommonModule.Composition;
using System.IO;
using ServiceModule.DAL;
using System.Data.Entity;

namespace ServiceModule.ViewModels
{
    /// <summary>
    /// Модель режима управления отчётами.
    /// </summary>
    public class ReportsAdminViewModel : BasicModuleContent
    {
        IDbService dbService;

        public ReportsAdminViewModel(IModule _parent, IDbService _dbService)
            : base(_parent)
        {
            Title = "Информация об отчётах";
            dbService = _dbService;
            LoadData();
            saveReportInfoCommand = new DelegateCommand(ExecSaveReportInfo, CanSaveReportInfo);
            deleteReportInfoCommand = new DelegateCommand(ExecDeleteReportInfo, CanDeleteReportInfo);
            runReportCommand = new DelegateCommand(ExecRunReportCommand, CanRunReport);
            runHistoryReportCommand = new DelegateCommand(ExecRunHistoryReportCommand, CanRunHistoryReport);
            changeHistoryReportCommand = new DelegateCommand(ExecChangeHistoryReportCommand, CanChangeHistoryReport);
            saveParamsSettingsCommand = new DelegateCommand(ExecSaveParamsSettingsCommand, CanSaveParamsSettings);
        }

        private void LoadData()
        {
            if (dbService == null) return;
            using (var db = new ServiceContext())
            {
                reports = new ObservableCollection<ReportInfoViewModel>(db.ReportInfos.ToArray().Select(r => new ReportInfoViewModel(dbService, r)));
            }
            allComponentTypeNames = Parent.ShellModel.Modules.Select(m => m.Info.Name).ToArray();
        }

        private string[] allComponentTypeNames;
        public string[] AllComponentTypeNames
        {
            get { return allComponentTypeNames; }
        }

        private string userFilterString;
        public string UserFilterString
        {
            get { return userFilterString; }
            set
            {
                if (SetAndNotifyProperty(() => UserFilterString, ref userFilterString, value)
                    && (String.IsNullOrWhiteSpace(userFilterString) || userFilterString[userFilterString.Length - 1] != ','))
                    RefreshUserFilter();
            }
        }

        private void RefreshUserFilter()
        {
            var view = CollectionViewSource.GetDefaultView(reports);
            if (String.IsNullOrWhiteSpace(userFilterString))
                view.Filter = null;
            else
            {
                HashSet<int> ids = null;
                HashSet<string> names = null;
                string stringToParse = null;
                if (!String.IsNullOrWhiteSpace(userFilterString))
                {
                    stringToParse = userFilterString.Trim().ToUpperInvariant();
                    if (stringToParse[stringToParse.Length - 1] == ',')
                        stringToParse = stringToParse.Remove(stringToParse.Length - 1);
                    if (!String.IsNullOrWhiteSpace(stringToParse) && stringToParse.Contains(','))
                    {
                        var strings = stringToParse.Split(',').Where(s => !String.IsNullOrWhiteSpace(s)).ToArray();
                        for (int i = 0; i < strings.Length; i++)
                        {
                            var curitem = strings[i];
                            if (curitem.All(c => Char.IsDigit(c)))
                            {
                                int newid = 0;
                                if (int.TryParse(curitem, out newid) && newid > 0)
                                {
                                    if (ids == null) ids = new HashSet<int>();
                                    ids.Add(newid);
                                }
                            }
                            else
                            {
                                if (names == null) names = new HashSet<string>();
                                names.Add(curitem.Trim().ToUpperInvariant());
                            }
                        }
                    }
                }

                view.Filter = r =>
                {
                    var rep = r as ReportInfoViewModel;
                    int id = 0;
                    return                        
                        String.IsNullOrWhiteSpace(stringToParse)
                        || ids != null && ids.Contains(rep.Id)
                        || names != null && names.Any(s => rep.Title.Trim().ToUpperInvariant().Contains(s))
                        || int.TryParse(stringToParse, out id) && rep.Id == id
                        || rep.Title.Trim().ToUpperInvariant().Contains(stringToParse);
                };
            }
            view.Refresh();
        }

        private void RefreshData()
        {            
        }

        private ObservableCollection<ReportInfoViewModel> reports;
        public ObservableCollection<ReportInfoViewModel> Reports
        {
            get { return reports; }
        }

        private ReportInfoViewModel selectedReport;
        public ReportInfoViewModel SelectedReport
        {
            get { return selectedReport; }
            set
            {
                if (selectedReport != value)
                {
                    selectedReport = value;
                    NotifyPropertyChanged(() => SelectedReport);
                    if (selectedReport != null)
                        IsNewSelected = false;
                }
            }
        }

        private bool isNewSelected = false;
        public bool IsNewSelected
        {
            get { return isNewSelected; }
            set
            {
                isNewSelected = value;
                NotifyPropertyChanged(() => IsNewSelected);
                MakeSelectedCopy();
            }
        }

        private EditedReportInfoViewModel selectedCopy;
        public EditedReportInfoViewModel SelectedCopy
        {
            get { return selectedCopy; }
            set 
            {
                if (SetAndNotifyProperty("SelectedCopy", ref selectedCopy, value))
                {
                    ReportParams = null;
                    ReportStatistics = null;
                    switch(curAdminMode)
                    {
                        case ReportAdminMode.Parameters: GetNewReportParams(); break;
                        case ReportAdminMode.Statistics: GetNewReportStatistics(); break;
                        default: break;
                    }
                }
            }
        }

        private void MakeSelectedCopy()
        {
            EditedReportInfoViewModel selReport = null;
            if (isNewSelected || selectedReport != null)
                selReport = new EditedReportInfoViewModel(!isNewSelected ? selectedReport.GetModel() : null);
            SelectedCopy = selReport;
        }

        private ICommand changeHistoryReportCommand;
        public ICommand ChangeHistoryReportCommand
        {
            get { return changeHistoryReportCommand; }
        }

        private bool CanChangeHistoryReport()
        {
            return curAdminMode == ReportAdminMode.Statistics && reportStatistics != null && reportStatistics.SelReportUserReportStat != null;
        }

        private void ExecChangeHistoryReportCommand()
        {
            var selModel = selectedReport.GetModel();
            var rModel = new ReportModel
            {
                ReportId = selModel.Id,
                Mode = ReportModes.Server,
                Title = selModel.Title,
                Description = selModel.Description,
                Path = selModel.Path,
                CategoryName = selModel.CategoryName,
                IsA3Enabled = selModel.IsA3Enabled ?? false,
                ParamsGetterName = selModel.ParamsGetter,
                ParamsGetterOptions = selModel.ParamsGetterOptions
            };
            if (reportStatistics.SelReportUserReportStat.ParsedParameters != null)
                rModel.Parameters = reportStatistics.SelReportUserReportStat.ParsedParameters.ToDictionary(p => p.Key, p => p.Value);

            DoReport(rModel);
        }

        private ICommand runHistoryReportCommand;
        public ICommand RunHistoryReportCommand
        {
            get { return runHistoryReportCommand; }
        }

        private bool CanRunHistoryReport()
        {
            return curAdminMode == ReportAdminMode.Statistics && reportStatistics != null && reportStatistics.SelReportUserReportStat != null;
        }
        
        private void ExecRunHistoryReportCommand()
        {
            var selModel = selectedReport.GetModel();
            var rModel = new ReportModel
            {
                ReportId = selModel.Id,
                Mode = ReportModes.Server,
                Title = selModel.Title,
                Description = selModel.Description,
                Path = selModel.Path,
                CategoryName = selModel.CategoryName,
                IsA3Enabled = selModel.IsA3Enabled ?? false,
                ParamsGetterName = selModel.ParamsGetter,
                ParamsGetterOptions = selModel.ParamsGetterOptions
            };
            if (reportStatistics.SelReportUserReportStat.ParsedParameters != null)
                rModel.Parameters = reportStatistics.SelReportUserReportStat.ParsedParameters.ToDictionary(p => p.Key, p => p.Value);
            var rvm = new ReportViewModel(Parent, rModel);
            rvm.TryOpen();
        }

        private ICommand runReportCommand;
        public ICommand RunReportCommand
        {
            get { return runReportCommand; }
        }
        
        private bool CanRunReport()
        {
            return !isNewSelected && selectedCopy != null && !selectedCopy.IsChanged();
        }

        private void ExecRunReportCommand()
        {
            var selModel = selectedReport.GetModel();
            var rModel = new ReportModel 
            {
                ReportId = selModel.Id,
                Mode = ReportModes.Server,
                Title = selModel.Title,
                Description = selModel.Description,
                Path = selModel.Path, 
                CategoryName = selModel.CategoryName, 
                IsA3Enabled = selModel.IsA3Enabled ?? false, 
                ParamsGetterName = selModel.ParamsGetter, 
                ParamsGetterOptions = selModel.ParamsGetterOptions
            };
            DoReport(rModel);
        }

        private void DoReport(ReportModel _report)
        {
            ReportService service = null;

            Action work = () =>
            {
                try
                {
                    service = new ReportService(Parent, _report);
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

        private ICommand deleteReportInfoCommand;
        public ICommand DeleteReportInfoCommand
        {
            get { return deleteReportInfoCommand; }
        }

        private bool CanDeleteReportInfo()
        {
            return !isNewSelected && selectedCopy != null && selectedReport != null;
        }

        private void ExecDeleteReportInfo()
        {
            var title = selectedReport.Title;
            var askDlg = new MsgDlgViewModel
            {
                Title = "Внимание",
                Message = "Удалить отчёт " + title + " ?",
                IsCancelable = true,
                OnSubmit = d =>
                {
                    Parent.CloseDialog(d);
                    DeleteReportInfo();
                }
            };
            Parent.OpenDialog(askDlg);
        }

        private void DeleteReportInfo()
        {
            var id = selectedCopy.PreviousId;
            var title = selectedReport.Title;
            using (var db = new ServiceContext())
            {
                try
                {
                    var dr = db.ReportInfos.Find(id);
                    if (dr != null)
                        db.ReportInfos.Remove(dr);
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    Parent.Services.ShowMsg("Ошибка", "Не удалось удалить отчёт [id={0}] \n\n{1}".Format(id, e.Message), true);
                    WorkFlowHelper.OnCrash(e, "Не удалось удалить отчёт [id={0}]".Format(id), true);
                }
            }
            reports.Remove(selectedReport);
            SelectedCopy = null;
            SelectedReport = null;
            Parent.Services.ShowMsg("Информация", String.Format("Данные отчёта\nId={0} {1}\nудалены из системы!", id, title), true);
        }

        private ICommand saveReportInfoCommand;
        public ICommand SaveReportInfoCommand
        {
            get { return saveReportInfoCommand; }
        }

        private bool CanSaveReportInfo()
        {
            return selectedCopy != null
                && selectedCopy.IsValid()
                && selectedCopy.IsChanged();
        }

        private void ExecSaveReportInfo()
        {
            var report = selectedCopy.GetEditedReportInfo();
            if (report == null)
            {
                Parent.Services.ShowMsg("Ошибка", "Не удалось получить изменённую информацию об отчёте", true);
                return;
            }
            
            var id = selectedCopy.PreviousId;
            var title = selectedCopy.Title;
            ReportInfo updatedReport = null;

            using (var db = new ServiceContext())
            {
                try
                {
                    if (isNewSelected)
                        db.Entry(report).State = System.Data.Entity.EntityState.Added;
                    else
                        db.Entry(report).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                    updatedReport = db.ReportInfos.Find(report.Id);
                }
                catch (Exception e)
                {
                    Parent.Services.ShowMsg("Ошибка", "Не удалось сохранить изменённый данные об отчёте [id={0}] \n\n{1}".Format(id, e.Message), true);
                    WorkFlowHelper.OnCrash(e, "Не удалось сохранить изменённый данные об отчёте [id={0}]".Format(id), true);
                }
            }

            if (!isNewSelected)
                reports.Remove(selectedReport);

            if (updatedReport != null)
            {
                var nrVm = new ReportInfoViewModel(dbService, updatedReport);
                var rAfter = reports.OrderBy(r => r.Id).FirstOrDefault(r => r.Id > updatedReport.Id);
                if (rAfter == null)
                    reports.Add(nrVm);
                else
                    reports.Insert(reports.IndexOf(rAfter), nrVm);
                SelectedReport = nrVm;
            }
        }

        public enum ReportAdminMode { ReportDetails = 0, Parameters, Statistics }

        private ReportAdminMode curAdminMode = ReportAdminMode.ReportDetails;
        public ReportAdminMode CurAdminMode
        {
            get { return curAdminMode; }
            set
            {
                if (SetAndNotifyProperty(() => CurAdminMode, ref curAdminMode, value))
                {
                    switch (curAdminMode)
                    {
                        case ReportAdminMode.Parameters:
                            if (reportParams == null)
                                GetNewReportParams();
                            break;
                        case ReportAdminMode.Statistics:
                            if (reportStatistics == null)
                                GetNewReportStatistics();
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private void GetNewReportStatistics()
        {
            if (selectedCopy == null) return;
            Action init = () => 
            {
                ReportStatistics = new ReportStatAdminViewModel(selectedCopy);
            };
            Parent.Services.DoWaitAction(init);
        }

        private ReportStatAdminViewModel reportStatistics;
        public ReportStatAdminViewModel ReportStatistics
        {
            get { return reportStatistics; }
            set { SetAndNotifyProperty(() => ReportStatistics, ref reportStatistics, value); }
        }

        private void GetNewReportParams()
        {
            if (selectedCopy == null) return;
            Action init = () => 
            {
                ReportParams = new ReportParametersAdminViewModel(selectedCopy);
            };
            Parent.Services.DoWaitAction(init);
        }

        private ReportParametersAdminViewModel reportParams;
        public ReportParametersAdminViewModel ReportParams
        {
            get { return reportParams; }
            set { SetAndNotifyProperty(() => ReportParams, ref reportParams, value); }
        }

        private ICommand saveParamsSettingsCommand;
        public ICommand SaveParamsSettingsCommand
        {
            get { return saveParamsSettingsCommand; }
        }

        private bool CanSaveParamsSettings()
        {
            return reportParams != null && reportParams.ParamsSettingsChanged;
        }

        private void ExecSaveParamsSettingsCommand()
        {
            reportParams.SaveParamsSettings();
            CurAdminMode = ReportAdminMode.ReportDetails;
        }

        //public override void Dispose()
        //{
        //    base.Dispose();
        //}
    }
}
