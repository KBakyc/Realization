using System;
using CommonModule.ViewModels;
using DAL;
using DataObjects;
using DataObjects.Interfaces;
using DataObjects.Helpers;
using System.Threading;

//using Realization.ViewModels;

namespace CommonModule.DataViewModels
{
    /// <summary>
    /// Модель отображения расширенных данных счёта-фактуры в таблицах.
    /// </summary>
    public class SfInListViewModel : BasicViewModel
    {
        private IDbService repository;
        
        public SfInListViewModel(IDbService _rep, int _idsf)
        {
            repository = _rep;
            sfRef = repository.GetSfInListInfo(_idsf);
            //CalcSumOpl();
        }

        public SfInListViewModel(IDbService _rep, SfInListInfo _sfRef)
        {
            repository = _rep;
            sfRef = _sfRef;
            //CalcSumOpl();
        }

        private SfInListInfo sfRef;
        public SfInListInfo SfRef
        {
            get { return sfRef; }
        }
        
        public PoupModel Poup 
        {
            get { return sfRef.Poup; }
        }

        /// <summary>
        /// Текущий(последний) статус счёта
        /// </summary>
        public LifetimeStatuses SfStatus 
        {
            get { return sfRef.Status; }
        }

        public PayStatuses PayStatus
        {
            get { return sfRef.PayStatus; }
        }


        public int NumSf
        {
            get { return sfRef.NumSf;  }
        }

        public SfTypeInfo SfType
        {
            get { return repository.GetSfTypeInfo(SfRef.SfType); } //SfTypeInfo.Get(SfRef.SfType); }
        }

        public bool IsESFN
        {
            get { return sfRef.IsESFN; }
            set 
            {
                sfRef.IsESFN = value;
                NotifyPropertyChanged("IsESFN");
            }
        }

        //private EsfnData[] esfn;
        //public EsfnData[] Esfn
        //{
        //    get 
        //    { 
        //        if ((IsESFN && esfn == null)
        //            && sfRef !=null && sfRef.IdSf > 0)
        //            esfn = repository.Get_ESFN(sfRef.IdSf);
        //        return esfn; 
        //    }
        //    set
        //    {
        //        SetAndNotifyProperty("Esfn", ref esfn, value);
        //    }
        //}

        public DateTime DatePltr
        {
            get { return SfRef.DatPltr; }
        }

        public DateTime DatUch
        {
            get { return SfRef.DatUch; }
        }

        private DateRange dateGr;
        public DateRange DateGr //дата отгрузки
        {
            get 
            {
                if (dateGr == null)
                    dateGr = repository.GetSfDateGrRange(SfRef.IdSf);
                return dateGr; 
            }
        }

        public string KodVal
        {
            get { return SfRef.KodVal ?? ""; }
        }

        private Valuta valuta;
        public Valuta Valuta
        {
            get 
            {
                if (valuta == null)
                {
                    valuta = repository.GetValutaByKod(String.IsNullOrEmpty(KodVal) ? "RB" : KodVal);
                }
                return valuta;
            }
        }

        // Плательщик
        private KontrAgent platelschik;
        public KontrAgent Platelschik
        {
            get
            {
                if (platelschik == null)
                {
                    platelschik = repository.GetKontrAgent(SfRef.Kpok);
                }
                return platelschik;
            }
            //set { platelschik = value; }
        }

        // Грузополучатель
        private KontrAgent poluchatel;
        public KontrAgent Poluchatel
        {
            get
            {
                if (poluchatel == null)
                {
                    int l_kpol = (int) SfRef.Kgr;
                    if (l_kpol == 0 || l_kpol == SfRef.Kpok)
                        poluchatel = Platelschik;
                    else
                        poluchatel = repository.GetKontrAgent(SfRef.Kgr);
                }
                return poluchatel;
            }
            //set { poluchatel = value; }
        }

        /// <summary>
        /// Короткое название способа транспортировки
        /// </summary>
        public string TranspName
        {
            get 
            {
                return SfRef.TrShortName;
            }
        }

        public decimal SumPltr
        {
            get { return SfRef.SumPltr; }
        }

        public decimal SumOpl
        {
            get { return SfRef.SumOpl; }
        }    

        public decimal SumOst
        {
            get { return SfRef.SumPltr == 0 ? 0 : SfRef.SumPltr - SfRef.SumOpl; }
        }

        /// <summary>
        /// Признак полной оплаты
        /// </summary>
        public bool IsClosed 
        {
            get { return SumPltr == SumOpl; }
        }

        public bool IsViewLoaded
        {
            get { return view != null; }
        }

        private PoupViewModel poupVmRef;
        public PoupViewModel PoupVmRef
        {
            get
            {
                if (poupVmRef == null && Poup.Kod != 0)
                    poupVmRef = new PoupViewModel(repository, Poup.Kod, SfRef.Pkod);
                return poupVmRef;
            }
        }

        private int isViewLoading = 0;

        /// <summary>
        /// Ленивая загрузка данных о счёте
        /// </summary>
        private SfViewModel view;
        public SfViewModel View
        {
            get
            {
                if (!IsViewLoaded && Interlocked.CompareExchange(ref isViewLoading, 1, 0) == 0)
                {
                    if (view == null)
                        System.Threading.Tasks.Task.Factory.StartNew(() => LoadViewModel(false));
                    else
                        Interlocked.CompareExchange(ref isViewLoading, 0, 1);
                }
                return view;
            }
            set
            {
                SetAndNotifyProperty("View", ref view, value);                
            }
        }

        private ReportModel[] sfReports;
        public ReportModel[] SfReports
        {
            get
            {
                if (sfReports == null)
                    sfReports = repository.GetSfReports(SfRef.IdSf);
                return sfReports;
            }
        }

        public void LoadViewModel(bool _lazy)
        {
            try
            {
                Interlocked.CompareExchange(ref isViewLoading, 1, 0);
                if (Poup.PayDoc == PayDocTypes.Sf)
                    View = new SfViewModel(repository, this, _lazy);
            }
            catch
            {
                View = null;
            }
            finally
            {
                Interlocked.CompareExchange(ref isViewLoading, 0, 1);
            }                
        }

        public SfPayPeriodModel Period
        {
            get
            {
                return Poup.PayDoc == PayDocTypes.Sf ? View.ActualSfPeriod : null;
            }
        }

        private bool isSelected;
        public bool IsSelected
        {
            get { return isSelected; }
            set 
            {
                if (value != isSelected)
                    isSelected = value;
                NotifyPropertyChanged("IsSelected");
            }
        }


    }
}