using System;
using System.Linq;
using System.ComponentModel.Composition;
using CommonModule.Commands;
using CommonModule.ViewModels;
using DataObjects;
using PredoplModule.ViewModels;
using CommonModule.Helpers;
using DataObjects.SeachDatas;

namespace PredoplModule.Commands
{
    /// <summary>
    /// Комманда открытия окна просмотра предоплат
    /// </summary>

    [Export("PredoplModule.ModuleCommand", typeof(ModuleCommand))]
    public class ShowPredoplsCommand : ModuleCommand
    {
        public ShowPredoplsCommand()
        {
            Label = "Просмотр предоплат";
        }

        public override void Execute(object parameter)
        {
            base.Execute(parameter);

            if (Parent == null) return;

            var nDialog = MakeFilterDialog();
            if (nDialog != null)
                Parent.OpenDialog(nDialog);
        }

        private BaseDlgViewModel MakeFilterDialog()
        {
            SelectedCompositeDlgViewModel res = null;
            
            var pvd = new PoupValDatesDlgViewModel(Parent.Repository)
            {
                Title = "По направлению"
            };

            var kad = new KpokDatesDlgViewModel(Parent.Repository)
            {
                Title = "По плательщику", IsKpok = true, IsKgr = false, IsKaTypeSelection = false
            };

            res = new SelectedCompositeDlgViewModel()
            {
                Title = "Просмотреть предоплаты",
                OnSubmit = SubmitDlg
            };

            res.Add(pvd);
            res.Add(kad);

            var multi = MakeMultiFilter();
            res.Add(multi);

            return res;
        }

        private BaseDlgViewModel MakeMultiFilter()
        {
            var multiFilter = new BaseCompositeDlgViewModel()
            {
                Title = "Разное",
                Name = "MultiFilter"
            };

            var numDlg = new NumDlgViewModel() 
            {
                Title = "Номер документа"
            };
            multiFilter.AddSelectable(numDlg, false);

            var datesDlg = new DateRangeDlgViewModel()
            {
                Title = "Диапазон дат",
                DatesLabel = null
            };
            multiFilter.AddSelectable(datesDlg, false);

            var poupDlg = new PoupSelectionViewModel(Parent.Repository, false, false)
            {
                Title = "Направление реализации",
                PoupTitle = null
            };
            multiFilter.AddSelectable(poupDlg, false);

            var kaDlg = new KaSelectionViewModel(Parent.Repository)
            {   
                Title = "Плательщик"                
            };
            multiFilter.AddSelectable(kaDlg, false);

            return multiFilter;
        }

        private void SubmitDlg(Object _dlg)
        {
            Parent.CloseDialog(_dlg);
            var dlg = _dlg as SelectedCompositeDlgViewModel;
            if (dlg == null) return;

            if (dlg.SelectedDialog is PoupValDatesDlgViewModel)
                SubmitPoupValDatesDlg(dlg.SelectedDialog as PoupValDatesDlgViewModel);
            else
                if (dlg.SelectedDialog is KpokDatesDlgViewModel)
                    SubmitKpokDatesDlg(dlg.SelectedDialog as KpokDatesDlgViewModel);
                else
                    if (dlg.SelectedDialog.Name == "MultiFilter")
                        SubmitKpokDatesDlg(dlg.SelectedDialog as BaseCompositeDlgViewModel);
        }

        private void SubmitKpokDatesDlg(BaseCompositeDlgViewModel _dlg)
        {
            if (_dlg == null) return;

            var conts = _dlg.InnerParts.Cast<SelectableDlgViewModelContainer>().ToArray();
            if (conts.All(c => !c.IsSelected)) return;

            var schData = new PredoplSearchData();

            if (conts[0].IsSelected)
            {
                var ddlg = conts[0].InnerViewModel as NumDlgViewModel;
                if (ddlg.Number > 0)
                    schData.Ndok = Decimal.ToInt32(ddlg.Number);
            }

            if (conts[1].IsSelected)
            {
                var ddlg = conts[1].InnerViewModel as DateRangeDlgViewModel;
                schData.Dfrom = ddlg.DateFrom;
                schData.Dto = ddlg.DateTo;
            }

            if (conts[2].IsSelected)
            {
                var pdlg = conts[2].InnerViewModel as PoupSelectionViewModel;
                if (pdlg.SelPoup != null)
                {
                    schData.Poup = pdlg.SelPoup.Kod;
                    if (pdlg.IsPkodEnabled && !pdlg.IsAllPkods)
                    {
                        var pkodModel = pdlg.SelPkods[0];
                        if (pkodModel != null)
                            schData.Pkod = pkodModel.Pkod;
                    }
                }
            }

            if (conts[3].IsSelected)
            {
                var kdlg = conts[3].InnerViewModel as KaSelectionViewModel;
                var kpokModel = kdlg.SelectedKA;
                if (kpokModel != null)
                    schData.Kpok = kpokModel.Kgr;
            }
            
            Action work = () =>
                {
                    var models = Parent.Repository.GetPredopls(schData);
                    var ncontent = new PredoplsArcViewModel(Parent, models)
                    {
                        Title = "Выбранные предоплаты"
                    };
                    ncontent.TryOpen();
                };

            SeekAndShowPredopls(work);
        }

        private void SubmitKpokDatesDlg(KpokDatesDlgViewModel _dlg)
        {
            if (_dlg == null) return;

            var schData = new PredoplSearchData();
            schData.Dfrom = _dlg.DatesSelection.DateFrom;
            schData.Dto = _dlg.DatesSelection.DateTo;
            schData.Kpok = _dlg.KaSelection.SelectedKA.Kgr;
            string kpokname = _dlg.KaSelection.SelectedKA.Name;

            Action work = () =>
                {
                    var models = Parent.Repository.GetPredopls(schData);
                    var ncontent = new PredoplsArcViewModel(Parent, models)
                    {
                        Title = "Выбранные предоплаты от " + kpokname
                    };
                    ncontent.TryOpen();
                };

            SeekAndShowPredopls(work);
        }

        private void SubmitPoupValDatesDlg(PoupValDatesDlgViewModel dlg)
        {
            if (dlg == null) return;

            PoupModel poupm = dlg.SelPoup;
            var dateFrom = dlg.DateFrom;
            var dateTo = dlg.DateTo;

            PkodModel pm = null;
            if (dlg.IsPkodEnabled && dlg.SelPkods.Length > 0)
                pm = dlg.SelPkods[0];
            
            Valuta v = dlg.SelVal;

            Action work = () => (new PredoplsArcViewModel(Parent, v, poupm, dateFrom, dateTo, pm) 
            {
                Title = "Предоплаты, принятые в реализацию"
            }).TryOpen();

            SeekAndShowPredopls(work);

        }

        private void SeekAndShowPredopls(Action work)
        {
            Parent.Services.DoWaitAction(work, "Ожидание выполнения", "Поиск предоплат...");
        }

    }
}
