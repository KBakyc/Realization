using System;
using System.Collections.Generic;
using System.Linq;
using CommonModule.Commands;
using CommonModule.Composition;
using CommonModule.Interfaces;
using CommonModule.ViewModels;
using SfModule.ViewModels;
using DataObjects;
using CommonModule.DataViewModels;
using CommonModule.Helpers;
using DataObjects.SeachDatas;

namespace SfModule.Commands
{
    //[Export("SfModule.ModuleCommand", typeof(ModuleCommand))]
    [ExportModuleCommand("SfModule.ModuleCommand", DisplayOrder=3f)]
    public class ShowSfsModuleCommand : ModuleCommand
    {
        public ShowSfsModuleCommand()
        {
            Label = "Просмотр счетов-фактур ";
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

            res = new SelectedCompositeDlgViewModel()
            {
                Title = "Просмотреть счета-фактуры",
                OnSubmit = SubmitDlg
            };

            var bynumrange = new BaseCompositeDlgViewModel()
            {
                Title = "По номерам",
                Name = "bynumrange"
            };

            var bynumfrom = new NumDlgViewModel
            {
                Title = "С номера",
                Name = "bynumfrom"
            };

            var bynumto = new NumDlgViewModel
            {
                Title = "По номер",
                Name = "bynumto"
            };

            bynumrange.Add(bynumfrom);
            bynumrange.Add(bynumto);
            bynumrange.Check = bynumrange.SetCheck(d=>d.DialogViewModels.OfType<NumDlgViewModel>().Any(vm => vm.IntValue > 0));
            //{
            //    var compDlg = dlg as BaseCompositeDlgViewModel;
            //    return compDlg.DialogViewModels.OfType<NumDlgViewModel>().Any(vm => vm.IntValue > 0);
            //};

            var bypoup = new PoupAndDatesDlgViewModel(Parent.Repository)
            {
                Title = "По направлению"
            };
            
            // по плательщику
            var byka = new KpokDatesDlgViewModel(Parent.Repository)
            {
                Title = "По контрагенту",
                IsKpok = true
            };

            res.Add(bynumrange);
            res.Add(bypoup);
            res.Add(byka);


            var multi = MakeMultiFilter();
            res.Add(multi);

            res.SelectedDialog = bypoup;
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

            var mpoupDlg = new MultiPoupSelectionViewModel(Parent.Repository, true, false, false)
            {
                Title = "Направление реализации",                
                PoupTitle = null
            };
            multiFilter.AddSelectable(mpoupDlg, false);

            var kaDlg = new KaSelectionViewModel(Parent.Repository)
            {
                Title = "Плательщик"
            };
            multiFilter.AddSelectable(kaDlg, false);

            var esfnDlg = new BaseCompositeDlgViewModel() 
            {
                Title = "ЭСФН"
            };
            esfnDlg.Add(new TxtDlgViewModel { Title = "ЭСФН №" });
            esfnDlg.Add(new TxtDlgViewModel { Title = "Бал. счёт №" });
            
            multiFilter.AddSelectable(esfnDlg, false);

            return multiFilter;
        }

        private void SubmitDlg(Object _dlg)
        {
            Parent.CloseDialog(_dlg);

            var dlg = _dlg as SelectedCompositeDlgViewModel;
            if (dlg == null) return;

            if (dlg.SelectedDialog.Name == "bynumrange")
            {
                var sel = dlg.SelectedDialog as BaseCompositeDlgViewModel;
                SeekByNumRangeDlg(sel.DialogViewModels[0] as NumDlgViewModel, sel.DialogViewModels[1] as NumDlgViewModel);
                return;
            }
            if (dlg.SelectedDialog is PoupAndDatesDlgViewModel)
            {
                ShowByPoupDatesDlg(dlg.SelectedDialog as PoupAndDatesDlgViewModel);
                return;
            }
            if (dlg.SelectedDialog is KpokDatesDlgViewModel)
            {
                ShowByKpokDatesDlg(dlg.SelectedDialog as KpokDatesDlgViewModel);
                return;
            }
            if (dlg.SelectedDialog.Name == "MultiFilter")
            {
                ShowByMulti(dlg.SelectedDialog as BaseCompositeDlgViewModel);
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

            Dictionary<PoupModel, PkodModel[]> poupData = null;
            if (conts[1].IsSelected)
            {
                var pdlg = conts[1].InnerViewModel as MultiPoupSelectionViewModel;
                poupData = pdlg.GetSelectedPoupsWithPkodsModels();
            }

            KontrAgent kpok = null;
            if (conts[2].IsSelected)
            {
                var kdlg = conts[2].InnerViewModel as KaSelectionViewModel;
                kpok = kdlg.SelectedKA;
            }

            string esfnBalSchet = null;
            string esfnNumber = null;
            if (conts[3].IsSelected)
            {
                var esfnDlg = conts[3].InnerViewModel as BaseCompositeDlgViewModel;
                if (esfnDlg != null)
                {
                    var enDlg = esfnDlg.InnerParts[0].InnerViewModel as TxtDlgViewModel;
                    if (enDlg != null && !String.IsNullOrWhiteSpace(enDlg.Text))
                        esfnNumber = enDlg.Text.Trim();

                    var bsDlg = esfnDlg.InnerParts[1].InnerViewModel as TxtDlgViewModel;
                    if (bsDlg != null && !String.IsNullOrWhiteSpace(bsDlg.Text))
                        esfnBalSchet = bsDlg.Text.Trim();
                }
            }

            Action work = () => ShowSfsByMulti(poupData, kpok, dfrom, dto, esfnBalSchet, esfnNumber);
            SeekAndShowSfs(work);
        }

        private void ShowByKpokDatesDlg(KpokDatesDlgViewModel _dlg)
        {
            if (_dlg == null) return;
            var ka = _dlg.KaSelection.SelectedKA;
            var dateFrom = _dlg.DatesSelection.DateFrom;
            var dateTo = _dlg.DatesSelection.DateTo;
            Action work = () => ShowSfsByKpokAndDates(ka, _dlg.IsKpok, _dlg.IsKgr, dateFrom, dateTo);
            SeekAndShowSfs(work);

        }

        private void ShowByPoupDatesDlg(PoupAndDatesDlgViewModel _dlg)
        {
            if (_dlg == null) return;

            PoupModel poup = _dlg.SelPoup;
            PkodModel pkod = null;
            if (_dlg.PoupSelection.IsPkodEnabled && !_dlg.PoupSelection.IsAllPkods)
                pkod = _dlg.PoupSelection.SelPkods[0];
            var dateFrom = _dlg.DateFrom;
            var dateTo = _dlg.DateTo;

            Action work = () => ShowSfsByPoupAndDates(poup, pkod, dateFrom, dateTo);
            SeekAndShowSfs(work);
        }

        private void ShowSfsByKpokAndDates(KontrAgent _ka, bool _isKpok, bool _isKgr, DateTime _dtFrom, DateTime _dtTo)
        {
            var sfs = GetSfs(0, 0, _isKpok ? _ka.Kgr : 0, _isKgr ? _ka.Kgr : 0, _dtFrom, _dtTo, null, null);

            ISfModule SfParent = Parent as ISfModule;
            if (SfParent == null) return;

            if (sfs.Length > 0)
            {
                var nContent = new R635ViewModel(Parent, sfs)
                {
                    Title = String.Format("Архив счетов-фактур по {2}: ({0}) {1}", _ka.Kgr, _ka.Name, (_isKpok ? "плательщику" : "") + (_isKpok && _isKgr ? " и " : "") + (_isKgr ? "получателю" : "")),
                    DateFrom = _dtFrom,
                    DateTo = _dtTo,
                    RefreshCommand = new DelegateCommand<R635ViewModel>(vm =>
                    {
                        Action wk = () =>
                        {
                            var newsfs = GetSfs(0, 0, _isKpok ? _ka.Kgr : 0, _isKgr ?  _ka.Kgr : 0, _dtFrom, _dtTo, null, null);
                            vm.LoadData(newsfs);
                        };
                        Parent.Services.DoWaitAction(wk, "Ожидание выполнения", "Выборка и обновление списка счетов...");
                    })
                };
                nContent.TryOpen();
            }
            else
                Parent.Services.ShowMsg("Результат", "Нет данных, удовлетворяющих указанным критериям.", true);
        }


        /// <summary>
        /// Выборка и формирование представления счетов по направлению за интервал дат
        /// </summary>
        /// <param name="_poup"></param>
        /// <param name="_dtFrom"></param>
        /// <param name="_dtTo"></param>
        private void ShowSfsByPoupAndDates(PoupModel _poup, PkodModel _pkod, DateTime _dtFrom, DateTime _dtTo)
        {
            short pkod = _pkod == null ? (short)0 : _pkod.Pkod;
            var sfs = GetSfs(_poup.Kod, pkod, 0, 0, _dtFrom, _dtTo, null, null).ToArray();

            ISfModule SfParent = Parent as ISfModule;
            if (SfParent == null) return;

            if (sfs.Length > 0)
            {
                var nContent = new R635ViewModel(Parent, sfs)
                            {
                                Title = "Архив счетов-фактур",
                                SelectedPoup = _poup,
                                SelectedPkod = _pkod,
                                DateFrom = _dtFrom,
                                DateTo = _dtTo,
                                RefreshCommand = new DelegateCommand<R635ViewModel>(vm =>
                                {
                                    Action wk = () =>
                                    {
                                        var newsfs = GetSfs(_poup.Kod, pkod, 0, 0, _dtFrom, _dtTo, null, null);
                                        vm.LoadData(newsfs);
                                    };
                                    Parent.Services.DoWaitAction(wk, "Ожидание выполнения", "Выборка и обновление списка счетов...");
                                })
                            };
                nContent.TryOpen();
            }
            else
                Parent.Services.ShowMsg("Результат", "Нет данных, удовлетворяющих указанным критериям.", true);
        }

        private List<SfInListViewModel> GetSfsByPoupData(Dictionary<int, short[]> _poupData, int _kpok, DateTime? _dtFrom, DateTime? _dtTo, string _esfnBalSchet, string _esfnNumber)
        {
            List<SfInListViewModel> sfs = null;
            if (_poupData == null || _poupData.Count == 0)
                sfs = GetSfs(0, 0, _kpok, 0, _dtFrom, _dtTo, _esfnBalSchet, _esfnNumber).ToList();
            else
            {
                sfs = new List<SfInListViewModel>();
                foreach (var ppkM in _poupData)
                {
                    int poup = ppkM.Key;
                    if (ppkM.Value == null || ppkM.Value.Length == 0)
                        sfs.AddRange(GetSfs(poup, 0, _kpok, 0, _dtFrom, _dtTo, _esfnBalSchet, _esfnNumber));
                    else
                    {
                        foreach (var pkod in ppkM.Value)
                            sfs.AddRange(GetSfs(poup, pkod, _kpok, 0, _dtFrom, _dtTo, _esfnBalSchet, _esfnNumber));
                    }
                }
            }
            return sfs;
        }

        private void ShowSfsByMulti(Dictionary<PoupModel, PkodModel[]> _poupData, KontrAgent _ka, DateTime? _dtFrom, DateTime? _dtTo, string _esfnBalSchet, string _esfnNumber)
        {
            int kpok = _ka == null ? 0 : _ka.Kgr;
            Dictionary<int, short[]> poupDataCodes = _poupData == null ? null 
                                                                       : _poupData.ToDictionary(kv => kv.Key.Kod, kv => kv.Value == null ? null 
                                                                                                                                         : kv.Value.Select(v => v.Pkod).ToArray());

            var sfs = GetSfsByPoupData(poupDataCodes, kpok, _dtFrom, _dtTo, _esfnBalSchet, _esfnNumber);

            ISfModule SfParent = Parent as ISfModule;
            if (SfParent == null) return;

            if (sfs.Count > 0)
            {
                var selPkods = _poupData != null ? _poupData.Where(kv => kv.Value != null).SelectMany( kv => kv.Value).Distinct().ToArray() 
                                                 : null;
                var nContent = new R635ViewModel(Parent, sfs)
                {
                    Title = String.IsNullOrWhiteSpace(_esfnBalSchet) ? "Архив счетов-фактур" 
                                                                     : ("Счета-фактуры балансового счёта " + _esfnBalSchet),
                    SelectedPoup = _poupData != null ? _poupData.Keys.FirstOrDefault() : null,
                    SelectedPkod = selPkods != null && selPkods.Length == 1 ? selPkods[0] : null,
                    DateFrom = _dtFrom,
                    DateTo = _dtTo ?? DateTime.Now,
                    RefreshCommand = new DelegateCommand<R635ViewModel>(vm =>
                    {
                        Action wk = () =>
                        {
                            var newsfs = GetSfsByPoupData(poupDataCodes, kpok, _dtFrom, _dtTo, _esfnBalSchet, _esfnNumber);
                            vm.LoadData(newsfs);
                        };
                        Parent.Services.DoWaitAction(wk, "Ожидание выполнения", "Выборка и обновление списка счетов...");
                    })
                };
                nContent.TryOpen();
            }
            else
                Parent.Services.ShowMsg("Результат", "Нет данных, удовлетворяющих указанным критериям.", true);
        }

        private SfInListViewModel[] GetSfs(int _poup, short _pkod, int _kpok, int _kgr, DateTime? _dtFrom, DateTime? _dtTo, string _esfnBalSchet, string _esfnNumber)
        {
            var l_sfs = Parent.Repository.GetSfsList(new SfSearchData{ Poup = _poup, Pkod = _pkod, Kpok = _kpok, DateFrom = _dtFrom, DateTo = _dtTo, ESFN_BalSchet = _esfnBalSchet, ESFN_Number = _esfnNumber})
                                         .OrderBy(s => s.NumSf)
                                         .Select(h => new SfInListViewModel(Parent.Repository, h)).ToArray();
            return l_sfs;
        }

        private void SeekByNumRangeDlg(NumDlgViewModel _dlgfrom, NumDlgViewModel _dlgto)
        {
            if (_dlgfrom == null || _dlgto == null) return;
            int numfrom = _dlgfrom.IntValue;
            int numto = _dlgto.IntValue;

            if (numfrom <= 0 && numto <= 0) return;

            Action work = () => SeekSfsByNumRange(numfrom, numto);
            SeekAndShowSfs(work);
        }

        /// <summary>
        /// Поиск счетов и отображение результатов
        /// </summary>
        /// <param name="_num"></param>
        private void SeekSfsByNumRange(int _numfrom, int _numto)
        {
            if (_numfrom <= 0 && _numto <= 0) return;

            ISfModule ISfParent = Parent as ISfModule;
            if (ISfParent == null) return;

            IEnumerable<SfInListViewModel> sfs = GetSfs(_numfrom, _numto);//Parent.Repository.GetSfsList(_num).Select(h => new SfInListViewModel(Parent.Repository, h));

            var nContent = new R635ViewModel(Parent, sfs)
            {
                Title = String.Format("Счета: {0}*", _numfrom),
                RefreshCommand = new DelegateCommand<R635ViewModel>(
                    vm =>
                    {
                        Action wk = () =>
                        {
                            var newsfs = GetSfs(_numfrom, _numto);
                            vm.LoadData(newsfs);
                        };
                        Parent.Services.DoWaitAction(wk, "Ожидание выполнения", "Выборка и обновление списка счетов...");
                    })
            };
            nContent.TryOpen();
        }

        private SfInListViewModel[] GetSfs(int _numfrom, int _numto)
        {
            if (_numfrom <= 0 && _numto <= 0) return null;

            SfInListViewModel[] res = null;
            IEnumerable<SfInListInfo> data;
            if (_numto == 0)
                data = Parent.Repository.GetSfsList(new SfSearchData { NumsfFrom = _numfrom, NumsfTo = _numfrom });
            else
                data = Parent.Repository.GetSfsList(new SfSearchData { NumsfFrom = _numfrom, NumsfTo = _numto });
            
            if (data != null)
                res = data.OrderBy(s => s.NumSf).Select(h => new SfInListViewModel(Parent.Repository, h)).ToArray();
            
            return res;
        }

        private void SeekAndShowSfs(Action work)
        {
            Parent.Services.DoWaitAction(work, "Ожидание выполнения", "Поиск счетов-фактур...");
        }
    }
}
