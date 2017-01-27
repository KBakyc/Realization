using System;
using System.Linq;
using CommonModule.Commands;
using CommonModule.Composition;
using CommonModule.ViewModels;
using OtgrModule.ViewModels;
using DataObjects;
using DataObjects.SeachDatas;

namespace OtgrModule.Commands
{
    //[Export("SfModule.ModuleCommand", typeof(ModuleCommand))]
    [ExportModuleCommand("OtgrModule.ModuleCommand", DisplayOrder = 3f)]
    public class ShowOtgruzModuleCommand : ModuleCommand
    {
        private bool isInRealiz;

        public ShowOtgruzModuleCommand()
        {
            Label = "Архив отгрузки";
        }

        public override void Execute(object parameter)
        {
            base.Execute(parameter);

            if (Parent == null) return;

            isInRealiz = true;
            otgrArcVM = null;

            var nDialog = MakeFilterDialog();
            if (nDialog != null)
                Parent.OpenDialog(nDialog);
        }

        private BaseDlgViewModel MakeFilterDialog()
        {            
            var fltDlg = new SelectedCompositeDlgViewModel()
            {
                Title = "Просмотреть принятую отгрузку"
            };

            var bynum = new BaseCompositeDlgViewModel
            {
                Title = "По № документа",
                Name = "BYNUM"
            };

            var bytype = new InvoiceTypeSelectionViewModel(Parent.Repository) 
            {
                Title = "Тип документа",
                IsAllSelectOption = true                
            };
            bytype.SelInvoiceType = bytype.AllSelectOption;

            var bydoc = new TxtDlgViewModel
            {
                Title = "Документ №"
            };
            var byrw = new TxtDlgViewModel
            {
                Title = "ЖД накладная №"
            };
            var bynv = new NumDlgViewModel
            {
                Title = "Вагон №"
            };      
            bynum.Add(bytype);      
            bynum.Add(bydoc);
            bynum.Add(byrw);
            bynum.Add(bynv);
            bynum.Check = bynum.SetCheck(d=>d.DialogViewModels.OfType<NumDlgViewModel>().Any(vm => vm.IntValue > 0) 
                                          ||d.DialogViewModels.OfType<TxtDlgViewModel>().Any(vm => !String.IsNullOrWhiteSpace(vm.Text)));

            var bypoup = new PoupAndDatesDlgViewModel(Parent.Repository)
            {
                Title = "По направлению",
                Name = "BYPOUP"
            };

            fltDlg.Add(bynum);
            fltDlg.Add(bypoup);

            var multi = MakeMultiFilter();
            fltDlg.Add(multi);

            fltDlg.SelectedDialog = bypoup;

            var res = new BaseCompositeDlgViewModel() 
            {
                Title = "Просмотреть принятую отгрузку",
                OnSubmit = SubmitDlg
            };
            res.Add(fltDlg);

            var chdlg = new ChoicesDlgViewModel(new Choice { Header = "Несколько", IsChecked = false }, 
                                                new Choice { Header = "В реализации", IsChecked = isInRealiz })
            {
                Title = "Режим"
            };
            res.Add(chdlg);

            return res;
        }

        private BaseDlgViewModel MakeMultiFilter()
        {
            var multiFilter = new BaseCompositeDlgViewModel()
            {
                Title = "Разное",
                Name = "MultiFilter"
            };

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

            var kpokDlg = new KaSelectionViewModel(Parent.Repository)
            {
                Title = "Плательщик"
            };
            multiFilter.AddSelectable(kpokDlg, false);
            
            var kgrDlg = new KaSelectionViewModel(Parent.Repository)
            {
                Title = "Получатель / Отправитель"
            };
            multiFilter.AddSelectable(kgrDlg, false);

            var kprDlg = new ProductSelectionViewModel(Parent.Repository)
            {
                Title = "Продукт / услуга"
            };
            multiFilter.AddSelectable(kprDlg, false);

            var nvDlg = new NumDlgViewModel
            {
                Title = "Вагон №",
                Check = (d) => (d as NumDlgViewModel).IntValue > 0
            };
            multiFilter.AddSelectable(nvDlg, false);

            return multiFilter;
        }

        private void SubmitDlg(Object _dlg)
        {            
            var dlg = _dlg as BaseCompositeDlgViewModel;
            if (dlg == null) return;
            var fltDlg = (dlg.DialogViewModels[0] as SelectedCompositeDlgViewModel);
            if (fltDlg == null) return;
            
            var optDlg = dlg.DialogViewModels[1] as ChoicesDlgViewModel;
            bool isMany = optDlg != null && (optDlg.Groups.Values.First()[0].IsChecked ?? false);
            if (!isMany) Parent.CloseDialog(dlg); 
            isInRealiz = optDlg != null && (optDlg.Groups.Values.First()[1].IsChecked ?? false);           

            if (fltDlg.SelectedDialog.Name == "BYNUM")
            {
                ShowByNumDlg(fltDlg.SelectedDialog as BaseCompositeDlgViewModel);
                return;
            }

            if (fltDlg.SelectedDialog.Name == "BYPOUP")
            {
                ShowByPoupDatesDlg(fltDlg.SelectedDialog as PoupAndDatesDlgViewModel);
                return;
            }
            if (fltDlg.SelectedDialog.Name == "MultiFilter")
            {
                ShowByMulti(fltDlg.SelectedDialog as BaseCompositeDlgViewModel);
                return;
            }
        }

        private void ShowByMulti(BaseCompositeDlgViewModel _dlg)
        {
            if (_dlg == null) return;

            var conts = _dlg.InnerParts.Cast<SelectableDlgViewModelContainer>().ToArray();
            if (conts.All(c => !c.IsSelected)) return;

            DateTime? dfrom = null;
            DateTime? dto = null;
            if (conts[0].IsSelected)
            {
                var ddlg = conts[0].InnerViewModel as DateRangeDlgViewModel;
                dfrom = ddlg.DateFrom;
                dto = ddlg.DateTo;
            }

            PoupModel poupM = null;
            PkodModel pkodM = null;
            if (conts[1].IsSelected)
            {
                var pdlg = conts[1].InnerViewModel as PoupSelectionViewModel;
                poupM = pdlg.SelPoup;
                if (pdlg.IsPkodEnabled && !pdlg.IsAllPkods)
                    pkodM = pdlg.SelPkods[0];
            }

            KontrAgent kpokM = null;
            if (conts[2].IsSelected)
            {
                var kpdlg = conts[2].InnerViewModel as KaSelectionViewModel;
                kpokM = kpdlg.SelectedKA;
            }
            
            KontrAgent kgrM = null;
            if (conts[3].IsSelected)
            {
                var kgdlg = conts[3].InnerViewModel as KaSelectionViewModel;
                kgrM = kgdlg.SelectedKA;
            }

            ProductInfo prod = null;
            if (conts[4].IsSelected)
            {
                var proddlg = conts[4].InnerViewModel as ProductSelectionViewModel;
                prod = proddlg.SelectedProduct;
            }

            int? poup = null;
            if (poupM != null) poup = poupM.Kod;

            short? pkod = null;
            if (pkodM != null) pkod = pkodM.Pkod;

            int? kpok = null;
            if (kpokM != null) kpok = kpokM.Kgr;
            
            int? kgr = null;
            if (kgrM != null) kgr = kgrM.Kgr;

            int? kpr = null;
            if (prod != null) kpr = prod.Kpr;

            int? nv = null;
            if (conts[5].IsSelected)
            {
                var nvdlg = conts[5].InnerViewModel as NumDlgViewModel;
                if (nvdlg.IntValue > 0)
                    nv = nvdlg.IntValue;
            }


            Action work = () =>
            {
                var otgr = Parent.Repository.GetOtgrArc(new OtgruzSearchData(isInRealiz) { Dfrom = dfrom, Dto = dto, Poup = poup, Pkod = pkod, Kpok = kpok, Kgr = kgr, Kpr = kpr, Nv = nv });
                LoadShowOtgrData(otgr);
            };
            
            Parent.Services.DoWaitAction(work, "Ожидание выполнения", "Выборка из архива отгрузки...");
        }

        private void ShowByNumDlg(BaseCompositeDlgViewModel dlg)
        {
            if (dlg == null) return;

            InvoiceType type = (dlg.DialogViewModels[0] as InvoiceTypeSelectionViewModel).SelInvoiceType;
            int? typeid = type == null ? (int?)null : type.IdInvoiceType;
            string docnum = (dlg.DialogViewModels[1] as TxtDlgViewModel).Text;
            string rwnum = (dlg.DialogViewModels[2] as TxtDlgViewModel).Text;
            int nv = (dlg.DialogViewModels[3] as NumDlgViewModel).IntValue;

            if (String.IsNullOrWhiteSpace(docnum) && String.IsNullOrWhiteSpace(rwnum) && nv == 0) return;

            Action work = () =>
            {
                var otgr = Parent.Repository.GetOtgrArc(new OtgruzSearchData(isInRealiz) { InvoiceTypeId = typeid, DocumentNumber = docnum, RwBillNumber = rwnum, Nv = nv });
                LoadShowOtgrData(otgr);
            };
            
            Parent.Services.DoWaitAction(work, "Ожидание выполнения", "Выборка из архива отгрузки...");
        }

        private void ShowByPoupDatesDlg(PoupAndDatesDlgViewModel dlg)
        {
            if (dlg == null) return;
            var poup = dlg.SelPoup;
            PkodModel pkod = null;
            if (dlg.IsPkodEnabled && dlg.SelPkods.Length > 0)
                pkod = dlg.SelPkods[0];
            var dateFrom = dlg.DateFrom;
            var dateTo = dlg.DateTo;

            Action work = () => (new OtgrArcViewModel(Parent, poup, pkod, dateFrom, dateTo, isInRealiz)).TryOpen();

            Parent.Services.DoWaitAction(work, "Ожидание выполнения", "Выборка из архива отгрузки...");
        }

        private OtgrArcViewModel otgrArcVM = null;

        private void LoadShowOtgrData(OtgrLine[] _odata)
        {
            if (_odata == null || _odata.Length == 0)
            {
                Parent.Services.ShowMsg("Результат", "Данные по отгрузке не найдены.", true);
                return; 
            }
            if (otgrArcVM == null)
            {
                otgrArcVM = new OtgrArcViewModel(Parent, _odata) { Title = "Выборка из архива отгрузки" };
                otgrArcVM.TryOpen();
            }
            else
                otgrArcVM.LoadOtgrArc(_odata, false);
            
        }
    }
}
