using System;
using System.Linq;
using CommonModule.Helpers;
using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Interfaces;
using System.Xml.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using DataObjects.Helpers;


//using Realization.ViewModels;

namespace CommonModule.DataViewModels
{
    public class SfViewModel : BasicViewModel, ITrackable
    {
        private IDbService repository;

        public SfViewModel(IDbService _rep, SfModel _sfRef, bool _islazy)
        {
            repository = _rep;
            sfRef = _sfRef;
            if (!_islazy) CollectInfo();
            SfStatus = repository.GetSfStatus(_sfRef.IdSf);
        }

        public SfViewModel(IDbService _rep, SfModel _sfRef)
            :this(_rep, _sfRef, false)
        {}

        public SfViewModel(IDbService _rep, SfInListViewModel _sfilvm, bool _islazy)
        {
            repository = _rep;
            sfRef = repository.GetSfModel(_sfilvm.SfRef.IdSf);
            SfStatus = _sfilvm.SfStatus;
            SumOpl = _sfilvm.SumOpl;
            if (!_islazy) CollectInfo();
        }

        private SfModel sfRef;
        public SfModel SfRef
        {
            get { return sfRef; }
        }

        /// <summary>
        /// Текущий(последний) статус счёта
        /// </summary>
        public LifetimeStatuses SfStatus { get; set; }

        public int Poup { get { return sfRef.Poup; } }

        public int NumSf
        {
            get { return SfRef.NumSf;  }
            set
            {
                if (value != SfRef.NumSf)
                {
                    SfRef.NumSf = value;
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("NumSf");
                }
            }
        }

        public SfTypeInfo SfType
        {
            get { return repository.GetSfTypeInfo(SfRef.SfTypeId); }//SfTypeInfo.Get(SfRef.SfTypeId); }
        }
        
        public DateTime DatePltr
        {
            get { return SfRef.DatPltr; }
            set
            {
                if (value != SfRef.DatPltr)
                {
                    SfRef.DatPltr = value;
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("DatePltr");
                }
                
            }
        }

        public DateTime? DateBuch
        {
            get { return SfRef.DatBuch; }
            set
            {
                if (value != SfRef.DatBuch)
                {
                    SfRef.DatBuch = value;
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("DateBuch");
                }
            }
        }


        /// <summary>
        /// Дополнительная информация по срокам оплаты
        /// </summary>
        public SfPayPeriodModel SfPeriod
        {
            get { return SfRef.SfPeriod; }
            set
            {
                if (value != SfRef.SfPeriod)
                {
                    SfRef.SfPeriod = value;
                    NotifyPropertyChanged("SfPeriod");
                }
            }
        }

        private SfPayPeriodModel actualSfPeriod;
        public SfPayPeriodModel ActualSfPeriod
        {
            get
            {
                if (actualSfPeriod == null)
                    GetSfPeriod();
                return actualSfPeriod;
            }
        }

        private void GetSfPeriod()
        {
            var sfPeriodModel = SfPeriod;
            if (sfPeriodModel == null || sfPeriodModel.DatStart == null || sfPeriodModel.LastDatOpl == null)
                sfPeriodModel = repository.GetActualSfPeriod(SfRef.IdSf);
            actualSfPeriod = sfPeriodModel;
        }

        public DateTime DateGr //дата отгрузки
        {
            get { return DateGrFrom; }
        }

        private DateTime? dateGrFrom;
        public DateTime DateGrFrom
        {
            get
            {
                if (dateGrFrom == null)
                    GetDateGrs();
                return dateGrFrom.Value;
            }
        }

        private void GetDateGrs()
        {
            if (SfRef != null && SfRef.IdSf > 0)
            {
                var datgrs = repository.GetSfDateGrRange(SfRef.IdSf);
                dateGrFrom = datgrs.DateFrom;
                dateGrTo = datgrs.DateTo;
            }
            else
                dateGrFrom = dateGrTo = DatePltr;
        }

        private DateTime? dateGrTo;
        public DateTime DateGrTo
        {
            get
            {
                if (dateGrFrom == null)
                    GetDateGrs();
                return dateGrTo.Value;
            }
        }

        public string DateGrString
        {
            get
            {
                return DateGrFrom == DateGrTo ? DateGrFrom.ToString("dd.MM.yyyy")
                                              : String.Format("c {0:dd.MM.yyyy}\nпо {1:dd.MM.yyyy}", DateGrFrom, DateGrTo);
            }
        }

        private Valuta sfVal;
        /// <summary>
        /// Валюта счёта
        /// </summary>
        public Valuta SfVal
        {
            get
            {
                if (sfVal == null)
                {
                    sfVal = repository.GetValutaByKod(SfRef.KodVal);
                }
                return sfVal;
            }
        }

        // Поставщик
        private KontrAgent postavschik;
        public KontrAgent Postavschik
        {
            get
            {
                if (postavschik == null)
                {
                    postavschik = repository.GetKontrAgent(CommonModule.CommonSettings.OurKontrAgentKod);
                    //postavschik.Address = "Республика Беларусь. Витебская обл., г.Новополоцк";
                }
                return postavschik;
            }
            //set { postavschik = value; }
        }
        
        // Грузоотправитель
        private KontrAgent otpravitel;
        public KontrAgent Otpravitel
        {
            get
            {
                if (otpravitel == null)
                    GetOtpravitel();
                return otpravitel;
            }
            //set { otpravitel = value; }
        }

        private void GetOtpravitel()
        {
            int l_kotpr = (int)SfRef.Kotpr;
            if (l_kotpr == 0 || l_kotpr == repository.OurKgr)
                otpravitel = Postavschik;
            else
                otpravitel = repository.GetKontrAgent(l_kotpr);
        }
        
        // Плательщик
        private KontrAgent platelschik;
        public KontrAgent Platelschik
        {
            get
            {
                if (platelschik == null)
                    platelschik = repository.GetKontrAgent(SfRef.Kpok);
                return platelschik;
            }
            //set { platelschik = value; }
        }

        public string PlatOkpoTitle { get; set; }
        public string PlatOkpoValue { get; set; }
    
        private void SelectOkpoData()
        {
            if (String.IsNullOrEmpty(Platelschik.Kpp))
            {
                PlatOkpoTitle = "ОКПО";
                PlatOkpoValue = Platelschik.Okpo;
            }
            else
            {
                PlatOkpoTitle = "КПП";
                PlatOkpoValue = Platelschik.Kpp;            
            }      
        }

        // Грузополучатель
        private KontrAgent poluchatel;
        public KontrAgent Poluchatel
        {
            get
            {
                if (poluchatel == null)
                    GetPoluchatel();
                return poluchatel;
            }
            //set { poluchatel = value; }
        }

        private void GetPoluchatel()
        {
            int l_kpol = (int)SfRef.Kgr;
            if (l_kpol == 0 || l_kpol == SfRef.Kpok)
                poluchatel = Platelschik;
            else
                poluchatel = repository.GetKontrAgent(SfRef.Kgr);
        }

        private bool? hasResourceOwner;
        private KontrAgent resourceOwner;
        public KontrAgent ResourceOwner
        {
            get
            {
                if (hasResourceOwner == null)
                    GetResourceOwner();
                return resourceOwner;
            }
            //set { poluchatel = value; }
        }

        private void GetResourceOwner()
        {
            resourceOwner = repository.GetSfResourceOwner(SfRef.IdSf);
            hasResourceOwner = (resourceOwner != null);
        }

        private BankInfo postBank; // Банк поставщика
        public BankInfo PostBank
        {
            get
            {
                if (postBank == null)
                    postBank = repository.GetBankInfo((int)SfRef.IdDog, repository.OurKgr);
                return postBank;
            }
        }
        
        public String GrOtpName //Название грузоотправителя
        {
            get
            {
                return Postavschik == Otpravitel ? "тот же" : String.Format("{0} {1}",Otpravitel.Kgr, Otpravitel.FullName);
            }
        } 

        public String GrPolAddress //Адрес грузополучателя
        {
            get
            {
                return Poluchatel == Platelschik ? "" : Poluchatel.Address;
            }
        }

        public String GrPolName //Название грузополучателя
        {
            get
            {
                return Poluchatel == Platelschik ? "тот же" : 
                    (Poluchatel.Kgr == CommonModule.CommonSettings.OurKontrAgentKod ? Poluchatel.FullName : String.Format("{0} {1}", Poluchatel.Kgr, Poluchatel.FullName));
            }
        }

        public String GrOtpAddress //Адрес грузоотправителя
        {
            get
            {
                return Postavschik == Otpravitel ? "" : Otpravitel.Address;
            }
        }


        private RailStation fromStation;
        public RailStation FromStation
        {
            get
            {
                if (SfRef.StOtpr > 0 && fromStation == null)
                    fromStation = repository.GetRailStation(SfRef.StOtpr);
                return fromStation;
            }
        }

        private RailStation toStation;
        public RailStation ToStation
        {
            get
            {
                if (SfRef.StPol > 0 && toStation == null)
                    toStation = repository.GetRailStation(SfRef.StPol);
                return toStation;
            }
        }

        private BankInfo platBank; // Банк плательщика
        public BankInfo PlatBank
        {
            get
            {
                if (platBank == null)
                    platBank = repository.GetBankInfo(SfRef.IdDog, SfRef.Kpok);
                return platBank;
            }
        }

        private DogInfo dogovor; // данные о договоре
        public DogInfo Dogovor
        {
            get
            {
                if (dogovor == null)
                    dogovor = repository.GetDogInfo(SfRef.IdDog, false);
                return dogovor;
            }
        }

        private string dopStr; // дополнения к договору
        public string DopStr
        {
            get
            {
                if (dopStr == null && sfRef != null)
                    dopStr = GetDopStr(sfRef.IdSf);
                return dopStr;
            }
        }

        private string GetDopStr(int _idsf)
        {
            string res = "";
            if (_idsf > 0)
            {
                var dinfos = repository.GetSfDopDogInfos(_idsf);
                if (dinfos != null && dinfos.Length > 0)
                    res = String.Join(", ", dinfos.Select(di => 
                        (String.IsNullOrEmpty(di.DopOsn) ? "" : String.Format("доп.{0} от {1:dd.MM.yy}", di.DopOsn, di.DatDop)) +
                        (String.IsNullOrEmpty(di.AltOsn) ? "" : String.Format(" изм.{0} от {1:dd.MM.yy}", di.AltOsn, di.DatAlt))
                        ).ToArray());
                else
                    res = Dogovor.DopOsn ?? "";
            }
            return res;
        }

       
        //Общий вес
        private WeightInfo weight;
        public string Weight 
        {
            get
            {
                if (weight == null)
                    weight = repository.GetSfWeightInfo(SfRef.IdSf);
                string fmt = "";
                if (weight != null)
                    fmt = "{0:0.".PadRight(5 + weight.Precision, '0') + "###;#;#} {1}";
                return weight == null ? "" : String.Format(fmt,weight.Weight,weight.Edizm);
            }
        }

        // название валюты цены в шапке таблицы
        private string valOfCenaStr; 
        public string ValOfCenaStr
        {
            get
            {
                if (valOfCenaStr == null)
                    valOfCenaStr = GetValOfCenaString();
                return valOfCenaStr;
            }
        }

        private string GetValOfCenaString()
        {
            string res = "";
            if (SfProductLines != null && SfProductLines.Length > 0 && SfProductLines[0].CenProd > 0)
                res = SfProductLines[0].ProductLineInfo.ValName;
            return res;
        }

        public decimal KolProd
        {
            get 
            {
                if (weight == null)
                    weight = repository.GetSfWeightInfo(SfRef.IdSf);
                return weight.Weight; 
            }
        }

        //public decimal SumProd
        //{
        //    get { return SfItogInfo.TableLine.SumProd; }
        //}

        //public decimal SumAkc // акциз и ндс за акциз
        //{
        //    get { return SfItogInfo.TableLine.SumAkc; }
        //}


        //public decimal SumNds
        //{
        //    get { return SfItogInfo.TableLine.NdsSum; }
        //}

        public decimal SumPltr
        {
            get { return SfRef.SumPltr; } // SfItogInfo.TableLine.SumItog; }
        }

        public string SumProp
        {
            get
            {
                return Sumprop.SumInVal(SumPltr, SfRef.KodVal);
            }
        }


        private bool isLoaded = false;

        //private bool isLoading = false;
        //public bool IsLoading
        //{
        //    get { return isLoading; }
        //    set { SetAndNotifyProperty("IsLoading", ref isLoading, value); }
        //}

        /// <summary>
        /// Загружает итоговую информацию о счёте (итоги по платежам и итоги по счёту)
        /// </summary>
        public void CollectInfo()
        {
            if (sfRef == null) return;
            sfVal = repository.GetValutaByKod(SfRef.KodVal);
            postavschik = repository.GetKontrAgent(CommonModule.CommonSettings.OurKontrAgentKod);
            GetOtpravitel();
            platelschik = repository.GetKontrAgent(SfRef.Kpok);
            GetPoluchatel();
            GetResourceOwner();
            postBank = repository.GetBankInfo((int)SfRef.IdDog, repository.OurKgr);
            if (SfRef.StOtpr > 0)
                fromStation = repository.GetRailStation(SfRef.StOtpr);
            if (SfRef.StPol > 0)
                toStation = repository.GetRailStation(SfRef.StPol);
            platBank = repository.GetBankInfo(SfRef.IdDog, SfRef.Kpok);
            dogovor = repository.GetDogInfo(SfRef.IdDog, false);
            weight = repository.GetSfWeightInfo(SfRef.IdSf);

            LoadPrils();

            sumOpl = repository.GetSfSumOpl(SfRef.IdSf);

            SelectOkpoData();

            CheckESFN();

            isLoaded = true;
        }

        private decimal? sumOpl;
        /// <summary>
        /// Оплачено
        /// </summary>
        public decimal SumOpl
        {
            get
            {
                if (sumOpl == null)
                    sumOpl = repository.GetSfSumOpl(SfRef.IdSf);
                return sumOpl.GetValueOrDefault();
            }
            set
            {
                SetAndNotifyProperty("SumOpl", ref sumOpl, value);
            }
        }

        private SfProductModel[] sfPrils;
        public SfProductModel[] SfPrils
        {
            get
            {
                if (!isLoaded && sfPrils == null)
                    sfPrils = repository.GetSfProducts(SfRef.IdSf).ToArray();
                return sfPrils;
            }
        }

        //продуктовые строчки счёта
        private SfLineViewModel[] sfProductLines;
        public SfLineViewModel[] SfProductLines
        {
            get
            {
                if (!isLoaded && sfProductLines == null)
                    LoadPrils();
                return sfProductLines;
            }
        }

        private void LoadPrils()
        {
            if (SfPrils != null && SfPrils.Length > 0)
                sfProductLines = sfPrils.Select(l => new SfLineViewModel(repository, l, sfRef.KodVal != "RB")).ToArray();
        }

        private const string PRINTABLE_NOTES_ELEMENT_NAME = "PrintableNotes";
        private const string VZAMEN_SF_ELEMENT_NAME = "VzamenSf";

        private XElement xMemo;
        public XElement XMemo
        {
            get
            {
                if (xMemo == null)
                    xMemo = GetMemoElement();
                return xMemo;
            }
        }


        private bool vzamenSfLoaded;
        private KeyValuePair<int, DateTime>? vzamenSf;
        public KeyValuePair<int, DateTime>? VzamenSf
        {
            get
            {
                if (vzamenSf == null && !vzamenSfLoaded)
                    vzamenSf = GetVzamenSf();
                return vzamenSf;
            }
            set
            {
                vzamenSf = value;
                var num = value.HasValue ? value.Value.Key : 0;
                var dat = value.HasValue ? value.Value.Value : DatePltr;
                SetVzamenSfInMemo(num, dat);
                if (TrackingState == TrackingInfo.Unchanged)
                    TrackingState = TrackingInfo.Updated;
                NotifyPropertyChanged("VzamenSf");
            }
        }

        private KeyValuePair<int, DateTime>? GetVzamenSf()
        {
            KeyValuePair<int, DateTime>? res = null;
            XElement xmemo = XMemo;
            try
            {
                XElement vzamenSfElement = null;
                if (xmemo.Name != VZAMEN_SF_ELEMENT_NAME)
                    vzamenSfElement = xmemo.Element(VZAMEN_SF_ELEMENT_NAME);
                else
                    vzamenSfElement = xmemo;
                if (vzamenSfElement != null)
                {
                    int vzNumsf = 0;
                    DateTime vzDateSf;
                    var numSfAttr = vzamenSfElement.Attribute("NumSf");
                    if (numSfAttr != null)
                    {
                        int.TryParse(numSfAttr.Value, out vzNumsf);
                        if (vzNumsf != 0 && vzNumsf != NumSf)
                        {
                            var dateSfAttr = vzamenSfElement.Attribute("DateSf");
                            if (numSfAttr != null && DateTime.TryParseExact(dateSfAttr.Value, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out vzDateSf))
                                res = new KeyValuePair<int, DateTime>(vzNumsf, vzDateSf);
                        }
                    }

                }
            }
            catch
            {
                res = null;
            }

            vzamenSfLoaded = true;
            return res;            
        }

        private string printableNotes;
        public string PrintableNotes
        {
            get
            {
                if (printableNotes == null)
                    printableNotes = GetPrintableNotes();
                return printableNotes;
            }
            set
            {
                if (value != printableNotes)
                {
                    printableNotes = value;
                    SetPrintableNotesInMemo(value);
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("PrintableNotes");
                }
            }
        }

        private string GetPrintableNotes()
        {
            string res = "";
            XElement xmemo = XMemo;
            try
            {
                XElement prNotesElement = null;
                if (xmemo.Name != PRINTABLE_NOTES_ELEMENT_NAME)
                    prNotesElement = xmemo.Element(PRINTABLE_NOTES_ELEMENT_NAME);
                else
                    prNotesElement = xmemo;
                if (prNotesElement != null)
                {
                    var prNotesTextAttr = prNotesElement.Attribute("Text");
                    if (prNotesTextAttr != null)
                        res = prNotesTextAttr.Value;
                    else
                        res = prNotesElement.Value;
                }
            }
            catch 
            {
                res = "";
            }

            return res;
        }

        private void SetPrintableNotesInMemo(string _prNotes)
        {
            XElement prNotesElement = String.IsNullOrEmpty(_prNotes) ? null : CreatePrintableNotesElement(_prNotes);
            XElement memoElement = XMemo;
            if (memoElement.Name == PRINTABLE_NOTES_ELEMENT_NAME)
                memoElement = prNotesElement;
            else
            {
                XElement oldNotesElement = memoElement.Element(PRINTABLE_NOTES_ELEMENT_NAME);
                if (oldNotesElement != null)
                    oldNotesElement.Remove();
                if (prNotesElement != null)
                    memoElement.Add(prNotesElement);
            }

            SerialiseMemo(memoElement);
        }

        private void SetVzamenSfInMemo(int _numsf, DateTime _datesf)
        {
            XElement vzsfElement = _numsf == 0 ? null : CreateVzamenSfElement(_numsf, _datesf);
            XElement memoElement = XMemo;
            if (memoElement.Name == VZAMEN_SF_ELEMENT_NAME)
                memoElement = vzsfElement;
            else
            {
                XElement oldvzsfElement = memoElement.Element(VZAMEN_SF_ELEMENT_NAME);
                if (oldvzsfElement != null)
                    oldvzsfElement.Remove();
                if (vzsfElement != null)
                    memoElement.Add(vzsfElement);
            }

            SerialiseMemo(memoElement);
        }

        private void SerialiseMemo(XElement _memoElement)
        {
            xMemo = _memoElement;
            string sMemo = null;
            if (_memoElement != null && (_memoElement.HasElements || _memoElement.HasAttributes))
            {
                var ncnt = _memoElement.Elements().Count();
                if (ncnt == 1)
                    _memoElement = _memoElement.Elements().FirstOrDefault();
                sMemo = _memoElement.ToString(SaveOptions.DisableFormatting);
            }
            sfRef.Memo = sMemo;
        }


        private XElement GetMemoElement()
        {
            XElement res = null;
            string memo = sfRef.Memo;
            if (String.IsNullOrEmpty(memo))
                res = new XElement("Memo");
            else
                try
                {
                    res = XElement.Parse(memo);
                }
                catch
                {
                    res = new XElement("Memo");
                }
            if (res.Name != "Memo")
            {
                var parsed = res;
                res = new XElement("Memo");
                res.Add(parsed);
            }
            return res;
        }

        private XElement CreatePrintableNotesElement(string _prNotes)
        {
            XElement prNotesElement = new XElement(PRINTABLE_NOTES_ELEMENT_NAME,
                                                   new XAttribute("Text", _prNotes));
            return prNotesElement;
        }

        private XElement CreateVzamenSfElement(int _numsf, DateTime _datesf)
        {
            XElement vzsfElement = new XElement(VZAMEN_SF_ELEMENT_NAME,
                                                   new XAttribute("NumSf", _numsf.ToString()),
                                                   new XAttribute("DateSf", _datesf.ToString("dd.MM.yyyy")));
            return vzsfElement;
        }

        private bool? isMainReportFormExists;
        private ReportModel mainReportForm;
        public ReportModel MainReportForm
        {
            get 
            {
                if (!isMainReportFormExists.HasValue)
                {
                    mainReportForm = repository.GetSfPrintForm(SfRef.IdSf);
                    isMainReportFormExists = mainReportForm != null;
                }
                return mainReportForm; 
            }
        }

        public bool IsEsfnExists { get { return esfn != null && esfn.Length > 0; } }

        private EsfnDataViewModel[] esfn;
        public EsfnDataViewModel[] Esfn
        {
            get 
            {
                if (esfn == null) CheckESFN();
                return esfn; 
            }
            set 
            {
                if (SetAndNotifyProperty("Esfn", ref esfn, value))
                {
                    NotifyPropertyChanged("IsEsfnExists");
                    NotifyPropertyChanged("EsfnSumItog");
                }
            }
        }        

        public decimal EsfnSumItog
        {
            get { return IsEsfnExists ? esfn.Sum(e => e.RosterTotalCost) : 0M; }
        }

        public void CheckESFN()
        {                        
            //System.Threading.Thread.Sleep(15000);
            var esfn = repository.Get_ESFN(sfRef.IdSf);
            if (esfn != null)
                Esfn = esfn.Select(e => new EsfnDataViewModel(repository, e)).ToArray() ;
        }

        #region ITrackable Members

        public TrackingInfo TrackingState
        {
            get
            {
                return SfRef.TrackingState;
            }
            set
            {
                SfRef.TrackingState = value;
            }
        }

        #endregion
    }
}