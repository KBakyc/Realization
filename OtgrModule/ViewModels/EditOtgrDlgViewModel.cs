using System;
using System.Linq;
using CommonModule.Commands;
using CommonModule.Helpers;
using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Helpers;
using DataObjects.Interfaces;
using System.Windows.Input;
using System.Collections.Generic;
using OtgrModule.Helpers;

namespace OtgrModule.ViewModels
{
    public class EditOtgrDlgViewModel : BaseDlgViewModel
    {
        private OtgrLine oldModel;
        private OtgrLine newModel;
        private PoupSelectionViewModel poupDld;
        private ValSelectionViewModel valDlg;
        private InvoiceTypeSelectionViewModel itypeDlg;
        private KaSelectionViewModel platDlg;
        private KaSelectionViewModel grpolDlg;
        private ProductSelectionViewModel prodDlg;
        private PDogListViewModel dogDlg;
        private CountrySelectionViewModel strDlg;

        private IDbService repository;

        public EditOtgrDlgViewModel(IDbService _rep, OtgrLine _pm, bool _issubscribe)
        {
            repository = _rep;
            oldModel = _pm;
            
            if (_pm != null)
            {
                newModel = DeepCopy.Make(_pm); //.DataClone();
                Title = "Изменение отгрузки/услуги";
            }
            else
            {
                var now = DateTime.Now;
                newModel = new OtgrLine()
                {
                    Datgr = now,
                    Datnakl = now,
                    SourceId = 1,
                    TrackingState = TrackingInfo.Created
                };
                Title = "Добавление отгрузки/услуги";
            }
            
            LoadData(_issubscribe);            
        }

        public EditOtgrDlgViewModel(IDbService _rep, OtgrLine _pm)
            :this(_rep, _pm, true)
        {}

        private List<MeasureUnit> measureUnits;
        public List<MeasureUnit> MeasureUnits { get { return measureUnits; } }

        private MeasureUnit selectedMeasureUnit;
        public MeasureUnit SelectedMeasureUnit
        {
            get { return selectedMeasureUnit; }
            set 
            {
                //if (value != null && value.Id < 0) value = null;
                SetAndNotifyProperty("SelectedMeasureUnit", ref selectedMeasureUnit, value);
                NotifyPropertyChanged("IsShowDensity");
            }
        }

        private void LoadData(bool _issubscribe)
        {
            if (dogDlg != null)
                dogDlg.PropertyChanged -= dogDlg_PropertyChanged;
            dogDlg = new PDogListViewModel(repository);
            if (poupDld != null)
                poupDld.PropertyChanged -= poupDld_PropertyChanged;
            poupDld = new PoupSelectionViewModel(repository) 
            {
                PoupTitle = null, PkodsTitle = null
            };
            valDlg = new ValSelectionViewModel(repository);            
            itypeDlg = new InvoiceTypeSelectionViewModel(repository);

            SetPlatEditor(null, false);
            SetGrpolEditor(null, false);
            
           
            // if (prodDlg != null)
           //     prodDlg.PropertyChanged -= prodDlg_PropertyChanged;
            prodDlg = new ProductSelectionViewModel(repository) { IsFiltered = true };
            CollectMeasureUnits();

            //GetAllInvoiceTypes();
            GetAllVidcens();
            AllVidAkcs = repository.GetVidAkcs();

            if (oldModel != null)
                RefreshVMs(oldModel);
            else
                InitNew();                            


            //agreeDlg = GetNewAgreeSelectionViewModel();
            //if (newModel.SourceId != 0) 
            SetAllRegionsEnable(true);

            if (_issubscribe)
                SubscribeViewModelChanges();            
        }

        private void CollectMeasureUnits()
        {
            measureUnits = new List<MeasureUnit> { new MeasureUnit { Id=-1, IsNeedDensity=false, FullName="- Неизвестно -", ShortName="?"} };
            measureUnits.AddRange(repository.GetMeasureUnits(null));
        }

        /// <summary>
        /// Установка редактора для плательщика
        /// </summary>
        /// <param name="_ed"></param>
        /// <param name="_subscribe"></param>
        public void SetPlatEditor(KaSelectionViewModel _ed, bool _subscribe)
        {          
            if (platDlg != null)
                platDlg.PropertyChanged -= platDlg_PropertyChanged;
            platDlg = _ed ?? new KaSelectionViewModel(repository) { Title = "Плательщик по договору" };
            
            if (_subscribe)
                platDlg.PropertyChanged += platDlg_PropertyChanged;
        }

        /// <summary>
        /// Установка редактора для грузополучателя
        /// </summary>
        /// <param name="_ed"></param>
        /// <param name="_subscribe"></param>
        public void SetGrpolEditor(KaSelectionViewModel _ed, bool _subscribe)
        {
            if (grpolDlg != null)
                grpolDlg.PropertyChanged -= grpolDlg_PropertyChanged;
            grpolDlg = _ed ?? new KaSelectionViewModel(repository) { Title = "Получатель / отправитель" };

            if (_subscribe)
                grpolDlg.PropertyChanged += grpolDlg_PropertyChanged;
        }


        public void SubscribeViewModelChanges()
        {
            if (poupDld != null)
                poupDld.PropertyChanged += poupDld_PropertyChanged;
            if (platDlg != null)
                platDlg.PropertyChanged += platDlg_PropertyChanged;
            if (grpolDlg != null)
                grpolDlg.PropertyChanged += grpolDlg_PropertyChanged;
            if (dogDlg != null)
                dogDlg.PropertyChanged += dogDlg_PropertyChanged;
            if (prodDlg != null)
                prodDlg.PropertyChanged += prodDlg_PropertyChanged;
            if (valDlg != null)
                valDlg.PropertyChanged += valDlg_PropertyChanged;
        }

        

        private void InitNew()
        {
            CollectKodfs();
            SetNewProdNds();
            SetOtgrTransport();
        }

        void valDlg_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelVal")
            {
                if (valDlg.SelVal.Kodval == CommonModule.CommonSettings.OurKodVal)
                    DatKurs = null;
                else
                    if (DatKurs == null) DatKurs = Datgr;
                NotifyPropertyChanged("IsDatKursEnabled"); 
            }
        }
        
        void prodDlg_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedProductItem")
            {
                SelectCenaFromVidcen();
                SelectMeasureUnitFromProd();
                CollectNewProdAkcData();
                NotifyPropertyChanged("IsCenaEnabled"); 
                NotifyPropertyChanged("CenaLabel");
                NotifyPropertyChanged("IsShowAkcData");
                NotifyPropertyChanged("AkcStake");
                NotifyPropertyChanged("AkcValuta");
            }
        }

        void dogDlg_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelPDogInfo")
            {
                var newd = dogDlg.SelPDogInfo;
                if (newd != null)
                {
                    CollectProducts();
                    //SelectMeasureUnitFromProd();
                    SelVidcen = allVidcens.SingleOrDefault(v => v.Kod == newd.ModelRef.Vidcen);
                    ProvozSpis = dogDlg.SelPDogInfo.ProvozSpis;
                }
            }
        }

        private void CollectProducts()
        {
            if (dogDlg.SelPDogInfo != null)
            {
                var dogkpr = dogDlg.SelPDogInfo.Product.Kpr;
                var oldkpr = prodDlg.SelectedProductItem == null ? newModel.Kpr : prodDlg.SelectedProductItem.Value.Kpr;
                string pat = BusinessLogicHelper.MakeKProdPat(dogkpr);
                var prod = repository.GetProductsByPat(pat).Where(p => p.IsActive// && p.IsGood 
                                                                       && p.Kpr.ToString().Length == dogkpr.ToString().Length);
                prodDlg.PopulateProductList(prod);
                if (oldkpr != 0 && prodDlg.ProductList.Any(li => li.Value.Kpr == oldkpr))
                    prodDlg.SelectedProductItem = prodDlg.ProductList.SingleOrDefault(pi => pi.Value.Kpr == oldkpr);
                else
                    prodDlg.SelectedProductItem = prodDlg.ProductList.SingleOrDefault(pi => pi.Value.Kpr == dogkpr);                
            }
        }

        private void SelectMeasureUnitFromProd()
        {
            if (prodDlg != null && prodDlg.SelectedProductItem != null && measureUnits != null && measureUnits.Count > 0 && prodDlg.SelectedProductItem.Value.MeasureUnitId > 0)
                SelectedMeasureUnit = measureUnits.FirstOrDefault(u => u.Id == prodDlg.SelectedProductItem.Value.MeasureUnitId);
        }

        void poupDld_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelPoup")
            {
                var newpoup = poupDld.SelPoup;
                if (newpoup != null)
                {
                    CollectDogs();
                    CollectKodfs();                    
                    SetOtgrTransport();
                    NotifyPropertyChanged("SelKodf");
                }
            }
        }

        private void CollectKodfs()
        {
            var poup = poupDld.SelPoup;
            if (poup != null)
            {
                AllKodfs = GetAllMyKodfs(poup.Kod);
                int lastKodf = (selKodf == null ? Remember.GetValue<int>("SelKodf") 
                                                : selKodf.Kodf);
                if (allKodfs.Length > 0 && lastKodf != 0)
                {
                    var lastkfm = allKodfs.SingleOrDefault(k => k.Kodf == lastKodf);
                    selKodf = lastkfm;
                }
                else
                    selKodf = null;

            }
        }

        void platDlg_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedKA")
            {
                var newpok = platDlg.SelectedKA;
                if (newpok != null)
                {
                    if (grpolDlg.SelectedKA == null)
                    {
                        grpolDlg.PopulateKaList(new KontrAgent[] { newpok });
                        grpolDlg.SelectedKA = newpok;
                    }
                    else
                        CollectDogs();
                }
            }
        }
        
        void grpolDlg_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedKA")
            {
                var newgp = grpolDlg.SelectedKA;
                if (newgp != null)
                {
                    CollectDogs();
                }
            }
        }

        private void CollectDogs()
        {
            if (platDlg != null && platDlg.SelectedKA != null
                && poupDld != null && poupDld.SelPoup != null)
            {
                var poup = poupDld.SelPoup.Kod;
                int pkod = 0;
                if (poupDld.SelPkods != null && poupDld.SelPkods.Length > 0)
                    pkod = poupDld.SelPkods[0].Pkod;

                var kpok = platDlg.SelectedKA.Kgr;
                var kgr = (grpolDlg == null || grpolDlg.SelectedKA == null) ? 0 : grpolDlg.SelectedKA.Kgr;
                var pdogsdata = repository.GetPDogInfosByKaPoup(kpok, poup, (short)Datgr.Year);
                var pdogs = pdogsdata == null ? new List<PDogInfoModel>() 
                                              : pdogsdata.OrderBy(d => d.Osn).ToList();

                if (oldModel != null && oldModel.Kdog > 0 && oldModel.Poup == poup && oldModel.Kpok == kpok)
                {
                    // если в выбранных договорах нет текущего, добавляем текущий
                    if (!pdogs.Any(di => di.Kdog == oldModel.Kdog))
                    {
                        var cpdoginfo = repository.GetPDogInfoByKdog(oldModel.Kdog);
                        if (cpdoginfo != null)
                            pdogs.Insert(0, cpdoginfo);
                    }
                }

                dogDlg.LoadData(pdogs);

                if (dogDlg.PDogInfos.Length == 1)
                    dogDlg.SelPDogInfo = dogDlg.PDogInfos[0];
                else
                    dogDlg.SelPDogInfo = (newModel.Kdog == 0 ? null : dogDlg.PDogInfos.SingleOrDefault(d => d.ModelRef.Kdog == newModel.Kdog));

            }
        }

        private void RefreshVMs(OtgrLine _pm)
        {
            poupDld.SelPoup = poupDld.Poups.FirstOrDefault(p => p.Kod == _pm.Poup);
            if (poupDld.IsPkodEnabled)
            {
                var selpkod = poupDld.Pkods.FirstOrDefault(p => p.Value.Pkod == _pm.Pkod);
                if (selpkod != null)
                    selpkod.IsSelected = true;
            }
            CollectKodfs();
            if (allKodfs != null && allKodfs.Length > 0)
                selKodf = allKodfs.SingleOrDefault(k => k.Kodf == newModel.Kodf);

            valDlg.SelVal = valDlg.ValList.FirstOrDefault(v => v.Kodval == _pm.Kodcen);
            itypeDlg.SelInvoiceType = _pm.IdInvoiceType.HasValue ? itypeDlg.InvoiceTypesList.FirstOrDefault(t => t.IdInvoiceType == _pm.IdInvoiceType.Value) : null;
            
            // Kpok
            KontrAgent pok = platDlg.KaList.SingleOrDefault(k => k.Kgr == _pm.Kpok);
            if (pok == null)
            {
                pok = repository.GetKontrAgent(_pm.Kpok);
                if (pok != null)
                    platDlg.PopulateKaList(new KontrAgent[] { pok });
            }
            platDlg.SelectedKA = pok;
            //--

            // Grpol
            //SetGrpolTitle();
            KontrAgent grp = grpolDlg.KaList.SingleOrDefault(k => k.Kgr == _pm.Kgr);
            if (grp == null)
            {
                grp = repository.GetKontrAgent(_pm.Kgr);
                if (grp != null)
                    grpolDlg.PopulateKaList(new KontrAgent[] { grp });
            }
            grpolDlg.SelectedKA = grp;
            //--

            //OtgrTransport = repository.GetTransport((short)selKodf.Kodf, PoupVM.SelPoup.Kod);
            SetOtgrTransport();

            CollectDogs();            

            //dogDlg.SelPDogInfo = dogDlg.PDogInfos.SingleOrDefault(d => d.ModelRef.Kdog == _pm.Kdog);

            CollectProducts();
            prodDlg.SelectedProductItem = prodDlg.ProductList.SingleOrDefault(pi => pi.Value.Kpr == _pm.Kpr);          
            CollectAkcStake();
            
            if (_pm.MeasureUnitId > 0 && measureUnits != null && measureUnits.Count > 0)
                selectedMeasureUnit = measureUnits.FirstOrDefault(m => m.Id == _pm.MeasureUnitId);
            //SelectMeasureUnitFromProd();

            //if (allInvoiceTypes.Length > 0)
            //    SelInvoiceType = allInvoiceTypes.SingleOrDefault(it => it.IdInvoiceType == _pm.IdInvoiceType);

            if (allVidcens.Length > 0)
                SelVidcen = allVidcens.SingleOrDefault(vc => vc.Kod == _pm.Vidcen);

            if (AllVidAkcs.Length > 0)
                selectedVidAkc = AllVidAkcs.FirstOrDefault(va => va.Id == newModel.VidAkc);

            if (IsSelectCountry)
                InitCountryVM();

            GetProdNdsStake();
            if (selProdNdsStake.Value != _pm.Prodnds || String.IsNullOrWhiteSpace(selProdNdsStake.Key) && ProdNdsStakes.Any(kv => kv.Value == _pm.Prodnds))
                SelectProdNdsStake(_pm.Prodnds);
            if (_pm.SumNds.HasValue)
                SumNds = _pm.SumNds.Value;

            if (IsRailway)
            {
                KodStFrom = _pm.Stotpr;
                KodStTo = _pm.Stgr;
            }
        }

        private void SaveData()
        {
            if (newModel == null) return;
            if (itypeDlg.SelInvoiceType != null)
                newModel.IdInvoiceType = itypeDlg.SelInvoiceType.IdInvoiceType;
            newModel.Poup = PoupVM.SelPoup.Kod;
            newModel.Pkod = PoupVM.IsPkodEnabled ? PoupVM.SelPkods[0].Pkod : default(short);
            newModel.Kodcen = ValVM.SelVal.Kodval;
            newModel.Kpok = PlatVM.SelectedKA.Kgr;
            newModel.Kgr = GrpolVM.SelectedKA.Kgr;
            newModel.Kdog = DogVM.SelPDogInfo.ModelRef.Kdog;
            newModel.Kpr = ProdVM.SelectedProduct.Kpr;
            newModel.Kodf = (short)SelKodf.Kodf;
            newModel.Vidcen = newModel.Cena != 0 ? SelVidcen.Kod : 0;
            newModel.VidAkc = VidAkc;
            newModel.AkcStake = AkcStake;
            newModel.AkcKodVal = AkcValuta != null ? AkcValuta.Kodval : null; ;
            newModel.KodDav = DogVM.SelPDogInfo.ModelRef.KodDav;
            newModel.PrVzaim = DogVM.SelPDogInfo.ModelRef.Prvzaim;
            newModel.TransportId = otgrTransport != null ? otgrTransport.Id : (short)0;

            newModel.WL_S = !String.IsNullOrEmpty(newModel.KodDav) ? "ДД" : (newModel.PrVzaim == 3 ? "СН" : "ГЗ");
            newModel.Prodnds = SelProdNdsStake.Value;
            newModel.SumNds = SumNds;
            newModel.Kstr = (IsSelectCountry && strDlg != null && strDlg.SelCountry != null) ? (short)strDlg.SelCountry.Kstr
                                                                                             : GrpolVM.SelectedKA.Kstr;
            newModel.IdSpackage = ProdVM.SelectedProduct.IdSpackage;
            newModel.IdProdcen = curProdcen == null ? 0 : curProdcen.IdProdcen;
            if (selectedMeasureUnit == null || selectedMeasureUnit.Id < 0)
                newModel.MeasureUnitId = null;
            else
                newModel.MeasureUnitId = selectedMeasureUnit.Id;
                
            newModel.Density = IsShowDensity ? Density : 0;
            if (!IsShowDensity)
                newModel.Density = 0;
            if (newModel.Dopusl == 0 && newModel.Ndst_dop != 0) newModel.Ndst_dop = 0;
            if (newModel.Sper == 0 && newModel.Ndssper == 0) newModel.Nds = 0;      

            if (IsRailway)
            {
                newModel.Stotpr = stFrom == null ? 0 : stFrom.Kodst;
                newModel.Stgr = stTo == null ? 0 : stTo.Kodst;
            }
            
            if (oldModel != null && !oldModel.DataEqualsTo(newModel))
                newModel.TrackingState = TrackingInfo.Updated;
        }

        //public bool ChangesEnabled
        //{
        //    get { return newModel.SourceId != 0; }
        //}

        public PoupSelectionViewModel PoupVM
        {
            get { return poupDld; }
        }

        public ValSelectionViewModel ValVM
        {
            get { return valDlg; }
        }

        public InvoiceTypeSelectionViewModel InvoiceTypesVM
        {
            get { return itypeDlg; }
        }

        public KaSelectionViewModel PlatVM
        {
            get { return platDlg; }
        }

        public KaSelectionViewModel GrpolVM
        {
            get { return grpolDlg; }
        }

        public PDogListViewModel DogVM
        {
            get { return dogDlg; }
        }

        public ProductSelectionViewModel ProdVM
        {
            get { return prodDlg; }
        }

        public CountrySelectionViewModel CountryVM
        {
            get { return strDlg; }
        }

        private void InitCountryVM()
        {
            if (strDlg == null)
                strDlg = new CountrySelectionViewModel(repository, false);
            int newkstr = newModel.Kstr;
            if (newkstr == 0)
            {
                if (IsRailway && stFrom != null && stTo != null)
                    newkstr = newModel.Stgr == repository.OurKodStan ? stFrom.Kstr : stTo.Kstr;
                if (newkstr == 0 && grpolDlg.SelectedKA != null)
                    newkstr = grpolDlg.SelectedKA.Kstr;
            }
            strDlg.SelectCountryByCode(newkstr);
            NotifyPropertyChanged("CountryVM");
        }

        //private InvoiceType[] allInvoiceTypes;
        //public InvoiceType[] AllInvoiceTypes
        //{
        //    get { return allInvoiceTypes; }
        //    set { SetAndNotifyProperty("AllInvoiceTypes", ref allInvoiceTypes, value); }
        //}

        //private void GetAllInvoiceTypes()
        //{
        //    AllInvoiceTypes = repository.GetInvoiceTypes();
        //}

        //private InvoiceType selInvoiceType;
        //public InvoiceType SelInvoiceType
        //{
        //    get { return selInvoiceType; }
        //    set { SetAndNotifyProperty("SelInvoiceType", ref selInvoiceType, value); }
        //}

        private Vidcen[] allVidcens;
        public Vidcen[] AllVidcens
        {
            get { return allVidcens; }
            set { SetAndNotifyProperty("AllVidcens", ref allVidcens, value); }
        }

        private void GetAllVidcens()
        {
            AllVidcens = repository.GetVidcens();
        }

        private Vidcen selVidcen;
        public Vidcen SelVidcen
        {
            get { return selVidcen; }
            set 
            {
                if (value != selVidcen)
                {
                    selVidcen = value;
                    NotifyPropertyChanged("SelVidcen");
                    SelectCenaAndValFromVidcen();
                    NotifyPropertyChanged("IsCenaEnabled");
                    NotifyPropertyChanged("IsNdsEnabled");
                }
            }
        }

        private void SelectCenaAndValFromVidcen()
        {
            SelectValFromVidcen();
            SelectCenaFromVidcen();
        }
        
        private void SelectValFromVidcen()
        {
            if (selVidcen != null)
            {
                var newval = selVidcen.Kodval.Trim();
                if (!String.IsNullOrEmpty(newval) && ValVM.SelVal != null && newval != ValVM.SelVal.Kodval)
                {
                    var newvm = ValVM.ValList.SingleOrDefault(v => v.Kodval == newval);
                    ValVM.SelVal = newvm;
                }
            }
        }

        private void SelectCenaFromVidcen()
        {
            if (selVidcen != null)
            {
                if (selVidcen.InSprav)
                {
                    GetCena();
                    if (curProdcen != null)
                        SelectProdNdsStake(curProdcen.NdsStake);
                }
                else
                    curProdcen = null;
            }
        }

        private Prodcen curProdcen = null;

        private void GetCena()
        {
            var selprod = ProdVM.SelectedProduct;
            if (selprod != null && selVidcen != null)
            {
                curProdcen = repository.GetCena(selprod.Kpr, selVidcen.Kod, selprod.IdSpackage, Datgr);
                if (curProdcen != null)
                {
                    if (!selVidcen.IncludeNDS)
                        Cena = curProdcen.Cena;
                    else
                    {
                        decimal cenawoNDS = 0;
                        if (curProdcen.NdsStake > 0)
                        {
                            cenawoNDS = curProdcen.NdsTax > 0 ? curProdcen.Cena - curProdcen.NdsTax
                                                         : curProdcen.Cena / ((curProdcen.NdsStake + 100) / 100);
                            SelectProdNdsStake(curProdcen.NdsStake);
                        }
                        else
                            cenawoNDS = curProdcen.Cena / (((selProdNdsStake.Value > 0 ? selProdNdsStake.Value : 0) + 100) / 100);
                        Cena = Math.Round(cenawoNDS, 6);
                    }
                    newModel.IdProdcen = curProdcen.IdProdcen;
                }
            }
        }

        // Доступность регионов редактирования
        public void SetAllRegionsEnable(bool _is)
        {
            IsPoupEdEnabled = IsKodfEdEnabled = IsDocumentNumberEdEnabled = IsDatgrEdEnabled = IsDatnaklEdEnabled = IsPeriodEdEnabled 
                = IsPlatEdEnabled = IsGrpolEdEnabled = IsCountryEdEnabled = IsDogEdEnabled = IsProdEdEnabled = IsKolfEdEnabled 
                = IsVidcenEdEnabled = IsCenaEdEnabled = IsValEdEnabled = IsNdsEdEnabled = IsProvozEdEnabled = IsNvagEdEnabled = IsRwBillEdEnabled
                = IsStFromEdEnabled = IsStToEdEnabled = IsDatacceptEdEnabled = IsDatarrivalEdEnabled = IsDatKursEdEnabled
                = IsVidAkcEdEnabled = IsSumNdsEdEnabled = IsNomavtEdEnabled = IsGnprcEdEnabled = IsMarshrutEdEnabled = IsInvoiceTypeEdEnabled
                = _is;
        }

        public bool IsPoupEdEnabled { get; set; }
        public bool IsKodfEdEnabled { get; set; }
        public bool IsDocumentNumberEdEnabled { get; set; }
        public bool IsDatgrEdEnabled { get; set; }
        public bool IsDatnaklEdEnabled { get; set; }
        public bool IsDatarrivalEdEnabled { get; set; }
        public bool IsDatacceptEdEnabled { get; set; }
        public bool IsPeriodEdEnabled { get; set; }
        public bool IsPlatEdEnabled { get; set; }
        public bool IsGrpolEdEnabled { get; set; }
        public bool IsCountryEdEnabled { get; set; }
        public bool IsDogEdEnabled { get; set; }
        public bool IsProdEdEnabled { get; set; }
        public bool IsKolfEdEnabled { get; set; }
        public bool IsVidcenEdEnabled { get; set; }
        public bool IsCenaEdEnabled { get; set; }
        public bool IsValEdEnabled { get; set; }
        public bool IsDatKursEdEnabled { get; set; }
        public bool IsNdsEdEnabled { get; set; }
        public bool IsProvozEdEnabled { get; set; }
        public bool IsNvagEdEnabled { get; set; }
        public bool IsRwBillEdEnabled { get; set; }
        public bool IsStFromEdEnabled { get; set; }
        public bool IsStToEdEnabled { get; set; }
        public bool IsVidAkcEdEnabled { get; set; }
        public bool IsSumNdsEdEnabled { get; set; }
        public bool IsNomavtEdEnabled { get; set; }
        public bool IsGnprcEdEnabled { get; set; }
        public bool IsMarshrutEdEnabled { get; set; }
        public bool IsInvoiceTypeEdEnabled { get; set; }
        
        //

        private KodfModel[] allKodfs;
        public KodfModel[] AllKodfs
        {
            get { return allKodfs; }
            set { SetAndNotifyProperty("AllKodfs", ref allKodfs, value); }
        }

        private KodfModel[] GetAllMyKodfs(int _poup)
        {
            KodfModel[] res = null;

            var akfs = repository.GetKodfs();
            var mykfs = CommonModule.CommonSettings.GetMyKodfs(_poup);

            if (mykfs != null)
            {
                if (mykfs.Contains(0))
                    res = akfs;
                else
                    res = akfs.Join(mykfs, kf => kf.Kodf, i => i, (kf, i) => kf).ToArray();
            }
            return res;
        }

        private KodfModel selKodf;
        public KodfModel SelKodf
        {
            get { return selKodf; }
            set 
            {
                if (value != selKodf && value != null)
                {
                    selKodf = value;
                    NotifyPropertyChanged("SelKodf");
                    Remember.SetValue("SelKodf", value.Kodf);
                    SetOtgrTransport();
                }
            }
        }

        private void SetOtgrTransport()
        {
            if (selKodf == null)
                OtgrTransport = null;
            else
            {
                var newTransp = repository.GetTransport((short)selKodf.Kodf, PoupVM.SelPoup.Kod);
                if (newTransp == null || otgrTransport == null || newTransp.Id != otgrTransport.Id)
                {
                    OtgrTransport = newTransp;
                    if (oldModel == null || (!IsRailway && !IsTube))
                        SetSperNdsSt();
                }
            }
        }

        private void SetSperNdsSt()
        {
            if (!IsRailway && !IsTube)
            {
                Sper = 0;
                NdsSper = 0;
                NdsStSper = 0;
                NdsStDopusl = 0;
                return;
            }

            decimal newsperndsst = repository.GetNDSByTypeOnDate(NdsTypes.Provoz, Datgr);
            //var newndsItem = new KeyValuePair<string, decimal>(String.Format("Ставка {0:N0} %", newnds), newnds);

            NdsStSper = newsperndsst;
            NdsStDopusl = newsperndsst;
            NdsSper = CalcNds(Sper, newsperndsst);
            NdsDopusl = CalcNds(Dopusl, newsperndsst);
        }

        private decimal CalcNds(decimal _sum, decimal _stake)
        {
            //decimal res = 0;

            //var newsumnds = _sum * _stake / 100;
            //var kodval = ValVM.SelVal == null ? "RB" : ValVM.SelVal.Kodval;
            //res = repository.ConvertSumToVal(newsumnds, kodval, kodval, Datgr);
            var kodval = ValVM.SelVal == null ? "RB" : ValVM.SelVal.Kodval;
            return OtgrHelper.CalcNds(repository, _sum, _stake, kodval, Datgr); 
        }

        private void CalcProduct()
        {
            if (ProdVM.SelectedProduct == null) return;

            var kodval = ValVM.SelVal == null ? "RB" : ValVM.SelVal.Kodval;
            SumProd = OtgrHelper.CalcProduct(repository, Cena, kodval, Kolf, ProdVM.SelectedProduct.IsCena, Datgr);
            var ndsStake = selProdNdsStake.Value;
            if (ndsStake == -1)
                ndsStake = 0;
            SumNds = CalcNds(SumProd, ndsStake);
            //SumNds = repository.ConvertSumToVal(sNds, kodval, kodval, Datgr);
            //SumItog = SumProd + SumNds;
        }

        private decimal sumProd;
        public decimal SumProd 
        {
            get { return sumProd; }
            set { SetAndNotifyProperty("SumProd", ref sumProd, value); } 
        }

        private decimal sumNds;
        public decimal SumNds
        {
            get { return sumNds; }
            set 
            { 
                SetAndNotifyProperty("SumNds", ref sumNds, value);
                SumItog = sumProd + sumNds;
            }
        }

        private decimal sumItog;
        public decimal SumItog
        {
            get { return sumItog; }
            set { SetAndNotifyProperty("SumItog", ref sumItog, value); }
        }


        private Transport otgrTransport;
        public Transport OtgrTransport
        {
            get { return otgrTransport; }
            set
            {
                if (value != otgrTransport)
                {
                    otgrTransport = value;                    
                    if (IsSelectCountry)
                        InitCountryVM();
                    SetGrpolTitle();
                    NotifyNewTransport();
                }
            }
        }

        private void NotifyNewTransport()
        { 
            NotifyPropertyChanged("OtgrTransport");
            NotifyPropertyChanged("IsRailway");
            NotifyPropertyChanged("IsTube");
            NotifyPropertyChanged("IsAvto");
            NotifyPropertyChanged("IsSelectCountry");
        }

        private void SetGrpolTitle()
        {
            string newTitle = @"Получатель / Отправитель";
            if (otgrTransport != null)
                switch (otgrTransport.Direction)
                {
                    case Directions.In: newTitle = "Отправитель"; break;
                    case Directions.Out: newTitle = "Получатель"; break;
                }
            grpolDlg.Title = newTitle;
        }

        public bool IsRailway { get { return otgrTransport != null && otgrTransport.Id == 3; } }
        public bool IsTube { get { return otgrTransport != null && otgrTransport.Id == 2; } }
        public bool IsAvto { get { return otgrTransport != null && (otgrTransport.Id == 1 || otgrTransport.Id == 6 || otgrTransport.Id == 7); } }

        public bool IsSelectCountry { get { return IsAvto || IsRailway; } }

        private List<KeyValuePair<string, decimal>> prodNdsStakes;
        public List<KeyValuePair<string, decimal>> ProdNdsStakes
        {
            get
            {
                if (prodNdsStakes == null)
                    InitProdNdsStakes();
                return prodNdsStakes;
            }
        }

        private KeyValuePair<string, decimal> selProdNdsStake;
        public KeyValuePair<string, decimal> SelProdNdsStake
        {
            get { return selProdNdsStake; }
            set 
            {
                if (value.Value != selProdNdsStake.Value || value.Key != selProdNdsStake.Key)
                {
                    selProdNdsStake = value;
                    NotifyPropertyChanged("SelProdNdsStake");
                    CalcProduct();
                }
            }
        }

        private void InitProdNdsStakes()
        {
            prodNdsStakes = new List<KeyValuePair<string, decimal>>();
            prodNdsStakes.Add(new KeyValuePair<string,decimal>("Ставка 0 %", 0));
            var stakes = repository.GetNdsRatesOnDate(DateTime.Today).Values.Where(v => v != 0).Distinct();
            prodNdsStakes.AddRange(stakes.Select(s => new KeyValuePair<string, decimal>(String.Format("Ставка {0:N0} %", s), s)));
            //prodNdsStakes.Add(new KeyValuePair<string, decimal>("Без НДС", -1));
        }

        private decimal GetProdNdsStake()
        {
            decimal newnds = repository.GetNDSByTypeOnDate(NdsTypes.Product, Datgr);
            
            //var newndsItem = new KeyValuePair<string, decimal>(String.Format("Ставка {0:N0} %", newnds), newnds);

            //if (ProdNdsStakes.Count == 1)
            //    ProdNdsStakes.Add(newndsItem);
            //else
            //    ProdNdsStakes[1] = newndsItem;

            return newnds;
        }

        private void SetNewProdNds()
        {
            var newnds = GetProdNdsStake();
            SelProdNdsStake = ProdNdsStakes.FirstOrDefault(ns => ns.Value == newnds);// ProdNdsStakes[1];
        }

        private void SelectProdNdsStake(decimal _ndsstake)
        {
            if (ProdNdsStakes.Exists(kv => kv.Value == _ndsstake))
                SelProdNdsStake = ProdNdsStakes.SingleOrDefault(kv => kv.Value == _ndsstake);
            else
            {
                var ndsStakeItem = new KeyValuePair<string, decimal>(String.Format("Ставка {0:N0} %", _ndsstake), _ndsstake);
                ProdNdsStakes.Add(ndsStakeItem);
                SelProdNdsStake = ndsStakeItem;
            }
        }

        private bool? isDateRange;
        public bool IsDateRange 
        {
            get { return isDateRange ?? Period == 6; }
            set
            {
                isDateRange = value;
                NotifyPropertyChanged("IsDateRange");
                if (isDateRange.Value)
                    Period = 6;
            }
        }

        protected override void ExecuteSubmit()
        {
            SaveData();
            base.ExecuteSubmit();
        }

        public DateTime Datgr
        {
            get
            {
                return newModel.Datgr;
            }
            set
            {
                if (value != newModel.Datgr)
                {
                    newModel.Datgr = value;
                    if (!IsDateRange)
                        Datnakl = value;
                    NotifyPropertyChanged("Datgr");
                    SetAllNds();
                }
            }
        }

        private void SetAllNds()
        {
            if (oldModel != null) return;
            SetNewProdNds();
            if (IsRailway || IsTube)
                SetSperNdsSt();
        }

        public DateTime Datnakl
        {
            get
            {
                return newModel.Datnakl;
            }
            set
            {
                if (value != newModel.Datnakl)
                {
                    newModel.Datnakl = value;
                    NotifyPropertyChanged("Datnakl");
                }
            }
        }

        public DateTime? Datarrival
        {
            get
            {
                return newModel.Datarrival;
            }
            set
            {
                if (value != newModel.Datarrival)
                {
                    newModel.Datarrival = value;
                    NotifyPropertyChanged("Datarrival");
                }
            }
        }

        public DateTime? Dataccept
        {
            get
            {
                return newModel.Dataccept;
            }
            set
            {
                if (value != newModel.Dataccept)
                {
                    newModel.Dataccept = value;
                    NotifyPropertyChanged("Dataccept");
                }
            }
        }     
        


        public DateTime? DatKurs
        {
            get
            {
                return newModel.DatKurs;
            }
            set
            {
                if (value != newModel.DatKurs)
                {
                    newModel.DatKurs = value;
                    NotifyPropertyChanged("DatKurs");
                }
            }
        }  

        public string DocumentNumber
        {
            get { return newModel.DocumentNumber; }
            set
            {
                if (newModel.DocumentNumber != value)
                {
                    newModel.DocumentNumber = value;
                    NotifyPropertyChanged("DocumentNumber");
                }
            }
        }

        public bool IsDatKursEnabled
        {
            get { return valDlg != null && valDlg.SelVal != null && valDlg.SelVal.Kodval != CommonModule.CommonSettings.OurKodVal; }
        }

        public bool IsCenaEnabled
        {
            get { return IsCenaEdEnabled && selVidcen != null && !selVidcen.InSprav; }
        }

        public bool IsNdsEnabled
        {
            get { return IsNdsEdEnabled && curProdcen == null; }
        }

        public string CenaLabel
        {
            get { return (ProdVM.SelectedProduct != null && !ProdVM.SelectedProduct.IsCena) || Kolf == 0 ? "Сумма" : "Цена"; }
        }

        public decimal Cena
        {
            get { return newModel.Cena; }
            set
            {
                if (value != newModel.Cena)
                {
                    newModel.Cena = value;
                    NotifyPropertyChanged("Cena");
                    CalcProduct();
                }
            }
        }

        public decimal Kolf
        {
            get { return newModel.Kolf; }
            set
            {
                if (value != newModel.Kolf)
                {
                    newModel.Kolf = value;
                    NotifyPropertyChanged("Kolf");
                    NotifyPropertyChanged("CenaLabel");
                    CalcProduct();
                }
            }
        }

        public string RwBillNumber 
        { 
            get { return newModel.RwBillNumber; }
            set 
            {
                if (newModel.RwBillNumber != value)
                {
                    newModel.RwBillNumber = value;
                    NotifyPropertyChanged("RwBillNumber");
                }
            }
        }

        public int Nvag 
        { 
            get { return newModel.Nv; }
            set 
            {
                if (newModel.Nv != value)
                {
                    newModel.Nv = value;
                    var vagInfo = repository.GetVagonInfo(value);
                    newModel.PrSv = vagInfo == null ? false : vagInfo.PrSv;
                    NotifyPropertyChanged("PrSv");
                }
            }
        }

        public bool PrSv { get { return newModel.PrSv; } }

        private int kodStFrom;
        public int KodStFrom
        {
            get { return kodStFrom; }
            set
            {
                if (value > 0)
                {
                    kodStFrom = value;
                    StFrom = repository.GetRailStation(value);
                    NotifyPropertyChanged("KodStFrom");
                }
            }
        }

        private RailStation stFrom;
        public RailStation StFrom
        {
            get { return stFrom; }
            set { SetAndNotifyProperty("StFrom", ref stFrom, value); }
        }

        private int kodStTo;
        public int KodStTo
        {
            get { return kodStTo; }
            set
            {
                if (value > 0)
                {
                    kodStTo = value;
                    StTo = repository.GetRailStation(value);
                    NotifyPropertyChanged("KodStTo");
                    InitCountryVM();
                }
            }
        }

        private RailStation stTo;
        public RailStation StTo
        {
            get { return stTo; }
            set { SetAndNotifyProperty("StTo", ref stTo, value); }
        }

        public bool ProvozSpis
        {
            get { return newModel.Provoz == 1; }
            set
            {
                newModel.Provoz = (short)(value ? 1 : 0);
                NotifyPropertyChanged("ProvozSpis");
            }
        }

        public decimal Sper
        {
            get { return newModel.Sper; }
            set
            {
                if (value != newModel.Sper)
                {
                    newModel.Sper = value;
                    NdsSper = CalcNds(value, NdsStSper);
                    NotifyPropertyChanged("Sper");
                }
            }
        }

        public decimal NdsSper
        {
            get { return newModel.Ndssper; }
            set
            {
                if (value != newModel.Ndssper)
                {
                    newModel.Ndssper = value;
                    NotifyPropertyChanged("NdsSper");
                    if (value == 0)
                        NdsStSper = 0;
                }
            }
        }

        public decimal NdsStSper
        {
            get { return newModel.Nds; }
            set
            {
                if (value != newModel.Nds)
                {
                    newModel.Nds = value;
                    NdsSper = CalcNds(Sper, value);
                    NotifyPropertyChanged("NdsStSper");
                }
            }
        }

        public decimal Dopusl
        {
            get { return newModel.Dopusl; }
            set
            {
                if (value != newModel.Dopusl)
                {
                    newModel.Dopusl = value;
                    NdsDopusl = CalcNds(value, NdsStDopusl);
                    NotifyPropertyChanged("Dopusl");
                }
            }
        }

        public decimal NdsDopusl
        {
            get { return newModel.Ndsdopusl; }
            set
            {
                if (value != newModel.Ndsdopusl)
                {
                    newModel.Ndsdopusl = value;
                    NotifyPropertyChanged("NdsDopusl");
                }
            }
        }

        public decimal NdsStDopusl
        {
            get { return newModel.Ndst_dop; }
            set
            {
                if (value != newModel.Ndst_dop)
                {
                    newModel.Ndst_dop = value;
                    NdsDopusl = CalcNds(Dopusl, value);
                    NotifyPropertyChanged("NdsStDopusl");
                }
            }
        }

        public bool IsShowBought{ get { return prodDlg.SelectedProduct != null && !prodDlg.SelectedProduct.IsService; } }
        public bool Bought 
        { 
            get { return newModel.Bought; }
            set
            {
                if (value != newModel.Bought)
                {
                    newModel.Bought = value;
                    NotifyPropertyChanged("Bought");
                }
            }
        }

        // акциз
        public bool IsShowAkcData{ get { return prodDlg.SelectedProduct != null && prodDlg.SelectedProduct.IdAkcGroup != 0; } }

        public VidAkcModel[] AllVidAkcs { get; set; }

        private VidAkcModel selectedVidAkc;
        public VidAkcModel SelectedVidAkc
        {
            get { return selectedVidAkc; }
            set { SetAndNotifyProperty("SelectedVidAkc", ref selectedVidAkc, value); }
        }

        public int VidAkc
        {
            get { return selectedVidAkc == null ? 0 : selectedVidAkc.Id; }
        }

        private decimal? akcStake;
        public decimal AkcStake
        {
            get
            {
                //if (akcStake == null)
                //    CollectAkcStake();
                return akcStake ?? 0;
            }
            //set { SetAndNotifyProperty("AkcStake", ref akcStake, value); } 
        }

        private void CollectNewProdAkcData()
        {
            var akcProd = prodDlg.SelectedProduct;
            akcStake = 0;
            AkcValuta = null;
            if (akcProd == null || akcProd.IdAkcGroup == 0)
                SelectedVidAkc = null;
            else
            {
                CollectAkcStake();
                if (AllVidAkcs.Length > 0)
                    SelectedVidAkc = AllVidAkcs.FirstOrDefault(va => va.Id == newModel.VidAkc);
            }

        }

        private void CollectAkcStake()
        {

            var akcProd = prodDlg.SelectedProduct;
            akcStake = 0;

            if (akcProd != null && akcProd.IdAkcGroup > 0)
            {
                var akcStakeData = repository.GetAkcStake(akcProd.Kpr, newModel.Datgr);
                if (akcStakeData != null)
                {
                    akcStake = akcStakeData.Value;
                    if (AkcValuta == null || akcStakeData.Key != AkcValuta.Kodval)
                        AkcValuta = repository.GetValutaByKod(akcStakeData.Key);
                }
                else
                    AkcValuta = null;
            }
        }

        public Valuta AkcValuta { get; set; }
        //
        
        public int Period
        {
            get { return newModel.Period; }
            set
            {
                if (value != newModel.Period)
                {
                    newModel.Period = value;
                    NotifyPropertyChanged("Period");
                }
            }
        }
        
        public string Nomavt
        {
            get { return newModel.Nomavt; }
            set
            {
                if (value != newModel.Nomavt)
                {
                    newModel.Nomavt = value;
                    NotifyPropertyChanged("Nomavt");
                }
            }
        }
        
        public string Gnprc
        {
            get { return newModel.Gnprc; }
            set
            {
                if (value != newModel.Gnprc)
                {
                    newModel.Gnprc = value;
                    NotifyPropertyChanged("Gnprc");
                }
            }
        }
        
        public string Marshrut
        {
            get { return newModel.Marshrut; }
            set
            {
                if (value != newModel.Marshrut)
                {
                    newModel.Marshrut = value;
                    NotifyPropertyChanged("Marshrut");
                }
            }
        }

        public bool IsShowDensity { get { return selectedMeasureUnit != null && selectedMeasureUnit.IsNeedDensity; } }
        public decimal Density
        {
            get { return newModel.Density; }
            set
            {
                if (value != newModel.Density)
                {
                    newModel.Density = value;
                    NotifyPropertyChanged("Density");
                }
            }
        }

        public override bool IsValid()
        {
            return base.IsValid() && ValidateOtgruz();
        }

        private bool ValidateOtgruz()
        {
            bool res = true;
            bool tres;
            errors.Clear();

            tres = !IsInvoiceTypeEdEnabled || itypeDlg.SelInvoiceType != null;
            if (!tres)
            {
                res = false;
                errors.Add("Укажите тип документа");
            }

            tres = !IsPoupEdEnabled || PoupVM.IsValid();
            if (!tres)
            {
                res = false;
                errors.Add("Проверьте направление реализации");
            }

            tres = !IsKodfEdEnabled || SelKodf != null;
            if (!tres)
            {
                res = false;
                errors.Add("Не выбрана форма документа");
            }

            tres = !IsPlatEdEnabled || PlatVM.IsValid();
            if (!tres)
            {
                res = false;
                errors.Add("Проверьте плательщика по договору");
            }
            
            tres = !IsGrpolEdEnabled || GrpolVM.IsValid() && GrpolVM.SelectedKA.Kgr > 0;
            if (!tres)
            {
                res = false;
                errors.Add("Проверьте получателя / отправителя");
            }

            tres = !IsDogEdEnabled || DogVM.IsValid();
            if (!tres)
            {
                res = false;
                errors.Add("Проверьте договор");
            }

            tres = !IsProdEdEnabled || ProdVM.IsValid();
            if (!tres)
            {
                res = false;
                errors.Add("Проверьте продукт / услугу");
            }

            tres = !IsValEdEnabled || ValVM.IsValid();
            if (!tres)
            {
                res = false;
                errors.Add("Проверьте валюту");
            }

            tres = !IsVidcenEdEnabled || SelVidcen != null;
            if (!tres)
            {
                res = false;
                errors.Add("Не выбран вид цены");
            }

            tres = !IsDatgrEdEnabled || Datgr.Year >= 2000;
            if (!tres)
            {
                res = false;
                errors.Add("Проверьте дату отгрузки");
            }

            tres = !IsDatnaklEdEnabled || !IsDateRange || Datnakl <= Datgr;
            if (!tres)
            {
                res = false;
                errors.Add("Проверьте начальную дату");
            }


            tres = !IsRailway || !IsRwBillEdEnabled || (!String.IsNullOrWhiteSpace(RwBillNumber) && RwBillNumber.Length <= 8);
            if (!tres)
            {
                res = false;
                errors.Add("Проверьте номер ЖД накладной (до 8 знаков)");
            }
            
            tres =  !IsRailway || !IsNvagEdEnabled || (Nvag > 0 && Nvag <= 99999999);
            if (!tres)
            {
                res = false;
                errors.Add("Проверьте номер вагона (до 8 знаков)");
            }

            tres = !IsRailway || (stFrom != null && stTo != null);//|| !IsNvagEdEnabled 
            if (!tres)
            {
                res = false;
                errors.Add("Проверьте станции отправления / назначения");
            }

            tres = !IsDocumentNumberEdEnabled || !String.IsNullOrWhiteSpace(DocumentNumber);
            if (!tres)
            {
                res = false;
                errors.Add("Проверьте номер документа");
            }

            tres = !IsShowAkcData || selectedVidAkc != null && akcStake > 0 && AkcValuta != null;
            if (!tres)
            {
                res = false;
                errors.Add("Неверная информация по акцизу");
            }

            NotifyPropertyChanged("IsHasErrors");
            NotifyPropertyChanged("Errors");

            return res;
        }

        public bool IsHasErrors { get { return errors.Count > 0; } }

        private List<string> errors = new List<string>();

        public string[] Errors { get { return errors.ToArray(); } }

        /// <summary>
        /// Результат
        /// </summary>
        public OtgrLine NewModel
        {
            get { return newModel; }
        }

        public OtgrLine OldModel
        {
            get { return oldModel; }
        }

    }
}
