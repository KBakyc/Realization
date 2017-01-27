using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.Interfaces;
using DataObjects;
using Microsoft.Reporting.WinForms;
using CommonModule.ViewModels;

namespace CommonModule.Helpers
{
    public class ReportService
    {
        Choice A4 = new Choice() { Header = "A4", IsSingleInGroup = true, GroupName = "Формат", IsChecked = true };
        Choice A3 = new Choice() { Header = "A3", IsSingleInGroup = true, GroupName = "Формат" };
        Choice Print = new Choice() { Header = "Печать", IsSingleInGroup = true, GroupName = "Вывод", IsChecked = true };
        Choice Preview = new Choice() { Header = "Просмотр", IsSingleInGroup = true, GroupName = "Вывод" };

        private IModule parent;
        private ReportModel reportModel;
        private Report actualReport;
        private ReportMode reportFeatures;
        private List<ReportParameter> reportParams;
        private IReportParametersGetter paramsGetter;

        public ReportService(IModule _parent, ReportModel _rep)
        {
            parent = _parent;
            reportModel = _rep;
            Init();
        }

        public ReportModel ReportModel { get { return reportModel; } }
        public Report ActualReport { get { return actualReport; } }
        public IEnumerable<ReportParameter> ReportParameters { get { return reportParams; } }

        private void Init()
        {
            if (reportModel == null) return;
            actualReport = ReportHelper.GetReportFromModel(reportModel);
            paramsGetter = ResolveParamsDialogGetter();
        }

        private IReportParametersGetter ResolveParamsDialogGetter()
        {
            IReportParametersGetter res = null;
            if (reportModel == null) return null;
            if (!String.IsNullOrEmpty(reportModel.ParamsGetterName))
                res = LookupNongenericDialogGetter();
            if (res == null)
                res = new GenericReportParametersGetter();
            if (res is GenericReportParametersGetter)
                (res as GenericReportParametersGetter).SetActualReport(actualReport);
            return res;
        }

        private IReportParametersGetter LookupNongenericDialogGetter()
        {
            var container = parent.ShellModel.Container;
            var getter = container.GetValueAndClearNonShared<IReportParametersGetter>(reportModel.ParamsGetterName);
            return getter;
        }

        public void ExecuteReport()
        {
            GetReportParameters();
        }

        private ICloseViewModel paramsDialog;

        private void GetReportParameters()
        {
            if (paramsGetter == null || parent == null) return;
            paramsDialog = paramsGetter.GetDialog(reportModel, OnParametersSubmitted);
            if (paramsDialog == null) return;
            parent.OpenDialog(paramsDialog);
        }

        private void OnParametersSubmitted()
        {
            parent.CloseDialog(paramsDialog);
            paramsDialog = null;
            reportParams = paramsGetter.ReportParameters.ToList();
            SelectModes();
        }

        private void SelectModes()
        {
            List<Choice> choices = new List<Choice>();
            reportFeatures = paramsGetter.GetReportFeatures();
            if (reportFeatures != null)
            {
                if (reportFeatures.isCanA3)
                {
                    choices.Add(A4);
                    choices.Add(A3);
                }
                if (reportFeatures.IsCanPrint)
                    choices.Add(Print);
                if (reportFeatures.isCanView)
                    choices.Add(Preview);

                if (choices.Count > 0)
                    parent.OpenDialog(new ChoicesDlgViewModel(choices.ToArray())
                    {
                        Title = "Выберите режимы отчёта ",
                        OnSubmit = OnModesSubmitted
                    });
            }
            else
                RunReport();

        }

        private void OnModesSubmitted(Object _d)
        {
            parent.CloseDialog(_d);
            var dlg = _d as ChoicesDlgViewModel;
            if (dlg == null) return;

            if ((A3.IsChecked ?? false) && actualReport != null && reportModel != null)
            {
                reportModel.Path += @"_A3";
                actualReport.InitActualReport(reportModel);
            }
            RunReport();
        }

        private void RunReport()
        {
            if (Print.IsChecked ?? false)
                PrintReport();
            else
            if (Preview.IsChecked ?? false)
                ShowReport();
        }

        private void PrintReport()
        {
            if (reportFeatures != null && String.IsNullOrEmpty(reportFeatures.SplitingParameter))
                PrintSingleReport();
            else
                SplitReportAndPrint(reportFeatures.SplitingParameter);
        }

        private void PrintSingleReport()
        {
            //if (actualReport == null) return;
            //if (reportParams != null && reportParams.Count > 0)
            //    actualReport.SetParameters(reportParams);
            //new PrintHelper().PrintReport(parent, actualReport, false, false);
            var ph = GetPrintHelperSettings();
            if (ph != null)
                PrintSingleReport(reportParams, ph, true);
        }

        private PrintHelper GetPrintHelperSettings()
        {
            PrintHelper printH = new PrintHelper();
            return printH.GetPrintSettings() ? printH : null;
        }
        
        private void PrintSingleReport(IEnumerable<ReportParameter> _pars, PrintHelper _ph, bool _isAsync)
        {
            if (actualReport == null || _pars == null || _ph == null) return;
            actualReport.SetParameters(_pars);            
            try
            {
                Action printWork = () => DoActualPrintReport(_ph, isAlterTopMargin, _isAsync);
                DoPrintWork(_ph, printWork, _isAsync);
            }
            catch (Exception ex)
            {
                WorkFlowHelper.OnCrash(ex);
            }
        }

        private void DoPrintWork(PrintHelper _ph, Action _printWork, bool _isAsync)
        {
            Action printWork = _isAsync ? () => parent.Services.DoWaitAction(_printWork, "Подождите", "Подготовка к печати...")
                                        : _printWork;
            if (_ph.CurrentPrinterSettings.CanDuplex && (int)_ph.CurrentPrinterSettings.Duplex > 1)
                CheckForAlterMarginsAndThen(printWork);
            else
                printWork();
        }

        private bool isAlterTopMargin;

        private void CheckForAlterMarginsAndThen(Action _after)
        {
            var chMarginsDlg = new ChoicesDlgViewModel(new Choice { GroupName = "Margins", Header = "Не менять", IsSingleInGroup = true, IsChecked = true },
                                                       new Choice
                                                       {
                                                           GroupName = "Margins",
                                                           Header = "Менять верхнее с нижним",
                                                           IsSingleInGroup = true,
                                                           IsChecked = false,
                                                           Info = "При двусторонней печати, для нечетных страниц, значения верхнего и нижнего полей будут заменены друг на друга. Для удобства подшивки и чтения, при двусторонней печати с переворотом по вертикали."
                                                       })
            {
                Title = "Настройка полей для двусторонней печати",
                OnSubmit = (d) =>
                {
                    var dlg = d as ChoicesDlgViewModel;
                    isAlterTopMargin = dlg.Groups.First().Value[1].IsChecked ?? false;
                    parent.CloseDialog(d);
                    if (_after != null)
                        _after();
                }
            };
            parent.OpenDialog(chMarginsDlg);
        }

        private void DoActualPrintReport(PrintHelper _ph, bool _isAlterTopMargin, bool _isAsync)
        {
            if (_ph == null) return;
            
            Action work = () => _ph.PrintReport(parent, actualReport, _isAlterTopMargin, true);
            
            if (_isAsync)
                parent.Services.DoWaitAction(work, "Подождите", "Подготовка к печати...");
            else
                work();            
        }

        private void SplitReportAndPrint(string _parName)
        {
            var splitPar = reportParams.SingleOrDefault(p => p.Name == _parName);
            if (splitPar == null) return;            
            PrintHelper pH = GetPrintHelperSettings();
           
            Action printWork = () =>
            {
                var splParamsGroups = paramsGetter.GetSplittedParams(splitPar);
                foreach (var pg in splParamsGroups)
                {
                    actualReport.SetParameters(pg);
                    DoActualPrintReport(pH, isAlterTopMargin, false);
                }
            };

            DoPrintWork(pH, printWork, true);
        }

        private void ShowReport()
        {
            if (reportFeatures != null && String.IsNullOrEmpty(reportFeatures.SplitingParameter))
                (new ReportViewModel(parent, this)).TryOpen();
            else
                SplitReportAndShow(reportFeatures.SplitingParameter);
        }

        private void SplitReportAndShow(string _parName)
        {
            var splitPar = reportParams.SingleOrDefault(p => p.Name == _parName);
            if (splitPar == null) return;
            var splParamsGroups = paramsGetter.GetSplittedParams(splitPar);
            foreach (var pg in splParamsGroups)
                ShowSingleReport(pg);
        }

        private void ShowSingleReport(IEnumerable<ReportParameter> _pars)
        {
            if (actualReport == null || _pars == null) return;
            reportParams = _pars.ToList();
            (new ReportViewModel(parent, this)).TryOpen();
        }
    }
}
