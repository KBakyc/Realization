using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using CommonModule.Commands;
using CommonModule.Interfaces;
using CommonModule.ViewModels;
using DataObjects;
using CommonModule.Helpers;
using CommonModule.ModuleServices;
using CommonModule.DataViewModels;
using System.Collections.ObjectModel;
using DotNetHelper;


namespace PredoplModule.ViewModels
{
    public class ClosePredoplViewModel : BasicModuleContent
    {
        public ClosePredoplViewModel(IModule _parent, KontrAgent _plat, Valuta _val, PoupModel _poup, DateTime _datzakr, PkodModel _pkod)
            :base(_parent)
        {
            platelschik = _plat;
            predoplVal = _val;
            selectedPoup = _poup;
            selectedPkod = _pkod;
            datZakr = _datzakr;            

            LoadData();
            RefreshCommand = new DelegateCommand(RefreshData);
        }

        private short closeMode = 0;

        /// <summary>
        /// Режим закрытия (погашения) 0 - счета, 1 - возвраты, 2 - штрафные санкции
        /// </summary>
        public short CloseMode
        {
            get { return closeMode; }
            set 
            {
                if (value != closeMode)
                {
                    closeMode = value;
                    //Predopls = closeMode == 0 ? predoplsForSfs : predoplsForVozv;
                    //NotifyPropertyChanged("CloseMode");
                }
            }
        }

        // Плательщик
        private KontrAgent platelschik;
        public KontrAgent Platelschik
        {
            get
            {
                return platelschik;
            }
            //set
            //{
            //    ChangeCurrentKontrAgent(value);
            //}
        }

        private void ChangeCurrentKontrAgent(KontrAgent _newka)
        {
            platelschik = _newka;
            NotifyPropertyChanged("Platelschik");
        }

        // Валюта
        private readonly Valuta predoplVal;
        public Valuta PredoplVal
        {
            get
            {
                return predoplVal;
            }
        }

        // Выбранное направление реализации
        private readonly PoupModel selectedPoup;
        public PoupModel SelectedPoup
        {
            get
            {
                return selectedPoup;
            }
        }

        private readonly PkodModel selectedPkod;
        public PkodModel SelectedPkod
        {
            get
            {
                return selectedPkod;
            }
        }

        // дата закрытия предоплат
        private DateTime datZakr;
        public DateTime DatZakr
        {
            get
            {
                return datZakr;
            }
        }

        // неоплаченые счета
        private ObservableCollection<Selectable<OutSfViewModel>> outstSfs = new ObservableCollection<Selectable<OutSfViewModel>>();
        public ObservableCollection<Selectable<OutSfViewModel>> OutstSfs
        {
            private set
            {
                if (value != outstSfs)
                {
                    outstSfs = value;
                    NotifyPropertyChanged("OutstSfs");
                }
            }
            get
            {
                return outstSfs;
            }
        }

        // штрафные санкции
        private ObservableCollection<Selectable<PenaltyViewModel>> penalties = new ObservableCollection<Selectable<PenaltyViewModel>>();
        public ObservableCollection<Selectable<PenaltyViewModel>> Penalties
        {
            private set
            {
                if (value != penalties)
                {
                    penalties = value;
                    NotifyPropertyChanged("Penalties");
                }
            }
            get
            {
                return penalties;
            }
        }

        /// <summary>
        /// Список предоплат
        /// </summary>
        public PredoplsListViewModel PredoplsList { get; set; }

        /// <summary>
        /// Непогашенные возвраты
        /// </summary>
        public PredoplsListViewModel VozvratsList { get; set; }


        private Selectable<OutSfViewModel>[] GetOutSfsFromDb()
        {
            short pkod = selectedPkod == null ? (short)0 : selectedPkod.Pkod;
            var data = Parent.Repository.GetKaOutstandingSfs(platelschik.Kgr,
                                                                predoplVal.Kodval,
                                                                selectedPoup.Kod,
                                                                pkod,
                                                                datZakr);
            return data.Select(o => new Selectable<OutSfViewModel>(new OutSfViewModel(Parent.Repository, o), false))
                       .OrderBy(s => s.Value.LastDatOpl).ThenBy(s => s.Value.DatGr).ThenBy(s => s.Value.NumSf)
                       .ToArray();     
       
        }

        private Selectable<PenaltyViewModel>[] GetOutPensFromDb()
        {
            var data = Parent.Repository.GetKaOutstandingPens(platelschik.Kgr,
                                                              predoplVal.Kodval,
                                                              selectedPoup.Kod,
                                                              datZakr);
            return data.Select(o => new Selectable<PenaltyViewModel>(new PenaltyViewModel(Parent.Repository, o), false))
                       .OrderBy(s => s.Value.Datkro)
                       .ToArray();
        }

        public PayDocTypes PayDoc { get { return selectedPoup.PayDoc; } }

        /// <summary>
        /// Загрузка данных
        /// </summary>
        private void LoadData()
        {
            //isSyncsOk = null;

            Selectable<OutSfViewModel>[] outsfsdata = null;
            Selectable<PenaltyViewModel>[] outpendata = null;

            if (PayDoc == PayDocTypes.Penalty)
            {
                outpendata = GetOutPensFromDb();
                CloseMode = 2;
            }
            else
                outsfsdata = GetOutSfsFromDb();

            // выбираем предоплаты и возвраты
            var allpredopls = Parent.Repository.GetPredoplsForClose(platelschik.Kgr, 
                                                                    predoplVal.Kodval, 
                                                                    selectedPoup.Kod, 
                                                                    datZakr, 
                                                                    SelectedPkod == null ? default(short) : SelectedPkod.Pkod )                                                                    
                                                                    .ToArray();

            IEnumerable<PredoplModel> pModels = null;

            if (outsfsdata != null && outsfsdata.Any(s => s.Value.SumPltr < 0))
                pModels = allpredopls.Where(p => p.Direction == 0).OrderBy(p => p.DatZakr).ThenByDescending(p => p.DatVvod);
            else
                pModels = allpredopls.Where(p => p.Direction == 0 && p.DatZakr == null).OrderBy(p => p.DatVvod);
            
            Parent.ShellModel.UpdateUi(() =>
            {               
                outstSfs.Clear();
                if (outsfsdata != null)
                {
                    outstSfs.AddRange(outsfsdata);
                    if (CurSf != null)
                        CurSf = OutstSfs.SingleOrDefault(s => s.Value.IdSf == CurSf.Value.IdSf);
                }

                penalties.Clear();
                if (outpendata != null)
                {
                    penalties.AddRange(outpendata);
                    if (curPen != null)
                        curPen = penalties.SingleOrDefault(s => s.Value.PenRef.Id == curPen.Value.PenRef.Id);
                }

                if (PredoplsList == null)
                    PredoplsList = new PredoplsListViewModel(Parent.Repository, pModels);
                else
                    PredoplsList.LoadData(pModels);

                var vModels = allpredopls.Where(p => p.Direction == 1).OrderBy(p => p.DatVvod);

                if (VozvratsList == null)
                    VozvratsList = new PredoplsListViewModel(Parent.Repository, vModels);
                else
                    VozvratsList.LoadData(vModels);
            }, true, false);
        }
       
        /// <summary>
        /// Выбраный счёт
        /// </summary>
        private Selectable<OutSfViewModel> curSf;
        public Selectable<OutSfViewModel> CurSf
        {
            get { return curSf; }
            set
            {
                if (value != curSf)
                {
                    curSf = value;
                    //NotifyPropertyChanged("CurSf");
                }
            }
        }

        /// <summary>
        /// Выбраный штраф
        /// </summary>
        private Selectable<PenaltyViewModel> curPen;
        public Selectable<PenaltyViewModel> CurPen
        {
            get { return curPen; }
            set
            {
                if (value != curPen)
                {
                    curPen = value;
                    //NotifyPropertyChanged("CurSf");
                }
            }
        }

        /// <summary>
        /// Комманда сохранения оплат в базу
        /// </summary>
        private ICommand submitSinksCommand;
        public ICommand SubmitSinksCommand
        {
            get
            {
                if (submitSinksCommand == null)
                    submitSinksCommand = new DelegateCommand(ExecSubmitSinks, CanSubmitSinks);
                return submitSinksCommand;
            }
        }
        private bool CanSubmitSinks()
        {
            return payactions.Any();
                //&& IsSyncsOk;
        }
        private void ExecSubmitSinks()
        {
            //if (!IsSyncsOk)
            //    ShowSyncErrorAndAskForTry("Предыдущая операция синхронизации оплаты завершена с ошибкой.");
            //else
                Parent.OpenDialog(new SubmitSinksDlgViewModel
                {
                    PayActions = payactions.OrderBy(pa => pa.DatDoc)
                                           .ThenBy(pa => pa.IdPo)
                                           .ThenBy(pa => pa.Idsf).ToList(),
                    OnSubmit = DoSubmitSinks
                });
        }

        private void DoSubmitSinks(Object _dlg)
        {
            Parent.CloseDialog(_dlg);

            Parent.Services.DoWaitAction(SubmitSinksAction, "Подождите", "Оплата...");//, RepeatableSyncToDbfAction);
        }

        private void SubmitSinksAction(WaitDlgViewModel _wd)
        {
            bool totalres = true;
            bool res = true;
            var pacopy = payactions.ToArray();// копируем, коллекцию для возможности её изменения при перечислении
            List<int> idsfs = new List<int>();
            List<int> idpos = new List<int>();
            for (int i = 0; i < pacopy.Length; i++)
            {
                var pa = pacopy[i];
                int idpo = 0;
                switch(pa.PayActionType)
                {
                    case PayActionTypes.Sf:
                        _wd.Message = String.Format("Оплата {3}/{4}: \nПредоплата № {0} Счёт № {1}  Сумма {2:#,##} ...", pa.Ndoc, pa.Numsf, pa.SumOpl, i + 1, pacopy.Length);
                        res = SfPayByPredoplAction(pa);
                        int idsf = pa.Idsf;
                        if (res && idsf > 0 && !idsfs.Contains(idsf))
                            idsfs.Add(idsf);
                        idpo = pa.IdPo;
                        if (res && idpo > 0 && idsf > 0 && !idpos.Contains(idpo))
                            idpos.Add(idpo);
                        break;
                    case PayActionTypes.Vozvrat:
                        res = Parent.Repository.DoPredoplVozvrat(pa.IdPo, pa.Idsf, DatZakr);
                        if (res && payactions.Contains(pa))
                            payactions.Remove(pa);
                        break;
                    case PayActionTypes.Penalty:
                        _wd.Message = String.Format("Оплата {3}/{4}: \nПредоплата № {0} Претензия № {1}  Сумма {2:#,##} ...", pa.Ndoc, pa.Numsf, pa.SumOpl, i + 1, pacopy.Length);
                        res = PenaltyPayByPredoplAction(pa);
                        idpo = pa.IdPo;
                        if (res && idpo > 0 && !idpos.Contains(idpo))
                            idpos.Add(idpo);
                        break;
                    default:
                        res = false;
                        break;
                }

                _wd.Message += (res ? "Ok" : "Ошибка");
                totalres &= res;
            }

            if (totalres)
                KaDebts = KaDebts.Where(d => d.Platelschik.Kgr != Platelschik.Kgr).ToArray();

            var actiontime = DateTime.Now;

            for (int i = 0; i < idsfs.Count; i++)
            {
                Parent.Repository.SetSfCurPayStatus(idsfs[i], PayActions.Payment, actiontime);
            }

            for (int i = 0; i < idpos.Count; i++)
            {
                Parent.Repository.SetPredolpStatus(idpos[i], PredoplStatuses.Payed, actiontime);
            }

            if (payactions.Count > 0)
                Parent.OpenDialog(new SubmitSinksDlgViewModel
                {
                    Title = "Ошибка при подтверждении следующих оплат",
                    PayActions = payactions
                });

            Parent.ShellModel.UpdateUi(RefreshData, true, false);
        }
        
        /// <summary>
        /// Команда списания неоплаченных остатков со счетов
        /// </summary>
        private ICommand closeSfPayRemainsCommand;
        public ICommand CloseSfPayRemainsCommand
        {
            get
            {
                if (closeSfPayRemainsCommand == null)
                    closeSfPayRemainsCommand = new DelegateCommand(ExecCloseSfPayRemains, CanCloseSfPayRemains);
                return closeSfPayRemainsCommand;
            }
        }
        private bool CanCloseSfPayRemains()
        {
            return !IsReadOnly && (CanPaySfs() || CanVozvrat());
        }
        private void ExecCloseSfPayRemains()
        {            
            MsgDlgViewModel dlg = new MsgDlgViewModel() { Title = "Внимание" };
            if (CloseMode == 0)
            {
                dlg.Message = "Списать остатки по выбранным счетам?";
                dlg.OnSubmit = DoCloseSfPayRemains;
            }
            else
            {
                dlg.Message = "Списать остатки по возврату?";
                dlg.OnSubmit = DoCloseVozvratRemains;
            }

            Parent.OpenDialog(dlg);
        }

        private void DoCloseVozvratRemains(Object _dlg)
        {
            Parent.CloseDialog(_dlg);

            var vozv = VozvratsList.SelectedPredopl;
            var payActionsSum = vozv.Ostatok;

            payactions.Add(new PayAction
            {
                PayActionType = PayActionTypes.Vozvrat,
                IdPo = 0,
                Ndoc = 0,
                DatDoc = DatZakr,
                Idsf = vozv.Idpo,
                Numsf = vozv.NomDok,
                DatPltr = vozv.DatDok,
                IdPrilsf = 0,
                PayGroupId = 0,
                SumOpl = payActionsSum,
                Whatfor = "Списание остатков по возврату"
            });

            vozv.SumOtgr += payActionsSum;
            VozvratsList.SelectedPredopl = null;

        }

        private void DoCloseSfPayRemains(Object _dlg)
        {
            Parent.CloseDialog(_dlg);

            var selsfs = OutstSfs.Where(s => s.IsSelected);
            foreach (var sf in selsfs)
            {
                int idsf = sf.Value.IdSf;
                //var sfost = sf.Value.SumPltr - sf.Value.SumOpl + sf.Value.SumKor;
                var sfost = sf.Value.SumPltr - sf.Value.SumOpl;

                var sfprost = Parent.Repository.GetSfProducts(idsf).Select(p => new ProductOstViewModel(Parent.Repository, p)).Where(po => po.SumOst != 0);
                foreach (var po in sfprost)
                {
                    if (payactions != null && payactions.Count > 0)
                    {
                        var curPayActionsSum = payactions.Where(pa => pa.PayActionType == PayActionTypes.Sf && pa.IdPrilsf == po.IdPrilSf)
                                                        .Sum(pa => pa.SumOpl);
                        po.SumOst -= curPayActionsSum;
                    }
                }

                var sfprils = sfprost.Where(po => po.SumOst != 0).ToArray();
                
                var totalPrilsOst = sfprils.Sum(p => p.SumOst);
                if (totalPrilsOst != sfost)
                {
                    var emsg = String.Format("Несоответствие остатка по счёту с учётом корректировок с суммой остатков по приложениям.\nОстаток по счёту №{0} = {1}\nСумма остатков по приложениям: {2}", sf.Value.NumSf, sfost, totalPrilsOst);
                    Parent.Services.ShowMsg("Ошибка",emsg, true);
                    return;
                }


                for (int i = 0; i < sfprils.Length; i++ )
                    payactions.Add(new PayAction
                    {
                        PayActionType = PayActionTypes.Sf,
                        IdPo = 0,
                        Ndoc = 0,
                        DatDoc = datZakr,
                        Idsf = idsf,
                        Numsf = sf.Value.NumSf,
                        DatPltr = sf.Value.DatPltr ?? sf.Value.DatGr,
                        IdPrilsf = sfprils[i].IdPrilSf,
                        PayGroupId = 0,
                        SumOpl = sfprils[i].SumOst,
                        Whatfor = "Списание остатков по счёту"
                    });

                sf.Value.SumOpl += sfost;
                
                sf.IsSelected = false;
            }
               
        }

        /// <summary>
        /// Комманда погашения предоплаты на счёт (оплата счёта)
        /// </summary>
        private ICommand sinkDebtCommand;
        public ICommand SinkDebtCommand
        {
            get
            {
                if (sinkDebtCommand == null)
                    sinkDebtCommand = new DelegateCommand(ExecSinkCommand, CanExecSinkCommand);
                return sinkDebtCommand;
            }
        }

        private bool CanExecSinkCommand()
        {
            return  !IsReadOnly && ( CanPay() || CanVozvrat() && PredoplsList.SelectedPredopl != null && PredoplsList.SelectedPredopl.Ostatok > 0 );
        }

        private bool CanPay()
        {
            return (CanPaySfs() || CanPayPens())
                //&& PredoplsList.SelectedPredopl != null && PredoplsList.SelectedPredopl.Ostatok != 0;
                && PredoplsList.SelectedPredopl != null && (closeMode != 0 && PredoplsList.SelectedPredopl.Ostatok != 0 || 
                                                            outstSfs != null && (outstSfs.Where(s => s.IsSelected).All(s => s.Value.SumPltr > 0) && PredoplsList.SelectedPredopl.Ostatok != 0 || 
                                                            outstSfs.Where(s => s.IsSelected).All(s => s.Value.SumPltr < 0) && PredoplsList.SelectedPredopl.SumOtgr > 0));
        }

        private bool CanPaySfs()
        {
            if (OutstSfs == null || CloseMode != 0) return false;

            var selsfs = OutstSfs.Where(s => s.IsSelected);
            //return selsfs.Any() && selsfs.All(s => s.Value.SumPltr > s.Value.SumOpl);
            return selsfs.Any() && selsfs.All(s => s.Value.SumPltr > 0 && s.Value.SumPltr > s.Value.SumOpl || s.Value.SumPltr < 0 && s.Value.SumPltr < s.Value.SumOpl && s.Value.SumOpl <= 0);
        }

        private bool CanPayPens()
        {
            if (penalties == null || CloseMode != 2) return false;

            var selpens = penalties.Where(s => s.IsSelected);
            return selpens.Any()  && selpens.All(s => s.Value.SumOst > 0);
        }


        private bool CanVozvrat()
        {
            return CloseMode == 1 && VozvratsList.SelectedPredopl != null;
        }


        private void ExecSinkCommand()
        {
            switch (CloseMode)
            {
                case 0: SfPayByPredopl(); break;
                case 1: DoPredoplVozvrat(); break;
                case 2: PenPayByPredopl(); break;
            }
        }

        private void DoPredoplVozvrat()
        {
            var predopl = PredoplsList.SelectedPredopl;
            var vozvrat = VozvratsList.SelectedPredopl;
            var payActionSum = Math.Min(predopl.Ostatok, vozvrat.Ostatok);

            if (predopl.Ostatok < payActionSum)
            {
                Parent.Services.ShowMsg("Ошибка", "Остаток предоплаты меньше возвращаемой суммы.", true);
                return;
            }

            payactions.Add(new PayAction
            {
                PayActionType = PayActionTypes.Vozvrat,
                IdPo = predopl.Idpo,
                Ndoc = predopl.NomDok,
                DatDoc = predopl.DatDok,
                Idsf = vozvrat.Idpo,
                Numsf = vozvrat.NomDok,
                DatPltr = vozvrat.DatDok,
                IdPrilsf = 0,
                PayGroupId = 0,
                SumOpl = payActionSum,
                Whatfor = "Возврат предоплаты"
            });

            predopl.SumOtgr += payActionSum;
            vozvrat.SumOtgr += payActionSum;
            PredoplsList.SelectedPredopl = null;
            VozvratsList.SelectedPredopl = null;
        }

        private void PenPayByPredopl()
        {
            var selPred = PredoplsList.SelectedPredopl;
            var availSum = selPred.Ostatok;            
            var sDlg = new NumDlgViewModel()
            {
                Title = String.Format("Погашение предоплаты №{0}.\nОстаток: {1:N2}", selPred.NomDok, availSum),
                Width = 250,
                IsSelectAll = true,
                Number = availSum,
                Label = "Сумма погашения",
                OnSubmit = (d) => 
                {
                    var dlg = d as NumDlgViewModel;
                    var sum = dlg.Number;
                    Parent.CloseDialog(d);
                    if (sum > availSum)
                        Parent.Services.ShowMsg("Ошибка", "Введённая сумма погашения больше остатка предоплаты!", true);
                    else
                        DoPenPayByPredopl(sum);
                }
            };

            Parent.OpenDialog(sDlg);
        }

        private void DoPenPayByPredopl(decimal _sumopl)
        {
            var predopl = PredoplsList.SelectedPredopl;
            var selpens = penalties.Where(p => p.IsSelected);

            decimal totalSumOpl = 0;

            foreach (var scpen in selpens)
            {
                var cpen = scpen.Value;
                var payActionSum = Math.Min(_sumopl, cpen.SumOst);

                payactions.Add(new PayAction
                {
                    PayActionType = PayActionTypes.Penalty,
                    IdPo = predopl.Idpo,
                    Ndoc = predopl.NomDok,
                    DatDoc = predopl.DatDok,
                    Idsf = cpen.PenRef.Id,
                    Numsf = cpen.PenRef.Rnpl,
                    DatPltr = cpen.PenRef.Datgr,
                    IdPrilsf = cpen.PenRef.Id,
                    SumOpl = payActionSum,
                    Whatfor = "Оплата штрафной санкции"
                });

                cpen.SumOpl += payActionSum;
                _sumopl -= payActionSum;
                totalSumOpl += payActionSum;

                //if (cpen.IsClosed)
                scpen.IsSelected = false;

                if (_sumopl <= 0) break;
            }

            predopl.SumOtgr += totalSumOpl;
            PredoplsList.SelectedPredopl = null;
        }

        private void ReportError()
        {
            Parent.OpenDialog(new MsgDlgViewModel()
            {
                Title = "Ошибка",
                Message = "Операция завершена неудачно.",
                OnSubmit = (md) =>
                    { 
                        Parent.CloseDialog(md); 
                        LoadData(); 
                    }
            });
        }

        private int[] GetSelectedIdSfsInChosenOrder() // выбирает счета в порядке их отображения на экране
        {
            int[] res = null;
            if (OutstSfs != null)
            {
                var view = System.Windows.Data.CollectionViewSource.GetDefaultView(OutstSfs) as System.Windows.Data.ListCollectionView;
                if (view != null)
                    res = view.Cast<Selectable<OutSfViewModel>>().Where(i => i.IsSelected).Select(i => i.Value.IdSf).ToArray();
                else
                    res = OutstSfs.Where(o => o.IsSelected).Select(o => o.Value.IdSf).ToArray();
            }
            return res;
        }

        private void SfPayByPredopl()
        {
            var selIdsfs = GetSelectedIdSfsInChosenOrder();
            if (selIdsfs == null || selIdsfs.Length == 0) return;

            Parent.OpenDialog(new SelectPayGroupForPayDlgViewModel(Parent.Repository, PredoplsList.SelectedPredopl, selIdsfs, payactions)
            {
                Title = "Погашение предоплаты",
                OnSubmit = DoSfPayByPredopl
            });
        }

        private void DoSfPayByPredopl(Object _dlg)
        {
            Parent.CloseDialog(_dlg);
            var dlg = _dlg as SelectPayGroupForPayDlgViewModel;
            if (dlg == null) return;

            AddPayAction(dlg);
        }

        private void AddPayAction(SelectPayGroupForPayDlgViewModel _dlg)
        {
            Dictionary<ProductOstViewModel,decimal> sfprilpays = null;
            var totalsumopl = _dlg.SumOpl;
            var predopl = PredoplsList.SelectedPredopl;
            var paygroupid = _dlg.SelectedMode.PayGroupId;
            var paytype = _dlg.SelectedMode.PayType;
            var selectedProductOsts = _dlg.SelectedProductOsts;//.Where(po => po.SumOst > 0);
            var selectedIdprilsfs = selectedProductOsts.Select(po => po.IdPrilSf);

            if (payactions.Exists(pa => pa.IdPo == predopl.Idpo
                                     && (pa.PayGroupId == paygroupid || pa.PayGroupId == 0)
                                     && (pa.PayType == paytype || pa.PayType == 0)
                                     && selectedIdprilsfs.Any(pr => pr == pa.IdPrilsf)))
            {
                Parent.Services.ShowMsg("Ошибка выбора объектов оплаты", "Подобная оплата уже запланирована", true);
                return;
            }

            if (paytype != 0)
                sfprilpays = selectedProductOsts.ToDictionary(p => p,
                                                   p => p.PayOsts.Where(po => po.PayType == paytype)
                                                                       .Sum(po => po.Summa));
            else
                if (paygroupid != 0)
                    sfprilpays = selectedProductOsts.ToDictionary(p => p,
                                                       p => p.PayOsts.Where(po => po.PayGroupId == paygroupid)
                                                                           .Sum(po => po.Summa));
                else
                    sfprilpays = selectedProductOsts.ToDictionary(p => p,
                                                       p => p.PayOsts.Sum(po => po.Summa));
            
            decimal sumoplost = totalsumopl;
            foreach (var sfp in sfprilpays.Where(p => p.Value != 0))
            {
                int idsf = sfp.Key.SfProduct.IdSf;
                var sf = OutstSfs.SingleOrDefault(s => s.Value.IdSf == idsf);
                var sfost = sf.Value.SumPltr - sf.Value.SumOpl;
                decimal realmaxpay = 0;
                decimal realpay = 0;
                if (sfost < 0 && sumoplost < 0) // отрицательное погашение
                {
                    realmaxpay = sumoplost > sfost ? sumoplost : sfost;
                    realpay = sfp.Value > realmaxpay ? sfp.Value : realmaxpay;
                }
                else
                {
                    realmaxpay = sumoplost > sfost ? sfost : sumoplost;
                    realpay = sfp.Value > realmaxpay ? realmaxpay : sfp.Value;
                }
                sumoplost -= realpay;
                sf.Value.SumOpl += realpay;

                payactions.Add(new PayAction
                {
                    PayActionType = PayActionTypes.Sf,
                    IdPo = predopl.Idpo,
                    Ndoc = predopl.NomDok,
                    DatDoc = predopl.DatDok,
                    Idsf = idsf,
                    Numsf = sf.Value.NumSf,
                    DatPltr = sf.Value.DatPltr ?? sf.Value.DatGr,
                    IdPrilsf = sfp.Key.IdPrilSf,
                    PayGroupId = paygroupid,
                    PayType = paytype,
                    SumOpl = realpay,
                    Whatfor = paygroupid == 0 ? "Оплата за всё по счёту" 
                                              : paygroupid == 1 ? sfp.Key.ProductName + (paytype == 1 || paytype == 0 ? "" : " (" + _dlg.SelectedMode.PayName + ")")
                                                                : _dlg.SelectedMode.PayName
                });

                if (sumoplost == 0)
                    break;
            }
            
            predopl.SumOtgr += totalsumopl;
            if (predopl.Ostatok == 0)
                PredoplsList.SelectedPredopl = null;
            foreach(var sf in outstSfs.Where(os => os.IsSelected))
                sf.IsSelected = false;
        }

        private List<PayAction> payactions = new List<PayAction>();

        private bool SfPayByPredoplAction(PayAction _pa)
        {
            bool result = false;            
            result = Parent.Repository.SfPayByPredopl(_pa.IdPo, _pa.IdPrilsf, _pa.PayGroupId, _pa.PayType, datZakr, _pa.SumOpl);
            if (result && payactions.Contains(_pa))
                payactions.Remove(_pa);
            return result;
        }
        
        private bool PenaltyPayByPredoplAction(PayAction _pa)
        {
            bool result = false;            
            result = Parent.Repository.PenaltyPayByPredopl(_pa.IdPo, _pa.IdPrilsf, datZakr, _pa.SumOpl);
            if (result && payactions.Contains(_pa))
                payactions.Remove(_pa);
            return result;
        }
        private void RefreshData()
        {
            payactions.Clear();
            Parent.Services.DoWaitAction(LoadData, "Подождите", "Обновление данных");
        }

        private KaTotalDebtViewModel[] kaDebts;
        public KaTotalDebtViewModel[] KaDebts 
        { 
            get
            {
                if (kaDebts == null)
                {
                    short pkod = selectedPkod == null ? (short)0 : selectedPkod.Pkod;
                    KaDebts = Parent.Repository.GetTotalDebts(predoplVal.Kodval, selectedPoup.Kod, pkod, datZakr)
                              .Select(d => new KaTotalDebtViewModel(Parent.Repository, d)).ToArray();
                }
                return kaDebts;
            }
            set
            {
                kaDebts = value;
            }
        }

        private ICommand changeKontrAgentCommand;

        public ICommand ChangeKontrAgentCommand
        {
            get
            {
                if (changeKontrAgentCommand == null)
                    changeKontrAgentCommand = new DelegateCommand(ExecChangeKontrAgent);
                return changeKontrAgentCommand;
            }
        }
        private void ExecChangeKontrAgent()
        {
            Action work = () =>
            {
                var OtherKaDebts = KaDebts
                    .Where(d => d.Platelschik.Kgr != Platelschik.Kgr);

                var nDlg = new SelKaWithDebtsDlgViewModel(OtherKaDebts)
                {
                    Title = "Выбор должника для погашения",
                    SelVal = predoplVal,
                    SelDate = datZakr,
                    SelPoup = selectedPoup,
                    SelPkod = selectedPkod,
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

            ChangeCurrentKontrAgent(dlg.SelectedVm.Platelschik);
            RefreshData();
        }


        /// <summary>
        /// Комманда обновления
        /// </summary>
        public ICommand RefreshCommand { get; set; }

        private ICommand closePredoplCommand;

        /// <summary>
        /// Команда списания остатков и закрытия предоплаты
        /// </summary>
        public ICommand СlosePredoplCommand
        {
            get
            {
                if (closePredoplCommand == null)
                    closePredoplCommand = new DelegateCommand(ExecСlosePredopl, CanСlosePredopl);
                return closePredoplCommand;
            }
        }
        private void ExecСlosePredopl()
        {
            var dlg = new MsgDlgViewModel
            {
                Title = "Внимание",
                Message = "Списать остатки по выбранной предоплате?",
                OnSubmit = DoClosePredopl
            };
            Parent.OpenDialog(dlg);

        }
        private bool CanСlosePredopl()
        {
            return PredoplsList != null && PredoplsList.SelectedPredopl != null
                   && PredoplsList.SelectedPredopl.PredoplRef.DatZakr == null
                   && PredoplsList.SelectedPredopl.Ostatok > 0
                   && (OutstSfs == null || OutstSfs.Count == 0 || OutstSfs.All(s => !s.IsSelected))
                   && (penalties == null || penalties.Count == 0 || penalties.All(s => !s.IsSelected));
        }
        private void DoClosePredopl(Object _dlg)
        {
            Parent.CloseDialog(_dlg);

            var predopl = PredoplsList.SelectedPredopl;
            var payActionsSum = predopl.Ostatok;

            payactions.Add(new PayAction
                    {
                        PayActionType = PayActionTypes.Sf,
                        IdPo = predopl.Idpo,
                        Ndoc = predopl.NomDok,
                        DatDoc = predopl.DatDok,
                        Idsf = 0,
                        Numsf = 0,
                        DatPltr = DatZakr,
                        IdPrilsf = 0,
                        PayGroupId = 0,
                        SumOpl = payActionsSum,
                        Whatfor = "Списание остатков по предоплате"
                    });

            predopl.SumOtgr += payActionsSum;
            PredoplsList.SelectedPredopl = null;
        }
    }
}