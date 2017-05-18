using System;
using System.Linq;
using System.Windows.Input;
using CommonModule.Commands;
using CommonModule.Interfaces;
using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Collections;
using System.Collections.Generic;
using OtgrModule.Helpers;
using System.Collections.ObjectModel;


namespace OtgrModule.ViewModels
{
    /// <summary>
    /// Модель режима приёмки новой отгрузки в реализацию из внешних источников.
    /// </summary>
    public class GetOtgrViewModel : BasicModuleContent
    {
        private OtgrLine[] otgrs;

        public GetOtgrViewModel(IModule _parent, IEnumerable<OtgrLine> _otgrs)
            : this(_parent)
        {
            otgrs = _otgrs.ToArray();
        }

        public GetOtgrViewModel(IModule _parent)
            : base(_parent)
        {
            Title = "Приём отгрузки/услуг из внешних источников";
            CommonInit();
        }

        /// <summary>
        /// Общая инициализация компонента при создании
        /// </summary>
        private void CommonInit()
        {
            LoadOtgrArc();
        }

        private void LoadOtgrArc()
        {
            if (otgrs == null)
                OtgrRows = new ObservableCollection<OtgrLineViewModel>();
            else
                OtgrRows = new ObservableCollection<OtgrLineViewModel>(
                                                                        otgrs.Select(m => new OtgrLineViewModel(Parent.Repository, m))
                                                                      );
        }

        private OtgrLineViewModel selectedOtgr;
        /// <summary>
        /// Выбранная отгрузка
        /// </summary>
        public OtgrLineViewModel SelectedOtgr
        {
            get { return selectedOtgr; }
            set
            {
                if (value != selectedOtgr)
                {
                    foreach (var o in otgrRows.Where(or => or != value && or.IsChecked))
                        o.IsChecked = false;
                    selectedOtgr = value;
                    if (selectedOtgr != null)
                        selectedOtgr.Totals = OtgrHelper.GetOtgrTotals(selectedOtgr, otgrRows);
                    NotifyPropertyChanged("SelectedOtgr");
                }
            }
        }

        /// <summary>
        /// Строки отгрузок
        /// </summary>
        private ObservableCollection<OtgrLineViewModel> otgrRows;
        public ObservableCollection<OtgrLineViewModel> OtgrRows
        {
            get
            {
                return otgrRows;
            }
            set
            {
                if (value != otgrRows)
                    otgrRows = value;
                NotifyPropertyChanged("OtgrRows");
            }
        }

        private ICommand deleteCommand;

        /// <summary>
        /// Комманда удаления выбранной отгрузки
        /// </summary>
        public ICommand DeleteCommand
        {
            get
            {
                if (deleteCommand == null)
                    deleteCommand = new DelegateCommand(ExecDeleteCommand, CanExecDeleteCommand);
                return deleteCommand;
            }
        }
        private bool CanExecDeleteCommand()
        {
            return OtgrRows.Any(r => r.IsChecked);
        }

        private void ExecDeleteCommand()
        {
            var selOtgr = OtgrRows.Where(r => r.IsChecked).ToArray();
            DeleteOtgruz(selOtgr);
        }

        private string GetOtgrString(OtgrLineViewModel _o)
        {
            return string.Format("Накладная №{0} на \"{1}\"", _o.Otgr.DocumentNumber, _o.Product.Name)
                             + (_o.TransportId == 3 ? string.Format(" вагон №{0}", _o.Otgr.Nv) : "");
        }

        private void DeleteOtgruz(OtgrLineViewModel[] _selOtgr)
        {
            for (int i = 0; i < _selOtgr.Length; i++)
                OtgrRows.Remove(_selOtgr[i]);
        }

        private ICommand editCommand;
        public ICommand EditCommand
        {
            get
            {
                if (editCommand == null)
                    editCommand = new DelegateCommand(ExecEditCommand, CanEdit);
                return editCommand;
            }
        }
        private bool CanEdit()
        {
            return selectedOtgr != null;
        }
        private void ExecEditCommand()
        {            
            var ndlg = new EditOtgrDlgViewModel(Parent.Repository, SelectedOtgr.Otgr, false)
            {
                OnSubmit = DoEditOtgr
            };

            PrepareEditDlg(ndlg);
            //ndlg.SubscribeViewModelChanges();

            Parent.OpenDialog(ndlg);
        }

        private void PrepareEditDlg(EditOtgrDlgViewModel _d)
        {
            var selOtgr = SelectedOtgr.Otgr;

            if (selOtgr.SourceId == 2) // из обменной базы ЖД
            {
                var aDavs = GetDavsByPoup(selOtgr.Poup);
                IEnumerable<KontrAgent> aKpoks = Enumerable.Empty<KontrAgent>();                
                if (selOtgr.Kpok != 0 && !aDavs.Any(k => k.Kgr == selOtgr.Kpok))
                    aKpoks = Enumerable.Repeat(SelectedOtgr.Pokupatel, 1);
                if (aDavs != null && aDavs.Length > 0)
                    aKpoks = aKpoks.Concat(aDavs);

                if (aKpoks != null)
                {                    
                    var platDlg = new KaSelectionViewModel(Parent.Repository, aKpoks)
                    {
                        Title = "Давалец / плательщик"
                        ,IsSearchEnabled = false
                    };
                    _d.SetPlatEditor(platDlg, true);
                    var selpok = platDlg.KaList.SingleOrDefault(k => k.Kgr == selOtgr.Kpok);
                    platDlg.SelectedKA = selpok;
                    
                }

                int stKgr = 0;

                if (selOtgr.Stgr != Parent.Repository.OurKodStan)
                {
                    _d.GrpolVM.Title = "Грузополучатель";
                    stKgr = selOtgr.Stgr;
                }
                else
                {
                    _d.GrpolVM.Title = "Отправитель";
                    stKgr = selOtgr.Stotpr;
                }

                if (stKgr != 0)
                {
                    var kgrs = Parent.Repository.GetKontragentsByRwStation(stKgr);
                    if (kgrs != null)
                    {
                        foreach (var ka in kgrs)
                            if (!_d.GrpolVM.KaList.Any(k => k.Kgr == ka.Kgr))
                                _d.GrpolVM.KaList.Add(ka);
                    }
                }

                _d.SubscribeViewModelChanges();

                if (_d.PoupVM.SelPoup.IsDav) //для недавальческих направлений оставить возможность выбора (нет возможности определить верное направление)
                {
                    _d.IsPoupEdEnabled = false;
                }

                _d.IsKodfEdEnabled = _d.IsDocumentNumberEdEnabled = _d.IsPeriodEdEnabled
                                   = _d.IsVidcenEdEnabled = _d.IsCenaEdEnabled = _d.IsValEdEnabled = _d.IsNdsEdEnabled 
                                   = _d.IsCountryEdEnabled
                                   = false;
                _d.IsStFromEdEnabled = selOtgr.Stotpr == 0;
                _d.IsStToEdEnabled = selOtgr.Stgr == 0;
            }
        }

        private Dictionary<int,KontrAgent[]> allDavs = new Dictionary<int,KontrAgent[]>();
        
        private KontrAgent[] GetDavsByPoup(int _poup)
        {
            KontrAgent[] res = null;
            if (allDavs.ContainsKey(_poup))
                res = allDavs[_poup];
            else
            {
                res = Parent.Repository.GetDavsByPoup(_poup);
                allDavs[_poup] = res;
            }
            return res;
        }

        private void DoEditOtgr(Object _d)
        {
            var dlg = _d as EditOtgrDlgViewModel;
            if (dlg == null) return;
            Parent.CloseDialog(_d);

            var oldotgr = selectedOtgr.Otgr;
            var newotgr = dlg.NewModel;

            //if (newotgr.SourceId == 2)
            //    newotgr.KodDav = dlg.PlatVM.SelectedKA.Koddav;

            if (oldotgr.Kpok != newotgr.Kpok || oldotgr.Kgr != newotgr.Kgr || oldotgr.Kdog != newotgr.Kdog 
             || oldotgr.Kpr != newotgr.Kpr || (oldotgr.MeasureUnitId ?? 0) != (newotgr.MeasureUnitId ?? 0)
             || oldotgr.KodDav != newotgr.KodDav || oldotgr.WL_S != newotgr.WL_S || oldotgr.Provoz != newotgr.Provoz 
             || oldotgr.Stgr != newotgr.Stgr || oldotgr.Stotpr != newotgr.Stotpr || oldotgr.Datgr != newotgr.Datgr
             || oldotgr.IdInvoiceType != newotgr.IdInvoiceType) 
            {
                var naklItems = otgrRows.Where(r => r.DocumentNumber == oldotgr.DocumentNumber && r.Datgr == oldotgr.Datgr && r.Otgr.Kgr == oldotgr.Kgr && r != selectedOtgr);
                if (naklItems.Any())
                    ChangeNakl(naklItems, newotgr);
                
                // замена продукта в выделенных записях, если продуты были одинаковые и договор не менялся
                if (oldotgr.Kpr != newotgr.Kpr && oldotgr.Kdog == newotgr.Kdog)
                {
                    var selRowsWithProdLike = otgrRows.Where(r => r.IsChecked && r != selectedOtgr && r.Otgr.Kpr == oldotgr.Kpr).ToArray(); //&& IntLike(newotgr.Kpr, r.Otgr.Kpr)).ToArray();
                    for (int i = 0; i < selRowsWithProdLike.Length; i++)
                    {
                        var cr = selRowsWithProdLike[i];
                        var crIndex = otgrRows.IndexOf(cr);
                        cr.Otgr.Kpr = newotgr.Kpr;
                        ChangeOtgrItem(crIndex, cr.Otgr);
                    }
                }
            }
            
            var selIndex = otgrRows.IndexOf(selectedOtgr);
            ChangeOtgrItem(selIndex, newotgr);

        }

        //private bool IntLike(int _checked, int _pattern)
        //{
        //    if (_checked == _pattern) return true;
                        
        //    var pattxt = _pattern.ToString();
        //    var chtxt = _checked.ToString();
        //    var lenp = pattxt.Length;
        //    var lenc = chtxt.Length;
        //    if (lenp != lenc) return false;

        //    var sb = new System.Text.StringBuilder();
        //    for (int i = lenp - 1; i >= 0; i--)
        //        if (pattxt[i] != '0') sb.Insert(0, pattxt[i]);
        //    if (chtxt.StartsWith(sb.ToString())) return true;
                
        //    return false;
        //}

        private void ChangeOtgrItem(int _index, OtgrLine _newdata)
        {
            var newOtgrViewModel = new OtgrLineViewModel(Parent.Repository, _newdata);
            otgrRows.RemoveAt(_index);
            otgrRows.Insert(_index, newOtgrViewModel);
            CheckOtgrLine(newOtgrViewModel);
        }

        private void ChangeNakl(IEnumerable<OtgrLineViewModel> _orows, OtgrLine _newotgr)
        {
            var orows = _orows.ToArray();
            for(int i = 0; i< orows.Length; i++)
            {
                var or = orows[i];
                var rIndex = otgrRows.IndexOf(or);
                var odata = or.Otgr;
                odata.Poup = _newotgr.Poup;
                odata.DocumentNumber = _newotgr.DocumentNumber;
                odata.RwBillNumber = _newotgr.RwBillNumber;
                odata.Datgr = _newotgr.Datgr;
                odata.Datnakl = _newotgr.Datnakl;
                odata.Kgr = _newotgr.Kgr;
                odata.Kpok = _newotgr.Kpok;
                odata.KodDav = _newotgr.KodDav;
                odata.Kpr = _newotgr.Kpr;
                odata.MeasureUnitId = _newotgr.MeasureUnitId;
                odata.Kdog = _newotgr.Kdog;
                odata.Kodcen = _newotgr.Kodcen;
                odata.Stotpr = _newotgr.Stotpr;
                odata.Stgr = _newotgr.Stgr;
                odata.WL_S = _newotgr.WL_S;
                odata.Provoz = _newotgr.Provoz;
                odata.IdInvoiceType = _newotgr.IdInvoiceType;
                ChangeOtgrItem(rIndex, odata);
            }
        }

        private void CheckOtgrLine(OtgrLineViewModel _otgrVM)
        {
            var otgr = _otgrVM.Otgr;
            List<string> errors = new List<string>();
            if (String.IsNullOrWhiteSpace(otgr.DocumentNumber)) errors.Add(@"Неверный номер документа");
            if (otgr.Datgr == default(DateTime)) errors.Add(@"Неверная дата");
            if (otgr.Kpok <= 0) errors.Add(@"Неверный код плательщика");
            if (otgr.Kgr <= 0) errors.Add(@"Неверный код грузоотправителя/грузополучателя");
            if (otgr.Kpr <= 0) errors.Add(@"Неверный код продукта/услги");
            if (otgr.Kdog <= 0) errors.Add("Неверный договор");
            if (String.IsNullOrEmpty(otgr.WL_S)) errors.Add("Неверный тип отгрузки (ДД,ГЗ,СН)");
            
            _otgrVM.StatusMsgs = errors.ToArray();
            _otgrVM.StatusType = (short)(errors.Count > 0 ? 100 : 0);
        }

        private ICommand getInRawFromXChCommand;
        public ICommand GetInRawFromXChCommand
        {
            get
            {
                if (getInRawFromXChCommand == null)
                    getInRawFromXChCommand = new DelegateCommand(ExecGetInRawFromXChCommand);
                return getInRawFromXChCommand;
            }
        }

        private void ExecGetInRawFromXChCommand()
        {
            var ndlg = new DateRangeDlgViewModel(true)
            {
                Title = "Укажите период для загрузки",
                OnSubmit = DoSelectNewRawOtgrFromXCh
            };

            Parent.OpenDialog(ndlg);
        }

        private void DoSelectNewRawOtgrFromXCh(Object _d)
        {
            var dlg = _d as DateRangeDlgViewModel;
            if (dlg == null) return;
            Parent.CloseDialog(_d);

            DoSelectNewOtgrFromXCh(dlg.DateFrom, dlg.DateTo, InOtgrTypes.RawMaterials);
        }

        private ICommand getInEmptyFromXChCommand;
        public ICommand GetInEmptyFromXChCommand
        {
            get
            {
                if (getInEmptyFromXChCommand == null)
                    getInEmptyFromXChCommand = new DelegateCommand(ExecGetInEmptyFromXChCommand);
                return getInEmptyFromXChCommand;
            }
        }

        private void ExecGetInEmptyFromXChCommand()
        {
            var ndlg = new DateRangeDlgViewModel(true)
            {
                Title = "Укажите период для загрузки",
                OnSubmit = DoSelectNewInEmptyOtgrFromXCh
            };

            Parent.OpenDialog(ndlg);
        }

        private ICommand getOutEmptyFromXChCommand;
        public ICommand GetOutEmptyFromXChCommand
        {
            get
            {
                if (getOutEmptyFromXChCommand == null)
                    getOutEmptyFromXChCommand = new DelegateCommand(ExecGetOutEmptyFromXChCommand);
                return getOutEmptyFromXChCommand;
            }
        }

        private void ExecGetOutEmptyFromXChCommand()
        {
            var ndlg = new DateRangeDlgViewModel(true)
            {
                Title = "Укажите период для загрузки",
                OnSubmit = DoSelectNewOutEmptyOtgrFromXCh
            };

            Parent.OpenDialog(ndlg);
        }
        
        private void DoSelectNewOutEmptyOtgrFromXCh(Object _d)
        {
            var dlg = _d as DateRangeDlgViewModel;
            if (dlg == null) return;
            Parent.CloseDialog(_d);

            DoSelectNewOtgrFromXCh(dlg.DateFrom, dlg.DateTo, InOtgrTypes.OutEmptyWagons);
        }

        private void DoSelectNewInEmptyOtgrFromXCh(Object _d)
        {
            var dlg = _d as DateRangeDlgViewModel;
            if (dlg == null) return;
            Parent.CloseDialog(_d);

            DoSelectNewOtgrFromXCh(dlg.DateFrom, dlg.DateTo, InOtgrTypes.InEmptyWagons);
        }

        private void DoSelectNewOtgrFromXCh(DateTime _from, DateTime _to, InOtgrTypes _ot)
        {
            OtgrLine[] newotgr = null;

            Action work = () =>
            {
                newotgr = Parent.Repository.GetOtgrFromXChange(_from, _to, _ot);
            };

            Action afterwork = () =>
            {
                if (newotgr == null || newotgr.Length == 0)
                    Parent.Services.ShowMsg("Результат", "За указанный период отгрузка не обнаружена", true);
                else
                    ShowSelectNewDlg(newotgr);
            };

            Parent.Services.DoWaitAction(work, "Подождите", "Выборка информации из обменной базы ЖД", afterwork);
        }

        private void ShowSelectNewDlg(OtgrLine[] _newotgr)
        {
            var ndlg = new SelectOtgrFromExtViewModel(Parent.Repository, _newotgr) 
            {
                Title = "В обменной базе найдена следующая отгрузка",
                OnSubmit = GetNewOtgrFromXch
            };

            Parent.OpenDialog(ndlg);
        }

        private void GetNewOtgrFromXch(Object _d)
        {
            var dlg = _d as SelectOtgrFromExtViewModel;
            if (dlg == null) return;
            Parent.CloseDialog(_d);

            foreach (var o in otgrRows.Where(d => d.IsChecked))
                o.IsChecked = false;       

            foreach(var o in dlg.OtgrData.Where(d => d.IsChecked))
            {
                otgrRows.Add(o);
                CheckOtgrLine(o);
            }

            SelectedOtgr = dlg.OtgrData.Where(d => d.IsChecked).FirstOrDefault();
        }

        private ICommand submitCommand;
        public ICommand SubmitCommand
        {
            get
            {
                if (submitCommand == null)
                    submitCommand = new DelegateCommand(ExecSubmitCommand, CanExecSubmitCommand);
                return submitCommand;
            }
        }

        private bool CanExecSubmitCommand()
        {
            return otgrRows != null && otgrRows.Count > 0 && !otgrRows.Any(r => r.HasErrors);
        }

        private void ExecSubmitCommand()
        {
            var ndlg = new MsgDlgViewModel()
            {
                Title = "Подтверждение",
                Message = "Сохранить введённую отгрузку?",
                OnSubmit = DoSubmitNewOtgr
            };

            Parent.OpenDialog(ndlg);
        }

        private void DoSubmitNewOtgr(Object _d)
        {
            Parent.CloseDialog(_d);
            SubmitOtgruz();
        }

        private bool SubmitSingleOtgr(OtgrLine _ol)
        {
            bool res = true;
            res = Parent.Repository.AddOtgruz(_ol, null);
            return res;
        }

        private void SubmitOtgruz()
        {
            string retmess = null;
            Dictionary<OtgrLineViewModel, bool> res = new Dictionary<OtgrLineViewModel, bool>();

            Action<WaitDlgViewModel> work = (w) =>
            {
                foreach (var or in otgrRows)
                {
                    string curOtgrStr = GetOtgrString(or);
                    w.Message = curOtgrStr;
                    bool sres = SubmitSingleOtgr(or.Otgr);
                    res[or] = sres;
                    retmess += curOtgrStr + (sres ? " : Сохранена\n" : " : Не сохранена\n");
                }
            };

            Action afterwork = () =>
            {
                Parent.OpenDialog(new MsgDlgViewModel()
                {
                    Title = "Результат",
                    Message = retmess,
                    OnSubmit = d =>
                    {
                        Parent.CloseDialog(d);

                        foreach (var kv in res.Where(kv => kv.Value))
                            OtgrRows.Remove(kv.Key);
                        SelectedOtgr = OtgrRows.FirstOrDefault(r => r.IsChecked);
                    }
                });
            };

            Parent.Services.DoWaitAction(work, "Ожидание выполнения", "Сохранение отгрузки...", afterwork);

        }
    }
}