using System;
using System.Linq;
using System.ComponentModel.Composition;
using CommonModule;
using CommonModule.Helpers;
using CommonModule.Interfaces;
using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Interfaces;
using System.Collections.Generic;
using Microsoft.Reporting.WinForms;
using DAL;


namespace InfoModule.Helpers
{
    [Export("InfoModule.FDebCredParamsGetter", typeof(IReportParametersGetter))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class FDebCredParamsGetter : BaseReportParametersGetter
    {
        private IDbService repository = CommonSettings.Repository;

        public override BaseDlgViewModel GetDialog(ReportModel _repInfo, Action _onSubmit)
        {
            onSubmitDlg = _onSubmit;
            ReportInfo = _repInfo;

            var dialog = new BaseCompositeDlgViewModel
            {
                Title = "Параметры отчёта" + Environment.NewLine + _repInfo.Title,
                OnSubmit = OnParamsSubmitted
            };

            ChoicesDlgViewModel cdlg = new ChoicesDlgViewModel(debChoice, creChoice) 
            {
                Title = "Вид задолженности"                 
            };

            DateTime odt = DateTime.Now;

            DateDlgViewModel ddlg = new DateDlgViewModel(false)
            {
                Title = "По состоянию на",
                //MaxDate = odt.AddDays(1),
                SelDate = odt.AddDays(1-odt.Day)
            };

            ChoicesDlgViewModel syncDlg = new ChoicesDlgViewModel(syncChoice)
            {
                Title = "Сброс в АРМ Дебиторы/Кредиторы"
            };
            
            dialog.Add(cdlg);
            dialog.Add(ddlg);
            dialog.Add(syncDlg);

            return dialog;
        }

        private Choice debChoice = new Choice {GroupName = "Задолженность", Header = "Дебиторская", IsSingleInGroup = true};
        private Choice creChoice = new Choice {GroupName = "Задолженность", Header = "Кредиторская", IsSingleInGroup = true };
        private Choice syncChoice = new Choice {Header = "Сбросить", IsSingleInGroup = false, IsChecked = false};

        private Action onSubmitDlg;

        private DateTime onDate;
        private bool fExists;
        private DebtTypes debtType;

        private void OnParamsSubmitted(Object _dlg)
        {
            var dlg = _dlg as BaseCompositeDlgViewModel;
            if (dlg == null) return;

            var parent = dlg.Parent;
            parent.CloseDialog(dlg);

            debtType = (debChoice.IsChecked ?? false) ? DebtTypes.Debet : DebtTypes.Credit;

            var ddlg = dlg.DialogViewModels[1] as DateDlgViewModel;
            if (ddlg.SelDate == null) return;
            onDate = ddlg.SelDate.Value;

            parent.CloseDialog(dlg);

            repParams.Clear();
            repParams.Add(new ReportParameter("ondate", onDate.ToString("yyyy-MM-dd")));
            repParams.Add(new ReportParameter("debkred", debtType == DebtTypes.Debet ? "0" : "1"));
            repParams.Add(new ReportParameter("ConnString", CommonSettings.ConnectionString));

            AskParamsAndMakeFDebCred(parent);
        }

        private void AskForAction(IModule _parent, Action _makeaction)
        {
            Choice show = new Choice() { GroupName = "Выберите", Header = "Показать", IsChecked = true, IsSingleInGroup = true };
            Choice make = new Choice() { GroupName = "Выберите", Header = "Сформировать заново", IsChecked = false, IsSingleInGroup = true };

            var askDlg = new ChoicesDlgViewModel(show, make)
            {
                Title = "Внимание! Отчёт уже был сформирован",
                OnSubmit = d =>
                {
                    _parent.CloseDialog(d);
                    if (make.IsChecked == true)
                        _makeaction();
                    else
                        Continuation();
                }
            };

            _parent.OpenDialog(askDlg);
        }

        private void Continuation()
        {
            if (onSubmitDlg != null)
                onSubmitDlg();
        }

        private void AskParamsAndMakeFDebCred(IModule _parent)
        {
            var dlg = new MultiPoupSelectionViewModel(repository, true, true)
            {
                Title = "Выберите направления реализации",
                IsCanSelectPkod = true,
                OnSubmit = SubmitMakeFDebCred
            };

            _parent.OpenDialog(dlg);
        }

        private void SubmitMakeFDebCred(Object _dlg)
        {
            var dlg = _dlg as MultiPoupSelectionViewModel;
            if (dlg == null) return;

            var parent = dlg.Parent;
            parent.CloseDialog(dlg);

            var poupsdata = dlg.GetSelectedPoupsWithPkodsCodes();
            var poups = poupsdata.Keys.ToArray();
            var pkods = poupsdata.Values.Where(v => v != null).SelectMany(v => v).ToArray();

            Action work = () => FormFDebCredAction(parent, poups, pkods, onDate, debtType);
            if (parent == null)
                work();
            else
                parent.Services.DoWaitAction(work, "Подождите", "Формирование ведомости...", Continuation);
        }

        private void FormFDebCredAction(IModule _parent, int[] _poups, short[] _pkods, DateTime _odate, DebtTypes _type)
        {
            bool isupload = syncChoice.IsChecked ?? false;
            repository.MakeFDebCred(_poups, _pkods, _odate, _type, isupload);
            //if (isupload)
            //{
            //    DbfSyncer syncer = new DbfSyncer(repository);
            //    bool sres = syncer.SaveFDebCred(onDate, debtType);
            //    if (!sres && _parent != null)
            //        _parent.Services.ShowMsg("Ошибка при сбросе данных", syncer.LastError, true);
            //}
        }

    }
}
