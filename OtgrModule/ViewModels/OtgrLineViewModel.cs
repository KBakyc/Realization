using System;
using System.Linq;
using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Interfaces;
using CommonModule.DataViewModels;
using System.Collections.Generic;

namespace OtgrModule.ViewModels
{
    /// <summary>
    /// Модель отображения отгрузки.
    /// </summary>
    public class OtgrLineViewModel : BasicViewModel, ITrackable
    {
        private IDbService repository;

        private bool isFindSfs;

        public OtgrLineViewModel(IDbService _rep, OtgrLine otgr)
            :this(_rep, otgr, true)
        {
        }

        public OtgrLineViewModel(IDbService _rep, OtgrLine otgr, bool _isFindSfs)
        {
            this.Otgr = otgr;
            repository = _rep;
            isFindSfs = _isFindSfs;
        }

        public OtgrLine Otgr { get; set; }

        private InvoiceType docInvoiceType;
        public InvoiceType DocInvoiceType
        {
            get 
            {
                if (docInvoiceType == null && Otgr.IdInvoiceType.HasValue)
                    docInvoiceType = repository.GetInvoiceType(Otgr.IdInvoiceType.Value);
                return docInvoiceType; 
            }
        }

        public string DocName { get { return DocInvoiceType == null ? "док." : docInvoiceType.Notation; } }

        /// <summary>
        /// Номер документа
        /// </summary>

        public string DocumentNumber
        {
            get { return Otgr.DocumentNumber; }
            set
            {
                if (value != Otgr.DocumentNumber)
                {
                    Otgr.DocumentNumber = value;
                    NotifyPropertyChanged("DocumentNumber");
                }
            }
        }

        public string RwBillNumber
        {
            get { return Otgr.RwBillNumber; }
            set
            {
                if (value != Otgr.RwBillNumber)
                {
                    Otgr.RwBillNumber = value;
                    NotifyPropertyChanged("RwBillNumber");
                }
            }
        }

        private PoupViewModel poup;
        public PoupViewModel Poup
        {
            get
            {
                if (poup == null)
                    poup = new PoupViewModel(repository, Otgr.Poup, Otgr.Pkod);
                return poup;
            }
        }

        public int Nv
        {
            get { return Otgr.Nv; }
            set
            {
                if (value != Otgr.Nv)
                {
                    Otgr.Nv = value;
                    NotifyPropertyChanged("Nv");
                }
            }
        }

        private KontrAgent pokupatel;
        public KontrAgent Pokupatel
        {
            get
            {
                if (pokupatel == null)
                    pokupatel = repository.GetKontrAgent(Otgr.Kpok);
                return pokupatel;
            }
        }

        private KontrAgent poluchatel;
        public KontrAgent Poluchatel
        {
            get
            {
                if (poluchatel == null)
                    poluchatel = repository.GetKontrAgent(Otgr.Kgr);
                return poluchatel;
            }
        }

        private DogInfo dogovor; // данные о договоре
        public DogInfo Dogovor
        {
            get
            {
                if (dogovor == null)
                    dogovor = repository.GetDogInfo(Otgr.Kdog, true);
                return dogovor;
            }
        }

        private KodfModel kodf;
        public KodfModel Kodf
        {
            get 
            {
                if (kodf == null)
                    kodf = repository.GetKodf(Otgr.Kodf);
                return kodf; 
            }
        }

        public bool IsDateRange { get { return Otgr.Period == 6; } }

        public DateTime Datnakl
        {
            get { return Otgr.Datnakl; }
            set
            {
                if (value != Otgr.Datnakl)
                {
                    Otgr.Datnakl = value;
                    NotifyPropertyChanged("Datnakl");
                }
            }
        }

        public DateTime Datgr
        {
            get { return Otgr.Datgr; }
            set
            {
                if (value != Otgr.Datgr)
                {
                    Otgr.Datgr = value;
                    NotifyPropertyChanged("Datgr");
                }
            }
        }

        public DateTime? Datarrival
        {
            get { return Otgr.Datarrival != null 
                                         && Otgr.Datarrival.Value.Year < 2000 ? null : Otgr.Datarrival ; }
            set
            {
                if (value != Otgr.Datarrival)
                {
                    Otgr.Datarrival = value;
                    NotifyPropertyChanged("Datarrival");
                }
            }
        }
        
        public DateTime? DeliveryDate
        {
            get { return Otgr.DeliveryDate; }
            set
            {
                if (value != Otgr.DeliveryDate)
                {
                    Otgr.DeliveryDate = value;
                    NotifyPropertyChanged("DeliveryDate");
                }
            }
        }

        public DateTime DatBuch
        {
            get { return IsDateRange ? Otgr.Datgr : Otgr.Datnakl; }
        }

        private ProductInfo product;
        public ProductInfo Product
        {
            get
            {
                if (product == null)
                    product = repository.GetProductInfo(Otgr.Kpr);
                return product;
            }
        }

        public string ProductInfo 
        {
            get { return Otgr.Bought ? "Покупной ресурс" : null; }
        }

        public decimal AkcStake
        {
            get { return Otgr.AkcStake; }
            set
            {
                if (value != Otgr.AkcStake)
                {
                    Otgr.AkcStake = value;
                    NotifyPropertyChanged("AkcStake");
                }
            }
        }

        private Valuta akcVal;
        public Valuta AkcVal
        {
            get 
            {
                if (akcVal == null && AkcStake != 0)
                    akcVal = repository.GetValutaByKod(Otgr.AkcKodVal);
                return akcVal;
            }
        }

        private VidAkcModel vidAkc;
        public VidAkcModel VidAkc
        {
            get 
            {
                if (vidAkc == null && Otgr.VidAkc != 0)
                    vidAkc = repository.GetVidAkc(Otgr.VidAkc);
                return vidAkc;
            }
        }

        public decimal Kolf
        {
            get { return Otgr.Kolf; }
            set
            {
                if (value != Otgr.Kolf)
                {
                    Otgr.Kolf = value;
                    NotifyPropertyChanged("Kolf");
                }
            }
        }

        public decimal Cena
        {
            get { return Otgr.Cena; }
            set
            {
                if (value != Otgr.Cena)
                {
                    Otgr.Cena = value;
                    NotifyPropertyChanged("Cena");
                }
            }
        }


        //public short Provoz
        //{
        //    get { return Otgr.Provoz; }
        //    set 
        //    {
        //        if (value != Otgr.Provoz)
        //        {
        //            Otgr.Provoz = value;
        //            NotifyPropertyChanged("Provoz");
        //        }
        //    }
        //}

        public decimal Sper
        {
            get { return Otgr.Sper; }
            set
            {
                if (value != Otgr.Sper)
                {
                    Otgr.Sper = value;
                    NotifyPropertyChanged("Sper");
                }
            }
        }

        public decimal Ndssper
        {
            get { return Otgr.Ndssper; }
            set
            {
                if (value != Otgr.Ndssper)
                {
                    Otgr.Ndssper = value;
                    NotifyPropertyChanged("Ndssper");
                }
            }
        }
        
        public decimal Nds
        {
            get { return Otgr.Nds; }
            set
            {
                if (value != Otgr.Nds)
                {
                    Otgr.Nds = value;
                    NotifyPropertyChanged("Nds");
                }
            }
        }

        //public decimal? SumNds
        //{
        //    get { return Otgr.SumNds; }
        //    set
        //    {
        //        if (value != Otgr.SumNds)
        //        {
        //            Otgr.SumNds = value;
        //            NotifyPropertyChanged("SumNds");
        //        }
        //    }
        //}
        
        public decimal Dopusl
        {
            get { return Otgr.Dopusl; }
            set
            {
                if (value != Otgr.Dopusl)
                {
                    Otgr.Dopusl = value;
                    NotifyPropertyChanged("Dopusl");
                }
            }
        }

        public decimal Ndsdopusl
        {
            get { return Otgr.Ndsdopusl; }
            set
            {
                if (value != Otgr.Ndsdopusl)
                {
                    Otgr.Ndsdopusl = value;
                    NotifyPropertyChanged("Ndsdopusl");
                }
            }
        }

        public decimal Ndst_dop
        {
            get { return Otgr.Ndst_dop; }
            set
            {
                if (value != Otgr.Ndst_dop)
                {
                    Otgr.Ndst_dop = value;
                    NotifyPropertyChanged("Ndst_dop");
                }
            }
        }

        public short TransportId
        {
            get { return Otgr.TransportId; }
            set
            {
                if (value != Otgr.TransportId)
                {
                    Otgr.TransportId = value;
                    NotifyPropertyChanged("TransportId");
                }
            }
        }

        public bool IsChecked
        {
            get { return Otgr.IsChecked; }
            set
            {
                if (value != Otgr.IsChecked)
                {
                    Otgr.IsChecked = value;
                    NotifyPropertyChanged("IsChecked");
                }
            }

        }

        public string[] StatusMsgs
        {
            get { return Otgr.StatusMsgs; }
            set 
            {
                Otgr.StatusMsgs = value;
                NotifyPropertyChanged("StatusMsgs");
            }
        }

        public short StatusType
        {
            get { return Otgr.StatusType; }
            set
            {
                Otgr.StatusType = value;
                NotifyPropertyChanged("StatusType");
                NotifyPropertyChanged("HasErrors");
            }
        }

        public bool HasErrors
        {
            get { return StatusType > 99; }
        }

        public bool IsFindSfs { get { return isFindSfs; } }

        private bool? isEditEnabled;
        public bool IsEditEnabled
        {
            get
            {
                if (isEditEnabled == null)
                    isEditEnabled = GetIsEditEnabled();
                return isEditEnabled.Value;
            }
        }

        private bool GetIsEditEnabled()
        {
            return Otgr.Idp623 > 0 ? repository.GetIfOtgrCanBeEdited(Otgr.Idp623) : false;
        }

        public bool IsSperExists
        {
            get { return Otgr.TransportId == (short)TransportTypes.Railway || Otgr.Sper != 0 || Otgr.Dopusl != 0; }
        }

        public bool IsAkcizExists
        {
            get { return Otgr.VidAkc != 0 && Otgr.AkcStake != 0; }
        }

        public bool IsShowStations
        {
            get { return Otgr.Nv > 0; }
        }

        private RailStation stFrom;
        public RailStation StFrom
        {
            get
            {
                if (IsShowStations && Otgr.Stotpr > 0 && stFrom == null)
                    stFrom = repository.GetRailStation(Otgr.Stotpr);
                return stFrom;
            }
        }

        private RailStation stTo;
        public RailStation StTo
        {
            get
            {
                if (IsShowStations && Otgr.Stgr > 0 && stTo == null)
                    stTo = repository.GetRailStation(Otgr.Stgr);
                return stTo;
            }
        }


        private SfInListInfo[] otgrAllSfs;

        public SfInListInfo[] OtgrAllSfs
        {
            get
            {
                if (!isSfsLoaded && isFindSfs && Otgr.Idp623 > 0)
                    LoadSfs();
                return otgrAllSfs;
            }
        }

        private bool isSfsLoaded = false;
        public bool IsSfsLoaded
        {
            get { return isSfsLoaded; }
            //set { SetAndNotifyProperty("IsSfsLoaded", ref isSfsLoaded, value); }
        }

        private bool isSfsLoading = false;

        public void LoadSfs()
        {
            if (isSfsLoading) return;
            isSfsLoading = true;
            otgrAllSfs = repository.GetSfsByOtgruz(Otgr.Idp623);
            isSfsLoaded = true;
            isSfsLoading = false;
            NotifyPropertyChanged("IsSfsLoaded");
            NotifyPropertyChanged("OtgrAllSfs");
            NotifyPropertyChanged("OtgrSfs");
            NotifyPropertyChanged("IsSfsExists");
        }

        private SfProductPayModel[] activePays;
        public SfProductPayModel[] ActivePays
        {
            get
            {
                if (activePays == null)
                    activePays = GetActivePays();
                return activePays;
            }
        }

        private SfProductPayModel[] GetActivePays()
        {
            SfProductPayModel[] res = null;
            if (Otgr.Idp623 > 0)
            {
                var activePrilsfs = OtgrSfs.SelectMany(s => repository.GetSfProducts(s.IdSf));
                res = activePrilsfs.SelectMany(p => repository.GetSfLinePays(p.IdprilSf)).ToArray();
            }
            return res;
        }

        public bool IsSfsExists { get { return OtgrSfs.Any(); } }       

        public IEnumerable<SfInListInfo> OtgrSfs
        {
            get
            {
                if (otgrAllSfs == null || otgrAllSfs.Length == 0)
                    return Enumerable.Empty<SfInListInfo>();
                else
                    return otgrAllSfs.Where(s => s.Status != LifetimeStatuses.Created && s.Status != LifetimeStatuses.Deleted);
            }
        }

        public Dictionary<string, KeyValuePair<string,decimal>> Totals { get; set; }

        public bool IsTotalsExists { get { return Totals != null && Totals.Count > 0; } }

        public bool IsInRealiz { get { return Otgr.Idp623 > 0; } }

        public bool IsVozvrat
        {
            get { return Otgr.IdVozv.GetValueOrDefault() > 0; }
        }

        private Country destinationCountry;
        public Country DestinationCountry
        {
            get 
            {
                if (destinationCountry == null)
                    destinationCountry = repository.GetCountries(Otgr.Kstr).FirstOrDefault();
                return destinationCountry; 
            }
        }

        #region ITrackable Members

        public TrackingInfo TrackingState
        {
            get
            {
                return Otgr.TrackingState;
            }
            set
            {
                if (value != Otgr.TrackingState)
                {
                    Otgr.TrackingState = value;
                    NotifyPropertyChanged("TrackingState");
                }
            }
        }

        #endregion
    }
}
