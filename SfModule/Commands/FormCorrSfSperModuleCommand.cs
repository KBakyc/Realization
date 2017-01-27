using System;
using System.Linq;
using CommonModule.Commands;
using CommonModule.Composition;
using CommonModule.Interfaces;
using CommonModule.ViewModels;
using DataObjects;
using SfModule.ViewModels;
using CommonModule.Helpers;
using DataObjects.Helpers;

namespace SfModule.Commands
{
    [ExportModuleCommand("SfModule.ModuleCommand", DisplayOrder = 1.2f)]
    public class FormCorrSfSperModuleCommand : ModuleCommand
    {
        private PoupModel selPoup;
        private OtgrDocModel[] selDocs;

        public FormCorrSfSperModuleCommand()
        {
            Label = "Сформировать корректировочный счёт по провозной плате";
            GroupName = "Формирование корректировочных счетов-фактур";
        }

        protected override int MinParentAccess
        {
            get { return 2; }
        }

        public override void Execute(object parameter)
        {
            base.Execute(parameter);

            if (Parent == null) return;

            if (!Parent.SelectContent<AcceptSfsViewModel>(null))
                CheckUnacceptedAndMakeNew();                
        }

        private void CheckUnacceptedAndMakeNew()
        {
            SfModel[] sfs = null;
            DateTime dateFrom = DateTime.Now;
            DateTime dateTo = dateFrom;
            int poup = 0;

            Action work = () =>
            {
                sfs = Parent.Repository.SelectUnacceptedSfs().OrderBy(s => s.NumSf).ToArray();
                if (sfs != null && sfs.Length > 0)
                {
                    DateRange dr = Parent.Repository.GetSfDateGrRange(sfs[0].IdSf);
                    dateFrom = dr.DateFrom;
                    dateTo = dr.DateTo;
                    poup = sfs[0].Poup;
                    for (int i = 1; i < sfs.Length; i++)
                    {
                        dr = Parent.Repository.GetSfDateGrRange(sfs[i].IdSf);
                        if (dr.DateFrom < dateFrom) dateFrom = dr.DateFrom;
                        if (dr.DateTo > dateTo) dateTo = dr.DateTo;
                    }
                }
            };

            Action afterwork = () =>
            {
                if (sfs == null || sfs.Length == 0)
                {
                    var ndlg = new BaseCompositeDlgViewModel
                    {
                        Title = "Приём данных",
                        OnSubmit = CollectOtgruz
                    };

                    ndlg.Add(new PoupSelectionViewModel(Parent.Repository)
                    {
                        Title = "Направление реализации",
                        PoupTitle = null
                    });
                    ndlg.Add(new NumDlgViewModel { Title = "Номер ЖД перечня" });
                    ndlg.Add(new NumDlgViewModel { Title = "Год" });

                    Parent.OpenDialog(ndlg);
                }
                else
                {
                    var nContent = new AcceptSfsViewModel(Parent, sfs)
                    {
                        Title = "Сформировано",
                        SelectedPoup = Parent.Repository.Poups[poup],
                        DateFrom = dateFrom,
                        DateTo = dateTo
                    };
                    nContent.TryOpen();
                }
            };

            Parent.Services.DoWaitAction(work, "Ожидание выполнения", "Загрузка неподтверждённых счетов-фактур...", afterwork);
        }

        private void CollectOtgruz(Object _dlg)
        {
            Parent.CloseDialog(_dlg);
            var dlg = _dlg as BaseCompositeDlgViewModel;
            if (dlg == null) return;

            var poupselection = dlg.DialogViewModels[0] as PoupSelectionViewModel;
            if (poupselection == null) return;
            var rwlistvm = dlg.DialogViewModels[1] as NumDlgViewModel;
            if (rwlistvm == null) return;
            var yearvm = dlg.DialogViewModels[2] as NumDlgViewModel;
            if (yearvm == null) return;

            selPoup = poupselection.SelPoup;
            int numrwlist = rwlistvm.IntValue;
            int year = yearvm.IntValue;

            Action work = () =>  DoCollectAndShowOtgruz(numrwlist, year, selPoup.Kod);

            Parent.Services.DoWaitAction(work, "Подождите", "Выборка отгрузки по перечню");
        }

        private void DoCollectAndShowOtgruz(int _numrwlist, int _year, int _poup)
        {

            var data = Parent.Repository.GetOtgrDocsForCorrSfSperByPerech(_numrwlist, _year, _poup);

            if (data == null || data.Length == 0)
                Parent.Services.ShowMsg("Результат", "Отгрузка не найдена", true);
            else
                Parent.OpenDialog(new Corrsf2OtgrDocsViewModel(Parent.Repository, data)
                {
                    Title = "Документы для формирования корректировочного счёта",
                    OnSubmit = SelectCorrSfData
                });
        }

        private Choice useoldnumsf = new Choice { Header = "Прежние номера счетов", IsChecked = false, IsSingleInGroup = false };

        /// <summary>
        /// После выбора накладных
        /// </summary>
        /// <param name="_dlg"></param>
        private void SelectCorrSfData(Object _dlg)
        {
            Parent.CloseDialog(_dlg);

            var dlg = _dlg as Corrsf2OtgrDocsViewModel;
            if (dlg == null) return;

            selDocs = dlg.SelectedOtgrDocs.ToArray();

            var chdlg = new ChoicesDlgViewModel(useoldnumsf)
            {
                Title = "Доп. параметры"
            };

            var ddlg = new DateDlgViewModel()
            {

                Title = "Дата счёта",
                DateLabel = null,
                SelDate = DateTime.Now,
            };

            var newdlg = new BaseCompositeDlgViewModel
            {
                Title = "Сформировать корректировочный счёт",
                OnSubmit = MakeCorrSf
            };

            newdlg.Add(ddlg);
            newdlg.Add(chdlg);

            Parent.OpenDialog(newdlg);
        }

        /// <summary>
        /// После выбора реквизитов счёта
        /// (формирование)
        /// </summary>
        /// <param name="_dlg"></param>
        private void MakeCorrSf(Object _dlg)
        {
            Parent.CloseDialog(_dlg);

            var dlg = _dlg as BaseCompositeDlgViewModel;
            if (dlg == null) return;

            var ddlg = dlg.DialogViewModels[0] as DateDlgViewModel;
            if (ddlg == null) return;

            if (ddlg.SelDate == null) return;
            DateTime datpltr = ddlg.SelDate.Value;
            bool oldsf = useoldnumsf.IsChecked ?? false;

            Action work = () =>
            {
                var formedSfs = Parent.Repository.MakeCorrSfSper(selDocs, selPoup.Kod, datpltr, oldsf);
                ShowUnaccepted(formedSfs);
            };

            Parent.Services.DoWaitAction(work, "Формирование корректировочного счёта", "Обработка данных");
        }

        /// <summary>
        /// Отображение неподтверждённых счетов
        /// </summary>
        /// <param name="_sfs"></param>
        private void ShowUnaccepted(SfModel[] _sfs)
        {
            ISfModule SfParent = Parent as ISfModule;
            if (SfParent == null) return;

            if (_sfs != null && _sfs.Length > 0)
            {
                var nContent = new AcceptSfsViewModel(Parent, _sfs)
                {
                    Title = "Сформировано корр. счета",
                    SelectedPoup = selPoup,
                };
                nContent.TryOpen();
            }
            else
                Parent.Services.ShowMsg("Результат", "Нет данных, удовлетворяющих указанным критериям.", true);
        }
    }
}
