using System;
using System.ComponentModel.Composition;
using System.Linq;
using CommonModule.Commands;
using CommonModule.ViewModels;
using DataObjects;
using PredoplModule.ViewModels;

namespace PredoplModule.Commands
{
    /// <summary>
    /// Комманда модуля для запуска режима погашения предоплат на сформированные счета-фактуры.
    /// </summary>
    [Export("PredoplModule.ModuleCommand", typeof(ModuleCommand))]
    public class SinkPredoplsCommand : ModuleCommand
    {
        public SinkPredoplsCommand()
        {
            Label = "Погашение предоплат";
        }

        protected override int MinParentAccess
        {
            get { return 2; }
        }

        //public override bool CanExecute(object parameter)
        //{
        //    return base.CanExecute(parameter);
        //}

        public override void Execute(object parameter)
        {
            base.Execute(parameter);

            if (Parent == null) return;

            if (Parent.SelectContent<ClosePredoplViewModel>(null)) return;

            var today = DateTime.Today;
            DateTime dateZakr = today.Day < 15 ? today.AddDays(-today.Day)
                                               : new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));

            var pvdDlg = new PoupValDateDlgViewModel(Parent.Repository, dateZakr)
            {
                Title = "Параметры"
            };

            var modeDlg = new ChoicesDlgViewModel(
                new Choice { GroupName = "Отобразить", Header = "Все", IsSingleInGroup = true, IsChecked = false },
                new Choice { GroupName = "Отобразить", Header = "Погашения", IsSingleInGroup = true, IsChecked = true },
                new Choice { GroupName = "Отобразить", Header = "Остатки счетов", IsSingleInGroup = true, IsChecked = false },
                new Choice { GroupName = "Отобразить", Header = "Остатки предоплат", IsSingleInGroup = true, IsChecked = false }
                ) 
                {
                    Title = "Режим"
                };

            var newDlg = new BaseCompositeDlgViewModel
            {
                Title = "Погашать предоплаты",
                OnSubmit = SubmitDlg
            };

            newDlg.Add(pvdDlg);
            newDlg.Add(modeDlg);

            Parent.OpenDialog(newDlg);
        }

        private KaTotalDebtViewModel[] kaDebts;

        private void SubmitDlg(object _dlg)
        {
            Parent.CloseDialog(_dlg);

            var cdlg = _dlg as BaseCompositeDlgViewModel;
            if (cdlg == null) return;

            var pvdDlg = cdlg.DialogViewModels[0] as PoupValDateDlgViewModel;
            if (pvdDlg == null) return;

            PoupModel poupm = pvdDlg.SelPoup;
            PkodModel pm = pvdDlg.PoupSelection.IsPkodEnabled && pvdDlg.SelPkods.Length > 0 ? pvdDlg.SelPkods[0] : null;
            short pkod = pm == null ? (short)0 : pm.Pkod;
            Valuta v = pvdDlg.SelVal;
            DateTime dz = pvdDlg.SelDate.GetValueOrDefault();

            var modeDlg = cdlg.DialogViewModels[1] as ChoicesDlgViewModel;
            if (modeDlg == null) return;

            Func<KaTotalDebt, bool> filter = d => true;

            ChoiceViewModel selChoise = modeDlg.Groups.First().Value.SingleOrDefault(c => c.IsChecked ?? false);
            if (selChoise != null && selChoise.Header != "Все")
            {
                switch(selChoise.Header)
                {
                    case "Погашения": filter = d => (d.SumNeopl != 0 || d.SumVozvrat != 0) && d.SumPredopl != 0; break;
                    case "Остатки счетов": filter = d => d.SumNeopl != 0; break;
                    case "Остатки предоплат": filter = d => d.SumPredopl != 0; break;
                }
            }

            Action work = () => 
            {
                var data = Parent.Repository.GetTotalDebts(v.Kodval, poupm.Kod, pkod, dz).Where(filter);
                kaDebts = data.Select(d => new KaTotalDebtViewModel(Parent.Repository, d)).ToArray();
                
                var nDlg = new SelKaWithDebtsDlgViewModel(kaDebts)
                {
                    Title = "Выбор должника для погашения",
                    SelVal = v,
                    SelDate = dz,
                    SelPoup = poupm,
                    SelPkod = pm,
                    OnSubmit = SubmitSelKaWithDebts
                };
                Parent.OpenDialog(nDlg);                
            };

            Parent.Services.DoWaitAction(work, "Ожидание выполнения", "Выборка контрагентов...");
        }

        private void SubmitSelKaWithDebts(object _dlg)
        {
            Parent.CloseDialog(_dlg);
            var dlg = _dlg as SelKaWithDebtsDlgViewModel;
            if (dlg == null) return;

            KontrAgent ka = dlg.SelectedVm.Platelschik;
            Valuta v = dlg.SelVal;
            var p = dlg.SelPoup;
            DateTime dz = dlg.SelDate;
            PkodModel pk = dlg.SelPkod;

            var pm = Parent as PagesModuleViewModel;
            if (pm != null)
                pm.RemoveSimilarContents<ClosePredoplViewModel>();
            
            Action work = () =>
            {
                var nContent = new ClosePredoplViewModel(Parent, ka, v, p, dz, pk) 
                {
                    Title = "Погашение предоплат на счета-фактуры и возвраты",
                    KaDebts = kaDebts
                }; 
                nContent.TryOpen();
            };

            Parent.Services.DoWaitAction(work, "Ожидание выполнения", "Выборка данных для погашения контрагентов...");
        }
    }
}
