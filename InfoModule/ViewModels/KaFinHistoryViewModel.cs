using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using CommonModule.Commands;
using CommonModule.Interfaces;
using CommonModule.ViewModels;
using DataObjects;
using CommonModule.Helpers;
using CommonModule.DataViewModels;

namespace InfoModule.ViewModels
{
    public class KaFinHistoryViewModel : BasicModuleContent
    {
        private KontrAgent kaRef;
        private Dictionary<PoupModel, PkodModel[]> poups;
        private DateTime date1;
        private DateTime date2;
        private List<Selectable<SfInListViewModel>> kaSfsList; // счета - фактуры
        private Selectable<SfInListViewModel> selectedSf;
        private List<Selectable<PredoplViewModel>> kaPredoplsList; // поступления в отчётном периоде
        private Selectable<PredoplViewModel> selectedPredopl;
        private List<ValOst> inPredoplsOst; // входящие остатки предоплат
        private List<ValOst> inVozvratOst; // входящие остатки возвратов
        private List<ValOst> allPredSumOpl; // суммы финансовых поступлений за период
        private List<ValOst> allSfsSumOtgr; // суммы выписанных счетов за период
        private List<ValOst> outPredoplsOst; // исходящие остатки предоплат
        private List<ValOst> outVozvratOst; // исходящие остатки возвратов
        private List<ValOst> inSfsNeoplOst; // входящие остатки неоплаченных счетов
        private List<ValOst> outSfsNeoplOst; // исходящие остатки неоплаченных счетов
        private List<ValOst> outSaldos; // исходящее сальдо по валютам

        private ISfModule sfModule;
        private ICommand goToSfsCommand;

        private IPredoplModule predoplModule;
        private ICommand goToPredoplsCommand;

        public KaFinHistoryViewModel(IModule _parent, KontrAgent kaRef, Dictionary<PoupModel, PkodModel[]> _poups, DateTime _date1, DateTime _date2)
            : base(_parent)
        {
            this.kaRef = kaRef;
            this.poups = _poups;
            this.date1 = _date1;
            this.date2 = _date2;
            if (Parent != null)
            {
                sfModule = Parent.ShellModel.Container.GetExportedValueOrDefault<ISfModule>();
                predoplModule = Parent.ShellModel.Container.GetExportedValueOrDefault<IPredoplModule>();
                Init();
            }
        }

        private void Init()
        {
            LoadKaSfsAndPredopls();
            LoadOst();
            CalcSaldos();            
            goToSfsCommand = new DelegateCommand(ExecGoToSfs, CanGoToSfs);//() => sfModule != null && kaSfsList.Any(s => s.IsSelected));            
            goToPredoplsCommand = new DelegateCommand(ExecGoToPredopls, () => predoplModule != null && kaPredoplsList.Any(p => p.IsSelected));
        }

        private bool CanGoToSfs()
        {
            bool res = sfModule != null && kaSfsList.Any(s => s.IsSelected);
            return res;
        }

        /// <summary>
        /// Подсчет сальдо
        /// </summary>
        private void CalcSaldos()
        {
            if (outSfsNeoplOst == null || outPredoplsOst == null || outVozvratOst == null) return;
            IEnumerable<string> allVals = outSfsNeoplOst.Select(o => o.KodVal)
                                            .Union(outPredoplsOst.Select(o => o.KodVal))
                                            .Union(kaPredoplsList.Select(o => o.Value.ValPropl.Kodval))
                                            .Union(outVozvratOst.Select(o => o.KodVal));
            outSaldos = new List<ValOst>();
            foreach (string v in allVals)
            {
                decimal saldo = outPredoplsOst.Where(s => s.KodVal == v).Select(s => s.Summa).SingleOrDefault()
                    //+ kaPredoplsList.Where(p => p.Value.PredoplRef.Idpo == 0 && p.Value.PredoplRef.KodVal == v).Sum(p => p.Value.IsVozvrat ? -p.Value.SumPropl : p.Value.SumPropl) // непринятые предоплаты уже в исходящих остатках
                                - outSfsNeoplOst.Where(s => s.KodVal == v).Select(s => s.Summa).SingleOrDefault()
                                - outVozvratOst.Where(s => s.KodVal == v).Select(s => s.Summa).SingleOrDefault();
                outSaldos.Add(new ValOst() { KodVal = v, Summa = saldo });
            }
        }

        private ValOst[] GetSfsNeoplOstOnDate(DateTime _onDate)
        {
            if (kaRef == null || poups == null || poups.Count == 0) return null;
            
            ValOst[] res = null;
            List<ValOst> allPoupOsts = new List<ValOst>();

            foreach (var p in poups)
            {
                var poup = p.Key.Kod;
                var pkods = GetPkods(p.Key);
                
                allPoupOsts.AddRange(Parent.Repository.GetSfsNeoplOstOnDate(kaRef.Kgr, poup, pkods, _onDate));
            }

            res = allPoupOsts.GroupBy(o => o.KodVal)
                             .Select(g => new ValOst { KodVal = g.Key, Kgr = kaRef.Kgr, Summa = g.Sum(gi => gi.Summa) })
                             .OrderBy(r => r.KodVal)
                             .ToArray();
            return res;
        }

        private ValOst[] GetPredoplOstOnDate(DateTime _onDate)
        {
            if (kaRef == null || poups == null || poups.Count == 0) return null;

            ValOst[] res = null;
            List<ValOst> allPoupOsts = new List<ValOst>();

            foreach (var p in poups)
            {
                var poup = p.Key.Kod;
                var pkods = GetPkods(p.Key);

                for (int i = 0; i < pkods.Length; i++)
                    allPoupOsts.AddRange(Parent.Repository.GetPredoplOstOnDate(kaRef.Kgr, poup, pkods[i], _onDate));
            }

            res = allPoupOsts.GroupBy(o => new { o.IsVozvrat, o.KodVal })
                             .Select(g => new ValOst 
                             {
                                 Kgr = kaRef.Kgr,
                                 KodVal = g.Key.KodVal,
                                 IsVozvrat = g.Key.IsVozvrat,
                                 Summa = g.Sum(gi => gi.Summa)
                             })
                            .OrderBy(r => r.KodVal)
                            .ToArray();
            return res;
        }

        /// <summary>
        /// Загрузка остатков
        /// </summary>
        private void LoadOst()
        {
            if (kaRef == null || poups == null) return;
            inPredoplsOst = new List<ValOst>();
            inVozvratOst = new List<ValOst>();
            outPredoplsOst = new List<ValOst>();
            outVozvratOst = new List<ValOst>();
            inSfsNeoplOst = new List<ValOst>();
            outSfsNeoplOst = new List<ValOst>();
            DateTime nextDate = Date2.AddDays(1);

            var ost = GetPredoplOstOnDate(Date1);
            if (ost != null)
                foreach (var co in ost)
                {
                    if (co.IsVozvrat)
                        inVozvratOst.Add(co);
                    else
                        inPredoplsOst.Add(co);
                }

            //ost = GetPredoplOstOnOut();            
            var outpredost = GetPredoplOstOnDate(nextDate).ToList();
            
            // добавляем в исх. остатки непринятые предоплаты
            var neprGroups = kaPredoplsList.Where(p => p.Value.Idpo == 0).Select(p => p.Value)
                                           .GroupBy(i => new {kodval = i.ValPropl.Kodval, isvozvrat = i.IsVozvrat})
                                           .Select(g => new{kodval = g.Key.kodval, isvozvrat = g.Key.isvozvrat, sumost = g.Sum(i => i.SumPropl)});
            foreach (var npr in neprGroups)
            {
                var vo = outpredost.SingleOrDefault(v => v.KodVal == npr.kodval && v.IsVozvrat == npr.isvozvrat);
                if (vo == null)
                    outpredost.Add(new ValOst { IsVozvrat = npr.isvozvrat, KodVal = npr.kodval, Summa = npr.sumost });
                else
                    vo.Summa += npr.sumost;
            }

            if (outpredost != null)
                foreach (var co in outpredost)
                {
                    if (co.IsVozvrat)
                        outVozvratOst.Add(co);
                    else
                        outPredoplsOst.Add(co);
                }

            ost = GetSfsNeoplOstOnDate(Date1);
            if (ost != null)
                foreach (var co in ost)
                    inSfsNeoplOst.Add(co);

            ost = GetSfsNeoplOstOnDate(nextDate);
            //ost = GetSfsNeoplOstOnOut();

            if (ost != null)
                foreach (var co in ost)
                    outSfsNeoplOst.Add(co);
        }

        private string poupsLabel;
        public string PoupsLabel
        {
            get
            {
                if (poupsLabel == null)
                    poupsLabel = MakePoupsLabel();
                return poupsLabel;
            }
        }

        private string MakePoupsLabel()
        {
            var pcount = poups.Keys.Count;
            var pkcount = poups.Values.Where(v => v != null).SelectMany(vi => vi).Count();
            bool ishort = pcount + pkcount > 5;
            var res = String.Join(", ", poups.Select(p => MakeSinglePoupLabel(p.Key, p.Value, ishort)).ToArray());
            return res;
        }

        private string MakeSinglePoupLabel(PoupModel _pm, PkodModel[] _pkds, bool _short)
        {
            string res = String.Format("[{0}] {1}",_pm.Kod, _short ? _pm.ShortName : _pm.Name);
            if (_pkds != null && _pkds.Length > 0)
                res += " (" + String.Join(", ", _pkds.Select(pk => String.Format("{0}-{1}", pk.Pkod, _short ? pk.ShortName : pk.Name)).ToArray()) + ")";
            return res;
        }


        private short[] GetPkods(PoupModel _pm)
        {
            var pkodModels = poups[_pm];
            short[] pkods = null;
            if (pkodModels == null || pkodModels.Length == 0 || pkodModels.Any(pk => pk.Pkod == 0))
                pkods = new short[] { 0 };
            else
                pkods = pkodModels.Select(pkm => pkm.Pkod).ToArray();
            return pkods;
        }

        private void GetAllPoupSfsAndPredopls()
        {
            kaSfsList = new List<Selectable<SfInListViewModel>>();
            kaPredoplsList = new List<Selectable<PredoplViewModel>>();
            
            if (poups == null || poups.Count == 0 || kaRef == null) return;
            var kaCode = kaRef.Kgr;

            foreach (var p in poups)
            {
                var poup = p.Key.Kod;
                var pkods = GetPkods(p.Key);

                var sfs = Parent.Repository.GetKaSfDebtsInPeriod(kaCode, poup, pkods, date1, date2);
                if (sfs != null)
                    foreach (var sf in sfs.OrderBy(s => s.DatUch))
                    {
                        var sfvm = new SfInListViewModel(Parent.Repository, sf);
                        kaSfsList.Add(new Selectable<SfInListViewModel>(sfvm));
                    }

                var prs = Parent.Repository.GetPredoplsByKpok(kaCode, poup, pkods, date1, date2);
                if (prs != null)
                    foreach (var pr in prs)
                    {
                        var prvm = new PredoplViewModel(Parent.Repository, pr);
                        kaPredoplsList.Add(new Selectable<PredoplViewModel>(prvm));
                    }
            }
        }

        /// <summary>
        /// Загрузка счетов
        /// </summary>
        private void LoadKaSfsAndPredopls()
        {
            if (kaRef == null || poups == null || poups.Count == 0) return;

            allPredSumOpl = new List<ValOst>();
            allSfsSumOtgr = new List<ValOst>();

            GetAllPoupSfsAndPredopls();

            var sfinfos = kaSfsList.Select(s => s.Value);

            // группируем по валютам и подсчитываем движение
            var sfvalgrp = sfinfos.GroupBy(i => i.KodVal)
                                .Select(g => new
                                {
                                    KodVal = g.Key,
                                    SumPltr = g.Sum(e => e.SumPltr)
                                    //,SumOpl = g.Sum(e => e.SumOpl)
                                })
                                .OrderBy(r => r.KodVal);
            foreach (var v in sfvalgrp)
            {
                allSfsSumOtgr.Add(new ValOst { KodVal = v.KodVal, Summa = v.SumPltr });
                //allSfsSumOpl.Add(new ValOst { KodVal = v.KodVal, Summa = v.SumOpl });
            }

            var predinfos = kaPredoplsList.Select(s => s.Value);

            // группируем по валютам и подсчитываем движение
            var prvalgrp = predinfos.GroupBy(p => p.PredoplRef.KodVal)
                                    .Select(g => new
                                    {
                                        KodVal = g.Key,
                                        SumPropl = g.Sum(e => e.IsVozvrat ? -e.SumPropl : e.SumPropl)
                                        //,SumOpl = g.Sum(e => e.SumOpl)
                                    })
                                    .OrderBy(r => r.KodVal);
            foreach (var v in prvalgrp)
                allPredSumOpl.Add(new ValOst { KodVal = v.KodVal, Summa = v.SumPropl });
        }

        public KontrAgent KaRef
        {
            get { return kaRef; }
            set { kaRef = value; }
        }

        public Dictionary<PoupModel, PkodModel[]> Poups
        {
            get { return poups; }
            set { poups = value; }
        }

        public DateTime Date1
        {
            get { return date1; }
            set { date1 = value; }
        }

        public DateTime Date2
        {
            get { return date2; }
            set { date2 = value; }
        }


        public List<Selectable<SfInListViewModel>> KaSfsList
        {
            get { return kaSfsList; }
            set
            {
                if (value != kaSfsList)
                {
                    kaSfsList = value;
                    //                    NotifyPropertyChanged("KaSfsList");
                }
            }
        }

        public List<Selectable<PredoplViewModel>> KaPredoplsList
        {
            get { return kaPredoplsList; }
            set
            {
                if (value != kaPredoplsList)
                {
                    kaPredoplsList = value;
                    //                    NotifyPropertyChanged("KaPredoplsList");
                }
            }
        }

        /// <summary>
        /// Непогашенные остатки предоплат на начало периода
        /// </summary>
        public List<ValOst> InPredoplsOst
        {
            get { return inPredoplsOst; }
            set
            {
                if (value != inPredoplsOst)
                {
                    inPredoplsOst = value;
                    //                    NotifyPropertyChanged("InPredoplsOst");
                }
            }
        }

        /// <summary>
        /// Непогашенные остатки возвратов на начало периода
        /// </summary>
        public List<ValOst> InVozvratOst
        {
            get { return inVozvratOst; }
            set
            {
                if (value != inVozvratOst)
                {
                    inVozvratOst = value;
                    //                    NotifyPropertyChanged("InVozvratOst");
                }
            }
        }

        /// <summary>
        /// Непогашенные остатки предоплат на конец периода
        /// </summary>
        public List<ValOst> OutPredoplsOst
        {
            get { return outPredoplsOst; }
            set
            {
                if (value != outPredoplsOst)
                {
                    outPredoplsOst = value;
                    //                    NotifyPropertyChanged("InPredoplsOst");
                }
            }
        }

        /// <summary>
        /// Непогашенные остатки возвратов на конец периода
        /// </summary>
        public List<ValOst> OutVozvratOst
        {
            get { return outVozvratOst; }
            set
            {
                if (value != outVozvratOst)
                {
                    outVozvratOst = value;
                    //                    NotifyPropertyChanged("InVozvratOst");
                }
            }
        }

        /// <summary>
        /// Неоплаченные остатки счетов на начало периода
        /// </summary>
        public List<ValOst> InSfsNeoplOst
        {
            get { return inSfsNeoplOst; }
            set
            {
                if (value != inSfsNeoplOst)
                {
                    inSfsNeoplOst = value;
                    //                    NotifyPropertyChanged("InVozvratOst");
                }
            }
        }

        /// <summary>
        /// Выписано счетов на сумму в валюте за период
        /// </summary>
        public List<ValOst> AllSfsSumOtgr
        {
            get { return allSfsSumOtgr; }
            set
            {
                if (value != allSfsSumOtgr)
                {
                    allSfsSumOtgr = value;
                    //                    NotifyPropertyChanged("InVozvratOst");
                }
            }
        }

        /// <summary>
        /// Оплачено счетов на сумму в валюте за период
        /// </summary>
        public List<ValOst> AllPredSumOpl
        {
            get { return allPredSumOpl; }
            set
            {
                if (value != allPredSumOpl)
                {
                    allPredSumOpl = value;
                    //                    NotifyPropertyChanged("InVozvratOst");
                }
            }
        }

        /// <summary>
        /// Неоплаченные остатки счетов на конец периода
        /// </summary>
        public List<ValOst> OutSfsNeoplOst
        {
            get { return outSfsNeoplOst; }
            set
            {
                if (value != outSfsNeoplOst)
                {
                    outSfsNeoplOst = value;
                    //                    NotifyPropertyChanged("InVozvratOst");
                }
            }
        }

        /// <summary>
        /// Сальдо на конец периода
        /// </summary>
        public List<ValOst> OutSaldos
        {
            get { return outSaldos; }
            set
            {
                if (value != outSaldos)
                {
                    outSaldos = value;
                    //                    NotifyPropertyChanged("InVozvratOst");
                }
            }
        }

        public Selectable<SfInListViewModel> SelectedSf
        {
            get { return selectedSf; }
            set 
            {
                if (value != selectedSf)
                {
                    kaSfsList.ForEach(s => { if (s != value) s.IsSelected = false; });
                    selectedSf = value;
                    NotifyPropertyChanged("SelectedSf");
                    //NotifyPropertyChanged("SelectedSfView");
                }
            }
        }

        //public SfViewModel SelectedSfView
        //{
        //    get 
        //    { 
        //        return isShowSfDetails && selectedSf != null ? selectedSf.Value.View : null; 
        //    }
        //}

        private bool isShowSfDetails;
        public bool IsShowSfDetails
        {
            get { return isShowSfDetails; }
            set 
            { 
                SetAndNotifyProperty("IsShowSfDetails", ref isShowSfDetails, value);
                //NotifyPropertyChanged("SelectedSfView");
            }
        }
        
        private bool isShowPredoplDetails;
        public bool IsShowPredoplDetails
        {
            get { return isShowPredoplDetails; }
            set 
            {
                SetAndNotifyProperty("IsShowPredoplDetails", ref isShowPredoplDetails, value);
                NotifyPropertyChanged("SelectedPredopl");
            }
        }

        public Selectable<PredoplViewModel> SelectedPredopl
        {
            get { return selectedPredopl; }
            set { SetAndNotifyProperty("SelectedPredopl", ref selectedPredopl, value); }
        }

        /// <summary>
        /// Открытие модуля счетов с выбранным счётом
        /// </summary>
        public ICommand GoToSfsCommand { get { return goToSfsCommand; } }

        private void ExecGoToSfs()
        {
            if (sfModule == null) return;
            SfInListInfo[] sfs = null;
            PenaltyModel[] pens = null;
            bool toload = false;

            Action request = () =>
            {
                sfs = kaSfsList.Where(s => s.IsSelected && s.Value.Poup.PayDoc == PayDocTypes.Sf).Select(s => s.Value.SfRef).ToArray();//.Select(s => Parent.Repository.GetSfInListInfo(s.Value.SfRef.IdSf)).ToArray();
                if (sfs != null && sfs.Length > 0)
                {
                    if (date2 < DateTime.Now.Date) // пересчитываем суммы оплаты счетов, если период не конечный
                        for (int i = 0; i < sfs.Length; i++)
                            sfs[i].SumOpl = Parent.Repository.GetSfSumOpl(sfs[i].IdSf);

                    var numsfLstString = String.Join(",", sfs.Select(s => s.NumSf.ToString()).ToArray());
                    sfModule.ListSfs(sfs, "Счета-фактуры : " + numsfLstString);
                    toload = true;
                }
                pens = kaSfsList.Where(s => s.IsSelected && s.Value.Poup.PayDoc == PayDocTypes.Penalty).Select(s => Parent.Repository.GetPenaltyById(s.Value.SfRef.IdSf)).ToArray();//.Select(s => Parent.Repository.GetSfInListInfo(s.Value.SfRef.IdSf)).ToArray();
                if (pens != null && pens.Length > 0)
                {
                    var penLstString = String.Join(",", pens.Select(s => s.Rnpl.ToString()).ToArray());
                    sfModule.ListPenalties(pens, "Штрафные санкции : " + penLstString);
                    toload = true;
                }
            };
            Action after = () =>
            {
                if (toload)
                    Parent.ShellModel.LoadModule(sfModule);
            };

            Parent.Services.DoWaitAction(request, "Подождите", "Запрос обновлённых данных по выбранным счетам", after);
        }

        /// <summary>
        /// Открытие модуля предоплат с выбранными предоплатами
        /// </summary>
        public ICommand GoToPredoplsCommand { get { return goToPredoplsCommand; } }

        private void ExecGoToPredopls()
        {
            if (predoplModule == null) return;
            PredoplModel[] predopls = kaPredoplsList.Where(p => p.IsSelected && p.Value.Idpo > 0).Select(p => Parent.Repository.GetPredoplById(p.Value.Idpo)).ToArray();
            if (predopls != null && predopls.Length > 0)
            {
                var ndokLstString = String.Join(",", predopls.Select(p => p.Ndok.ToString()).ToArray());
                predoplModule.ListPredopls(predopls, "Выбранные предоплаты : " + ndokLstString);
                Parent.ShellModel.LoadModule(predoplModule);
            }
        }
    }
}
