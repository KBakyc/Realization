using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.ModuleServices;
using CommonModule.Interfaces;
using SfModule.ViewModels;
using DataObjects;
using CommonModule;
using CommonModule.ViewModels;
using CommonModule.DataViewModels;

namespace SfModule.Helpers
{
    /// <summary>
    /// Класс сервисных- и бизнес- операций модуля.
    /// </summary>
    public class SfService : BaseModuleService
    {
        public SfService(ISfModule _parent)
            : base(_parent)
        {
        }

        public void ShowSf(SfViewModel _svm)
        {
            if (_svm == null || _svm.MainReportForm == null) return;

            Action work = () => ExecShowReportSf(_svm.SfRef, _svm.MainReportForm);

            DoWaitAction(work, "Подождите", "Формирование предварительного просмотра счёта");
        }

        public void ShowSf(SfModel _sm)
        {
            if (_sm == null) return;

            Action work = () => 
            {
                var sfform = Parent.Repository.GetSfPrintForm(_sm.IdSf);
                if (sfform != null)
                    ExecShowReportSf(_sm, sfform);
            };

            DoWaitAction(work, "Подождите", "Формирование предварительного просмотра счёта");

        }

        private void ExecShowReportSf(SfModel _sm, ReportModel _rm)
        {
            if (_sm == null || _sm.IdSf == 0 || _rm == null) return;

            ApplyFeature withSigns = CommonSettings.GetNeedSignsModeForPoup(_sm.Poup);
            if (withSigns == ApplyFeature.Ask)
                ExecShowReportSfWithAsk(_rm);
            else
                DoShowSfReport(_rm, withSigns == ApplyFeature.Yes);
        }

        private void ExecShowReportSfWithAsk(ReportModel _rm)
        {
            Choice isPrintSigns = new Choice { GroupName = "Печатать", Header = "Подписи", IsChecked = true, IsSingleInGroup = false };
            var optDlg = new ChoicesDlgViewModel(isPrintSigns)
            {
                Title = @"Опции просмотра счёта",
                OnSubmit = d =>
                {
                    Parent.CloseDialog(d);
                    DoShowSfReport(_rm, isPrintSigns.IsChecked ?? false);
                }
            };
            Parent.OpenDialog(optDlg);
        }

        private void DoShowSfReport(ReportModel _rm, bool _sign)
        {
            _rm.Parameters["issign"] = _sign.ToString();
            (new ReportViewModel(Parent, _rm)).TryOpen();
        }

    }
}
