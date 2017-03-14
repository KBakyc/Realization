using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
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
using RwModule.Models;
using DAL;


namespace RwModule.ViewModels
{
    public class PayRwUslByPlatViewModel : BasicModuleContent
    {
        public PayRwUslByPlatViewModel(IModule _parent, DateTime _datzakr, RwUslType _vidusl)
            : base(_parent)
        {
            Title = "Погашение ЖД услуг платежами";
            datZakr = _datzakr;
            vidusl = _vidusl;

            RwPlats = new ObservableCollection<RwPlatViewModel>();
            VozvratsList = new ObservableCollection<RwPlatViewModel>();
            LoadData();
            RefreshCommand = new DelegateCommand(RefreshData);
        }

        private short closeMode = 0;

        /// <summary>
        /// Режим закрытия (погашения) 0 - платежи, 1 - возвраты
        /// </summary>
        public short CloseMode
        {
            get { return closeMode; }
            set
            {
                if (value != closeMode)
                {
                    closeMode = value;
                }
            }
        }

        // Тип погашаемых услуг
        private RwUslType vidusl;
        public RwUslType Vidusl
        {
            get { return vidusl; }
        }

        // дата закрытия платежей
        private DateTime datZakr;
        public DateTime DatZakr
        {
            get
            {
                return datZakr;
            }
        }

        // неоплаченые ЖД перечни
        private ObservableCollection<Selectable<RwListViewModel>> outRwLists = new ObservableCollection<Selectable<RwListViewModel>>();
        public ObservableCollection<Selectable<RwListViewModel>> OutRwLists
        {
            private set
            {
                if (value != outRwLists)
                {
                    outRwLists = value;
                    NotifyPropertyChanged("OutRwLists");
                }
            }
            get
            {
                return outRwLists;
            }
        }      

        /// <summary>
        /// Список платежей
        /// </summary>
        public ObservableCollection<RwPlatViewModel> RwPlats { get; set; }

        private RwPlatViewModel selectedRwPlat;
        public RwPlatViewModel SelectedRwPlat
        {
            get { return selectedRwPlat; }
            set { SetAndNotifyProperty("SelectedRwPlat", ref selectedRwPlat, value); }
        }


        /// <summary>
        /// Непогашенные возвраты
        /// </summary>
        public ObservableCollection<RwPlatViewModel> VozvratsList { get; set; }

        private RwPlatViewModel selectedVozvrat;
        public RwPlatViewModel SelectedVozvrat
        {
            get { return selectedVozvrat; }
            set { SetAndNotifyProperty("SelectedVozvrat", ref selectedVozvrat, value); }
        }

        private Selectable<RwListViewModel>[] GetOutRwListsFromDb()
        {
            RwList[] fromdb = null;
            using (var db = new RealContext())
            {
                try
                {
                    fromdb = db.RwLists.Include(l => l.RwDocs).Where(l => l.Paystatus != PayStatuses.TotallyPayed && l.RwlType == vidusl && l.RwDocs.Any(d => d.Rep_date <= datZakr)).ToArray();
                }
                catch (Exception e)
                {
                    CommonModule.Helpers.WorkFlowHelper.OnCrash(e);
                }
            }
            return fromdb == null ? null 
                                  : fromdb.Select(l => new Selectable<RwListViewModel>(new RwListViewModel(Parent.Repository, l))).Where(vm => vm.Value.Ostatok != 0).ToArray();
        }

        private RwPlat[] GetAllPlats()
        {
            var isnegativeexist = outRwLists.Any(l => l.Value.Ostatok < 0);
            var dateclosed = datZakr.AddMonths(-2);
            using (var db = new RealContext())
            {
                return db.RwPlats.Where(p => (p.Datzakr == null || isnegativeexist && p.Datzakr > dateclosed) && p.Datplat <= datZakr && p.Idusltype == vidusl).ToArray();
            }
        }

        /// <summary>
        /// Загрузка данных
        /// </summary>
        private void LoadData()
        {
            Selectable<RwListViewModel>[] outrwlists = null;
            outrwlists = GetOutRwListsFromDb();

            // выбираем предоплаты и возвраты
            var allplats = GetAllPlats();

            Parent.ShellModel.UpdateUi(() =>
            {
                outRwLists.Clear();
                CurRwList = null;
                if (outrwlists != null)
                    outRwLists.AddRange(outrwlists);

                RwPlats.Clear();
                RwPlats.AddRange(allplats.Where(p => p.Direction == RwPlatDirection.Out).Select(m => new RwPlatViewModel(m)));
                SelectedRwPlat = null;

                VozvratsList.Clear();
                VozvratsList.AddRange(allplats.Where(p => p.Direction == RwPlatDirection.In).Select(m => new RwPlatViewModel(m)));
                SelectedVozvrat = null;

            }, false, false);
        }

        /// <summary>
        /// Выбраный перечень
        /// </summary>
        private Selectable<RwListViewModel> curRwList;
        public Selectable<RwListViewModel> CurRwList
        {
            get { return curRwList; }
            set
            {
                if (value != curRwList)
                {
                    curRwList = value;
                    NotifyPropertyChanged("CurRwList");
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
        }
        private void ExecSubmitSinks()
        {            
            Parent.OpenDialog(new SubmitRwSinksDlgViewModel
            {
                PayActions = payactions.OrderBy(pa => pa.NumPlat).ToList(),
                OnSubmit = DoSubmitSinks
            });
        }

        private void DoSubmitSinks(Object _dlg)
        {
            Parent.CloseDialog(_dlg);

            Parent.Services.DoWaitAction(SubmitSinksAction, "Подождите", "Оплата...");
        }

        private void SubmitSinksAction(WaitDlgViewModel _wd)
        {
            var bh = new RwModule.Helpers.BusinessHelper(Parent.Repository, _wd);
                
            bool res = false;
            
            try
            {
                res = bh.SubmitSinksAction(payactions.ToArray(), datZakr);
            }
            catch (Exception e)
            {
                _wd.Message += "Ошибка";
                Parent.Services.ShowMsg("Ошибка (" + e.GetType().ToString() + ")", e.Message, true);
                CommonModule.Helpers.WorkFlowHelper.OnCrash(e, null, true);
            }

            if (!res)
                Parent.OpenDialog(new SubmitRwSinksDlgViewModel
                {
                    Title = "Ошибка при подтверждении погашений",
                    PayActions = payactions
                });
            else
                Parent.Services.ShowMsg("Результат", "Погашения оплат завершены успешно", false);

            Parent.ShellModel.UpdateUi(RefreshData, true, false);
        }                            

        /// <summary>
        /// Команда списания неоплаченных остатков со счетов
        /// </summary>
        private ICommand closeRwUslRemainsCommand;
        public ICommand CloseRwUslRemainsCommand
        {
            get
            {
                if (closeRwUslRemainsCommand == null)
                    closeRwUslRemainsCommand = new DelegateCommand(ExecCloseRwUslRemains, CanCloseRwUslRemains);
                return closeRwUslRemainsCommand;
            }
        }
        private bool CanCloseRwUslRemains()
        {
            return !IsReadOnly && (closeMode == 0 && outRwLists.Where(sl => sl.IsSelected).All(sl => sl.Value.Ostatok != 0) || closeMode == 1 && CanVozvrat());
        }
        private void ExecCloseRwUslRemains()
        {
            MsgDlgViewModel dlg = new MsgDlgViewModel() { Title = "Внимание" };
            if (CloseMode == 0)
            {
                dlg.Message = "Списать остатки по выбранным перечням?";
                dlg.OnSubmit = DoCloseRwUslRemains;
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

            payactions.Add(new RwPayActionViewModel(RwPayActionType.CloseVozvrat)
            {
                IdDoc = selectedVozvrat.Idrwplat,
                Summa = selectedVozvrat.Ostatok,
                NumPlat = selectedVozvrat.Numplat.ToString()
            });

            selectedVozvrat.Ostatok = 0;
            SelectedVozvrat = null;

        }

        private void DoCloseRwUslRemains(Object _dlg)
        {
            Parent.CloseDialog(_dlg);

            var selrwlists = outRwLists.Where(s => s.IsSelected && s.Value.Ostatok != 0);
            foreach (var l in selrwlists)
            {
                foreach (var d in l.Value.RwDocsCollection.Where(d => d.Ostatok != 0))
                {
                    var pa = new RwPayActionViewModel(Models.RwPayActionType.CloseUsl)
                    {
                        IdRwList = l.Value.Id_rwlist,
                        IdDoc = d.Id_rwdoc,
                        Summa = d.Ostatok,
                        NumDoc = d.Num_doc
                    };

                    d.Sum_opl += pa.Summa;
                    l.Value.Sum_opl += pa.Summa;
                    l.IsSelected = false;
                    payactions.Add(pa);
                }
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
            return !IsReadOnly && SelectedRwPlat != null && (CloseMode == 0 && CanPayRwUsl() || CloseMode == 1 && CanVozvrat()) ;
        }

        private bool CanPayRwUsl()
        {
            return SelectedRwPlat != null;//&& outRwLists.Any(l => l.IsSelected);
        }


        private bool CanVozvrat()
        {
            return selectedVozvrat != null && selectedVozvrat.Ostatok > 0;
        }


        private void ExecSinkCommand()
        {
            switch (CloseMode)
            {
                case 0: 
                    if (outRwLists.Any(l => l.IsSelected)) RwPayByPlat();
                    else Parent.Services.DoWaitAction(TrySelectRwPayByPlat);
                    break;
                case 1: DoRwPlatVozvrat(); break;
            }
        }

        /// <summary>
        /// Выбор перечней с подходящими суммами
        /// </summary>
        private void TrySelectRwPayByPlat()
        {
            var idxs = GetUnpayedRwListInChosenOrder().Select(s => outRwLists.IndexOf(s)).ToArray();
            var combi = DotNetExtensions.GetCombinations(idxs, true, 1, 0);
            foreach (var c in combi)
            {
                var sumost = c.Sum(ci => outRwLists[ci].Value.Ostatok);
                if (selectedRwPlat.Ostatok == sumost)
                {
                    for (int i = 0; i < outRwLists.Count; i++)
                        outRwLists[i].IsSelected = c.Contains(i);
                    return;
                }
            }
        }

        private Selectable<RwListViewModel>[] GetUnpayedRwListInChosenOrder() // выбирает перечни в порядке их отображения на экране
        {
            Selectable<RwListViewModel>[] res = null;
            if (outRwLists != null)
            {
                var view = System.Windows.Data.CollectionViewSource.GetDefaultView(outRwLists) as System.Windows.Data.ListCollectionView;
                if (view != null)
                    res = view.Cast<Selectable<RwListViewModel>>().Where(i => i.Value.Ostatok != 0).Select(i => i).ToArray();
                else
                    res = outRwLists.Where(i => i.Value.Ostatok != 0).Select(o => o).ToArray();
            }
            return res;
        }

        private void DoRwPlatVozvrat()
        {
            var plat = selectedRwPlat;
            var vozvrat = selectedVozvrat;
            var payActionSum = Math.Min(plat.Ostatok, vozvrat.Ostatok);

            payactions.Add(new RwPayActionViewModel(RwPayActionType.DoVozvrat)
            {
                IdRwPlat = plat.Idrwplat,
                IdDoc = vozvrat.Idrwplat,
                Summa = payActionSum,
                NumPlat = plat.Numplat.ToString(),
                NumDoc = vozvrat.Numplat.ToString()
            });

            plat.Ostatok += payActionSum;
            vozvrat.Ostatok -= payActionSum;
            SelectedRwPlat = null;
            SelectedVozvrat = null;
        }

        private IEnumerable<RwListViewModel> GetSelectedRwListsFromView()
        {
            var view = System.Windows.Data.CollectionViewSource.GetDefaultView(outRwLists);
            return view.OfType<Selectable<RwListViewModel>>().Where(l => l.IsSelected).Select(l => l.Value);
        }

        private void RwPayByPlat()
        {
            var selPlat = selectedRwPlat;
            var selUsls = GetSelectedRwListsFromView();//outRwLists.Where(l => l.IsSelected && l.Value.Ostatok != 0).Select(l => l.Value);
            var dlg = new SelectDocsForPayDlgViewModel(selPlat, selUsls.Where(l => l.Ostatok != 0))
            {
                Title = "Выберите документы для погашения",
                OnSubmit = AcceptRwPayUslsByPlat
            };

            Parent.OpenDialog(dlg);
        }       

        private void AcceptRwPayUslsByPlat(Object _dlg)
        {
            var dlg = _dlg as SelectDocsForPayDlgViewModel;
            Parent.CloseDialog(_dlg);
            if (dlg == null) return;

            var plat = selectedRwPlat;
            var selrwls = dlg.RwListsWithDocs.Keys.ToArray();
            var pactions = dlg.GetPayActions();
            for (int i = 0; i < pactions.Length; i++)
            {
                var pa = pactions[i];
                plat.Ostatok -= pa.Summa;                
                var rwl =  selrwls.FirstOrDefault(l => l.Value.RwDocsCollection.Exists(d => d.Id_rwdoc == pa.IdDoc)).Value;
                var rwd = rwl.RwDocsCollection.FirstOrDefault(d => d.Id_rwdoc == pa.IdDoc);
                rwd.Sum_opl += pa.Summa;
                rwl.Sum_opl += pa.Summa;
                payactions.Add(pa);
                //rwListPayActions.Add(pa, rwl);
            }
            outRwLists.Where(sl => sl.IsSelected && sl.Value.Ostatok == 0).ForEach(sl => sl.IsSelected = false);
            if (plat.Ostatok == 0) SelectedRwPlat = null;

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
        
        private List<RwPayActionViewModel> payactions = new List<RwPayActionViewModel>();
        //private Dictionary<RwPayActionViewModel, RwListViewModel> rwListPayActions = new Dictionary<RwPayActionViewModel, RwListViewModel>();
       
        private void RefreshData()
        {
            payactions.Clear();
            Parent.Services.DoWaitAction(LoadData, "Подождите", "Обновление данных");
        }

        /// <summary>
        /// Комманда обновления
        /// </summary>
        public ICommand RefreshCommand { get; set; }

        private ICommand closeRwPlatCommand;

        /// <summary>
        /// Команда списания остатков и закрытия предоплаты
        /// </summary>
        public ICommand СloseRwPlatCommand
        {
            get
            {
                if (closeRwPlatCommand == null)
                    closeRwPlatCommand = new DelegateCommand(ExecСloseRwPlat, CanСloseRwPlat);
                return closeRwPlatCommand;
            }
        }
        private void ExecСloseRwPlat()
        {
            var dlg = new MsgDlgViewModel
            {
                Title = "Внимание",
                Message = "Списать остатки по выбранному платежу?",
                OnSubmit = DoCloseRwPlat
            };
            Parent.OpenDialog(dlg);

        }
        private bool CanСloseRwPlat()
        {
            return selectedRwPlat != null
                   && selectedRwPlat.Datzakr == null
                   && selectedRwPlat.Ostatok > 0;
        }
        private void DoCloseRwPlat(Object _dlg)
        {
            Parent.CloseDialog(_dlg);
           
            var payActionsSum = selectedRwPlat.Ostatok;

            payactions.Add(new RwPayActionViewModel(RwPayActionType.ClosePlat)
            {
                IdRwPlat = selectedRwPlat.Idrwplat,
                Summa = payActionsSum,
                NumPlat = selectedRwPlat.Numplat.ToString()
            });

            selectedRwPlat.Ostatok = 0;
            SelectedRwPlat = null;
        }
    }
}