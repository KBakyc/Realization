using System;
using System.Collections.Generic;
using System.Linq;
using DataObjects;
using DataObjects.Helpers;
using DataObjects.Events;
using System.Linq.Expressions;
using DataObjects.Interfaces;
using DataObjects.SeachDatas;
using System.Xml.Linq;
using DotNetHelper;
using DataObjects.ESFN;

namespace DAL
{
    public enum Rekvisits { Unp, Okpo }

    /// <summary>
    /// Предоставляет компонентам приложения доступ к данным и операциям с БД.
    /// </summary>
    public class LinqDbService : IDbService
    {
        private static LinqDbService instance = new LinqDbService();        

        private LinqDbService()
        {}

        public static LinqDbService Instance { get { return instance; } }

        public string ConnectionString { get { return Properties.Settings.Default.RealConnectionString; } }
        private bool isReadOnly = Properties.Settings.Default.IsReadOnly;

        public event EventHandler<DataObjects.Events.ErrorEventArgs> OnError;

        private void NotifyOnError(DataObjects.Events.ErrorEventArgs _e)
        {
            _e.Raise(this, ref OnError);
        }

        public bool IsSilent { get; set; }

        /// <summary>
        /// Останов приложения и запись протокола
        /// </summary>
        /// <param name="_mess"></param>
        private void OnCrash(string _type, string _mess)
        {
            //LogToFile(null, String.Format("{0} : {1}", _type, _mess));
            if (!IsSilent)
            {
                if (OnError != null)
                    NotifyOnError(new ErrorEventArgs(_type, _mess));
                else
                    System.Windows.MessageBox.Show(_mess, _type);
            }
        }

        //private bool OnCrashRetry(string _type, string _mess)
        //{
        //    if (OnError != null)
        //        NotifyOnError(new ErrorEventArgs(_mess, _type, true));
        //    //LogToFile(null, String.Format("{0} : {1}", _type, _mess));
        //    var res = System.Windows.MessageBox.Show(_mess + "\nПовторить попытку синхронизации?", _type, System.Windows.MessageBoxButton.YesNo);
        //    return res == System.Windows.MessageBoxResult.Yes ? true : false;
        //}

        private bool? isOnline;
        public bool CheckOnlineStatus()
        {
            if (isOnline == null)
            {
                isOnline = false;
                using (var dc = new RealizationDCDataContext())
                    isOnline = dc.DatabaseExists();
            }
            return isOnline.Value;
        }

        /// <summary>
        /// Список прав доступа пользователя на компоненты
        /// </summary>
        private bool isUserComponentsAclLoaded;
        public bool IsUserComponentsAclLoaded
        {
            get { return isUserComponentsAclLoaded; }
            set { isUserComponentsAclLoaded = value; }
        }

        private Dictionary<string, int> userComponentsAcl;
        public Dictionary<string, int> UserComponentsAcl
        {
            get
            {
                if (!isUserComponentsAclLoaded && CheckOnlineStatus())
                {
                    using (var l_dc = new RealizationDCDataContext())
                        try
                        {
                            userComponentsAcl = l_dc.usp_GetUserComponents(UserToken).ToDictionary(r => r.ComponentTypeName, r => r.AccessLevel ?? 0);
                        }
                        catch (Exception e)
                        {
                            OnCrash(e.GetType().ToString(), e.Message);
                        }
                    isUserComponentsAclLoaded = true;
                }
                return userComponentsAcl;
            }
        }

        /// <summary>
        /// Возвращает уровень доступа к компоненту по его имени типа
        /// </summary>
        /// <param name="_cname"></param>
        /// <returns></returns>
        public int GetComponentAccess(string _cname)
        {
            if (String.IsNullOrEmpty(_cname))
                throw new ArgumentOutOfRangeException("_cname", "Имя компонента не должно быть пустым");

            if (UserComponentsAcl == null) return 0;

            int res;
            string ModuleName = _cname.Split(new char[] { '.' }).First();
            bool isModule = ModuleName == _cname;

            if (!UserComponentsAcl.TryGetValue(_cname, out res))
                if (!isModule && !UserComponentsAcl.TryGetValue(ModuleName, out res))
                    res = 0;
            return res;
        }

        // возвращает список валют
        public Valuta[] GetValutes()
        {
            Valuta[] res = null;
            using (var l_dc = new RealizationDCDataContext())
                try
                {
                    res = l_dc.uv_Valutas
                              .Where(v => v.IsActive > 0)
                              .Select(v => new Valuta(v.Kodval, v.NameVal, v.ShortName)).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            return res;
        }

        //кэш для подвидов реализации
        private Dictionary<int, Dictionary<short, PkodModel>> pkodsCache;

        public PkodModel[] GetPkods(int _poup)
        {
            PkodModel[] res = null;

            if (pkodsCache == null)
                LoadPkodsCacheFromDb();

            if (pkodsCache != null && _poup != 0 && pkodsCache.ContainsKey(_poup) 
                                                 && pkodsCache[_poup] != null)
                res = pkodsCache[_poup].Values.ToArray();

            return res;
        }

        public PkodModel GetPkod(int _poup, short _pkod)
        {
            PkodModel res = null;

            if (pkodsCache == null)
                LoadPkodsCacheFromDb();

            if (pkodsCache != null && _poup != 0 && pkodsCache.ContainsKey(_poup)
                                                 && pkodsCache[_poup] != null
                                                 && pkodsCache[_poup].ContainsKey(_pkod)
                )
                res = pkodsCache[_poup].Values.SingleOrDefault(pk => pk.Pkod == _pkod);

            return res;
        }

        private void LoadPkodsCacheFromDb()
        {
            using (var l_dc = new RealizationDCDataContext())
                try
                {
                    pkodsCache = l_dc.uv_Pkods.GroupBy(pk => pk.poup)
                        .ToDictionary(g => (int)g.Key, g => g.ToDictionary(pk => (short)pk.pkod, 
                                                                      pk => new PkodModel((short)pk.pkod)
                                                                      {
                                                                          Poup = (short)pk.poup,
                                                                          Name = pk.naipoup.Trim(),
                                                                          ShortName = pk.naishort.Trim(),
                                                                          Kpr = pk.kpr
                                                                      }));
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            foreach (var poup in pkodsCache.Keys)
                if (Poups.ContainsKey(poup))
                    Poups[poup].IsPkodsEnabled = true;
        }

        //кэш для валют
        private readonly Dictionary<string, Valuta> valList = new Dictionary<string, Valuta>();

        // возвращает валюту по коду
        public Valuta GetValutaByKod(string _kod)
        {
            Valuta ret = null;
            if (!String.IsNullOrWhiteSpace(_kod))
                if (!valList.ContainsKey(_kod))
                {
                    using (var l_dc = new RealizationDCDataContext())
                    {
                        try
                        {
                            ret = l_dc.uv_Valutas.Where(v => v.Kodval == _kod)
                                .Select(v => new Valuta(v.Kodval, v.NameVal, v.ShortName))
                                .SingleOrDefault();
                        }
                        catch (Exception e)
                        {
                            OnCrash(e.GetType().ToString(), e.Message);
                        }
                    }
                    if (ret == null)
                    {
                        string skod = "[" + _kod.Trim() + "]";
                        ret = new Valuta(_kod, "Неизв. валюта " + skod, skod);
                    }
                    valList[_kod] = ret;
                }
                else
                    ret = valList[_kod];
            return ret;
        }

        // возвращает реестр отгрузки
        public OtgrLine[] GetTempOtgr()
        {
            OtgrLine[] res = null;
            using (var l_dc = new RealizationDCDataContext())
                try
                {
                    res = (from tr in l_dc.temp_p623s
                           where tr.USERTOKEN == UserToken
                           orderby tr.DocumentNumber, tr.RwBillNumber, tr.NV
                           select new OtgrLine(tr.id)
                           {    
                               IdInvoiceType = tr.idInvoiceType,
                               DocumentNumber = tr.DocumentNumber,
                               RwBillNumber = tr.RwBillNumber,
                               Idrnn = tr.IDRNN,
                               Poup = tr.POUP ?? 0,
                               Pkod = tr.PKOD ?? 0,
                               Nv = tr.NV ?? 0,
                               Stotpr = tr.STOTPR ?? 0,
                               Stgr = tr.STGR ?? 0,
                               Kpok = tr.KPOK ?? 0,
                               Kgr = tr.KGR ?? 0,
                               Kdog = tr.KDOG ?? 0,
                               Datgr = tr.DATGR.GetValueOrDefault(),
                               Datnakl = tr.DATNAKL ?? tr.DATGR.GetValueOrDefault(),
                               Kodf = tr.KODF ?? 0,
                               Kolf = tr.KOLF ?? 0,
                               Cena = tr.CENA ?? 0,
                               Prodnds = tr.PRODNDS ?? 0,
                               SumNds = tr.sumnds,
                               Kodcen = tr.KODCEN,
                               Kpr = tr.KPR ?? 0,
                               Sper = tr.SPER ?? 0,
                               Nds = tr.NDS ?? 0,
                               Ndssper = tr.NDSSPER ?? 0,
                               Dopusl = tr.DOPUSL ?? 0,
                               Ndst_dop = tr.NDST_DOP ?? 0,
                               Ndsdopusl = tr.NDSDOPUSL ?? 0,
                               Provoz = tr.PROVOZ ?? 0,
                               TransportId = tr.TransportId ?? 0,
                               WL_S = tr.WL_S,
                               KodDav = tr.KODDAV,
                               Kstr = tr.KSTR ?? 0,
                               IdSpackage = tr.IDSPACKAGE ?? 0,
                               IdProdcen = tr.IDPRODCEN ?? 0,
                               PrVzaim = tr.PRVZAIM ?? 0,
                               IsChecked = tr.CHECKED ?? false,
                               Period = tr.PERIOD ?? 0,
                               Nomavt = tr.NOMAVT,
                               Gnprc = tr.GNPRC,
                               Marshrut = tr.MARSHRUT,
                               AkcStake = tr.AkcStake ?? 0,
                               AkcKodVal = tr.AkcKodVal,
                               VidAkc = tr.VIDAKC ?? 0,
                               IdSpurpose = tr.IDSPURPOSE ?? 0,
                               IdVozv = tr.IDVOZV ?? 0,
                               Maker = tr.MAKER ?? 0,
                               KodRaznar = tr.KodRaznar ?? 0,
                               TrackingState = TrackingInfo.Unchanged,
                               StatusMsgs = String.IsNullOrEmpty(tr.ErrorMsg) ? null : tr.ErrorMsg.Split(new char[] { ';' }),
                               StatusType = tr.ErrorType ?? 0
                           }).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            return res;
        }

        /// <summary>
        /// Подтверждение принятия отгрузки
        /// </summary>
        /// <param name="_chRows"></param>
        public void AcceptP623(OtgrLine[] _chRows)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return;
            }

            if (_chRows == null || _chRows.Length == 0) return;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    bool needsubmit = _chRows.Any(r => r.TrackingState == TrackingInfo.Updated);
                    foreach (var cr in _chRows.Where(r => r.TrackingState == TrackingInfo.Updated))
                    {
                        temp_p623 tr = l_dc.temp_p623s.Single(r => r.id == cr.Idp623);
                        tr.CHECKED = cr.IsChecked;

                    }
                    if (needsubmit)
                        l_dc.SubmitChanges();
                    l_dc.usp_AcceptOtgruz();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
        }

        // ID пользователя
        private int? s_usertoken;
        public int UserToken
        {
            get
            {
                if (s_usertoken == null)
                    s_usertoken = GetUserToken();
                return s_usertoken.Value;
            }
        }

        /// <summary>
        /// Возвращает текущий ID пользователя базы
        /// </summary>
        /// <returns></returns>
        private int GetUserToken()
        {
            int tok = 0;
            if (CheckOnlineStatus())
                using (var l_dc = new RealizationDCDataContext())
                {
                    try
                    {
                        tok = l_dc.uf_GetCurrentUserToken().GetValueOrDefault();
                    }
                    catch (Exception e)
                    {
                        OnCrash(e.GetType().ToString(), e.Message);
                    }
                }
            return tok;
        }

        public bool UpdateUserInfo(int _id, UserInfo _userInfo)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return false;
            }

            if (_userInfo == null || (_userInfo.Id == 0 && _id == 0)) return false;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    if (_id != 0)
                    {
                        var inDB = l_dc.ARM_Users.SingleOrDefault(i => i.id == _id);
                        if (inDB == null) return false;
                        
                        if (_userInfo.Id == 0)
                            l_dc.ARM_Users.DeleteOnSubmit(inDB);
                        else
                        {                            
                            inDB.id = _userInfo.Id;
                            inDB.UserName = _userInfo.Name;
                            inDB.FullName = _userInfo.FullName;
                            inDB.TabNum = _userInfo.TabNum;
                            inDB.Cex = _userInfo.Ceh;
                            inDB.EmailAddress = _userInfo.EmailAddress;
                            inDB.IsEnabled = _userInfo.IsEnabled;
                            inDB.IsSystem = _userInfo.IsSystem;
                            inDB.SecurityContext = _userInfo.Context;
                            inDB.ClientInfo = _userInfo.ClientInfo;                            
                            if (userInfoCache.ContainsKey(_userInfo.Id)) userInfoCache.Remove(_userInfo.Id);                            
                        }
                        if (_id != _userInfo.Id && userInfoCache.ContainsKey(_id)) userInfoCache.Remove(_id);
                        l_dc.SubmitChanges();
                        return true;
                    }
                    else
                    {
                        var newUser = new ARM_User() 
                        {
                            id = _userInfo.Id,
                            UserName = _userInfo.Name,
                            FullName = _userInfo.FullName,
                            TabNum = _userInfo.TabNum,
                            Cex = _userInfo.Ceh,
                            EmailAddress = _userInfo.EmailAddress,
                            IsEnabled = _userInfo.IsEnabled,
                            IsSystem = _userInfo.IsSystem,
                            SecurityContext = _userInfo.Context,
                            ClientInfo = _userInfo.ClientInfo
                        };
                        l_dc.ARM_Users.InsertOnSubmit(newUser);
                        l_dc.SubmitChanges();
                        return true;
                    }

                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }

            return false;
        }

        private Dictionary<int, UserInfo> userInfoCache = new Dictionary<int, UserInfo>();
        
        public UserInfo GetUserInfo(string _login)
        {
            if (String.IsNullOrWhiteSpace(_login)) return null;

            var cached = userInfoCache.Values.FirstOrDefault(u => u.Name == _login);
            if (cached != null) return cached;
            
            return GetUserInfo(u => u.UserName == _login).FirstOrDefault();;
        }

        public UserInfo GetUserInfo(int _id)
        {
            if (_id <= 0) return null;
            if (_id != UserToken && userInfoCache.ContainsKey(_id)) return userInfoCache[_id];            
                        
            return GetUserInfo(u => u.id == _id).FirstOrDefault();
        }

        private UserInfo[] GetUserInfo(Expression<Func<ARM_User, bool>> _filter)
        {
            UserInfo[] res = null;
            if (CheckOnlineStatus())
                using (var l_dc = new RealizationDCDataContext())
                {
                    try
                    {
                        var query = _filter == null ? l_dc.ARM_Users : l_dc.ARM_Users.Where(_filter);
                        res = query.Select(u => new UserInfo()
                        {
                            Id = u.id,
                            Name = u.UserName,
                            FullName = u.FullName,
                            TabNum = u.TabNum,
                            Ceh = u.Cex,
                            EmailAddress = u.EmailAddress,
                            ClientInfo = u.ClientInfo,
                            IsEnabled = u.IsEnabled,
                            IsSystem = u.IsSystem,
                            Context = u.SecurityContext
                        }).ToArray();
                    }
                    catch (Exception e)
                    {
                        OnCrash(e.GetType().ToString(), e.Message);
                    }
                }
            if (res != null && res.Length > 0)
                Array.ForEach(res, i => userInfoCache[i.Id] = i);
            return res;
        }

        public UserInfo[] GetAllUserInfos()
        {
            UserInfo[] res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.ARM_Users.Select(u => new UserInfo()
                    {
                        Id = u.id,
                        Name = u.UserName,
                        FullName = u.FullName,
                        TabNum = u.TabNum,
                        Ceh = u.Cex,
                        EmailAddress = u.EmailAddress,
                        ClientInfo = u.ClientInfo,
                        IsEnabled = u.IsEnabled,
                        IsSystem = u.IsSystem,
                        Context = u.SecurityContext
                    }).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
                return res;
            }
        }

        public UserInfoExt GetUserInfoExt(int _userid)
        {
            UserInfoExt res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var data = l_dc.usp_GetUserInfoExt(_userid).FirstOrDefault();
                    if (data != null)
                        res = new UserInfoExt { Position = data.position };
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        //кэш для контрагентов
        private readonly Dictionary<int, KontrAgent> kontrAgents = new Dictionary<int, KontrAgent>();
        public KontrAgent GetKontrAgent(int kgr)
        {
            KontrAgent l_ka = null;
            if (!kontrAgents.ContainsKey(kgr))
            {
                using (var l_dc = new RealizationDCDataContext())
                {
                    try
                    {
                        l_ka = l_dc.usp_GetKontragentByCode(kgr)
                                    .Select(k => new KontrAgent()
                                                     {
                                                         Kgr = (int)k.kgr,
                                                         Name = k.name,
                                                         FullName = k.fullname,
                                                         Address = k.address,
                                                         Okpo = k.okpo,                                                         
                                                         Kpp = k.kpp,
                                                         Inn = k.inn,
                                                         Kstr = (short)(k.kstr ?? 0),
                                                         City = k.gor,
                                                         Country = k.nstr
                                                     })
                                    .SingleOrDefault();
                    }
                    catch (Exception e)
                    {
                        OnCrash(e.GetType().ToString(), e.Message);
                    }
                }
                if (l_ka == null)
                {
                    l_ka = new KontrAgent
                    {
                        Kgr = kgr,
                        Name = "!!! - Контрагент не найден - !!!"
                    };
                }
                else
                    kontrAgents[kgr] = l_ka;
            }
            else
                l_ka = kontrAgents[kgr];

            return l_ka;
        }

        /// <summary>
        /// Возвращает контрагента - владельца сырья по счёту
        /// </summary>
        /// <param name="_idsf"></param>
        /// <returns></returns>
        public KontrAgent GetSfResourceOwner(int _idsf)
        {
            KontrAgent res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.uf_GetWlsInfo(_idsf)
                                .Select(k => new KontrAgent()
                                {
                                    Kgr = (int)k.kgr,
                                    Name = k.name,
                                    FullName = k.fullname,
                                    Address = k.address,
                                    Okpo = k.okpo,
                                    Kpp = k.kpp,
                                    Inn = k.inn,
                                    Kstr = (short)(k.kstr ?? 0),
                                    City = k.gor,
                                    Country = k.nstr
                                })
                                .SingleOrDefault();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        /// <summary>
        /// Возвращает контрагентов с кодами, начинающимися с указанных цифр
        /// </summary>
        /// <param name="_kgrpat"></param>
        /// <returns></returns>
        public KontrAgent[] GetKontrAgentsByCodePat(int _kgrpat)
        {
            KontrAgent[] res = null;
            using (var l_dc = new RealizationDCDataContext())
                try
                {
                    res = l_dc.usp_GetKontragentByCodeA(_kgrpat)
                        .Select(k => new KontrAgent()
                        {
                            Kgr = (int)k.kgr,
                            Name = k.name,
                            FullName = k.fullname,
                            Address = k.address,
                            Okpo = k.okpo,
                            Kpp = k.kpp,
                            Inn = k.inn,
                            Kstr = (short)(k.kstr ?? 0),
                            City = k.gor,
                            Country = k.nstr
                        }).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            return res;
        }

        /// <summary>
        /// Возвращает контрагентов с названиями, включающими указанную строку
        /// </summary>
        /// <param name="_namepat"></param>
        /// <returns></returns>
        public KontrAgent[] GetKontrAgentsByNamePat(string _namepat)
        {
            KontrAgent[] res = null;
            using (var l_dc = new RealizationDCDataContext())
                try
                {
                    res = l_dc.usp_GetKontragentByNameA(_namepat)
                        .Select(k => new KontrAgent()
                        {
                            Kgr = (int)k.kgr,
                            Name = k.name,
                            FullName = k.fullname,
                            Address = k.address,
                            Okpo = k.okpo,
                            Kpp = k.kpp,
                            Inn = k.inn,
                            Kstr = (short)(k.kstr ?? 0),
                            City = k.gor,
                            Country = k.nstr
                        }).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            return res;
        }

        //кэш для названий продуктов
        private readonly Dictionary<int, ProductInfo> products = new Dictionary<int, ProductInfo>();
        public ProductInfo GetProductInfo(int kpr)
        {
            ProductInfo l_pr = null;
            if (!products.ContainsKey(kpr))
            {
                l_pr = GetProductsByFilter(p => p.kpr == kpr).FirstOrDefault();
                
                if (l_pr != null)
                    products[kpr] = l_pr;
            }
            else
                l_pr = products[kpr];

            return l_pr;
        }

        private Dictionary<int, PoupModel> poups;
        public Dictionary<int, PoupModel> Poups
        {
            get
            {
                if (poups == null)
                {
                    using (var l_dc = new RealizationDCDataContext())
                        try
                        {
                            poups = l_dc.uv_Moups
                                .Select(p => new PoupModel()
                                {
                                    Kod = (short)p.kodpoup,
                                    Name = p.naipoup.Trim(),
                                    PlatName = p.plat,
                                    ShortName = p.npoup.Trim(),
                                    PayDoc = (PayDocTypes)(p.paydoc ?? 0),
                                    IsAkciz = p.isakciz,
                                    IsDogExp = p.isdogexp ?? false,
                                    IsActive = p.active,
                                    IsDav = p.isdav
                                }).ToDictionary(p => p.Kod);
                        }
                        catch (Exception e)
                        {
                            OnCrash(e.GetType().ToString(), e.Message);
                        }
                }
                return poups;
            }
        }

        private Dictionary<int, VidAkcModel> vidAkcs;
        public VidAkcModel[] GetVidAkcs()
        {
            VidAkcModel[] res = null;

            if (vidAkcs == null)
                CollectVidAkcsFromDb();            
            if (vidAkcs != null)
                res = vidAkcs.Values.ToArray();
          
            return res;
        }
        
        public VidAkcModel GetVidAkc(int _id)
        {
            VidAkcModel res = null;
            
            if (vidAkcs == null)
                CollectVidAkcsFromDb();
            if (vidAkcs != null && vidAkcs.ContainsKey(_id))
                res = vidAkcs[_id];

            return res;
        }

        private void CollectVidAkcsFromDb()
        {
            using (var l_dc = new RealizationDCDataContext())
                try
                {
                    vidAkcs = l_dc.uv_Svidakcs
                        .Select(d => new VidAkcModel
                        {
                            Id = d.VIDAKC,
                            Name = d.NAIM
                        }).ToDictionary(a => a.Id);
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
        }

        // формирование реестра отгрузки
        public void MakeTempP623(int _poup, short _pkod, DateTime _dateFrom, DateTime _dateTo)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return;
            }

            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    l_dc.CommandTimeout = 180;
                    l_dc.usp_CollectOtgruz(_poup, _pkod, _dateFrom, _dateTo);
                    l_dc.usp_CheckOtgruz();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
        }

        public OtgrLine GetOtgrLine(long _id, bool _inRealiz)
        {
            var oschdata = new OtgruzSearchData() { Id = _id , InRealiz = _inRealiz};
            return GetOtgrArc(oschdata).SingleOrDefault();
        }

        private OtgrLine[] GetOtgrArc(Expression<Func<uv_RealOtgrArc,bool>> _filt)
        {
            if (_filt == null) return null;

            OtgrLine[] res = null;

            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var data = l_dc.uv_RealOtgrArcs.Where(_filt);
                    res = OtgrDataToModel(data).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        public OtgrLine[] GetOtgrArc(OtgruzSearchData _schData)
        {
            if (_schData == null) return null;
            if (!_schData.InRealiz)
                return GetOtgrArcNotInRealiz(_schData);

            long? idp623 = _schData.Id;
            if (idp623.HasValue && idp623.Value != 0)
                return GetOtgrArc(o => o.idp623 == idp623);

            long? idrnn = _schData.IdRnn;
            
            //int? tn2 = _schData.Tn2;
            //int? rnn = _schData.Rnn;

            int? type = _schData.InvoiceTypeId;
            string docnum = _schData.DocumentNumber;
            string rwnum = _schData.RwBillNumber;
            
            int? nv = _schData.Nv; 
            DateTime? dfrom = _schData.Dfrom; 
            DateTime? dto = _schData.Dto;
            short? transportid = _schData.Transportid;
            int? poup = _schData.Poup;
            short? pkod = _schData.Pkod;
            int? kdog = _schData.Kdog;
            int? kpok = _schData.Kpok;
            int? kgr = _schData.Kgr;
            int? kpr = _schData.Kpr;

            Expression<Func<uv_RealOtgrArc, bool>> filter;

            List<Expression> exprList = new List<Expression>();

            var paramExpr = Expression.Parameter(typeof(uv_RealOtgrArc), "o");

            //
            //uv_RealOtgrArc test; test.KPOK
            //

            if (idrnn.HasValue && idrnn.Value != 0)
            {
                var idrnnMember = Expression.Property(paramExpr, "IDRNN");
                var idrnnData = Expression.Constant(idrnn.Value, typeof(long));
                var idrnnExpr = Expression.Equal(idrnnMember, idrnnData);
                exprList.Add(idrnnExpr);
            }

            if (poup.HasValue && poup.Value != 0)
            {
                var poupMember = Expression.Property(paramExpr, "POUP");
                var poupData = Expression.Constant(poup, typeof(int?));
                var poupExpr = Expression.Equal(poupMember, poupData);
                exprList.Add(poupExpr);
            }
            
            if (pkod.HasValue && pkod.Value != 0)
            {
                var pkodMember = Expression.Property(paramExpr, "PKOD");
                var pkodData = Expression.Constant(pkod, typeof(short?));
                var pkodExpr = Expression.Equal(pkodMember, pkodData);
                exprList.Add(pkodExpr);
            }

            if (type.HasValue && type.Value != 0)
            {
                var typeMember = Expression.Property(paramExpr, "idInvoiceType");
                var typeData = Expression.Constant(type, typeof(int?));
                var typeExpr = Expression.Equal(typeMember, typeData);
                exprList.Add(typeExpr);
            }

            var method_EndsWith = typeof(String).GetMethods().Where(m => m.Name == "EndsWith" && !m.IsStatic && m.GetParameters().Length == 1).FirstOrDefault();//.MakeGenericMethod(typeof(int));
            if (!String.IsNullOrWhiteSpace(docnum))
            {
                var dnMember = Expression.Property(paramExpr, "DocumentNumber");
                var dnData = Expression.Constant(docnum, typeof(string));
                var dnExpr = Expression.Call(dnMember, method_EndsWith, dnData);
                exprList.Add(dnExpr);
                exprList.Add(dnExpr);
            }

            if (!String.IsNullOrWhiteSpace(rwnum))
            {
                var rnMember = Expression.Property(paramExpr, "RwBillNumber");
                var rnData = Expression.Constant(rwnum, typeof(string));
                var rnExpr = Expression.Call(rnMember, method_EndsWith, rnData);
                exprList.Add(rnExpr);
            }            

            if (nv.HasValue && nv.Value != 0)
            {
                var nvMember = Expression.Property(paramExpr, "NV");
                var nvData = Expression.Constant(nv, typeof(int?));
                var nvExpr = Expression.Equal(nvMember, nvData);
                exprList.Add(nvExpr);
            }

            if (kdog.HasValue && kdog.Value != 0)
            {
                var kdogMember = Expression.Property(paramExpr, "KDOG");
                var kdogData = Expression.Constant(kdog, typeof(int?));
                var kdogExpr = Expression.Equal(kdogMember, kdogData);
                exprList.Add(kdogExpr);
            }

            if (kpok.HasValue && kpok.Value != 0)
            {
                var kpokMember = Expression.Property(paramExpr, "KPOK");
                var kpokData = Expression.Constant(kpok, typeof(int?));
                var kpokExpr = Expression.Equal(kpokMember, kpokData);
                exprList.Add(kpokExpr);
            }

            if (kgr.HasValue && kgr.Value != 0)
            {
                var kgrMember = Expression.Property(paramExpr, "KGR");
                var kgrData = Expression.Constant(kgr, typeof(int?));
                var kgrExpr = Expression.Equal(kgrMember, kgrData);
                exprList.Add(kgrExpr);
            }
            
            if (kpr.HasValue && kpr.Value != 0)
            {
                var kprMember = Expression.Property(paramExpr, "KPR");
                var kprData = Expression.Constant(kpr, typeof(int?));
                var kprExpr = Expression.Equal(kprMember, kprData);
                exprList.Add(kprExpr);
            }

            if (dfrom.HasValue || dto.HasValue)
            {
                var datgrMember = Expression.Property(paramExpr, "DATGR");
                if (dfrom.HasValue)
                {
                    var dfromData = Expression.Constant(dfrom, typeof(DateTime?));
                    var dfromExpr = Expression.GreaterThanOrEqual(datgrMember, dfromData);
                    exprList.Add(dfromExpr);
                }
                if (dto.HasValue)
                {
                    var dtoData = Expression.Constant(dto, typeof(DateTime?));
                    var dtoExpr = Expression.LessThanOrEqual(datgrMember, dtoData);
                    exprList.Add(dtoExpr);
                }
            }

            if (transportid.HasValue && transportid.Value != 0)
            {
                var trMember = Expression.Property(paramExpr, "TransportId");
                var trData = Expression.Constant(transportid, typeof(short?));
                var trExpr = Expression.Equal(trMember, trData);
                exprList.Add(trExpr);
            }

            if (exprList.Count == 0) return null;

            Expression combiExpr = exprList[0];

            for (int i = 1; i < exprList.Count; i++)
                combiExpr = Expression.AndAlso(combiExpr, exprList[i]);

            filter = Expression.Lambda<Func<uv_RealOtgrArc, bool>>(combiExpr, paramExpr);

            var otgr = GetOtgrArc(filter);

            return otgr;
        }

        private OtgrLine[] GetOtgrArcNotInRealiz(OtgruzSearchData _schData)
        {
            OtgrLine[] res = null;

            long? id = _schData.Id;
            
            int? nv = _schData.Nv;            
            string docnum = _schData.DocumentNumber;
            string rwnum = _schData.RwBillNumber;
            
            DateTime? dfrom = _schData.Dfrom;
            DateTime? dto = _schData.Dto;
            short? transportid = _schData.Transportid;
            int? poup = _schData.Poup;
            short? pkod = _schData.Pkod;
            int? kdog = _schData.Kdog;
            int? kpok = _schData.Kpok;
            int? kgr = _schData.Kgr;

            using (var l_dc = new RealizationDCDataContext())
               {
                try
                {
                    res = l_dc.usp_GetOtgruzNotInRealiz(id, docnum, rwnum, nv, dfrom, dto, poup, pkod, kdog, kpok, kgr)
                        .Select(d => new OtgrLine()
                        {
                            IdInvoiceType = d.IdInvoiceType,
                            DocumentNumber = d.DocumentNumber,
                            RwBillNumber = d.RwBillNumber,
                            Idrnn = d.Idrnn,
                            Series = d.Series,
                            Nv = (int)(d.Nv ??0),
                            Kpok = (int)d.Kpok,
                            Kgr = (int)d.Kgr,
                            Kdog = (int)d.Kdog,
                            Datgr = d.Datgr,
                            Datnakl = d.Datnakl ?? d.Datgr,
                            Dataccept = d.Dataccept,
                            Datarrival = d.Datarrival,
                            Datdrain = d.Datdrain,
                            DeliveryDate = d.DeliveryDate,
                            Kodf = (short)d.Kodf,
                            Poup = d.Poup ?? 0,
                            Pkod = (short)d.Pkod,
                            Kolf = d.Kolf,
                            Vidcen = (int)(d.Vidcen ?? 0),
                            Cena = d.Cena ?? 0,
                            Prodnds = d.Prodnds ?? 0,
                            SumNds = d.SumNds,
                            Kodcen = d.Kodcen,
                            Kpr = (int)d.Kpr,
                            Sper = d.Sper ?? 0,
                            Nds = d.Nds ?? 0,
                            Ndssper = d.Ndssper ?? 0,
                            Dopusl = d.Dopusl ?? 0,
                            Ndst_dop = d.Ndst_dop ?? 0,
                            Ndsdopusl = d.Ndsdopusl ?? 0,
                            Provoz = (short)(d.Provoz ?? 0),
                            Stgr = (int)(d.Stgr ?? 0),
                            Stotpr = (int)(d.Stotpr ?? 0),
                            TransportId = d.TransportId ?? 0,
                            WL_S = d.WL_S,
                            KodDav = d.KodDav,
                            Kstr = (short)d.Kstr,
                            IdSpackage = (short)(d.IdSpackage ?? 0),
                            IdProdcen = (int)(d.IdProdcen ?? 0),
                            PrVzaim = (short)(d.PrVzaim ?? 0),
                            SourceId = (short)d.SourceId,
                            Period = d.Period,
                            Nomavt = d.Nomavt,
                            Gnprc = d.Gnprc,
                            Marshrut = d.Marshrut,
                            Ndov = d.Ndov,
                            Fdov = d.Fdov,
                            DatDov = d.DatDov,
                            Bought = d.Bought ?? false,
                            VidAkc = (int)d.VidAkc,
                            AkcStake = d.AkcStake,
                            AkcKodVal = d.AkcKodVal,
                            IdSpurpose = (short?)d.IdSpurpose,
                            IdAct = d.IdAct,
                            IdVozv = d.IdVozv,
                            Maker = (int)(d.Maker ?? 0),
                            KodRaznar = (int)(d.KodRaznar ?? 0),
                            MeasureUnitId = d.MeasureUnitId,
                            Density = d.Density ?? 0M,
                            TrackingState = TrackingInfo.Unchanged
                        }).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }

            return res;
        }

        /// <summary>
        /// Выборка из архива отгрузки по счёту
        /// </summary>
        /// <param name="_idsf"></param>
        /// <returns></returns>
        public OtgrLine[] GetOtgrArc(int _idsf)
        {
            OtgrLine[] res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var oids = l_dc.usp_GetSfOtgruz(_idsf).Select(r => r.idp623).ToArray();
                    var data = l_dc.uv_RealOtgrArcs.Where(o => oids.Contains(o.idp623)).ToArray();
                    res = OtgrDataToModel(data).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        private IEnumerable<OtgrLine> OtgrDataToModel(IEnumerable<uv_RealOtgrArc> _data)
        {
            IEnumerable<OtgrLine> res = null;
            if (_data != null)
                res = _data.Select(tr => new OtgrLine((int)tr.idp623)
                                 {
                                     IdInvoiceType = tr.idInvoiceType,
                                     DocumentNumber = tr.DocumentNumber,
                                     RwBillNumber = tr.RwBillNumber,
                                     Idrnn = (int)tr.IDRNN,
                                     Series = tr.series,
                                     Nv = tr.NV ?? 0,
                                     Kpok = tr.KPOK ?? 0,
                                     Kgr = tr.KGR ?? 0,
                                     Kdog = tr.KDOG ?? 0,
                                     Datgr = tr.DATGR.GetValueOrDefault(),
                                     Datnakl = tr.DATNAKL ?? tr.DATGR.GetValueOrDefault(),
                                     Dataccept = tr.dataccept,
                                     Datarrival = tr.datarrival,
                                     Datdrain = tr.datdrain,
                                     DeliveryDate = tr.DeliveryDate,
                                     Kodf = tr.KODF ?? 0,
                                     Poup = tr.POUP ?? 0,
                                     Pkod = tr.PKOD ?? 0,
                                     Kolf = tr.KOLF ?? 0,
                                     Vidcen = tr.VIDCEN ?? 0,
                                     Cena = tr.CENA ?? 0,
                                     Prodnds = tr.PRODNDS ?? 0,
                                     SumNds = tr.sumnds,
                                     Kodcen = tr.KODCEN,
                                     DatKurs = tr.DATKURS,
                                     Kpr = tr.KPR ?? 0,
                                     Sper = tr.SPER ?? 0,
                                     Nds = tr.NDS ?? 0,
                                     Ndssper = tr.NDSSPER ?? 0,
                                     Dopusl = tr.DOPUSL ?? 0,
                                     Ndst_dop = tr.NDST_DOP ?? 0,
                                     Ndsdopusl = tr.NDSDOPUSL ?? 0,
                                     Provoz = tr.PROVOZ ?? 0,
                                     Stgr = tr.STGR ?? 0,
                                     Stotpr = tr.STOTPR ?? 0,
                                     TransportId = tr.TransportId ?? 0,
                                     WL_S = tr.WL_S,
                                     KodDav = tr.KODDAV,
                                     Kstr = tr.KSTR ?? 0,
                                     IdSpackage = tr.IDSPACKAGE ?? 0,
                                     IdProdcen = tr.IDPRODCEN ?? 0,                                     
                                     PrVzaim = tr.PRVZAIM ?? 0,
                                     SourceId = tr.SourceId,
                                     Period = tr.PERIOD ?? 0,
                                     Nomavt = tr.NOMAVT,
                                     Gnprc = tr.GNPRC,
                                     Marshrut = tr.MARSHRUT,
                                     Ndov = tr.NDOV,
                                     Fdov = tr.FDOV,
                                     DatDov = tr.DATDOV,
                                     Bought = tr.BOUGHT ?? false,
                                     VidAkc = tr.VIDAKC ?? 0,
                                     AkcStake = tr.AkcStake ?? 0,
                                     AkcKodVal = tr.AkcKodVal,
                                     IdSpurpose = tr.IDSPURPOSE ?? 0,
                                     IdAct = tr.IDACT ?? 0,
                                     IdVozv = tr.IDVOZV ?? 0,
                                     Maker = tr.MAKER ?? 0,
                                     KodRaznar = tr.KodRaznar ?? 0,
                                     MeasureUnitId = tr.measureUnitId,
                                     Density = tr.density ?? 0,
                                     TrackingState = TrackingInfo.Unchanged
                                 });
            return res;
        }

        // формирование счетов
        public SfModel[] MakeTempP635(int _poup, short _pkod, DateTime _dateFrom, DateTime _dateTo, int _userid, byte _dtaccmode, bool _oldnumsf, DateTime? _datesf)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return null;
            }

            SfModel[] res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                l_dc.CommandTimeout = 180;
                try
                {
                    var ids = l_dc.usp_make_Sfs(_poup, _pkod, _dateFrom, _dateTo, _userid, _dtaccmode, _oldnumsf, _datesf).Select(r => r.idsf ?? 0).ToArray();

                    res = SfHeaderToModel(
                              l_dc.SfHeaders.Where(s => ids.Contains(s.IdSf))
                          ).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        /// <summary>
        /// Приём сформированных счетов (возвращает не принятые)
        /// </summary>
        public int[] AcceptSfs(int[] _idssf)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return null;
            }

            bool? res = false;
            bool totres = true;
            int[] _uid = _idssf.ToArray();
            using (var l_dc = new RealizationDCDataContext())
            {
                l_dc.CommandTimeout = 180;
                try
                {
                    DateTime? dtaccept = null;
                    for (int i = 0; i < _idssf.Length; i++)
                    {
                        l_dc.usp_AcceptSf(_idssf[i], ref dtaccept, ref res);

                        if (res ?? false)
                            _uid[i] = 0;
                        totres &= (res ?? false);
                    }
                    if (!totres) ShowLastDbActionResult(l_dc, "usp_AcceptSf");
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            //---

            return _uid.Where(i => i != 0).ToArray();
        }

        /// <summary>
        /// Удаляет сформированные, но не принятые счета пользователя (статус счёта = 1)
        /// </summary>
        public void DeleteUnacceptedSfs()
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return;
            }

            using (var l_dc = new RealizationDCDataContext())
                try
                {
                    l_dc.usp_DeleteUnacceptedSfs(UserToken);
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
        }

        /// <summary>
        /// Выбирает сформированные, но не принятые счета пользователя (статус счёта = 1)
        /// </summary>
        public SfModel[] SelectUnacceptedSfs()
        {
            SfModel[] res = null;
            using (var l_dc = new RealizationDCDataContext())
                try
                {
                    res = SfHeaderToModel(
                            (from us in l_dc.uv_UnAcceptedIdSfs
                             from i in l_dc.SfHeaders
                             where (us.UserId == UserToken)// || UserToken == 1) 
                             && us.idsf == i.IdSf
                             select i)
                             ).ToArray();
                        //l_dc.usp_SelectUnacceptedSfs(UserToken)).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            return res;
        }

        private void ShowLastDbActionResult(RealizationDCDataContext _l_dc, string _actionName)
        {
            if (_l_dc == null) return;
            try
            {
                var lastres = _l_dc.dbActionsResults.Where(r => r.aUser == UserToken && !r.seen && (String.IsNullOrWhiteSpace(_actionName) || _actionName == r.aName)).ToArray();
                if (lastres.Any())
                {
                    Array.ForEach(lastres, r => r.seen = true);
                    _l_dc.SubmitChanges();
                    var unreadmess = String.Join("\n", lastres.Select(r => r.aMessage).ToArray());
                    if (!String.IsNullOrWhiteSpace(unreadmess))
                        OnCrash("Ошибка выполнения", unreadmess);
                }
            }
            catch (Exception e)
            {  
                OnCrash(e.GetType().ToString(), e.Message);
            }
        }

        /// <summary>
        /// Удаление (аннулирование счёта)
        /// </summary>
        /// <param name="_idsf"></param>
        /// <returns></returns>
        public bool DeleteSf(int _idsf)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return false;
            }

            bool? res = false;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    l_dc.usp_SfMarkDeleted(_idsf, ref res);
                    if (!(res ?? false)) ShowLastDbActionResult(l_dc, "usp_SfMarkDeleted");
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }           
            return res ?? false;
        }
        
        public bool PurgeSf(int _idsf)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return false;
            }

            bool? res = false;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    l_dc.usp_SfPurge(_idsf, ref res);
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res.GetValueOrDefault(false);
        }
        
        /// <summary>
        /// Поиск счёта по id
        /// </summary>
        /// <param name="pred"></param>
        /// <returns></returns>
        public SfModel GetSfModel(int _idsf)
        {
            SfModel res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var data = l_dc.SfHeaders.Where(i => i.IdSf == _idsf);
                    res = SfHeaderToModel(data).SingleOrDefault();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        /// <summary>
        /// Возвращает информацию о сроках оплаты счёта
        /// </summary>
        /// <param name="_idsf"></param>
        /// <returns></returns>
        public SfPayPeriodModel GetSfPeriod(int _idsf)
        {
            SfPayPeriodModel res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var h = l_dc.SfHeaders.Where(i => i.IdSf == _idsf).SingleOrDefault();
                    if (h != null)
                        res = h.SfPayPeriods.Select(p => new SfPayPeriodModel()
                                                             {
                                                                 Id = p.id,
                                                                 IdSf = p.idsf,
                                                                 DatStart = p.datstart,
                                                                 LastDatOpl = p.lastdatopl,
                                                                 Version = p.Version.ToArray(),
                                                                 TrackingState = TrackingInfo.Unchanged
                                                             })
                                            .SingleOrDefault();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        /// <summary>
        /// Возвращает информацию о сроках оплаты по умолчанию
        /// </summary>
        /// <param name="_idsf"></param>
        /// <returns></returns>
        public SfPayPeriodModel GetActualSfPeriod(int _idsf)
        {
            SfPayPeriodModel res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.uf_GetActualSfPeriod(_idsf)
                          .Select(p => new SfPayPeriodModel()
                          {
                            IdSf = _idsf,
                            DatStart = p.datstart,
                            LastDatOpl = p.lastdatopl,
                            TrackingState = TrackingInfo.Unchanged
                          }).SingleOrDefault();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        /// <summary>
        /// Формирование модели счёта из таблицы
        /// </summary>
        /// <param name="_in"></param>
        /// <returns></returns>
        private IEnumerable<SfModel> SfHeaderToModel(IEnumerable<SfHeader> _in)
        {
            if (_in != null)
                return _in.Select(h => new SfModel(h.IdSf, h.Version.ToArray())
                                           {
                                               NumSf = h.numsf,
                                               Poup = h.poup,
                                               IdDog = h.iddog,
                                               Kotpr = h.kotpr ?? 0,
                                               Kgr = h.kgr ?? 0,
                                               Kpok = h.kpok,
                                               StOtpr = h.stotpr ?? 0,
                                               StPol = h.stgr ?? 0,
                                               TransportId = h.TransportId ?? 0,
                                               DatPltr = h.datpltr,
                                               DatBuch = h.datbuch,
                                               SfPeriod = GetSfPeriod(h.IdSf),
                                               KodVal = h.kodval,
                                               SumPltr = h.sumpltr,
                                               SfTypeId = (short)(h.SfTypeId ?? 0),
                                               Memo = h.Memo,
                                               SfStatus = h.SfStatus,
                                               PayStatus = h.PayStatus,
                                               PayDate = h.PayDate,
                                               TrackingState = TrackingInfo.Unchanged
                                           });
            else
            {
                return null;
            }
        }

        private SfProductModel GetSfProductModelFromData(SfProduct _data)
        {
            SfProductModel res = null;
            res = new SfProductModel
            {
                IdprilSf = _data.idprilsf,
                Kdog = _data.kdog ?? 0,
                Kpr = _data.kpr,
                Kolf = _data.kolf ?? 0,
                Vidcen = _data.vidcen ?? 0,
                Bought = _data.bought ?? false,
                Maker = _data.maker ?? 0,
                //Varsch = _data.varsch ?? 0,
                Period = _data.period ?? 0,
                IdSf = _data.IdSf,
                Vozvrat = _data.vozvrat ?? 0,
                Idspackage = _data.idspackage ?? 0,
                DatGr = _data.datgr,
                DatKurs = _data.datkurs,
                KursVal = _data.kursval
            };
            return res;
        }

        /// <summary>
        /// По ID возвращает приложение счёта
        /// </summary>
        /// <param name="_idSf"></param>
        /// <returns></returns>
        public SfProductModel GetSfProduct(int _idprilsf)
        {
            SfProductModel res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var data = l_dc.SfProducts.Where(sl => sl.idprilsf == _idprilsf).SingleOrDefault();
                    res = GetSfProductModelFromData(data);
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        //по id счёта получить список его продуктовых строк
        public SfProductModel[] GetSfProducts(int _idSf)
        {
            SfProductModel[] res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.SfProducts.Where(sl => sl.IdSf == _idSf)
                        .Select(p => GetSfProductModelFromData(p)).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        // наименование транспорта (ж/д, самовывоз, трубопровод...)
        public string GetShortTransportName(short _trId)
        {
            string res = "";
            using (var l_dc = new RealizationDCDataContext())
                try
                {
                    res = l_dc.uf_GetTransportName(_trId, 1);
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            return res;
        }

        // информация о банке
        public BankInfo GetBankInfo(int _iddog, int _kontra)
        {
            BankInfo bi = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    if (_kontra == OurKgr)
                        bi = l_dc.uf_GetOurBankInfo(_iddog)
                                    .Select(b => new BankInfo()
                                                     {
                                                         BankName = b.bank,
                                                         Mfo = b.mfo,
                                                         Rsh = b.rsh
                                                     })
                                    .SingleOrDefault();
                    else
                        bi = l_dc.uf_GetPlatBankInfo(_iddog)
                                    .Select(b => new BankInfo()
                                    {
                                        BankName = b.bank,
                                        Mfo = b.mfo,
                                        Rsh = b.rsh
                                    })
                                    .SingleOrDefault();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return bi;
        }

        // Информация о ж/д станции
        public RailStation GetRailStation(int _kodst)
        {
            RailStation rs = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    rs = l_dc.uf_GetRailStation(_kodst)
                                .Select(s => new RailStation()
                                                 {
                                                     Kodst = (int)s.kodst,
                                                     StationName = s.StationName,
                                                     RailwaysName = s.RailwaysName,
                                                     Kstr = (int)(s.kstr ?? 0)
                                                 })
                                .SingleOrDefault();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return rs;
        }
        
        public DogInfo[] GetDogInfos(int _idARM)
        {
            DogInfo[] res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.uf_GetDogInfos(_idARM).Select(di => new DogInfo
                    { 
                        IdDog = (int)di.iddog, 
                        NaiOsn = di.NaiOsn, 
                        DatOsn = di.DatOsn,
                        DatDop = di.DatDop,
                        DopOsn = di.DopOsn, 
                        IdAgree = (int)di.idagree, 
                        Srok = (int)(di.srok ?? 0),
                        TypeRespite = (short)(di.typrespite ?? 0)
                    }).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        // информация по договору
        public DogInfo GetDogInfo(int _id, bool _iskdog)
        {
            DogInfo di = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    if (_iskdog)
                        di = l_dc.uf_GetDogInfoByKdog(_id)
                                    .Select(d => new DogInfo()
                                    {
                                        IdDog = (int)d.iddog,
                                        NaiOsn = d.NaiOsn,
                                        DatOsn = d.DatOsn,
                                        DopOsn = d.DopOsn,
                                        Idporsh = (int)d.idporsh,
                                        IdAgree = (int)(d.idagree ?? 0),
                                        Srok = (int)(d.srok ?? 0),
                                        TypeRespite = (short)(d.typrespite ?? 0)
                                    })
                                    .SingleOrDefault();
                    else
                        di = l_dc.uf_GetDogInfo(_id)
                                    .Select(d => new DogInfo()
                                                     {
                                                         IdDog = (int)d.iddog,
                                                         NaiOsn = d.NaiOsn,
                                                         DatOsn = d.DatOsn,
                                                         DopOsn = d.DopOsn,
                                                         Idporsh = (int)d.idporsh,
                                                         IdAgree = (int)(d.idagree ?? 0),
                                                         Srok = (int)(d.srok ?? 0),
                                                         TypeRespite = (short)(d.typrespite ?? 0)
                                                     })
                                    .SingleOrDefault();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return di;
        }

        // информация о дополнениях к договору по счёту
        public DogInfo[] GetSfDopDogInfos(int _idsf)
        {
            DogInfo[] di = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    di = l_dc.uf_GetSfDopDogs(_idsf).OrderBy(d => d.iddog)
                                .Select(d => new DogInfo()
                                {
                                    DopOsn = d.dopdog.Trim(),
                                    DatDop = d.dat_dopdog.GetValueOrDefault(),
                                    AltOsn = d.alterdog.Trim(),
                                    DatAlt = d.dat_alter.GetValueOrDefault()
                                })
                                .ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return di;
        }

        // наименование объёма упаковки
        public string GetPackageVolume(int _idspackage)
        {
            string l_pv = "";
            if (_idspackage != 0)
                using (var l_dc = new RealizationDCDataContext())
                {
                    try
                    {
                        var packinfo = l_dc.uf_GetPackageByCode(_idspackage).SingleOrDefault();
                        if (packinfo != null)
                            l_pv = packinfo.namevolume;
                    }
                    catch (Exception e)
                    {
                        OnCrash(e.GetType().ToString(), e.Message);
                    }
                }
            return l_pv;
        }

        public PredoplModel[] GetPredopsFromFinance(int _poup, int _kpok, int _ndok, DateTime _datvvod, string _kodval, byte _dir)
        {
            PredoplModel[] res = null;

            using (var l_dc = new RealizationDCDataContext())
                try
                {
                    l_dc.CommandTimeout = 180;
                    var data = l_dc.usp_GetPredoplsFromFinance(_poup, _kpok, _ndok, _datvvod, _kodval, _dir);
                    res = data.Select(d => new PredoplModel(0, null)
                        {
                            Poup = d.poup ?? 0,
                            Pkod = 0,
                            DatPropl = d.datpropl.GetValueOrDefault(),
                            DatVvod = d.datvvod.GetValueOrDefault(),
                            Kgr = d.kgr ?? 0,
                            Kpokreal = d.kpokreal ?? 0,
                            IdAgree = d.idagree ?? 0,
                            IdRegDoc = d.idRegDoc ?? 0,
                            KodVal = d.kodval,
                            KodValB = d.kodval,
                            Ndok = d.ndok ?? 0,
                            SumPropl = d.sumpropl ?? 0,
                            SumBank = d.sum_bank ?? 0,
                            Whatfor = d.whatfor,
                            Nop = d.nop ?? 0,
                            Direction = (short)d.Direction
                        }).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            
            return res;
        }

        // приём предоплат из VBANK
        public void GetPredoplFromBank(DateTime _dateFrom, DateTime _dateTo, string _valb, string _valp, int _poup, short[] _pkods, int _idbank)
        {
            using (var l_dc = new RealizationDCDataContext())
                try
                {
                    l_dc.CommandTimeout = 180;
                    string pkodsString = null;
                    if (_pkods != null && _pkods.Length > 0)
                        pkodsString = String.Join(",", _pkods.Select(k => k.ToString()).ToArray());
                    l_dc.usp_GetPredoplFromBank(_dateFrom, _dateTo, _valb, _valp, _poup, pkodsString, (short)_idbank);
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
        }

        // возвращает временные(пока еще не принятые предоплаты)
        public Dictionary<PredoplModel, AcceptableInfo> GetTmpPredopls()
        {
            Dictionary<PredoplModel, AcceptableInfo> res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.tmp_Predopls.Where(t => t.userid == UserToken && t.AcceptedStatus == 0)
                        .ToDictionary(t => new PredoplModel(t.id, null)
                        {
                            Poup = t.poup,
                            Pkod = t.pkod ?? 0,
                            DatPropl = t.datpropl.GetValueOrDefault(),
                            DatVvod = t.datvvod,
                            Kgr = t.kgr,
                            IdAgree = t.idagree ?? 0,
                            KodVal = t.kodval,
                            KursVal = t.kursval ?? 1,
                            KodValB = t.kodval_b,
                            Ndok = (int)(t.ndok ?? 0),
                            SumPropl = t.sumpropl ?? 0,
                            SumBank = t.sum_bank ?? 0,
                            Whatfor = t.whatfor,
                            Direction = t.Direction
                        }, t => new AcceptableInfo()
                        {
                            Infos = t.msgs.Split(new char[] { ';' }).Where(i => !String.IsNullOrEmpty(i)).ToArray(),
                            InfoType = t.whattodo ?? 0,
                            IsAccepted = t.priem
                        });
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        private void SaveTmpPredoplAction(RealizationDCDataContext _ldc, KeyValuePair<PredoplModel, AcceptableInfo> _tpd)
        {
            var tp = _ldc.tmp_Predopls.SingleOrDefault(t => t.id == _tpd.Key.Idpo && t.userid == UserToken); 
            tp.priem = _tpd.Value.IsAccepted;
            tp.idagree = _tpd.Key.IdAgree;
            tp.whatfor = _tpd.Key.Whatfor;
        }

        // сохранение изменений принимаемых предоплат
        public void SaveAndAcceptTmpPredopls(Dictionary<PredoplModel, AcceptableInfo> _chRows)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return;
            }

            if (_chRows != null && _chRows.Count > 0)
            {
                using (var l_dc = new RealizationDCDataContext())
                {
                    try
                    {
                        foreach (var l_cr in _chRows)
                        {
                            SaveTmpPredoplAction(l_dc, l_cr);
                        }
                        l_dc.SubmitChanges();
                    }
                    catch (Exception e)
                    {
                        OnCrash(e.GetType().ToString(), e.Message);
                    }
                }
            }
            AcceptTmpPredopls();
        }

        // приём отмеченных предоплат
        public void AcceptTmpPredopls()
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return;
            }

            int resSQL = 0;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    resSQL = l_dc.usp_AcceptPredopls();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }

                if (resSQL < 0)
                    OnCrash("Repository.AcceptPredopls", "Ошибка подтверждения предоплат");
            }
        }

        private int? ourKgr;
        public int OurKgr
        {
            get 
            {
                if (ourKgr == null)
                    ReadSettings();
                return ourKgr.Value; 
            }
        }

        private int? ourKodStan;
        public int OurKodStan
        {
            get 
            {
                if (ourKodStan == null)
                    ReadSettings();
                return ourKodStan.Value; 
            }
        }

        private void ReadSettings()
        {
            ourKodStan = ourKgr = 0;
            if (CheckOnlineStatus())
                using (var l_dc = new RealizationDCDataContext())
                {
                    try
                    {
                        var data = l_dc.Settings.FirstOrDefault();
                        if (data != null)
                        {
                            ourKgr = data.OurKodGrPol;
                            ourKodStan = data.OurKodStan.GetValueOrDefault();
                        }
                    }
                    catch (Exception e)
                    {
                        OnCrash(e.GetType().ToString(), e.Message);
                    }
                }
        }

        // выборка списка должников с суммами и датами
        public KaTotalDebt[] GetTotalDebts(string _kodval, int _poup, short _pkod, DateTime _datzakr)
        {
            KaTotalDebt[] res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.usp_GetTotalOutstandingsByKpok(_kodval, _poup, _pkod, _datzakr)
                        .OrderBy(d => d.kpok)
                        .Select(o => new KaTotalDebt()
                                         {
                                             DatZakr = o.datzakr.GetValueOrDefault(),
                                             Kodval = o.kodval,
                                             Kpok = o.kpok ?? 0,
                                             Poup = o.poup ?? 0,
                                             SumNeopl = o.sumneopl ?? 0,
                                             SumPredopl = o.sumpredopl,
                                             SumVozvrat = o.sumvozvrat
                                         }).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        // возвращает неоплаченные счета по плательщику
        public IEnumerable<SfInListInfo> GetKaOutstandingSfs(int _kpok, string _kodval, int _poup, short _pkod, DateTime _datzakr)
        {
            SfInListInfo[] res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.uf_GetKaOutstandingSfs(_kpok, _kodval, _poup, _pkod, _datzakr)
                              .Select(i =>
                                            new SfInListInfo(i.idsf ?? 0)
                                            {
                                                SumPltr = i.sumpltr ?? 0,
                                                SumOpl = i.sumopl ?? 0,
                                                OsnTxt = i.osntxt,  
                                                DopOsnTxt = i.doposntxt,
                                                DatUch = i.datuch.GetValueOrDefault(),
                                                DatStart = i.datstart.GetValueOrDefault(),
                                                LastDatOpl = i.lastdatopl.GetValueOrDefault()
                                            }
                                            ).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }

            return res;
        }

        // возвращает неоплаченные претензии по плательщику
        public PenaltyModel[] GetKaOutstandingPens(int _kpok, string _kodval, int _poup, DateTime _datzakr)
        {
            PenaltyModel[] res = null;

            res = GetPenaltyList(s => s.poup == _poup && s.datkro <= _datzakr && s.kodval == _kodval && s.kpok == _kpok && s.sumopl < s.sumpenalty);

            return res;
        }

        /// <summary>
        /// Возвращает информацию о счетах плательщика за период по направлению
        /// </summary>
        /// <param name="_kpok"></param>
        /// <param name="_poup"></param>
        /// <param name="_date1"></param>
        /// <param name="_date2"></param>
        /// <returns></returns>
        public SfInListInfo[] GetKaSfDebtsInPeriod(int _kpok, int _poup, short[] _pkods, DateTime _date1, DateTime _date2)
        {
            SfInListInfo[] res = null;
            PoupModel poupm = Poups[_poup];
            using (var l_dc = new RealizationDCDataContext())
                try
                {
                    switch (poupm.PayDoc)
                    {
                        case PayDocTypes.Sf:
                            {
                                string pkodsString = null;
                                if (_pkods != null && _pkods.Length > 0)
                                    pkodsString = String.Join(",", _pkods.Select(k => k.ToString()).ToArray());
                                res = l_dc.uf_GetSfsInfoByKpokPoupDates(_kpok, _poup, pkodsString, _date1, _date2)
                                    .Select(r => new SfInListInfo(r.Idsf)
                                    {
                                        Poup = poupm,
                                        Pkod = (short)(r.pkod ?? 0),
                                        NumSf = r.numsf,
                                        Kgr = r.kgr ?? 0,
                                        Kpok = r.kpok,
                                        DatUch = r.datbuch.GetValueOrDefault(),
                                        DatPltr = r.datpltr,
                                        SumPltr = r.SumPltr,
                                        SumOpl = r.SumOpl ?? 0,
                                        KodVal = r.kodval,
                                        TrShortName = r.TrShortName,
                                        Status = (LifetimeStatuses)r.SfStatus,
                                        PayStatus = (PayStatuses)r.PayStatus,
                                        SfType = (short)(r.SfTypeId ?? 0)
                                        //,
                                        //ESFN_Number = r.VatInvoiceNumber
                                    }).ToArray();
                                break;
                            }
                        case PayDocTypes.Penalty:
                            {
                                var pendata = l_dc.Penalties.Where(p => p.poup == _poup && p.kpok == _kpok && p.datgr >= _date1 && p.datgr <= _date2);
                                res = pendata.Select(r => new SfInListInfo(r.id) 
                                {
                                    Poup = poupm,
                                    Pkod = 0,
                                    NumSf = r.rnpl,
                                    Kgr = 0,
                                    Kpok = r.kpok,
                                    DatUch = r.datgr,
                                    DatPltr = r.datgr,
                                    SumPltr = r.sumpenalty,
                                    SumOpl = r.sumopl,
                                    KodVal = r.kodval,
                                    TrShortName = "штраф",
                                    Status = LifetimeStatuses.Accepted,
                                    PayStatus = r.sumopl == 0 ? PayStatuses.Unpayed 
                                                              : r.sumopl == r.sumpenalty ? PayStatuses.TotallyPayed : PayStatuses.Payed
                                }).ToArray();
                                break;
                            }
                    }
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            return res;

        }

        /// <summary>
        /// Возвращает остатки неоплаченных счетов по валютам на дату для контрагента
        /// </summary>
        /// <returns></returns>
        public ValOst[] GetSfsNeoplOstOnDate(int _kpok, int _poup, short[] _pkods, DateTime _ondate)
        {
            ValOst[] res = null;
            using (var l_dc = new RealizationDCDataContext())
                try
                {
                    var pkodstr = (_pkods == null || _pkods.Length == 0 || _pkods.Any(pk => pk == 0)) 
                                      ? "" 
                                      : String.Join(",", _pkods.Select(pk => pk.ToString()).ToArray());

                    res = l_dc.usp_GetSfsOstsByKpokOnDate(_kpok, _poup, pkodstr, _ondate)
                        .Select(r => new ValOst
                        {
                            Kgr = r.kpok,
                            KodVal = r.kodval,
                            Summa = r.sumost ?? 0
                            //,IsVozvrat = r.isvozv == 1
                        }).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            return res;
        }

        // возвращает непогашенные предоплаты по плательщику
        public PredoplModel[] GetPredoplsForClose(int _kpok, string _kodval, int _poup, DateTime _datzakr, short _pkod)
        {
            PredoplModel[] res = null;
            using (var l_dc = new RealizationDCDataContext())
                try
                {
                    res = //l_dc.usp_GetUnclosedPredopls(_kpok, _kodval, _poup, _datzakr, _pkod)
                        l_dc.Predopls.Where(p => p.kgr == _kpok && p.kodval == _kodval && p.poup == _poup && (_pkod == 0 || p.pkod == _pkod) && p.datvvod <= _datzakr //)
                                              && (p.datzakr == null || p.Direction == 0 && p.datzakr >= _datzakr.AddMonths(-2)))
                                     .Select(p => GetPredoplModelFromData(p)).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            return res;
        }

        /// <summary>
        /// Возвращает остатки предоплат и возвратов по валютам на дату для контрагента
        /// </summary>
        /// <returns></returns>
        public ValOst[] GetPredoplOstOnDate(int _kpok, int _poup, short _pkod, DateTime _ondate)
        {
            ValOst[] res = null;
            using (var l_dc = new RealizationDCDataContext())
                try
                {
                    res = l_dc.usp_GetPredoplsOstsByKpokOnDate(_kpok, _poup, _pkod, _ondate)
                        .Select(r => new ValOst
                        {
                            Kgr = r.kgr ?? 0,
                            KodVal = r.kodval,
                            Summa = r.sumost ?? 0,
                            IsVozvrat = r.isvozv ?? false
                        }).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            return res;
        }

        public PredoplModel[] GetPredopls(PredoplSearchData _schData)
        {
            if (_schData == null) return null;

            string kodval = _schData.Kodval; 
            int? poup = _schData.Poup;
            short? pkod = _schData.Pkod; 
            int? kpok = _schData.Kpok;
            DateTime? dfrom = _schData.Dfrom;
            DateTime? dto = _schData.Dto;
            int? ndok = _schData.Ndok;

            if (kodval == null && poup == null && kpok == null && dfrom == null && dto == null && ndok == null) return null;

            PredoplModel[] res = null;

            Expression<Func<Predopl, bool>> filter;

            //Predopl pred = null; var pp = pred.kgr

            List<BinaryExpression> exprList = new List<BinaryExpression>();

            var paramExpr = Expression.Parameter(typeof(Predopl), "p");
            
            if (ndok.HasValue && ndok > 0)
            {
                var valMember = Expression.Property(paramExpr, "ndok");
                var valData = Expression.Constant(ndok, typeof(int?));
                var valExpr = Expression.Equal(valMember, valData);
                exprList.Add(valExpr);
            }

            if (!String.IsNullOrEmpty(kodval))
            {
                var valMember = Expression.Property(paramExpr, "kodval");
                var valData = Expression.Constant(kodval, typeof(string));
                var valExpr = Expression.Equal(valMember, valData);
                exprList.Add(valExpr);
            }

            if (poup.HasValue && poup.Value > 0)
            {
                var poupMember = Expression.Property(paramExpr, "poup");
                var poupData = Expression.Constant(poup.Value, typeof(int));
                var poupExpr = Expression.Equal(poupMember, poupData);
                exprList.Add(poupExpr);
            }
            
            if (pkod.HasValue && pkod.Value > 0)
            {
                var pkodMember = Expression.Property(paramExpr, "pkod");
                var pkodData = Expression.Constant(pkod, typeof(short?));
                var pkodExpr = Expression.Equal(pkodMember, pkodData);
                exprList.Add(pkodExpr);
            }

            if (kpok.HasValue && kpok.Value > 0)
            {
                var kpokMember = Expression.Property(paramExpr, "kgr");
                var kpokData = Expression.Constant(kpok.Value, typeof(int));
                var kpokExpr = Expression.Equal(kpokMember, kpokData);
                exprList.Add(kpokExpr);
            }

            if (dfrom.HasValue || dto.HasValue)
            {
                var datMember = Expression.Property(paramExpr, "datvvod");
                if (dfrom.HasValue)
                {
                    var dfromData = Expression.Constant(dfrom.Value, typeof(DateTime));
                    var dfromExpr = Expression.GreaterThanOrEqual(datMember, dfromData);
                    exprList.Add(dfromExpr);
                }
                if (dto.HasValue)
                {
                    var dtoData = Expression.Constant(dto.Value, typeof(DateTime));
                    var dtoExpr = Expression.LessThanOrEqual(datMember, dtoData);
                    exprList.Add(dtoExpr);
                }
            }

            if (exprList.Count == 0) return null;

            BinaryExpression combiExpr = exprList[0];

            for (int i = 1; i < exprList.Count; i++)
                combiExpr = Expression.AndAlso(combiExpr, exprList[i]);

            filter = Expression.Lambda<Func<Predopl, bool>>(combiExpr, paramExpr);

            res = GetPredopls(filter);

            return res;
        }

        private PredoplModel[] GetPredopls(Expression<Func<Predopl, bool>> _filter)
        {
            if (_filter == null) return null;

            PredoplModel[]res = null;

            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.Predopls.Where(_filter).Select(p => GetPredoplModelFromData(p)).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }

            return res;        
        }

        /// <summary>
        /// Выборка поступлений по плательщику за период по направлению реализации (принятые и из финансов)
        /// </summary>
        /// <param name="_kpok"></param>
        /// <returns></returns>
        public PredoplModel[] GetPredoplsByKpok(int _kpok, int _poup, short[] _pkods, DateTime _date1, DateTime _date2)
        {
            PredoplModel[] res = null;
            using (var l_dc = new RealizationDCDataContext())
                try
                {
                    var pkodstr = (_pkods == null || _pkods.Length == 0 || _pkods.Any(pk => pk == 0))
                                      ? ""
                                      : String.Join(",", _pkods.Select(pk => pk.ToString()).ToArray());

                    res = l_dc.usp_GetIncomeFromKpokInRange(_kpok, _poup, pkodstr, _date1, _date2)
                                .Select(p =>
                                    new PredoplModel(p.idpo ?? 0, null)
                                  {
                                      IdRegDoc = p.idRegDoc ?? 0,
                                      IdAgree = p.idagree ?? 0,
                                      Poup = p.poup ?? 0,
                                      Pkod = p.pkod ?? 0,
                                      DatPropl = p.datpropl.GetValueOrDefault(),
                                      DatVvod = p.datvvod.GetValueOrDefault(),
                                      DatZakr = p.datzakr,
                                      Kgr = p.kgr ?? 0,
                                      KodVal = p.kodval,
                                      KursVal = p.kursval ?? 1,
                                      KodValB = p.kodvalb,
                                      Ndok = p.ndok ?? 0,
                                      SumBank = p.sumbank ?? 0,
                                      SumPropl = p.sumpropl ?? 0,
                                      SumOtgr = p.sumotgr ?? 0,
                                      Whatfor = p.whatfor,
                                      IdTypeDoc = p.idtypedoc ?? (byte)0,
                                      Direction = p.direction ?? 0
                                  }).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            return res;
        }

        /// <summary>
        /// Возвращает предоплаты, которыми оплачен указанный счёт
        /// </summary>
        /// <param name="_idsf"></param>
        /// <returns></returns>
        public PredoplModel[] GetPredoplsByPaydoc(int _idpaydoc, PayDocTypes _paydoctype)
        {
            PredoplModel[] res = null;
            using (var l_dc = new RealizationDCDataContext())
                try
                {
                    IQueryable<Predopl> data = null;
                    switch (_paydoctype)
                    {
                        case PayDocTypes.Sf: 
                            data = (from g in l_dc.SfProducts
                                    from pp in l_dc.SfProductPays
                                    from pa in l_dc.PaysArcs
                                    from p in l_dc.Predopls
                                    where g.IdSf == _idpaydoc && g.idprilsf == pp.idprilsf && pp.id == pa.idpay && pa.idpo == p.idpo
                                    select p
                                   ).Distinct();                                  
                            break;
                        case PayDocTypes.Penalty:
                            data = (from pe in l_dc.Penalties
                                    from pa in l_dc.PaysPens
                                    from p in l_dc.Predopls
                                    where pe.id == _idpaydoc && pe.id == pa.idpen && pa.idpo == p.idpo
                                    select p
                                   ).Distinct(); 
                            break;
                        default: break;
                    }
                    res = data//l_dc.usp_GetPayDocPredopls(_idpaydoc, (byte)_paydoctype)
                          .Select(p => GetPredoplModelFromData(p)).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            return res;
        }

        /// <summary>
        /// Формирует модель предоплаты из данных
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private PredoplModel GetPredoplModelFromData(Predopl p)
        {
            PredoplModel res = null;
            if (p != null)
                res = new PredoplModel(p.idpo, p.Version.ToArray())
                          {
                              Poup = p.poup,
                              Pkod = p.pkod ?? 0,
                              DatPropl = p.datpropl.GetValueOrDefault(),
                              DatVvod = p.datvvod,
                              DatZakr = p.datzakr,
                              Kgr = p.kgr,
                              Kpokreal = p.kpokreal ?? 0,
                              Nop = p.nop ?? 0,
                              Prpropl = p.prpropl ?? (short)0,
                              IdAgree = p.idagree ?? 0,
                              KodVal = p.kodval,
                              KursVal = p.kursval ?? 1,
                              DatKurs = p.datkurs,
                              KodValB = p.kodval_b,
                              IdRegDoc = p.idRegDoc ?? 0,
                              Ndok = p.ndok ?? 0,
                              SumPropl = p.sumpropl,
                              SumBank = p.sum_bank ?? 0,
                              SumOtgr = p.sumotgr ?? 0,
                              Whatfor = p.whatfor,
                              Prim = p.Prim,
                              IdTypeDoc = p.idtypedoc,
                              Direction = p.Direction
                          };
            return res;
        }

        // возвращает строку договора (основание) предоплаты
        public string GetPredoplOsn(int _pid)
        {
            string res = "";
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var info = l_dc.uf_GetPredoplsInfo(_pid).SingleOrDefault();
                    if (info != null)
                        res = info.osntxt;
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        /// <summary>
        /// Возвращает строки приложения счёта
        /// </summary>
        /// <param name="_idprilsf"></param>
        /// <returns></returns>
        public SfTableLine[] GetSfLine(int _idprilsf)
        {
            SfTableLine[] l_res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    l_res = l_dc.uf_SfLinePaysAgr(_idprilsf).OrderBy(l => l.rnum)
                        .Select(l =>
                                new SfTableLine(l.rnum == 0 ? LineTypes.Product : LineTypes.DopPay)
                                    {
                                        Name = l.l_name,
                                        KodProd = l.l_kod == null ? 0 : l.l_kod.Value,
                                        EdIzm = l.l_edizm,
                                        KolProd = l.l_kolprod == null ? 0 : l.l_kolprod.Value,
                                        CenProd = l.l_cenprod == null ? 0 : l.l_cenprod.Value,
                                        SumProd = l.l_sumprod == null ? 0 : l.l_sumprod.Value,
                                        ValStr = l.l_valname,
                                        SumInfo = l.l_suminfo,
                                        SumAkc = l.l_sumakc == null ? 0 : l.l_sumakc.Value,
                                        NdsSt = l.l_ndsst == null ? 0 : l.l_ndsst.Value,
                                        NdsSum = l.l_ndssum == null ? 0 : l.l_ndssum.Value,
                                        SumItog = l.l_sumitog == null ? 0 : l.l_sumitog.Value,
                                        ColumnsFormatData = l.l_formatstrings
                                    }
                        ).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return l_res;
        }

        /// <summary>
        /// Возвращает текущий статус счёта-фактуры
        /// </summary>
        /// <param name="_idsf"></param>
        /// <returns></returns>
        public LifetimeStatuses GetSfStatus(int _idsf)
        {
            LifetimeStatuses res = LifetimeStatuses.Unknown;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var sf = l_dc.SfHeaders.SingleOrDefault(s => s.IdSf == _idsf);
                    if (sf != null)
                        res = (LifetimeStatuses)sf.SfStatus;
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        /// <summary>
        /// Оплата приложения счёта предоплатой
        /// </summary>
        /// <param name="_idsf"></param>
        /// <param name="_idpo"></param>
        public bool SfPayByPredopl(int _idpo, int _idprilsf, byte _paygroup, byte _paytype, DateTime _dz, decimal _sumopl)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return false;
            }

            bool res = false;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var sqlresult = l_dc.usp_SfPayByPredopl(_idpo, _idprilsf, _paygroup, _paytype, _dz, _sumopl);
                    res = sqlresult == 0 ? true : false;
                    if (!res)
                        ShowLastDbActionResult(l_dc, "usp_SfPayByPredopl");
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        /// <summary>
        /// Отмена оплаты счёта
        /// </summary>
        /// <param name="_idsf"></param>
        /// <param name="_idpo"></param>
        //public void SfUndoPays(int _idsf)
        //{
        //    using (var l_dc = new RealizationDCDataContext())
        //    {
        //        try
        //        {
        //            l_dc.usp_SfUndoPaysByPredopl(_idsf, 0, null, null, false);
        //        }
        //        catch (Exception e)
        //        {
        //            OnCrash(e.GetType().ToString(), e.Message);
        //        }
        //    }
        //}

        /// <summary>
        /// Отмена оплаты штрафной санкции
        /// </summary>
        /// <param name="_idsf"></param>
        /// <param name="_idpo"></param>
        public void PenaltyUndoPays(int _idpen)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return;
            }

            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    l_dc.usp_PenaltyUndoPaysByPredopl(_idpen, null, null);
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
        }

        /// <summary>
        /// Отмена погашений предоплаты
        /// </summary>
        /// <param name="_idsf"></param>
        /// <param name="_idpo"></param>
        //public bool PredoplUndoPays(int _idpo)
        //{
        //    bool res = false;
        //    using (var l_dc = new RealizationDCDataContext())
        //    {
        //        try
        //        {
        //            var outres = l_dc.usp_SfUndoPaysByPredopl(0, _idpo, null, null, false);
        //            res = (outres == 0);
        //        }
        //        catch (Exception e)
        //        {
        //            OnCrash(e.GetType().ToString(), e.Message);
        //        }
        //    }
        //    return res;
        //}

        public bool UndoPayAction(PayAction _pa, DateTime _at)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return false;
            }

            bool res = false;

            if (_pa == null || (_pa.IdPo == 0 && _pa.Idsf == 0)) return res;

            int idpo = _pa.IdPo;
            int idsf = _pa.Idsf;

            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    int undoores = 0;
                    if (_pa.PayActionType == PayActionTypes.Vozvrat) // возврат
                        undoores = l_dc.usp_UnDoPredoplVozvrat(idsf, idpo, _at);
                    else // предоплата
                        if (_pa.PayActionType == PayActionTypes.Penalty) // оплата штрафных санкций
                            undoores = l_dc.usp_PenaltyUndoPaysByPredopl(idsf, idpo, _at);
                        else
                        {
                            undoores = l_dc.usp_SfUndoPaysByPredopl(idsf, idpo, _pa.PayTime, _at, true);
                            if (undoores != 0) ShowLastDbActionResult(l_dc, "usp_SfUndoPaysByPredopl");
                        }
                    res = undoores == 0;                    
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }

                return res;
            }
        }

        /// <summary>
        /// Обновление шапки счёта
        /// </summary>
        /// <param name="_sf"></param>
        /// <param name="isSuccess"></param>
        /// <returns></returns>
        public SfModel SfHeaderUpdate(SfModel _sf, out bool isSuccess)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                isSuccess = false;
                return null;
            }

            bool? res = false;
            SfModel sfres = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = (l_dc.usp_UpdateSfHeader(_sf.IdSf, _sf.NumSf, _sf.DatPltr, _sf.DatBuch, _sf.Memo, _sf.Version) == 0);
                    sfres = GetSfModel(_sf.IdSf);
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }

            isSuccess = res.GetValueOrDefault(false);
            return sfres;
        }

        /// <summary>
        /// Обновление составляющей суммы счёта
        /// </summary>
        /// <param name="_sf"></param>
        /// <param name="isSuccess"></param>
        /// <returns></returns>
        public SfProductPayModel SfProductPayUpdate(SfProductPayModel _pay, out bool isSuccess)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                isSuccess = false;
                return null;
            }

            bool? res = false;
            SfProductPayModel payres = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var data = l_dc.usp_UpdateSfProductPay(_pay.Id, _pay.Summa, _pay.Isaddtosum, ref res);
                    payres = data.Select(p => new SfProductPayModel(p.id, p.PayType, p.idprilsf)
                    {
                        Stake = p.stake ?? 0,
                        Kodval = p.kodval,
                        Kursval = p.kursval ?? 0,
                        Kolf = p.kolf ?? 0,
                        Summa = p.summa ?? 0,
                        Isaddtosum = p.isaddtosum ?? false,
                        TrackingState = TrackingInfo.Unchanged
                    }).SingleOrDefault();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }

            isSuccess = res.GetValueOrDefault(false);
            return payres;
        }

        /// <summary>
        /// Обновляет информацию о сроках оплаты счёта
        /// </summary>
        /// <param name="_sfp"></param>
        /// <param name="isSuccess"></param>
        /// <returns></returns>
        public SfPayPeriodModel SfPeriodUpdate(SfPayPeriodModel _sfp, out bool isSuccess)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                isSuccess = false;
                return null;
            }

            int? res = 0;
            SfPayPeriodModel sfpres = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    sfpres = l_dc.usp_UpdateSfPayPeriod(_sfp.Id, _sfp.DatStart, _sfp.LastDatOpl, _sfp.Version, ref res)
                                    .Select(p => new SfPayPeriodModel()
                                    {
                                        Id = p.id,
                                        IdSf = p.idsf,
                                        DatStart = p.datstart,
                                        LastDatOpl = p.lastdatopl,
                                        Version = p.Version.ToArray(),
                                        TrackingState = TrackingInfo.Unchanged
                                    }).SingleOrDefault();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }

            isSuccess = res.GetValueOrDefault(0) == 0;

            return sfpres;
        }

        public bool SfKroInfoUpdate(int _idsf, DateTime? _krodate)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return false;
            }

            bool res = true;
            
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var sres = l_dc.usp_UpdateSfKroInfo(_idsf, _krodate);
                    res = sres == 0;
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                    res = false;
                }
            }

            return res;
        }
        
        public DateTime? GetSfKroInfo(int _idsf)
        {
            DateTime? res = null;

            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var kroinfo = l_dc.SfKroInfos.Where(k => k.idsf == _idsf).SingleOrDefault();
                    if (kroinfo != null)
                        res = kroinfo.krodate;
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }

            return res;
        }

        public SfPayPeriodModel SfPeriodInsert(SfPayPeriodModel _sfp, out bool isSuccess)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                isSuccess = false;
                return null;
            }

            int? res = 0;
            SfPayPeriodModel sfpres = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    sfpres = l_dc.usp_InsertSfPayPeriod(_sfp.IdSf, _sfp.DatStart, _sfp.LastDatOpl, ref res)
                                    .Select(p => new SfPayPeriodModel()
                                    {
                                        Id = p.id,
                                        IdSf = p.idsf,
                                        DatStart = p.datstart,
                                        LastDatOpl = p.lastdatopl,
                                        Version = p.Version.ToArray(),
                                        TrackingState = TrackingInfo.Unchanged
                                    }).SingleOrDefault();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            isSuccess = res.GetValueOrDefault(0) == 0;
            return sfpres;
        }

        /// <summary>
        /// Вставляет предоплату в базу
        /// </summary>
        /// <param name="_pr"></param>
        /// <returns></returns>
        public PredoplModel PredoplInsert(PredoplModel _pr, int _iStatus, out bool _result)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                _result = false;
                return null;
            }

            PredoplModel res = null;
            bool? result = false;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var idpo = l_dc.usp_InsertPredopl(_pr.Poup, _pr.Pkod, _pr.Kgr, _pr.Kpokreal, _pr.Ndok, _pr.DatPropl, _pr.DatVvod,
                                            _pr.Nop, _pr.Prpropl, _pr.KodVal, _pr.KursVal, _pr.DatKurs, _pr.KodValB, _pr.IdRegDoc, _pr.IdAgree, _pr.SumPropl, _pr.SumBank,
                                            _pr.Whatfor, _pr.IdTypeDoc, _pr.Direction, _pr.Prim, _pr.IdSourcePO, _iStatus, ref result);
                    if (result ?? false & idpo > 0)
                        res = GetPredoplModelFromData(l_dc.Predopls.FirstOrDefault(p => p.idpo == idpo));
                    else
                        ShowLastDbActionResult(l_dc, "usp_InsertPredopl");
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            _result = result.GetValueOrDefault();
            return res;
        }

        /// <summary>
        /// Обновляет, если возможно предоплату в базе и возвращает изменённые значения
        /// </summary>
        /// <param name="_pr"></param>
        /// <returns></returns>
        public PredoplModel PredoplUpdate(PredoplModel _pr, out bool isSuccess)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                isSuccess = false;
                return null;
            }

            PredoplModel res = null;
            int? lresult = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    l_dc.usp_UpdatePredopl(_pr.Idpo, _pr.Poup, _pr.Pkod, _pr.Kgr, _pr.Kpokreal, _pr.Ndok,
                                           _pr.DatPropl, _pr.DatVvod, _pr.DatZakr, _pr.Nop, _pr.Prpropl,
                                           _pr.KodVal, _pr.KursVal, _pr.DatKurs, _pr.KodValB, _pr.IdRegDoc, _pr.IdAgree,
                                           _pr.SumPropl, _pr.SumBank, _pr.Whatfor, _pr.Prim, _pr.IdTypeDoc, _pr.Version,
                                           ref lresult);

                    res = GetPredoplModelFromData(l_dc.Predopls.FirstOrDefault(p => p.idpo == _pr.Idpo));
                    if (!(isSuccess = lresult.GetValueOrDefault(0) == 0))
                        ShowLastDbActionResult(l_dc, "usp_UpdatePredopl");                    
                }
                
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                    isSuccess = false;
                }               
            }
            return res;
        }

        /// <summary>
        /// Возвращает счетa, оплаченные предоплатой
        /// </summary>
        /// <param name="_idpo"></param>
        /// <returns></returns>
        public SfModel[] GetSfsPayedByPredopl(int _idpo)
        {
            SfModel[] res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    //var data = l_dc.uf_GetPayedSfsByPredopl(_idpo);
                    //res = SfHeaderToModel(data).ToArray();
                    var data = (from pa in l_dc.PaysArcs
                                from pay in l_dc.SfProductPays
                                from g in l_dc.SfProducts
                                from i in l_dc.SfHeaders
                                where pa.idpo == _idpo && pa.idpay == pay.id && pay.idprilsf == g.idprilsf && g.IdSf == i.IdSf
                                select i)
                                .Distinct();
                    res = SfHeaderToModel(data).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        /// <summary>
        /// Возвращает возвраты, погашенные на предоплату
        /// </summary>
        /// <param name="_idpo"></param>
        /// <returns></returns>
        public PredoplModel[] GetPredoplVozvrats(int _idpo)
        {
            PredoplModel[] res = null;

            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var data = (from pv in l_dc.PaysVozvs
                                from p in l_dc.Predopls
                                where pv.idpo == _idpo && pv.idvozv == p.idpo
                                select p
                               ).Distinct();
                    res = data.Select(p => GetPredoplModelFromData(p)).ToArray();
                    //res = l_dc.uf_GetPredoplVozvrats(_idpo).Select(p => GetPredoplModelFromData(p)).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        /// <summary>
        /// Возвращает предоплаты, погашенные возвратом
        /// </summary>
        /// <param name="_idvozv"></param>
        /// <returns></returns>
        public PredoplModel[] GetPredoplsPayedByVozvrat(int _idvozv)
        {
            PredoplModel[] res = null;

            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    //res = l_dc.uf_GetPayedPredoplsByVozvrat(_idvozv).Select(p => GetPredoplModelFromData(p)).ToArray(); 
                    res = (from pv in l_dc.PaysVozvs
                           from p in l_dc.Predopls
                           where pv.idvozv == _idvozv && p.idpo == pv.idpo
                           select p).Distinct()
                           .Select(p => GetPredoplModelFromData(p)).ToArray(); 
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        /// <summary>
        /// Погашение возврата на предоплату (возврат части или полностью предоплаты)
        /// </summary>
        /// <param name="_idpo"></param>
        /// <param name="_idvozv"></param>
        /// <param name="_result"></param>
        public bool DoPredoplVozvrat(int _idpo, int _idvozv, DateTime _datzakr)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return false;
            }

            bool res = false;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.usp_DoPredoplVozvrat(_idpo, _idvozv, _datzakr) == 0;
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        /// <summary>
        /// Возвращает оплаченную сумму счёта
        /// </summary>
        /// <param name="_idsf"></param>
        /// <returns></returns>
        public decimal GetSfSumOpl(int _idsf)
        {
            decimal res = 0;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.uf_GetSfSumOpl(_idsf, null) ?? 0;
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        /// <summary>
        /// Возвращает список счетов
        /// </summary>
        /// <returns></returns>
        private SfInListInfo[] GetSfsList(Expression<Func<uv_SfsListView, bool>> _pr)
        {
            SfInListInfo[] res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                //l_dc.Log = new System.IO.StreamWriter("Linq2Sql.log",true);
                try
                {
                    res = l_dc.uv_SfsListViews.Where(_pr)
                        .Select(r => new SfInListInfo(r.IdSf)
                        {
                            Poup = Poups[r.poup],
                            Pkod = (short)(r.pkod),
                            NumSf = r.numsf,
                            Kgr = r.kgr ?? 0,
                            Kpok = r.kpok,
                            DatUch = r.datbuch.GetValueOrDefault(),
                            DatPltr = r.datpltr,
                            SumPltr = r.sumpltr,
                            SumOpl = r.sumopl ?? 0,
                            KodVal = r.kodval,
                            TrShortName = r.TrShortName,
                            Status = (LifetimeStatuses)r.SfStatus,
                            PayStatus = (PayStatuses)r.PayStatus,
                            SfType = (short)(r.SfTypeId ?? 0),
                            IsESFN = r.IsESFN ?? false
                        }).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }
      
        /// <summary>
        /// Возвращает список счетов по дате и плательщику (подтверждённые)
        /// </summary>
        public IEnumerable<SfInListInfo> GetSfsList(SfSearchData _schData)
        {
            if (_schData == null) return null;

            int? poup = _schData.Poup; 
            short? pkod = _schData.Pkod; 
            int? kpok = _schData.Kpok; 
            int? kgr = _schData.Kgr; 
            DateTime? dateFrom = _schData.DateFrom; 
            DateTime? dateTo = _schData.DateTo; 
            int? numsfFrom = _schData.NumsfFrom; 
            int? numsfTo = _schData.NumsfTo;
            string esfnbalSchet = _schData.ESFN_BalSchet;
            string esfnNumber = _schData.ESFN_Number;
            LifetimeStatuses status = _schData.Status;

            Expression<Func<uv_SfsListView, bool>> filter;

            List<Expression> exprList = new List<Expression>();

            var paramExpr = Expression.Parameter(typeof(uv_SfsListView), "s");

            if (poup.HasValue && poup.Value != 0)
            {
                var poupMember = Expression.Property(paramExpr, "poup");
                var poupData = Expression.Constant(poup.Value, typeof(int));
                var poupExpr = Expression.Equal(poupMember, poupData);
                exprList.Add(poupExpr);
            }         

            if (kpok.HasValue && kpok.Value != 0)
            {
                var kpokMember = Expression.Property(paramExpr, "kpok");
                var kpokData = Expression.Constant(kpok.Value, typeof(int));
                var kpokExpr = Expression.Equal(kpokMember, kpokData);
                exprList.Add(kpokExpr);
            }

            if (kgr.HasValue && kgr.Value != 0)
            {
                var kgrMember = Expression.Property(paramExpr, "kgr");
                var kgrData = Expression.Constant(kgr, typeof(int?));
                var kgrExpr = Expression.Equal(kgrMember, kgrData);
                exprList.Add(kgrExpr);
            }

            if (dateFrom.HasValue || dateTo.HasValue)
            {
                var dbuchMember = Expression.Property(paramExpr, "datbuch");
                if (dateFrom.HasValue)
                {                    
                    var dfromData = Expression.Constant(dateFrom, typeof(DateTime?));
                    var dfromExpr = Expression.GreaterThanOrEqual(dbuchMember, dfromData);
                    exprList.Add(dfromExpr);
                }
                if (dateTo.HasValue)
                {
                    var dtoData = Expression.Constant(dateTo, typeof(DateTime?));
                    var dtoExpr = Expression.LessThanOrEqual(dbuchMember, dtoData);
                    exprList.Add(dtoExpr);
                }
            }

            if (numsfFrom.HasValue && numsfFrom.Value != 0 || numsfTo.HasValue && numsfTo.Value != 0)
            {
                var nsfMember = Expression.Property(paramExpr, "numsf");
                if (numsfFrom.Value == numsfTo.Value
                    || (numsfFrom ?? 0) == 0
                    || (numsfTo ?? 0) == 0)
                {
                    var numsf = (numsfFrom ?? 0) == 0 ? numsfTo.Value : numsfFrom.Value;
                    var nsfData = Expression.Constant(numsf, typeof(int));
                    var nsfExpr = Expression.Equal(nsfMember, nsfData);
                    exprList.Add(nsfExpr);
                }
                else
                {
                    if (numsfFrom.HasValue && numsfFrom.Value != 0)
                    {
                        var nfromData = Expression.Constant(numsfFrom.Value, typeof(int));
                        var nfromExpr = Expression.GreaterThanOrEqual(nsfMember, nfromData);
                        exprList.Add(nfromExpr);
                    }
                    if (numsfTo.HasValue && numsfTo.Value != 0)
                    {
                        var ntoData = Expression.Constant(numsfTo.Value, typeof(int));
                        var ntoExpr = Expression.LessThanOrEqual(nsfMember, ntoData);
                        exprList.Add(ntoExpr);
                    }
                }
            }

            if (esfnNumber != null)
            {
                
                //
                int[] idsfs = null;
                using (var db = new RealizationDCDataContext())
                    idsfs = db.NdsInvoiceDatas.Where(d => d.VatInvoiceNumber.EndsWith(esfnNumber)).Select(d => d.idsf).ToArray();
                if (idsfs != null && idsfs.Length > 0)
                {
                    var idsfMember = Expression.Property(paramExpr, "IdSf");

                    var idsfE = Expression.Constant(idsfs);
                    var method = typeof(Enumerable).GetMethods().Where(m => m.Name == "Contains" && m.GetParameters().Length == 2).FirstOrDefault().MakeGenericMethod(typeof(int));
                    var idsfExpr = Expression.Call(method, idsfE, idsfMember);
                    exprList.Add(idsfExpr);
                }
            }

            BinaryExpression statusExpr = null;
            var stMember = Expression.Property(paramExpr, "SfStatus");
            if (status != 0)
            {
                var stData = Expression.Constant((byte)status, typeof(byte));
                statusExpr = Expression.Equal(stMember, stData);
                //exprList.Add(stExpr);
            }
            else
                statusExpr = Expression.NotEqual(stMember, Expression.Constant((byte)1));

            if (exprList.Count == 0) return Enumerable.Empty<SfInListInfo>();

            BinaryExpression combiExpr = statusExpr;        
            for (int i = 0; i < exprList.Count; i++)
                combiExpr = Expression.AndAlso(combiExpr, exprList[i]);

            filter = Expression.Lambda<Func<uv_SfsListView, bool>>(combiExpr, paramExpr);

            var sfs = GetSfsList(filter);

            IEnumerable<SfInListInfo> fltSfs = null;

            if (pkod.HasValue && pkod.Value != 0)
                fltSfs = sfs.Where(s => GetSfPkods(s.IdSf).Contains(pkod.Value));

            if (esfnbalSchet != null)
                fltSfs = (fltSfs ?? sfs).Where(s => s.IsESFN && GetSfEsfsDatas(s.IdSf).Any(ed => ed.Item1.StartsWith(esfnbalSchet)));

            if (fltSfs != null)
                sfs = fltSfs.ToArray();

            return sfs;
        }

        private int[] GetSfPkods(int _idsf)
        {
            int[] res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.SfProducts.Where(g => g.IdSf == _idsf)
                                         .Select(g => GetProductInfo(g.kpr).Pkod)
                                         .Distinct()
                                         .ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }   
    
        private Tuple<string, string>[] GetSfEsfsDatas(int _idsf)
        {
            Tuple<string, string>[] res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.NdsInvoiceDatas.Where(d => d.idsf == _idsf)
                                              .Select(d => Tuple.Create(d.BalSchet, d.VatInvoiceNumber))
                                              .ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        /// <summary>
        /// Возвращает обшую информацию по счёту
        /// </summary>
        /// <param name="_idsf"></param>
        /// <returns></returns>
        public SfInListInfo GetSfInListInfo(int _idsf)
        {
            return GetSfsList(s => s.IdSf == _idsf).SingleOrDefault();
        }

        /// <summary>
        /// Возвращает отчёты для указанного компонента системы
        /// </summary>
        /// <param name="_component"></param>
        /// <returns></returns>
        public ReportModel[] GetReports(string _component)
        {
            ReportModel[] reports = null;

            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var represp = l_dc.usp_GetReports(_component).ToArray();
                    reports = represp.Select(r => new ReportModel()
                    {
                        ReportId = r.id,
                        CategoryName = r.CategoryName,
                        Title = r.Title,
                        Description = r.Description,
                        Path = r.Path,
                        ParamsGetterName = r.ParamsGetter,
                        ParamsGetterOptions = r.ParamsGetterOptions,
                        IsA3Enabled = r.IsA3Enabled,
                        IsFavorite = r.IsFavorite ?? false
                    }).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return reports;
        }

        public ReportModel GetSfPrintForm(int _idsf)
        {         
            var form = GetSfReports(_idsf, 0).FirstOrDefault();
            return form;
        }

        /// <summary>
        /// Возвращает отчёты, доступные по указанному счёту
        /// </summary>
        /// <param name="_idsf"></param>
        /// <returns></returns>
        public ReportModel[] GetSfReports(int _idsf)
        {            
            return GetSfReports(_idsf, 1).ToArray();
        }

        private IEnumerable<ReportModel> GetSfReports(int _idsf, byte _type) // 0 - основная печатная форма, 1 - доп. отчёты по счёту
        {
            IEnumerable<ReportModel> reports = null;

            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var represp = l_dc.usp_GetSfReports(_idsf, _type).ToArray();
                    reports = represp.Select(r => new ReportModel()
                    {
                        Title = r.Title,
                        Description = r.Description,
                        Path = r.Path,
                        Parameters = new Dictionary<string, string> { { "IdSf", _idsf.ToString() }, { "ConnString", ConnectionString } },
                        ParamsGetterName = r.ParamsGetter,
                        ParamsGetterOptions = r.ParamsGetterOptions,
                        IsFavorite = r.IsFavorite ?? false
                    });
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return reports;
        }

        /// <summary>
        /// Удаление отгрузки
        /// </summary>
        /// <param name="_idp623"></param>
        /// <returns></returns>
        public bool DeleteOtgruz(OtgrLine _ol)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return false;
            }

            bool res = false;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    bool? rdata = false;
                    l_dc.usp_OtgrPurge(_ol.Idp623, ref rdata);
                    res = rdata ?? false;
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        /// <summary>
        /// Возвращает счета, связанные с указанной отгрузкой
        /// </summary>
        /// <param name="_idp623"></param>
        /// <returns></returns>
        public SfInListInfo[] GetSfsByOtgruz(long _idp623)
        {
            SfInListInfo[] res = null;

            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.usp_GetOtgruzSfs(_idp623).Select(r => new SfInListInfo(r.IdSf)
                    {
                        Poup = Poups[r.poup ?? 0],
                        Pkod = (short)r.pkod,
                        NumSf = r.numsf,
                        Kgr = r.kgr ?? 0,
                        Kpok = r.kpok,
                        DatUch = r.datbuch.GetValueOrDefault(),
                        DatPltr = r.datpltr,
                        SumPltr = r.sumpltr,
                        SumOpl = r.sumopl ?? 0,
                        KodVal = r.kodval,
                        TrShortName = r.TrShortName,
                        Status = (LifetimeStatuses)r.SfStatus,
                        PayStatus = (PayStatuses)r.PayStatus,
                        SfType = (short)(r.SfTypeId ?? 0),
                        IsESFN = r.IsESFN ?? false
                    }).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        /// <summary>
        /// Возвращает название платежа (составляющей суммы приложения счёта)
        /// </summary>
        /// <param name="_idprilsf"></param>
        /// <param name="_paytype"></param>
        /// <returns></returns>
        public string GetTunedPayName(int _idprilsf, int _paytype)
        {
            string res = "";
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.uf_TunePayName(_idprilsf, _paytype);
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        /// <summary>
        /// Возвращает составляющие суммы приложения счёта
        /// </summary>
        /// <param name="_idprilsf"></param>
        /// <returns></returns>
        public SfProductPayModel[] GetSfLinePays(int _idprilsf)
        {
            SfProductPayModel[] res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var data = l_dc.SfProductPays.Where(p => p.idprilsf == _idprilsf);
                    res = data.Select(p => new SfProductPayModel(p.id, p.PayType, p.idprilsf)
                    {
                        Stake = p.stake ?? 0,
                        Kodval = p.kodval,
                        Kursval = p.kursval ?? 0,
                        Kolf = p.kolf ?? 0,
                        Summa = p.summa ?? 0,
                        Isaddtosum = p.isaddtosum ?? false,
                        TrackingState = TrackingInfo.Unchanged
                    }).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        /// <summary>
        /// Возвращает составляющие суммы всех приложений счёта
        /// </summary>
        /// <param name="_idsf"></param>
        /// <returns></returns>
        public SfProductPayModel[] GetSfPays(int _idsf)
        {
            SfProductPayModel[] res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var prils = l_dc.SfProducts.Where(p => p.IdSf == _idsf).Select(p => p.idprilsf).ToArray();
                    res = prils.SelectMany(i => GetSfLinePays(i)).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        /// <summary>
        /// Возвращает информацию по договору на основании кода KDOG
        /// </summary>
        public PDogInfoModel GetPDogInfoByKdog(int _kdog)
        {
            PDogInfoModel res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.uf_GetPDogInfoByKDog(_kdog).Select(d => new PDogInfoModel()
                    {
                        Poup = (int)d.poup,
                        Kpok = (int)d.kpok,
                        Kgr = (int)d.kgr,
                        Kfond = (int)d.kfond,
                        Iddog = (int)d.iddog,
                        Kodval = d.kodval,
                        Datd = d.datd,
                        Osn = d.osn.Trim(),
                        Osntxt = d.osntxt,
                        Srok = (int)d.srok,
                        Prvzaim = (short)d.prvzaim,
                        Provoz = (short)d.provoz,
                        Dopdog = d.dopdog == null ? null : d.dopdog.Trim(),
                        Datdopdog = d.dat_dopdog,
                        AlterDog = d.alterdog == null ? null : d.alterdog.Trim(),
                        DatAlterDog = d.dat_alter,
                        SpecDog = d.specdog == null ? null : d.specdog.Trim(),
                        DatSpecDog = d.dat_spec,
                        Kodoid = (int)d.kodoid,
                        Fiooid = d.fiooid,
                        Kdog = (int)d.kdog,
                        Kprod = (int)d.kprod,
                        Idspackage = (int)d.idspackage,
                        Cenaprod = d.cenaprod,
                        Vidcen = (int)d.vidcen,
                        Kodvalcen = d.kodvalcen,
                        KodDav = d.koddav
                    }).SingleOrDefault();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), "Ошибка при выборе договора\n" + e.Message);
                }
            }
            return res;
        }

        /// <summary>
        /// Возвращает информацию по договорам на основании кода контрагента и направления реализации
        /// </summary>
        public PDogInfoModel[] GetPDogInfosByKaPoup(int _kodka, int _poup, short _gpos)//, int _kamode)
        {
            PDogInfoModel[] res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {

                    var data = l_dc.uf_GetDogInfoByKaPoup(_kodka, _poup, _gpos);//, _kamode);
                    res = data.Select(d => new PDogInfoModel()
                    {
                        Poup = (int)d.poup,
                        Kpok = (int)d.kpok,
                        Kgr = (int)d.kgr,
                        Kfond = (int)d.kfond,
                        Iddog = (int)d.iddog,
                        Kodval = d.kodval,
                        Datd = d.datd,
                        Osn = d.osn.Trim(),
                        Osntxt = d.osntxt,
                        Srok = (int)d.srok,
                        Prvzaim = (short)d.prvzaim,
                        Provoz = (short)d.provoz,
                        Dopdog = String.IsNullOrEmpty(d.dopdog) ? null : d.dopdog,
                        Datdopdog = d.dat_dopdog,
                        AlterDog = String.IsNullOrEmpty(d.alterdog) ? null : d.alterdog,
                        DatAlterDog = d.dat_alter,
                        SpecDog = String.IsNullOrEmpty(d.specdog) ? null : d.specdog,
                        DatSpecDog = d.dat_spec,
                        Kodoid = (int)d.kodoid,
                        Fiooid = d.fiooid,
                        Kdog = (int)d.kdog,
                        Kprod = (int)d.kprod,
                        Idspackage = (int)d.idspackage,
                        Cenaprod = d.cenaprod,
                        Vidcen = (int)d.vidcen,
                        Kodvalcen = d.kodvalcen,
                        KodDav = d.koddav
                    }).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), "Ошибка при выборе договоров\n" + e.Message);
                }
            }
            return res;
        }

        /// <summary>
        /// Возвращает отгрузочные документы для формирования корректировочного счёта
        /// </summary>
        /// <param name="_kdog"></param>
        /// <param name="_date1"></param>
        /// <param name="_date2"></param>
        /// <returns></returns>
        public OtgrDocModel[] GetOtgrDocsForCorrSf(int _kdog, DateTime _date1, DateTime _date2)
        {
            OtgrDocModel[] res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var data = l_dc.usp_CollectCorrSfOtgr(_kdog, _date1, _date2);
                    res = data.Select(d => new OtgrDocModel()
                    {
                        //OtgrDocType = (DocTypes)d.OtgrDocTypeId,
                        IdInvoiceType = d.IdInvoiceType ?? 0,
                        DocumentNumber = d.DocumentNumber,                        
                        Datgr = d.Datgr.GetValueOrDefault(),
                        Kdog = d.Kdog ?? 0,
                        Kpr = d.Kpr ?? 0,
                        Kolf = d.Kolf ?? 0,
                        Cenprod = d.CENPROD ?? 0,
                        Sumprod = 0,
                        KodCenprod = d.KODCENPROD,
                        KursCenprod = d.KURSCENPROD ?? 1,
                        NdsStake = d.ProdNdsStake,
                        KodValNds = d.KODVALNDS,
                        KursValNds = d.KURSVALNDS ?? 1,
                        IdPrilsf = d.idprilsf ?? 0,
                        IdCorrsf = d.IdCorrSf ?? 0
                    }).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        /// <summary>
        /// Возвращает отгрузочные документы для формирования акта на скидку
        /// </summary>
        /// <param name="_kdog"></param>
        /// <param name="_date1"></param>
        /// <param name="_date2"></param>
        /// <returns></returns>
        public OtgrDocModel[] GetOtgrDocsForBonusSf(int _oldiddog, int _newiddog, DateTime _date1, DateTime _date2)
        {
            OtgrDocModel[] res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var data = l_dc.usp_CollectBonusSfOtgr(_oldiddog, _newiddog, _date1, _date2);
                    res = data.Select(d => new OtgrDocModel()
                    {
                        IdInvoiceType = d.IdInvoiceType ?? 0,
                        DocumentNumber = d.DocumentNumber,  
                        Datgr = d.Datgr.GetValueOrDefault(),
                        Kpr = d.Kpr ?? 0,
                        Kdog = (int)(d.Kdog ?? 0),
                        Kolf = d.Kolf ?? 0,
                        Cenprod = d.CENPROD ?? 0,
                        Discount = d.DISCOUNT,
                        Sumprod = 0,
                        KodCenprod = d.KODCENPROD,
                        KursCenprod = d.KURSCENPROD ?? 1,
                        NdsStake = d.ProdNdsStake,
                        KodValNds = d.KODVALNDS,
                        KursValNds = d.KURSVALNDS ?? 1,
                        IdPrilsf = d.idprilsf,
                        IdCorrsf = d.IdCorrSf ?? 0
                    }).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        /// <summary>
        /// Возвращает отгрузочные документы для формирования корректировочного счёта по провозной плате
        /// </summary>
        /// <param name="_kdog"></param>
        /// <param name="_date1"></param>
        /// <param name="_date2"></param>
        /// <returns></returns>
        public OtgrDocModel[] GetOtgrDocsForCorrSfSperByPerech(int _numrwlist, int _year, int _poup)
        {
            OtgrDocModel[] res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var data = l_dc.usp_CollectCorrSfSperOtgrByPerech(_numrwlist, _year, _poup);
                    res = data.Select(d => new OtgrDocModel()
                    {
                        DocumentNumber = d.RwBillNumber,
                        Datgr = d.Datgr.GetValueOrDefault(),
                        Kolf = d.kolf ?? 0,
                        SumSper = d.sumsper ?? 0,
                        KodValSper = d.kodvalsper,
                        KursValSper = d.kursvalsper,
                        NdsStakeSper = d.ndsstakesper ?? 0,
                        SumNdsSper = d.sumnds ?? 0,
                        KodValNdsSper = d.kodvalndssper,
                        KursValNdsSper = d.kursvalndssper,
                        IdPrilsf = d.idprilsf,
                        IdCorrsf = d.IdCorrSf ?? 0
                    }).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        /// <summary>
        /// Возвращает продукты по отгрузочному документу
        /// </summary>
        /// <param name="_docnum"></param>
        /// <param name="_doctype"></param>
        /// <param name="_datgr"></param>
        /// <returns></returns>
        public ProductInfo[] GetProductsByOtgrDoc(string _documentNumber, int _idInvoiceType, DateTime _datgr, int _kdog)
        {
            ProductInfo[] res = null;

            Expression<Func<arc_p623, bool>> filter;            
            
            if (_kdog > 0)
                    filter = o => o.DocumentNumber == _documentNumber && o.idInvoiceType == _idInvoiceType && o.DATGR == _datgr && o.KDOG == _kdog;
                else
                    filter = o => o.DocumentNumber == _documentNumber && o.idInvoiceType == _idInvoiceType && o.DATGR == _datgr;

            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var data = l_dc.arc_p623s.Where(filter).Select(o => o.KPR ?? 0).Distinct().ToArray();
                    res = data.Select(k => GetProductInfo(k)).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }
        
        /// <summary>
        /// Формирует корректировочные счета по провозной плате
        /// </summary>
        /// <param name="_docs"></param>
        /// <param name="_kdog"></param>
        /// <param name="_datpltr"></param>
        /// <returns></returns>
        public SfModel[] MakeCorrSfSper(IEnumerable<OtgrDocModel> _docs, int _poup, DateTime _datpltr, bool _oldsf)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return null;
            }

            SfModel[] res = null;

            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    foreach (var d in _docs)
                    {
                        var ndbDoc = new temp_Corrsf2_Otgr()
                        {
                            RwBillNumber = d.DocumentNumber,
                            Datgr = d.Datgr,
                            SumSper = d.SumSper,
                            KodValSper = "RB",
                            KursValSper = 1,
                            NdsStakeSper = d.NdsStakeSper,
                            KodValNdsSper = "RB",
                            KursValNdsSper = 1,
                            SumNdsSper = d.SumNdsSper,
                            IdPrilSf = d.IdPrilsf,
                            UserId = UserToken
                        };
                        l_dc.temp_Corrsf2_Otgrs.InsertOnSubmit(ndbDoc);
                    }
                    l_dc.SubmitChanges();

                    var ids = l_dc.usp_make_CorrSf2(_poup, _datpltr, _oldsf).Select(r => r.idsf ?? 0).ToArray();

                    res = SfHeaderToModel(
                              l_dc.SfHeaders.Where(s => ids.Contains(s.IdSf))
                          ).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }

            return res;
        }

        /// <summary>
        /// Формирует корректировочные счета по цене продукта
        /// </summary>
        /// <param name="_docs"></param>
        /// <param name="_kdog"></param>
        /// <param name="_poup"></param>
        /// <param name="_datpltr"></param>
        /// <param name="_newcena"></param>
        /// <returns></returns>
        public SfModel[] MakeCorrSf(IEnumerable<OtgrDocModel> _docs, int _kdog, int _poup, DateTime _datpltr, decimal _newcena, bool _oldsf)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return null;
            }

            SfModel[] res = null;

            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    foreach (var d in _docs)
                    {
                        var ndbDoc = new temp_Corrsf_Otgr()
                        {
                            IdInvoiceType = d.IdInvoiceType,
                            DocumentNumber = d.DocumentNumber,
                            Datgr = d.Datgr,
                            Kdog = d.Kdog,
                            Kolf = d.Kolf,
                            Cenprod = d.Cenprod,
                            KodCenprod = d.KodCenprod,
                            KursCenprod = d.KursCenprod,
                            NdsStake = d.NdsStake,
                            KodValNds = d.KodValNds,
                            KursValNds = d.KursValNds,
                            CenProdDelta = d.Sumprod != 0 ? d.Sumprod : (_newcena - d.Cenprod),
                            IdPrilSf = d.IdPrilsf,
                            IdCorrSf = d.IdCorrsf,
                            UserId = UserToken
                        };
                        l_dc.temp_Corrsf_Otgrs.InsertOnSubmit(ndbDoc);
                    }
                    l_dc.SubmitChanges();

                    var ids = l_dc.usp_make_CorrSf(_poup, _kdog, _datpltr, _oldsf).Select(r => r.idsf ?? 0).ToArray();

                    res = SfHeaderToModel(
                              l_dc.SfHeaders.Where(s => ids.Contains(s.IdSf))
                          ).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }

            return res;
        }

        /// <summary>
        /// Формирует акты на скидки
        /// </summary>
        /// <param name="_docs"></param>
        /// <param name="_kdog"></param>
        /// <param name="_poup"></param>
        /// <param name="_datpltr"></param>
        /// <param name="_newcena"></param>
        /// <returns></returns>
        public SfModel[] MakeBonusSf(IEnumerable<OtgrDocModel> _docs, DateTime _datpltr, bool _oldsf)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return null;
            }

            SfModel[] res = null;

            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    foreach (var d in _docs)
                    {
                        var ndbDoc = new temp_Corrsf_Otgr()
                        {
                            IdInvoiceType = d.IdInvoiceType,
                            DocumentNumber = d.DocumentNumber,
                            Datgr = d.Datgr,
                            Kpr = d.Kpr,
                            Kdog = d.Kdog,
                            Kolf = d.Kolf,
                            Cenprod = d.Cenprod,
                            KodCenprod = d.KodCenprod,
                            KursCenprod = d.KursCenprod,
                            NdsStake = d.NdsStake,
                            KodValNds = d.KodValNds,
                            KursValNds = d.KursValNds,
                            CenProdDelta = d.Cenprod,
                            IdPrilSf = d.IdPrilsf,
                            IdCorrSf = d.IdCorrsf,
                            UserId = UserToken
                        };
                        l_dc.temp_Corrsf_Otgrs.InsertOnSubmit(ndbDoc);
                    }
                    l_dc.SubmitChanges();

                    var ids = l_dc.usp_make_BonusSf(_datpltr, _oldsf).Select(r => r.idsf ?? 0).ToArray();

                    res = SfHeaderToModel(
                            l_dc.SfHeaders.Where(s => ids.Contains(s.IdSf))
                          ).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }

            return res;
        }

        /// <summary>
        /// Возвращает курс валюты
        /// </summary>
        /// <param name="_date"></param>
        /// <param name="_kodval"></param>
        /// <returns></returns>
        public decimal GetKursVal(DateTime _date, string _kodval)
        {
            decimal res = 0;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.ExecuteQuery<decimal>(@"SELECT dbo.uf_Cur_kurs({0}, {1})", _date.ToString("yyyyMMdd"), _kodval).FirstOrDefault();

                    //var _k = l_dc.uf_Cur_kurs(_date, _kodval);
                    //res = _k ?? 1;
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res == 0M ? 1M : res;
        }

        /// <summary>
        /// Кэш для кодов форм
        /// </summary>
        private readonly Dictionary<int, KodfModel> kodfsCache = new Dictionary<int,KodfModel>();

        public KodfModel GetKodf(int _kodf)
        {
            KodfModel res = null;
            if (_kodf != 0)
                if (kodfsCache.ContainsKey(_kodf))
                    res = kodfsCache[_kodf];
                else
                {
                    res = GetKodfFromDb(_kodf);
                    if (res != null)
                        kodfsCache[_kodf] = res;
                }
            return res;
        }

        private KodfModel GetKodfFromDb(int _kodf)
        {
            KodfModel res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.uv_Skodfs.Where(kf => kf.kodf == _kodf)
                                        .Select(v => new KodfModel()
                                        {
                                            Kodf = (int)v.kodf,
                                            Name = v.naikodf.Trim()
                                        }).SingleOrDefault();                  
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        /// <summary>
        /// Возвращает коды форм
        /// </summary>
        /// <returns></returns>
        public KodfModel[] GetKodfs()
        {
            KodfModel[] res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.uv_Skodfs.Select(v => new KodfModel()
                    {
                        Kodf = (int)v.kodf,
                        Name = v.naikodf.Trim()
                    }).ToArray();                  
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        /// <summary>
        /// Возвращает неоплаченные остатки платежей приложения счёта
        /// </summary>
        /// <param name="_idsf"></param>
        /// <returns></returns>
        public SfPayOst[] GetSfPrilPaysOsts(int _idprilsf)
        {
            SfPayOst[] res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.uf_SfGetPaysOst(_idprilsf, DateTime.MaxValue).Select(r => new SfPayOst()
                    {
                        IdPrilSf = _idprilsf,
                        IdPay = r.idpay,
                        PayType = (byte)r.paytype,
                        PayGroupId = r.PayGroupId.HasValue ? (byte)r.PayGroupId.Value : (byte)0,
                        Summa = r.SumOst ?? 0
                    }).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;//res.Any(o => o.Summa < 0) ? null : res; // возвращаем только, если все остатки положительные
        }

        /// <summary>
        /// Возвращает составляющую приложения счёта по ID
        /// </summary>
        /// <param name="_idpay"></param>
        /// <returns></returns>
        public SfProductPayModel GetProductPayById(long _idpay)
        {
            SfProductPayModel res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.SfProductPays.Where(p => p.id == _idpay).Select(pp => new SfProductPayModel(pp.id, pp.PayType, pp.idprilsf)
                    {
                        Isaddtosum = pp.isaddtosum ?? false,
                        Kodval = pp.kodval,
                        Kolf = pp.kolf ?? 0,
                        Kursval = pp.kursval ?? 1,
                        Stake = pp.stake ?? 0,
                        Summa = pp.summa ?? 0,
                        TrackingState = TrackingInfo.Unchanged
                    }).SingleOrDefault();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        /// <summary>
        /// Возвращает модеть типа платежа счёта
        /// </summary>
        /// <param name="_ptype"></param>
        /// <returns></returns>
        public SfPayTypeModel GetPayTypeModel(short _ptype)
        {
            SfPayTypeModel res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.SfPayTypes.Where(pt => pt.PayType == _ptype).Select(pt => new SfPayTypeModel
                    {
                        PayType = pt.PayType,
                        PayGroupId = pt.PayGroupId ?? 0,
                        PayName = pt.PayName,
                        SfLine = pt.SfLine ?? 0,
                        SfLinePos = pt.SfLinePos ?? 0,
                        KolProdPrec = pt.KolProdPrec ?? 0
                    }).SingleOrDefault();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        /// <summary>
        /// Удаление предоплаты
        /// </summary>
        /// <param name="_idpo"></param>
        /// <returns></returns>
        public bool DeletePredopl(int _idpo)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return false;
            }

            bool res = false;

            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.usp_PredoplPurge(_idpo) == 0;
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        /// <summary>
        /// Возвращает предоплату по коду
        /// </summary>
        /// <param name="_idpo"></param>
        /// <returns></returns>
        public PredoplModel GetPredoplById(int _idpo)
        {
            PredoplModel res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var data = l_dc.Predopls.SingleOrDefault(p => p.idpo == _idpo);
                    if (data != null)
                        res = GetPredoplModelFromData(data);
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        /// <summary>
        /// Возвращает последний указанный статус счёта или NULL, если статус не найден
        /// </summary>
        /// <param name="_idsf"></param>
        /// <param name="_status"></param>
        /// <returns></returns>
        public SfStatusInfo GetSfStatusLastDateTime(int _idsf, LifetimeStatuses _status)
        {
            SfStatusInfo res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.uf_SfGetAnyLastStatusLog(_idsf, (int)_status)
                        .Select(r => new SfStatusInfo {
                            Id = r.id,
                            IdSf = r.IdSf,
                            SfStatus = (LifetimeStatuses)r.SfStatusId,
                            SfStatusDateTime = r.SfStatusDateTime,
                            UserId = r.UserId
                        }).SingleOrDefault();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        /// <summary>
        /// Возвращает имена и id банков из SCHET для приёмки предоплат
        /// </summary>
        /// <param name="_poup"></param>
        /// <param name="_pkod"></param>
        /// <returns></returns>
        public BankInfo[] GetBanksFromSchet(int _poup, short[] _pkods, string _kodval)
        {
            BankInfo[] res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    if (IsFindSchetaForAllPkods(_pkods))
                    {
                        res = l_dc.uf_GetSchetaByPoup(_poup, 0)
                            .Where(r => r.idbank > 0 && r.kodval == _kodval).GroupBy(r => r.idbank).SelectMany(g => g.Take(1))
                            .Select(r => new BankInfo
                            {
                                Id = (int)r.idbank,
                                BankName = String.Format("{0} - {1}", r.deb, r.NaimBank)
                            }).ToArray();
                    }
                    else
                    {
                        Dictionary<int,string> _res = new Dictionary<int,string>();
                        for (int i = 0; i < _pkods.Length; i++)
                        {
                            var data1 = l_dc.uf_GetSchetaByPoup(_poup, _pkods[i]).ToArray();
                            var data2 = data1.Where(r => r.idbank > 0 && r.kodval == _kodval).ToArray();
                            var data3 = data2.Where(r => !_res.ContainsKey((int)r.idbank));

                            var schetabypkod = l_dc.uf_GetSchetaByPoup(_poup, _pkods[i])
                                                   .Where(r => r.idbank > 0 && r.kodval == _kodval).ToArray()
                                                   .Where(r => !_res.ContainsKey((int)r.idbank));
                            foreach (var si in schetabypkod)
                                _res[(short)si.idbank] = String.Format("{0} - {1}", si.deb, si.NaimBank);
                        }
                        res = _res.Select(d => new BankInfo { Id = d.Key, BankName = d.Value }).ToArray();
                    }
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        private bool IsFindSchetaForAllPkods(short[] _pkods)
        {
            bool res = _pkods == null
                     || _pkods.Length == 0
                     || _pkods[0] == 0;
            return res;
        }

        private Expression<Func<uv_Agreement, AgreementModel>> AgreementModelSelector = a => new AgreementModel()
        {
            IdAgreement = a.idAgreement,
            IdPrimaryAgreement = a.idPrimaryAgreement ?? 0,
            IdAgreeDBF = a.idAgreeDBF,
            IdCounteragent = a.idCounteragent,
            NumberOfDocument = a.NumberOfDocument,
            DateOfDocument = (a.DateOfDocument ?? a.DateOfBegin).GetValueOrDefault(),
            DateOfBegin = a.DateOfBegin.GetValueOrDefault(),
            DateOfEnd = a.DateOfEnd.GetValueOrDefault(),
            IdStateType = a.idStateType,
            Contents = a.Contents
        };

        private Dictionary<int, AgreementModel> AgreementCache = new Dictionary<int, AgreementModel>();

        /// <summary>
        /// Возвращает договор Юр. отдела по Id
        /// </summary>
        /// <param name="_idAgree"></param>
        /// <returns></returns>
        public AgreementModel GetAgreementById(int _idAgree)
        {
            AgreementModel res = null;
            if (!AgreementCache.TryGetValue(_idAgree, out res))
                using (var l_dc = new RealizationDCDataContext())
                {
                    try
                    {
                        var data = l_dc.uv_Agreements.Where(a => a.idAgreement == _idAgree).AsEnumerable();
                        res = data
                              .Select(AgreementModelSelector.Compile()).SingleOrDefault();
                        AgreementCache[_idAgree] = res;
                    }
                    catch (Exception e)
                    {
                        OnCrash(e.GetType().ToString(), e.Message);
                    }
                }
            return res;
        }

        /// <summary>
        /// Возвращает договоры Юр. отдела по Id контрагента
        /// </summary>
        /// <param name="_kpok"></param>
        /// <returns></returns>
        public AgreementModel[] GetKpokAgreements(int _kpok)
        {
            AgreementModel[] res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = //l_dc.usp_GetKpokAgreements(_kpok)
                          l_dc.uv_Agreements.Where(a => a.idCounteragent == _kpok)
                          .OrderByDescending(a => a.DateOfDocument).ThenBy(a => a.idPrimaryAgreement).ThenBy(a => a.idAgreement)
                          .Select(AgreementModelSelector.Compile()).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        /// <summary>
        /// Возвращает состояние подкачки таблиц из DBF
        /// </summary>
        /// <returns></returns>
        public TableSyncStatus[] GetTablesSyncStatuses()
        {
            TableSyncStatus[] res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.usp_GetSyncFromDbfStatus()
                              .Select(s => new TableSyncStatus{
                                  TaskName = s.TaskName.Trim().ToLowerInvariant(),
                                  TableName = s.TableName.Trim().ToLowerInvariant(),
                                  TableDescription = s.TableDescr,
                                  Status = (SyncStatuses)s.Status,
                                  DtStart = s.dtStart.GetValueOrDefault(),
                                  DtEnd = s.dtEnd.GetValueOrDefault()
                              }).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        /// <summary>
        /// Возвращает название группы платежа
        /// </summary>
        /// <param name="_id"></param>
        /// <returns></returns>
        public string GetPayGroupName(short _id)
        {
            string res = "";
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var grp = l_dc.SfPayGroups.SingleOrDefault(g => g.PayGroupId == _id);
                    if (grp != null)
                        res = grp.PayGroupName;
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        /// <summary>
        /// Возвращает протокол (историю) по счёту
        /// </summary>
        /// <param name="_idsf"></param>
        /// <returns></returns>
        public HistoryInfo[] GetSfHistory(int _idsf)
        {
            HistoryInfo[] res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.uv_SfsHistories.Where(h => h.IdSf == _idsf).OrderBy(h => h.logId)
                              .Select(h => new HistoryInfo
                              {
                                  logId = h.logId,
                                  FullName = h.FullName,
                                  StatusDateTime = h.SfStatusDateTime, 
                                  StatusDescription = h.SfStatusName,
                                  UserId = h.UserId,
                                  UserName = h.UserName
                              })
                              .ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        /// <summary>
        /// Возвращает протокол (историю) по предоплате
        /// </summary>
        /// <param name="_idsf"></param>
        /// <returns></returns>
        public HistoryInfo[] GetPredoplHistory(long _idpo)
        {
            HistoryInfo[] res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.uv_PredoplHistories.Where(h => h.IdPo == _idpo).OrderBy(h => h.logId)
                              .Select(h => new HistoryInfo
                              {
                                  logId = h.logId,
                                  FullName = h.FullName,
                                  StatusDateTime = h.StatusDateTime,
                                  StatusDescription = h.StatusDescription,
                                  UserId = h.UserId,
                                  UserName = h.UserName
                              })
                              .ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        /// <summary>
        /// Возвращает протокол (историю) по счёту
        /// </summary>
        /// <param name="_idsf"></param>
        /// <returns></returns>
        public HistoryInfo[] GetOtgrHistory(long _idp623)
        {
            HistoryInfo[] res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.uv_OtgrHistories.Where(h => h.Idp623 == _idp623).OrderBy(h => h.logId)
                              .Select(h => new HistoryInfo
                              {
                                  logId = h.logId,
                                  FullName = h.FullName,
                                  StatusDateTime = h.StatusDateTime,
                                  StatusDescription = h.StatusDescription,
                                  UserId = h.UserId,
                                  UserName = h.UserName
                              })
                              .ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        /// <summary>
        /// Возвращает диапзон дат отгрузки счёта
        /// </summary>
        /// <param name="_idsf"></param>
        /// <returns></returns>
        public DateRange GetSfDateGrRange(int _idsf)
        {
            DateRange res = new DateRange();
            
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var data = l_dc.uf_GetSfDateGrRange(_idsf).SingleOrDefault();
                    if (data != null)
                    {
                        res.DateFrom = data.datefrom.GetValueOrDefault();
                        res.DateTo = data.dateto.GetValueOrDefault();
                    }
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        public bool IfSalesJournalExists(string _jName)
        {
            bool res = false;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.JournalsArcs.Any(j => j.JName == _jName);
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }


        /// <summary>
        /// Возвращает виды журналов
        /// </summary>
        /// <returns></returns>
        public JournalTypeModel[] GetJournalTypes(JournalKind _jkind)
        {
            JournalTypeModel[] res = null;

            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    if (_jkind == JournalKind.Sell)
                        res = l_dc.SalesJournalTypes.OrderBy(j => j.vid)
                            .Select(j => new JournalTypeModel 
                            {
                                JournalId = j.id,
                                JournalType = j.vid,
                                JournalName = j.nm_1, 
                                BalSchet = j.bs, 
                                Poup = j.poup, 
                                Pkod = j.pkod, 
                                Ceh = j.zex, 
                                Kodval = j.kodval, 
                                Kstr = j.kstr, 
                                Prsng = j.prsng, 
                                IsVozm = j.is_vozm,
                                Nds = j.nds,
                                TabIsp = j.tab_isp, 
                                TabNach = j.tab_nach, 
                                TrackingState = TrackingInfo.Unchanged
                            })
                            .ToArray();
                    else
                        res = l_dc.JourBuyTypes
                            .OrderBy(j => j.jbuytype)
                            .Select(j => new JournalTypeModel
                            {
                                JournalId = j.jbuytype,
                                JournalType = j.JFileName,
                                JournalName = j.buytypename + " (" + j.podvidname + ")"
                            })
                            .ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        public void MakeSalesJournal(string _vid, DateTime _dFrom, DateTime _dto, byte _podvid, byte _sftypes, 
                                     bool _issfinterval, DateTime? _sfFrom, DateTime? _sfTo, string _jname)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return;
            }

            using (var l_dc = new RealizationDCDataContext())
            {
                l_dc.CommandTimeout = 300;
                try
                {
                    l_dc.usp_MakeSalesJournal(_vid, _dFrom, _dto, _podvid, _sftypes, _issfinterval, _sfFrom, _sfTo, _jname);
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
        }

        //public string MakeBuyJournal(int _jbtype, DateTime _dFrom, DateTime _dto, string _jfname)
        //{
        //    if (isReadOnly)
        //    {
        //        OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
        //        return null;
        //    }

        //    string res = null;
        //    using (var l_dc = new RealizationDCDataContext())
        //    {
        //        l_dc.CommandTimeout = 300;
        //        try
        //        {
        //            l_dc.usp_MakeRWBuyingJournal((byte)_jbtype, _dFrom, _dto, _jfname, ref res);
        //        }
        //        catch (Exception e)
        //        {
        //            OnCrash(e.GetType().ToString(), e.Message);
        //        }
        //    }
        //    return res;
        //}   
     
        public void MakeF744(DateTime _toDate)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return;
            }

            using (var l_dc = new RealizationDCDataContext())
            {
                l_dc.CommandTimeout = 300;
                try
                {
                    l_dc.usp_MakeF744(_toDate);
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
        }
        
        public void MakeFDebCred(int[] _poups, short[] _pkods, DateTime _onDate, DebtTypes _debcred, bool _isupload)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return;
            }

            if (_poups == null || _poups.Length == 0) return;

            string poupstr = String.Join(",", _poups.Select(p => p.ToString()).ToArray());
            string pkodstr = "0";

            if (_pkods != null && _pkods.Length > 0 && _pkods[0] != 0)
                pkodstr = String.Join(",", _pkods.Select(p => p.ToString()).ToArray());

            byte debcred = (byte)_debcred;

            using (var l_dc = new RealizationDCDataContext())
            {
                l_dc.CommandTimeout = 300;
                try
                {
                    l_dc.usp_MakeFDebKred(poupstr, pkodstr, _onDate, debcred, _isupload);
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
        }
        
        public void SetSfCurPayStatus(int _idsf, PayActions _actId, DateTime _adt)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return;
            }

            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    l_dc.usp_SetSfCurPayStatus(_idsf, (int)_actId, _adt);
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
        }

        public void SetPredolpStatus(int _idpo, PredoplStatuses _newstatus, DateTime _adt)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return;
            }

            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    l_dc.usp_PredoplChangeStatus(_idpo, (int)_newstatus, _adt, UserToken);
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
        }

        public decimal ConvertSumToVal(decimal _sum, string _fromkod, string _tokod, DateTime? _ondate, decimal? _kursfrom, decimal? _kursto)
        {
            decimal res = 0;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    if (_kursfrom == null || _kursfrom == 0 || _kursto == null || _kursto == 0)
                        res = l_dc.uf_ConvertSumToValByKod(_sum, _fromkod, _tokod, _ondate ?? DateTime.Now) ?? 0;
                    else
                        res = l_dc.uf_ConvertSumToValByKurs(_sum, _kursfrom, _kursto, _tokod) ?? 0;

                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }

            return res;
        }

        /// <summary>
        /// возвращает данные о платёжном документе
        /// </summary>
        /// <param name="_idregdoc"></param>
        /// <returns></returns>
        public Dictionary<string, object> GetPayDocInfo(int _idregdoc, bool _bypostes = false) // _bypostes - по id из банка
        {
            Dictionary<string, object> res = new Dictionary<string,object>();
            if (_idregdoc != 0)
                using (var l_dc = new RealizationDCDataContext())
                {
                    try
                    {
                        if (_bypostes)
                            _idregdoc = l_dc.uv_Vbank_FINs.Where(b => b.idPostesPayDetail == _idregdoc).Select(b => b.idRegistrPayDocument ?? 0).FirstOrDefault();
                        if (_idregdoc > 0)
                        {
                            var data = l_dc.uf_GetPayDocInfo(_idregdoc).SingleOrDefault();
                            if (data != null)
                            {
                                BankInfo bi = new BankInfo
                                {
                                    BankName = data.BankName,
                                    Rsh = data.rsh
                                };
                                res.Add("bankinfo", bi);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        OnCrash(e.GetType().ToString(), e.Message);
                    }
                }

            return res;
        }

        public PayAction[] GetPayActions(int _idpo, int _idsf)
        {
            PayAction[] res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.usp_GetPayActions(_idpo, _idsf)
                        .Select(pa => new PayAction
                        {
                            PayActionType = (PayActionTypes)pa.PayActionTYpe,
                            IdPo = pa.IdPo,
                            Ndoc = pa.Ndoc,
                            DatDoc = pa.DatDoc.GetValueOrDefault(),
                            Idsf = pa.Idsf,
                            Numsf = pa.Numsf,
                            DatPltr = pa.DatPltr.GetValueOrDefault(),
                            IdPrilsf = pa.IdPrilsf,
                            PayGroupId  = (byte)pa.PayGroupId,
                            Whatfor = pa.Whatfor,
                            SumOpl = pa.SumOpl ?? 0,
                            KodVal = pa.KodVal,
                            DatOpl = pa.DatOpl.GetValueOrDefault(),
                            PayTime = pa.PayTime.GetValueOrDefault()
                        }).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }

            return res;
        }

        public PredoplSchetModel[] GetPredoplSchets()
        {
            PredoplSchetModel[] res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.sSchets.Select(s => new PredoplSchetModel
                    {
                        Id = s.id,
                        Poup = (short)s.poup,
                        Pkod = (short)s.pkod,
                        Deb = s.deb,
                        Kre = s.kre,
                        KodvalFrom = s.kodval,
                        KodvalTo = s.kodvaln,
                        RealSch = s.realsch,
                        IdBankGroup = s.idbankgroup,
                        RecType = s.rectype,
                        IsActive = s.isActive,
                        Kodnap = s.kodnap,
                        Kodvozv = s.kodvozv,
                        TrackingState = TrackingInfo.Unchanged
                    }).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }

            return res;
        }

        public void SaveSaleJournalTypes(IEnumerable<JournalTypeModel> _jrns)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return;
            }

            foreach (var j in _jrns)
            {
                switch (j.TrackingState)
                {
                    case TrackingInfo.Created: InsertJournalType(j); break;
                    case TrackingInfo.Deleted: DeleteJournalType(j); break;
                    case TrackingInfo.Updated: UpdateJournalType(j); break;
                }
            }
        }

        private void DeleteJournalType(JournalTypeModel _j)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return;
            }

            if (_j == null) return;

            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var delitem = l_dc.SalesJournalTypes.SingleOrDefault(j => j.id == _j.JournalId);
                    if (delitem != null)
                    {
                        l_dc.SalesJournalTypes.DeleteOnSubmit(delitem);
                        if (String.IsNullOrWhiteSpace(delitem.vid))
                        {                            
                            var arc = l_dc.JournalsArcs.Where(a => a.vid == _j.JournalType);
                            l_dc.JournalsArcs.DeleteAllOnSubmit(arc);                            
                        }
                        l_dc.SubmitChanges();
                    }
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
        }

        private void InsertJournalType(JournalTypeModel _j)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return;
            }

            if (_j == null) return;

            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var newitem = new SalesJournalType
                    { 
                        poup = _j.Poup,
                        pkod = _j.Pkod,
                        kodval = _j.Kodval,
                        vid = _j.JournalType, 
                        bs = _j.BalSchet, 
                        nm_1 = _j.JournalName, 
                        kstr = _j.Kstr, 
                        prsng = _j.Prsng, 
                        is_vozm = _j.IsVozm,
                        nds = _j.Nds,
                        zex = _j.Ceh,
                        tab_isp = _j.TabIsp, 
                        tab_nach = _j.TabNach
                    };
                    l_dc.SalesJournalTypes.InsertOnSubmit(newitem);
                    l_dc.SubmitChanges();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
        }

        private void UpdateJournalType(JournalTypeModel _j)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return;
            }

            if (_j == null) return;

            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var olditem = l_dc.SalesJournalTypes.SingleOrDefault(j => j.id == _j.JournalId);
                    if (olditem != null)
                    {
                        olditem.poup = _j.Poup;
                        olditem.pkod = _j.Pkod;
                        olditem.kodval = _j.Kodval;
                        olditem.bs = _j.BalSchet;
                        olditem.nm_1 = _j.JournalName;
                        olditem.kstr = _j.Kstr;
                        olditem.prsng = _j.Prsng;
                        olditem.is_vozm = _j.IsVozm;
                        olditem.nds = _j.Nds;
                        olditem.zex = _j.Ceh;
                        olditem.tab_isp = _j.TabIsp;
                        olditem.tab_nach = _j.TabNach;

                        if (olditem.vid != _j.JournalType)
                        {
                            var arc = l_dc.JournalsArcs.Where(a => a.vid == _j.JournalType);
                            foreach (var a in arc)
                                a.vid = _j.JournalType;
                        }
                        
                        olditem.vid = _j.JournalType;
                        
                        l_dc.SubmitChanges();
                    }
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
        }

        public void SavePredoplSchets(IEnumerable<PredoplSchetModel> _schets)
        {
            foreach (var sch in _schets)
            {
                switch(sch.TrackingState)
                {
                    case TrackingInfo.Created : InsertPredoplSchet(sch); break;
                    case TrackingInfo.Deleted : DeletePredoplSchet(sch); break;
                    case TrackingInfo.Updated : UpdatePredoplSchet(sch); break;
                }
            }
        }

        private void DeletePredoplSchet(PredoplSchetModel _schet)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return;
            }

            if (_schet == null) return;

            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var delitem = l_dc.sSchets.SingleOrDefault(s => s.id == _schet.Id);
                    if (delitem != null)
                    {
                        l_dc.sSchets.DeleteOnSubmit(delitem);
                        l_dc.SubmitChanges();
                    }
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
        }

        private void InsertPredoplSchet(PredoplSchetModel _schet)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return;
            }

            if (_schet == null) return;

            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var newitem = new sSchet
                    {
                        poup = _schet.Poup,
                        pkod = _schet.Pkod,
                        deb = _schet.Deb ?? String.Empty,
                        kre = _schet.Kre ?? String.Empty,
                        kodval = _schet.KodvalFrom ?? String.Empty,
                        kodvaln = _schet.KodvalTo ?? String.Empty,
                        realsch = _schet.RealSch ?? String.Empty,
                        idbankgroup = (byte)_schet.IdBankGroup,
                        rectype = _schet.RecType,
                        isActive = _schet.IsActive,
                        kodnap = _schet.Kodnap,
                        kodvozv = _schet.Kodvozv
                    };
                    l_dc.sSchets.InsertOnSubmit(newitem);
                    l_dc.SubmitChanges();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
        }

        private void UpdatePredoplSchet(PredoplSchetModel _schet)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return;
            }

            if (_schet == null) return;

            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var olditem = l_dc.sSchets.SingleOrDefault(s => s.id == _schet.Id);
                    if (olditem != null)
                    {
                        olditem.poup = _schet.Poup;
                        olditem.pkod = _schet.Pkod;
                        olditem.deb = _schet.Deb ?? String.Empty;
                        olditem.kre = _schet.Kre ?? String.Empty;
                        olditem.kodval = _schet.KodvalFrom ?? String.Empty;
                        olditem.kodvaln = _schet.KodvalTo ?? String.Empty;
                        olditem.realsch = _schet.RealSch ?? String.Empty;
                        olditem.idbankgroup = (byte)_schet.IdBankGroup;
                        olditem.rectype = _schet.RecType;
                        olditem.isActive = _schet.IsActive;
                        olditem.kodnap = _schet.Kodnap;
                        olditem.kodvozv = _schet.Kodvozv;
                        l_dc.SubmitChanges();
                    }
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
        }
        
        public BankInfo[] GetBankGroups()
        {
            BankInfo[] res = null;

            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.sBankGroups.Select(r => new BankInfo
                    {
                        Id = (int)r.id,
                        BankName = r.name
                    }).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }

            return res;
        }

        /// <summary>
        /// Возвращает продукты по шаблону
        /// </summary>
        /// <param name="_namepat"></param>
        /// <returns></returns>
        public ProductInfo[] GetProductsByPat(string _namepat)
        {
            if (String.IsNullOrEmpty(_namepat)) return null;
            _namepat = _namepat.Trim();

            ProductInfo[] res = null;

            Expression<Func<uv_Product, bool>> filter = null;
            int kprodpat = 0;

            if (int.TryParse(_namepat, out kprodpat))
                filter = p => p.kpr.ToString().StartsWith(_namepat);
            else
                filter = p => p.name.Contains(_namepat);

            res = GetProductsByFilter(filter);
            
            return res;
        }

        private ProductInfo[] GetProductsByFilter(Expression<Func<uv_Product, bool>> _filter)
        {
            ProductInfo[] res = null;

            using (var l_dc = new RealizationDCDataContext())
                try
                {
                    //var tw = new System.IO.StringWriter();
                    //l_dc.Log = tw;
                    res = l_dc.uv_Products.Where(_filter)
                                          .Select(p => new ProductInfo
                                          {
                                              Kpr = (int)p.kpr,
                                              Name = p.name,
                                              EdIzm = p.nei,
                                              Pkod = (int)p.pkod,
                                              IdSpackage = (short)p.idspackage,
                                              IsCena = p.iscena ?? false,
                                              IsGood = p.isgood ?? false,
                                              IsService = p.isService,
                                              IsActive = p.active,
                                              IdAkcGroup = p.idakcgroup ?? 0,
                                              IsInReal = p.inrealiz != 0,
                                              MeasureUnitId = p.measureUnitId ?? 0,
                                              IdProdType = p.IdProdType
                                          }).ToArray();
                    //var logstr = tw.ToString();
                    //tw.Close();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            return res;
        }

        public Vidcen[] GetVidcens()
        {
            Vidcen[] res = null;

            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.uv_Svidcens.Select(v => new Vidcen
                    {
                        Kod = (int)v.vidcen,
                        Kodval = v.kodval,
                        Name = v.naim,
                        Pres = v.pres,
                        InSprav = v.insprav == 1,
                        IncludeNDS = v.incndstax == 1,
                        Vidakc = (int)v.vidakc
                    }).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        public Dictionary<NdsTypes, decimal> GetNdsRatesOnDate(DateTime _ondate)
        {
            Dictionary<NdsTypes, decimal> res = null;

            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.uf_GetNdsRates(_ondate).ToDictionary(r =>  (NdsTypes)r.typends, r => r.nds);
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }


        public decimal GetNDSByTypeOnDate(NdsTypes _nt, DateTime _ondate)
        {
            decimal res = GetNdsRatesOnDate(_ondate)[_nt];

            //using (var l_dc = new RealizationDCDataContext())
            //{
            //    try
            //    {
            //        res = l_dc.uf_GetNDS((int)_nt, _ondate) ?? 0;
            //    }
            //    catch (Exception e)
            //    {
            //        OnCrash(e.GetType().ToString(), e.Message);
            //    }
            //}

            return res;
        }
        
        public Prodcen GetCena(int _kpr, int _vidcen, int _idspack, DateTime _ondate)
        {
            Prodcen res = null;

            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var data = l_dc.uf_GetCen(_kpr, _vidcen, _idspack, _ondate).SingleOrDefault();
                    if (data != null)
                        res = new Prodcen 
                        {
                            IdProdcen = (int)data.idprodcen, 
                            Cena = data.cena, 
                            NdsStake = data.nds > 0M ? data.nds : 0M, 
                            NdsTax = data.ndstax
                        };
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        public bool UpdateOtgruz(OtgrLine _ol, string _logDescr)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return false;
            }

            if (_ol == null) return false;

            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var sqlres = l_dc.usp_UpdateOtgruz(_ol.IdInvoiceType, _ol.DocumentNumber, _ol.RwBillNumber, 
                                                     _ol.Idp623, _ol.Poup, _ol.Pkod, _ol.PrVzaim, _ol.Provoz, _ol.Kpok, _ol.Kgr, _ol.Kodf, 
                                                     _ol.Datgr, _ol.Dataccept, _ol.Datarrival,
                                                     _ol.Nds, 
                                                     _ol.Idrnn, _ol.Sper, _ol.Ndssper,
                                                     null,// prsv
                                                     _ol.Nomavt, _ol.Kstr,
                                                     _ol.IdSpackage,
                                                     null,// mnt                                                    
                                                     _ol.IdSpurpose,
                                                     _ol.Stgr, _ol.Stotpr, _ol.Ndov, _ol.Fdov, _ol.DatDov,
                                                     _ol.Nv,
                                                     _ol.IdVozv,
                                                     _ol.IdAct,
                                                     _ol.Cena, _ol.Vidcen, _ol.Kodcen, _ol.DatKurs,
                                                     _ol.Datnakl, _ol.IdProdcen, _ol.Dopusl, _ol.Ndsdopusl, _ol.Ndst_dop,
                                                     _ol.Bought,
                                                     _ol.Maker,
                                                     _ol.WL_S, _ol.KodDav,
                                                     _ol.Kdog, 
                                                     _ol.VidAkc,
                                                     _ol.AkcStake,
                                                     _ol.AkcKodVal,
                                                     _ol.Kpr, 
                                                     _ol.Prodnds, _ol.SumNds,
                                                     _ol.Kolf,
                                                     _ol.Gnprc,
                                                     _ol.Marshrut,
                                                     _ol.Period,// period
                                                     _ol.TransportId,
                                                     _ol.SourceId, // sourceid : 1 - ручной ввод в реализацию
                                                     _ol.MeasureUnitId,
                                                     _ol.Density,
                                                     _logDescr
                                                     );

                    if (sqlres == -1) return false;
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                    return false;
                }
            }
            return true;
        }

        public bool AddOtgruz(OtgrLine _ol, string _logDescr)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return false;
            }

            if (_ol == null) return false;

            int newidp623;

            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    //var tw = new System.IO.StringWriter();
                    //l_dc.Log = tw;
                    newidp623 = l_dc.usp_AddOtgruz(_ol.IdInvoiceType, _ol.DocumentNumber, _ol.RwBillNumber, 
                                                     _ol.Poup, _ol.Pkod, _ol.PrVzaim, _ol.Provoz, _ol.Kpok, _ol.Kgr, _ol.Kodf, _ol.Datgr, _ol.Nds, _ol.Idrnn, _ol.Sper, _ol.Ndssper,
                                                     _ol.PrSv,
                                                     _ol.Nomavt, _ol.Kstr,
                                                     _ol.IdSpackage,
                                                     null,// mnt                                                    
                                                     _ol.IdSpurpose,
                                                     _ol.Stgr, _ol.Stotpr, _ol.Stn_per, _ol.Ndov, _ol.Fdov, _ol.DatDov,
                                                     _ol.Nv,
                                                     _ol.IdVozv,
                                                     _ol.IdAct,
                                                     _ol.Cena, _ol.Vidcen, _ol.Kodcen, _ol.DatKurs,
                                                     _ol.Datnakl,
                                                     _ol.Series,
                                                     _ol.IdProdcen, _ol.Dopusl, _ol.Ndsdopusl, _ol.Ndst_dop,
                                                     _ol.Bought,
                                                     _ol.Maker,
                                                     _ol.WL_S, _ol.KodDav,
                                                     _ol.Kdog, _ol.Kpr,
                                                     _ol.Prodnds, _ol.SumNds,
                                                     _ol.Kolf,
                                                     _ol.Gnprc,// gnprc
                                                     _ol.Marshrut,// marshrut
                                                     _ol.Period,// period
                                                     _ol.TransportId,
                                                     _ol.Dataccept, _ol.Datarrival, _ol.Datdrain,
                                                     _ol.SourceId, // sourceid : 1 - ручной ввод в реализацию, 2 - принято из обменной базы ЖД
                                                     _ol.VidAkc,
                                                     _ol.AkcStake,
                                                     _ol.AkcKodVal,
                                                     _ol.KodRaznar,
                                                     _ol.MeasureUnitId,
                                                     _ol.Density,
                                                     _logDescr
                                                     );
                        //var logstr = tw.ToString();
                        //tw.Close();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                    return false;
                }
            }

            if (newidp623 < 0)
                OnCrash("Ошибка при записи на SQL сервер", "newidp623 = l_dc.usp_AddOtgruz : возвращено отрицательное значение\n" + String.Format("IDRNN = {0}", _ol.Idrnn));
            else
                _ol.Idp623 = newidp623;

            return newidp623 >= 0;
        }

        public Transport GetTransport(short _kodf, int _poup)
        {
            Transport res = null;

            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var idtr = l_dc.uf_GetTransportId(_kodf, _poup) ?? 0;
                    if (idtr != 0)
                        res = l_dc.sTransportTypes.Where(t => t.Id == idtr)
                                                  .Select(t => new Transport 
                                                  {
                                                      Id = t.Id,
                                                      Name = t.Name,
                                                      ShortName = t.ShortName,
                                                      Direction = (Directions)l_dc.uf_GetOtgrDirection(_poup, _kodf)
                                                  }).SingleOrDefault();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        private Country CountryDataToModel(uv_Str _data)
        {
            Country res = null;
            if (_data != null)
                res = new Country 
                {
                    Kstr = (int)_data.kstr,
                    Name = _data.nstr,
                    ShortName = _data.snstr
                };
            return res;
        }

        private Dictionary<int, Country> countryCache;

        public Country[] GetCountries(int _kstr)
        {
            Country[] res = null;
            if (_kstr > 0 && countryCache != null && countryCache.ContainsKey(_kstr))
                return new Country[] { countryCache[_kstr] };

            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    if (_kstr > 0)
                    {
                        if (countryCache == null) countryCache = new Dictionary<int, Country>();
                        res = l_dc.uv_Strs.Where(c => c.kstr == _kstr).Select(s => CountryDataToModel(s)).ToArray();
                        if (res != null && res.Length == 1)
                            countryCache[res[0].Kstr] = res[0];
                    }
                    else
                        res = l_dc.uv_Strs.Select(s => CountryDataToModel(s)).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }
        
        public Tuple<DateTime,decimal,int>[] GetKurses(string _kodval, DateTime _ondate)
        {
            Tuple<DateTime, decimal, int>[] res = null;

            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.usp_GetKurses(_kodval, _ondate).Select(d => Tuple.Create(d.RateDate.GetValueOrDefault(), d.Rate.GetValueOrDefault(), d.Currency_Scale.GetValueOrDefault())).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        /// <summary>
        /// Возвращает ссылки на аннулированные счета, кототые были сформированы ранее на отгрузку указанного счёта
        /// </summary>
        /// <param name="_idsf"></param>
        /// <returns></returns>
        public SfModel[] GetOldSfs(int _idsf)
        {
            SfModel[] res = null;

            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.uf_GetOldSfs(_idsf)
                              .Select(r => new SfModel(r.idsf, null) 
                              {
                                  NumSf = r.numsf,
                                  DatPltr = r.datpltr
                              })
                              .ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        public Dictionary<OtgrLine, decimal> GetOtgrByAktSliv(string _nakt, DateTime _dakt)
        {
            Dictionary<OtgrLine, decimal> res = null;

            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.usp_GetOtgrByAktSliv(_nakt, _dakt)
                              .ToDictionary(r => new OtgrLine { 
                                                     Idrnn = (int)r.IDRNN, 
                                                     DocumentNumber = r.RwBillNumber,
                                                     RwBillNumber = r.RwBillNumber,
                                                     Nv = r.NV ?? 0, 
                                                     Kolf = r.KOLF ?? 0, 
                                                     Datgr = r.DATGR.GetValueOrDefault(),
                                                     Datnakl = r.datdrain.GetValueOrDefault(),
                                                     IsChecked = (r.KOLF != r.ves_akt) }, 
                                            r => r.ves_akt ?? 0);
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        public bool UpdateOtgrOnSliv(long _idrnn, DateTime _datgr, decimal _kolf, DateTime _datdrain)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return false;
            }

            bool res = true;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var sqlres = l_dc.usp_UpdateOtgruzByAktSliv(_idrnn, _kolf, _datdrain);
                    res = (sqlres != -1);
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                    res = false;
                }
            }
            return res;
        }

        private PenaltyModel[] GetPenaltyList(Expression<Func<Penalty, bool>> _pr)
        {
            PenaltyModel[] res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                //l_dc.Log = new System.IO.StreamWriter("Linq2Sql.log",true);
                try
                {
                    //var data = l_dc.Penalties.Where(_pr).ToArray();
                    res = l_dc.Penalties.Where(_pr).Select(r => new PenaltyModel
                        {
                            Id = r.id,
                            Poup = r.poup,
                            Kpok = r.kpok,
                            Kdog = r.kdog,
                            Nomish = r.nomish,
                            Nomkro = r.nomkro,
                            Rnpl = r.rnpl,
                            Datgr = r.datgr,
                            Datkro = r.datkro,
                            Sumpenalty = r.sumpenalty,
                            Kodval = r.kodval,
                            Sumopl = r.sumopl,
                            Datopl = r.datopl,
                            Kursval = r.kursval,
                            UserAdd = r.useradd,
                            DateAdd = r.dateadd,
                            UserKor = r.userkor ?? 0,
                            DateKor = r.datekor
                        }).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        public PenaltyModel[] GetPenaltyList(int _poup, DateTime _dateFrom, DateTime _dateTo)
        {
            var sfs = GetPenaltyList(s => s.poup == _poup && s.datkro >= _dateFrom && s.datkro <= _dateTo);
            return sfs;
        }
        
        public PenaltyModel GetPenaltyById(int _id)
        {
            var pen = GetPenaltyList(s => s.id == _id).SingleOrDefault();
            return pen;
        }
        
        public PenaltyModel InsertPenalty(PenaltyModel _pm)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return null;
            }

            if (_pm == null) return null;

            PenaltyModel res = null;
            
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var newitem = new Penalty
                    {
                        kdog = _pm.Kdog,
                        kodval = _pm.Kodval,
                        kpok = _pm.Kpok,
                        kursval = _pm.Kursval,
                        nomish = _pm.Nomish,
                        nomkro = _pm.Nomkro,
                        poup = _pm.Poup,
                        rnpl = _pm.Rnpl,
                        sumpenalty = _pm.Sumpenalty,
                        useradd = _pm.UserAdd,
                        dateadd = _pm.DateAdd,
                        datgr = _pm.Datgr,
                        datkro = _pm.Datkro,
                    };
                    l_dc.Penalties.InsertOnSubmit(newitem);
                    l_dc.SubmitChanges();

                    res = GetPenaltyById(newitem.id);
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }

            return res;
        }

        public bool UpdatePenalty(PenaltyModel _pm)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return false;
            }

            if (_pm == null) return false;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var olditem = l_dc.Penalties.SingleOrDefault(p => p.id == _pm.Id);
                    if (olditem != null)
                    {
                        olditem.kdog = _pm.Kdog;
                        olditem.kodval = _pm.Kodval;
                        olditem.kpok = _pm.Kpok;
                        olditem.kursval = _pm.Kursval;
                        olditem.nomish = _pm.Nomish;
                        olditem.nomkro = _pm.Nomkro;
                        olditem.poup = _pm.Poup;
                        olditem.rnpl = _pm.Rnpl;                        
                        olditem.sumpenalty = _pm.Sumpenalty;
                        olditem.useradd = _pm.UserAdd;
                        olditem.userkor = _pm.UserKor;
                        olditem.dateadd = _pm.DateAdd;
                        olditem.datekor = _pm.DateKor;
                        olditem.datgr = _pm.Datgr;
                        olditem.datkro = _pm.Datkro;
                        //olditem.sumopl = _pm.Sumopl;
                        //olditem.datopl = _pm.Datopl;
                        l_dc.SubmitChanges();
                    }
                    else
                        return false;
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                    return false;
                }                
            }
            return true;
        }

        public bool DeletePenalty(int _id)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return false;
            }

            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var delitem = l_dc.Penalties.SingleOrDefault(p => p.id == _id);
                    if (delitem != null)
                    {
                        l_dc.Penalties.DeleteOnSubmit(delitem);
                        l_dc.SubmitChanges();
                    }
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Оплата штрафных санкций
        /// </summary>
        /// <param name="_idpo"></param>
        /// <param name="_idpen"></param>
        /// <param name="_dz"></param>
        /// <param name="_sumopl"></param>
        /// <returns></returns>
        public bool PenaltyPayByPredopl(int _idpo, int _idpen, DateTime _dz, decimal _sumopl)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return false;
            }

            bool res = false;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var sqlresult = l_dc.usp_PenaltyPayByPredopl(_idpo, _idpen, _dz, _sumopl);
                    res = sqlresult == 0 ? true : false;
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                    res = false;
                }
            }
            return res;
        }

        // возвращает реестр отгрузки
        public OtgrLine[] GetOtgrFromXChange(DateTime _dfrom, DateTime _dto, InOtgrTypes _ot)
        {
            OtgrLine[] res = null;
            using (var l_dc = new RealizationDCDataContext())
                try
                {
                    var data = l_dc.usp_GetOtgruzFromXChange(_dfrom, _dto, (byte)_ot);
                    res = data.Select(d => new OtgrLine(0)
                           {
                               IdInvoiceType = d.idinvoicetype,
                               DocumentNumber = d.DocumentNumber,
                               RwBillNumber = d.RwBillNumber,
                               Poup = (short)d.poup,
                               Kodf = (short)d.kodf,
                               Nv = d.nv ?? 0,
                               Kgr = d.kgr ?? 0,
                               Kpok = d.kpok ?? 0,
                               Kdog = d.kdog ?? 0,
                               Datgr = d.datgr.GetValueOrDefault(),
                               Datnakl = d.datgr.GetValueOrDefault(),
                               Dataccept = d.dataccept,
                               Datarrival = d.datarrival,
                               Datdrain = d.datdrain,
                               Stotpr = d.stotpr ?? 0,
                               Stgr = d.stgr ?? 0,
                               Stn_per = d.stn_per ?? 0,
                               Kolf = d.kolf ?? 0,
                               Kpr = d.kpr ?? 0,
                               MeasureUnitId = d.measureunit,
                               Sper = d.sper ?? 0,
                               Nds = d.nds ?? 0,
                               Ndssper = d.ndssper ?? 0,
                               Dopusl = d.dopusl ?? 0,
                               Ndst_dop = d.ndst_dop ?? 0,
                               Ndsdopusl = d.nds_dopusl ?? 0,
                               Provoz = (short)(d.provoz ?? 0),
                               TransportId = 3, // ЖД
                               KodDav = d.koddav,
                               WL_S = d.wl_s, // давальческое
                               Kstr = d.kstr ?? (short)0,
                               PrSv = d.prsv ?? false,
                               TrackingState = TrackingInfo.Unchanged,
                               StatusMsgs = String.IsNullOrEmpty(d.ErrorMsg) ? null : d.ErrorMsg.Split(new char[] { ';' }),
                               StatusType = d.ErrorType ?? 0,
                               SourceId = 2 // из обменной базы
                           }).ToArray();

                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            return res;
        }

        /// <summary>
        /// Возвращает давальцев по выбранному виду реализации или по всем
        /// </summary>
        /// <param name="_poup"></param>
        /// <returns></returns>
        public KontrAgent[] GetDavsByPoup(int _poup)
        {
            KontrAgent[] res = null;
            using (var l_dc = new RealizationDCDataContext())
                try
                {
                    var data = l_dc.uf_GetDavsByPoup(_poup);
                    res = data.Select(d => new KontrAgent()
                    {
                        Kgr = (int)d.kgr,
                        Name = d.name,
                        FullName = d.FullName,
                        Address = d.address,
                        Okpo = d.okpo,
                        Kpp = d.kpp,
                        Inn = d.inn,
                        Kstr = (short)(d.kstr ?? 0),
                        City = d.gor,
                        Country = d.nstr,
                        Koddav = d.kd
                    }).ToArray();

                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            return res;
        }

        public KontrAgent[] GetKontragentsByRwStation(int _station)
        {
            KontrAgent[] res = null;
            using (var l_dc = new RealizationDCDataContext())
                try
                {
                    var data = l_dc.uf_GetKontragentsByRwStation(_station);
                    res = data.Select(d => new KontrAgent()
                    {
                        Kgr = (int)d.kgr,
                        Name = d.name,
                        FullName = d.FullName,
                        Address = d.address,
                        Okpo = d.okpo,
                        Kpp = d.kpp,
                        Inn = d.inn,
                        Kstr = (short)(d.kstr ?? 0),
                        City = d.gor,
                        Country = d.nstr
                    }).ToArray();

                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            return res;
        }

        /// <summary>
        /// Сохранение в базу настроек пользователя на направления реализации
        /// </summary>
        /// <param name="_sett"></param>
        /// <returns></returns>
        public bool SaveNaprSettings(Dictionary<int, int[]> _sett)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return false;
            }

            bool res = true;
            bool isall = false;
            if (_sett == null || _sett.Count == 0 || _sett.Any(kv => kv.Key == 0 && kv.Value.Any(v => v == 0)))
                isall = true;
            
            using (var l_dc = new RealizationDCDataContext())
                try
                {
                    var old = l_dc.UserNaprs.Where(un => un.userid == UserToken);
                    l_dc.UserNaprs.DeleteAllOnSubmit(old);
                    l_dc.SubmitChanges();
                    if (!isall)
                    {
                        foreach (var p in _sett)
                            foreach (var k in p.Value)
                            {
                                var news = new UserNapr { poup = p.Key, kodf = (short)k, pkod = 0, userid = UserToken };
                                l_dc.UserNaprs.InsertOnSubmit(news);
                            }
                        l_dc.SubmitChanges();
                    }                    
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                    res = false;
                }
            return res;
        }

        /// <summary>
        /// Загрузка настроек пользователя на направления реализации
        /// </summary>
        /// <returns></returns>
        public Dictionary<int, int[]> LoadNaprSettings()
        {
            Dictionary<int, int[]> res = null;

            using (var l_dc = new RealizationDCDataContext())
                try
                {
                    var udata = l_dc.UserNaprs.Where(un => un.userid == UserToken).GroupBy(v => v.poup);
                    if (!udata.Any())
                        res = new Dictionary<int, int[]> { { 0, new int[] { 0 } } };
                    else
                        res = udata.ToDictionary(g => g.Key, g => g.Select(v => (int)v.kodf).ToArray());
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }

            return res;
        }

        public bool GetIfOtgrCanBeEdited(long _idp623)
        {
            bool res = false;
            using (var l_dc = new RealizationDCDataContext())
                try
                {
                    res = l_dc.uf_GetIfOtgrCanBeEdited(_idp623) ?? false;
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            return res;
        }

        public OtgrLine[] GetRwListData(int _rwlistnum, int _year)
        {
            OtgrLine[] res = null;
            
            using (var l_dc = new RealizationDCDataContext())
                try
                {
                    res = l_dc.usp_GetRwListData(_rwlistnum, _year)
                        .Select(d => new OtgrLine 
                        { 
                            Idp623 = (int)d.id_rwlist,
                            RwBillNumber = d.RwBillNumber,
                            Datgr = d.datgr,
                            Sper = d.sper ?? 0,
                            Ndssper = d.spernds ?? 0,
                            Nds = d.sperndsst ?? 0,
                            Dopusl = d.dopusl ?? 0,
                            Ndsdopusl = d.dopnds ?? 0,
                            Ndst_dop = d.dopndsst ?? 0,
                            StatusType = (short)d.StatusType,
                            StatusMsgs = d.StatusMsgs == null ? null : d.StatusMsgs.Split(';')
                        })
                        .ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            
            return res;
        }

        public bool UpdateOtgrByRwList(int _idRwList, int _numRwList, OtgrLine _otgr)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return false;
            }

            bool res = false;

            using (var l_dc = new RealizationDCDataContext())
            using (var ts = new System.Transactions.TransactionScope())
            {
                try
                {
                    var sqlres = l_dc.usp_UpdateOtgruzByRwList(_otgr.Idrnn, _idRwList, _numRwList, _otgr.Sper, _otgr.Ndssper, _otgr.Nds, _otgr.Dopusl, _otgr.Ndsdopusl, _otgr.Ndst_dop);
                    res = (sqlres != -1);
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }

                if (res) ts.Complete();
            }
            
            return res;
        }

        public WeightInfo GetSfWeightInfo(int _idsf)
        {
            WeightInfo res = null;
            using (var l_dc = new RealizationDCDataContext())
                try
                {
                    res = l_dc.uf_GetSfWeight(_idsf).Select(r => new WeightInfo
                    {
                        Weight = r.weight ?? 0,
                        Edizm = r.nei,
                        Precision = r.precision ?? 0
                    }).SingleOrDefault();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            return res;
        }

        /// <summary>
        /// Возвращает подписи для документов (по направлению)
        /// </summary>
        /// <param name="_poup"></param>
        /// <returns></returns>
        public SignsInfo GetSigns(int _poup)
        {
            SignsInfo res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.uf_GetSigns(_poup).Select(si => new SignsInfo()
                    {
                        BossId = si.BossId,
                        BossPosition = si.BossPosition.Trim(),
                        BossFio = si.BossFIO.Trim(),
                        GlBuhId = si.GlBuhId,
                        GlBuhPosition = si.GlBuhPosition.Trim(),
                        GlBuhFio = si.GlBuhFIO.Trim()
                    }).FirstOrDefault();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        private Expression<Func<SignFio, SignatureInfo>> SignerModelSelector = s => new SignatureInfo()
        {
            Id = s.id_sign,
            Fio = s.fio_sign,
            Position = s.position,
            Short = s.@short,
            SignTypeId = s.idSignType
        };

        /// <summary>
        /// Возвращает список лиц, имеющих право подписи финансовых документов
        /// </summary>
        /// <param name="_poup"></param>
        /// <returns></returns>
        public SignatureInfo[] GetSigners(int _poup)
        {
            SignatureInfo[] res = null;
            using (var l_dc = new SignsDCDataContext())
            {
                try
                {
                    SignFio[] data = null;
                    if (_poup == 0)
                        data = l_dc.SignFios.ToArray();
                    else
                        data = l_dc.SignerPoups.Where(sp => sp.poup == _poup).Select(sp => sp.SignFio).ToArray();
                    if (data != null && data.Length > 0)
                        res = data.Select(SignerModelSelector.Compile()).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        /// <summary>
        /// Обновляет информацию о подписях документов
        /// </summary>
        /// <param name="_poup"></param>
        /// <param name="_boss"></param>
        /// <param name="_glbuh"></param>
        public void UpdateSigns(int _poup, int _boss, int _glbuh)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return;
            }

            if (_poup <= 0)
                return;

            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    l_dc.usp_UpdateSigns(_poup, _boss, _glbuh);
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
        }

        public int[] GetSignerPoups(int _idsigner)
        {
            int[] res = null;
            using (var l_dc = new SignsDCDataContext())
                try
                {
                    res = l_dc.SignerPoups.Where(sp => sp.idsign == _idsigner).Select(sp => sp.poup).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            return res;
        }
        
        public void SetSignerPoups(int _idsigner, params int[] _poups)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return;
            }

            using (var l_dc = new SignsDCDataContext())
                try
                {
                    var oldspoups = l_dc.SignerPoups.Where(sp => sp.idsign == _idsigner);
                    l_dc.SignerPoups.DeleteAllOnSubmit(oldspoups);       
                    if (_poups != null && _poups.Length > 0)
                    {
                        var newspoups = _poups.Select(p => new SignerPoup { idsign = _idsigner, poup = p});
                        l_dc.SignerPoups.InsertAllOnSubmit(newspoups);
                    }
                    l_dc.SubmitChanges();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
        }

        public void DeleteSigner(int _idsigner)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return;
            }

            using (var l_dc = new SignsDCDataContext())
                try
                {
                    var signer = l_dc.SignFios.SingleOrDefault(s => s.id_sign == _idsigner);
                    if (signer != null)
                        l_dc.SignFios.DeleteOnSubmit(signer);
                    l_dc.SubmitChanges();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
        }
        
        public int MergeSigner(SignatureInfo _signer)
        {
            int res = 0;
            if (_signer.Id == 0)
                res = InsertSigner(_signer);
            else
                res = UpdateSigner(_signer);
            return res;
        }

        private int InsertSigner(SignatureInfo _signer)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return 0;
            }

            int res = 0;
            using (var l_dc = new SignsDCDataContext())
                try
                {
                    SignFio newsigner = new SignFio { fio_sign = _signer.Fio, position = _signer.Position, @short = _signer.Short, idSignType = _signer.SignTypeId };
                    l_dc.SignFios.InsertOnSubmit(newsigner);
                    l_dc.SubmitChanges();
                    res = newsigner.id_sign;
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            return res;
        }

        private int UpdateSigner(SignatureInfo _signer)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return 0;
            }

            int res = 0;
            using (var l_dc = new SignsDCDataContext())
                try
                {
                    SignFio oldsigner = l_dc.SignFios.SingleOrDefault(s => s.id_sign == _signer.Id);
                    if (oldsigner != null)
                    {
                        oldsigner.fio_sign = _signer.Fio;
                        oldsigner.position = _signer.Position;
                        oldsigner.@short = _signer.Short;
                        oldsigner.idSignType = _signer.SignTypeId;
                        l_dc.SubmitChanges();
                        res = oldsigner.id_sign;
                    }                                       
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            return res;
        }

        public Dictionary<byte, string> GetSignTypes()
        {
            Dictionary<byte, string> res = null;
            using (var l_dc = new SignsDCDataContext())
            {
                try
                {
                    res = l_dc.SignTypes.ToDictionary(st => st.id, st => st.Name);
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }
        
        public KeyValueObj<string, decimal> GetAkcStake(int _kpr, DateTime _date)
        {
            KeyValueObj<string, decimal> res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.uf_AkcStakeValAgr(_kpr, _date)//.Where(r => !String.IsNullOrWhiteSpace(r.kodval))
                        .Select(r => new KeyValueObj<string, decimal>(r.kodval, r.stake)).FirstOrDefault();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        public bool DoOtgrVozvrat(OtgrLine _otgr, OtgrLine _vozv)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return false;
            }

            bool res = false;

            res = AddOtgruz(_vozv, "Ручной ввод возврата отгрузки по накладной №" + _otgr.DocumentNumber.ToString());
            if (!res)
            {
                OnCrash("Ошибка", "Ошибка добавления накладной на возврат");
                return false;
            }

            _otgr.IdVozv = _vozv.Idrnn;

            res = UpdateOtgruz(_otgr, "Возвращено по накладной №" + _vozv.DocumentNumber.ToString());
            if (!res)
            {
                OnCrash("Ошибка", "Ошибка привязки возврата к отгрузке");
                return false;
            }

            return res;
        }

        public bool UnDoOtgrVozvrat(OtgrLine _vozv)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return false;
            }

            if (_vozv == null || !_vozv.IdVozv.HasValue || _vozv.IdVozv == 0 || _vozv.Kolf >= 0) return false; //проверка на возвратную накладную

            bool res = true;
            OtgrLine otgr = GetOtgrLine(_vozv.IdVozv.Value, true);
            if (otgr == null) otgr = GetOtgrLine(_vozv.IdVozv.Value, false);
            if (otgr != null && otgr.IdVozv == _vozv.Idrnn)
            {
                otgr.IdVozv = 0;
                res = UpdateOtgruz(otgr, "Отмена возврата");
            }

            if (res)
                res = DeleteOtgruz(_vozv);

            return res;
        }

        public VagonInfo GetVagonInfo(int _nv)
        {
            if (_nv <= 0) return null;

            VagonInfo res = null;

            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.uf_GetVagonInfo(_nv).Select(r => new VagonInfo { Nv = _nv, PrSv = r.prsv == 1}).FirstOrDefault();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            
            return res;
        }

        public Dictionary<byte, string> GetBuhSchetRecTypes()
        {
            Dictionary<byte, string> res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.sSchetRecTypes.ToDictionary(st => st.recType, st => st.recTypeName);
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }
        
        public Dictionary<byte, string> GetJournalUnionRecTypes()
        {
            Dictionary<byte, string> res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.sJournalUnionRecTypes.ToDictionary(st => st.recType, st => st.recTypeName);
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        public MeasureUnit[] GetMeasureUnits(int? _id)
        {
            MeasureUnit[] res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var data = l_dc.usp_GetMeasureUnits(_id).ToArray();
                    res = //l_dc.usp_GetMeasureUnits(null)
                        data.Select(u => new MeasureUnit 
                    { 
                        Id = u.id,
                        ShortName = u.shortname,
                        FullName = u.fullname,
                        NeiStat = u.neistat,
                        IsNeedDensity = u.density2tons ?? false
                    }).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        private Lazy<Cache<short, SfTypeInfo>> localSfTypesCache = new Lazy<Cache<short, SfTypeInfo>>(()=>new Cache<short, SfTypeInfo>(instance.GetSfTypeInfoAction));        

        private SfTypeInfo GetSfTypeInfoAction(short _sfTypeId)
        {
            SfTypeInfo res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.SfTypes.Where(t => t.Id == _sfTypeId)
                                      .Select(t => new SfTypeInfo
                                      {
                                          SfTypeId = (short)t.Id,
                                          SfTypeDescription = t.SfTypeName
                                      })
                                      .FirstOrDefault();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        public SfTypeInfo GetSfTypeInfo(short _sfTypeId)
        {
            return localSfTypesCache.Value.GetItem(_sfTypeId);
        }

        public DateTime GetLastOplDate(DateTime _firstDate, int _srok, short _respiteType)
        {
            DateTime res = _firstDate;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.uf_Srok_opl(_firstDate, _srok, _respiteType) ?? _firstDate;
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        public OtgrDocModel[] GetSfPrilDocs(int _idprilsf)
        {
            OtgrDocModel[] res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.uf_GetSfPrilDocs(_idprilsf).Select(d => new OtgrDocModel 
                    { 
                        IdInvoiceType = d.idInvoiceType ?? 0,
                        DocumentNumber = d.DocumentNumber,
                        Datgr = d.DatDoc.GetValueOrDefault()
                    }).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        public TypePlatDoc[] GetTypePlatDocs()
        {
            TypePlatDoc[] res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.sTypePlatDocs.Select(d => new TypePlatDoc
                        {
                            Id = d.id,
                            Name = d.name,
                            IsDoocDeb = d.isdoocdeb
                        }).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        public Dictionary<DateTime, bool> GetDates(DateTime? _dtfrom, DateTime? _dto)
        {
            Dictionary<DateTime, bool> res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    IQueryable<Date> data = l_dc.Dates;
                    if (_dtfrom.HasValue) data = data.Where(d => d.Date1 >= _dtfrom.Value);
                    if (_dto.HasValue) data = data.Where(d => d.Date1 <= _dto.Value);
                    res = data.ToDictionary(d => d.Date1, d => d.Holiday);
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        public bool SetDateInfo(DateTime _dt, bool _isholyday)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return false; 
            }

            bool res = false;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var date = l_dc.Dates.Single(d => d.Date1 == _dt);
                    date.Holiday = _isholyday;
                    l_dc.SubmitChanges();
                    res = true;
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                    res = false;
                }
            }
            return res;
        }

        public bool SetReportFavorite(ReportModel _rep)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return false;
            }

            bool res = false;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var exFavorite = l_dc.UserReports.FirstOrDefault(ur => ur.iduser == UserToken && ur.idreport == _rep.ReportId);
                    if (_rep.IsFavorite)
                    {
                        if (exFavorite == null)
                            l_dc.UserReports.InsertOnSubmit(new UserReport { idreport = _rep.ReportId, iduser = UserToken });
                    }
                    else
                    {
                        if (exFavorite != null)
                            l_dc.UserReports.DeleteOnSubmit(exFavorite);
                    }
                    l_dc.SubmitChanges();
                    res = true;
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                    res = false;
                }
            }
            return res;
        }

        private Lazy<Cache<int, InvoiceType>> localInvoiceTypeCache = new Lazy<Cache<int, InvoiceType>>(() => new Cache<int, InvoiceType>(instance.GetInvoiceTypeAction));

        private InvoiceType GetInvoiceTypeAction(int _idInvoiceType)
        {
            InvoiceType res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.uv_InvoiceTypes.Where(t => t.idInvoiceType == _idInvoiceType)
                                      .Select(t => new InvoiceType
                                      {
                                          IdInvoiceType = t.idInvoiceType,
                                          NameOfInvoiceType = t.NameOfInvoiceType,
                                          Notation = t.Notation
                                      })
                                      .FirstOrDefault();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        public InvoiceType GetInvoiceType(int _idInvoiceType)
        {
            return localInvoiceTypeCache.Value.GetItem(_idInvoiceType);
        }

        public InvoiceType[] GetInvoiceTypes()
        {
            InvoiceType[] res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.uv_InvoiceTypes.Select(t => new InvoiceType
                    {
                        IdInvoiceType = t.idInvoiceType,
                        NameOfInvoiceType = t.NameOfInvoiceType,
                        Notation = t.Notation
                    }).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        public bool Delete_ESFN(int _idsf)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return false;
            }

            bool res = false;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.usp_ESFN_Delete(_idsf) == 0;
                    if (!res)
                        ShowLastDbActionResult(l_dc, "usp_ESFN_Delete");
                }
                catch (Exception e)
                {
                    res = false;
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }   

        public EsfnData[] Make_ESFN(int _idsf)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return null;
            }

            EsfnData[] res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                l_dc.CommandTimeout = 180;
                try
                {
                    bool? sqlres = false;
                    var esfnData = l_dc.usp_ESFN_Create(_idsf, ref sqlres).FirstOrDefault();
                    if (sqlres.HasValue && sqlres.Value && esfnData != null && !esfnData.IsException)
                        res = Get_ESFN_Action(l_dc, _idsf);
                    else
                        ShowLastDbActionResult(l_dc, "usp_ESFN_Create");
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        public bool Approve_ESFN(int _idsf, bool _onoff)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return false;
            }

            bool res = false;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = _onoff ? (l_dc.usp_ESFN_Approve(_idsf) == 0) : (l_dc.usp_ESFN_CancelApprove(_idsf) == 0);                    
                    if (!res)
                        ShowLastDbActionResult(l_dc, _onoff ? "usp_ESFN_Approve" : "usp_ESFN_CancelApprove");
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        private EsfnData[] Get_ESFN_Action(RealizationDCDataContext _dc, int _idsf)
        {
            EsfnData[] res = null;
            var esfnData = _dc.usp_ESFN_Get(_idsf);
            if (esfnData != null)
            {
                res = esfnData.Select(d => new EsfnData
                {
                    VatInvoiceId = d.VatInvoiceId,
                    VatInvoiceNumber = d.VatInvoiceNumber,
                    BalSchet = d.BalSchet,
                    InVatInvoiceId = d.InVatInvoiceId,
                    InVatInvoiceNumber = d.InVatInvoiceNumber,
                    ApprovedByUserFIO = d.ApproveUser,
                    PrimaryIdsf = d.idsfprim,
                    RosterTotalCost = d.RosterTotalCostVat ?? 0,
                    InvoiceType = (InvoiceTypes) (d.InvoiceTypeId.GetValueOrDefault())
                }).ToArray();
            }
            return res;
        }

        public EsfnData[] Get_ESFN(int _idsf)
        {
            EsfnData[] res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = Get_ESFN_Action(l_dc, _idsf);
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }


        private Tuple<InvoiceStatuses, string, string> Get_ESFN_Status_Action(RealizationDCDataContext _dc, int _vatinvoiceid)
        {
            Tuple<InvoiceStatuses, string, string> res = null;
            var statusData = _dc.usp_ESFN_Get_Status(_vatinvoiceid).FirstOrDefault();
            if (statusData != null)
                res = Tuple.Create((InvoiceStatuses)statusData.StatusId, statusData.StatusName, statusData.StatusMessage);
            return res;

        }

        public Tuple<InvoiceStatuses, string, string> Get_ESFN_Status(int _vatinvoiceid)
        {
            Tuple<InvoiceStatuses, string, string> res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = Get_ESFN_Status_Action(l_dc, _vatinvoiceid);
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        public Tuple<int, string, DateTime?, string, string, decimal?, string>[] Get_Income_ESFN(int _idsf)
        {
            Tuple<int, string, DateTime?, string, string, decimal?, string>[] res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.usp_ESFN_Get_Income(_idsf).Select(i => Tuple.Create(i.InvoiceId, i.NumberString, i.DateIssuance, i.ProviderName, i.ProviderUnp, i.RosterTotalCostVat, i.Documents)).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        public bool Set_Income_ESFN(int _idsf, int? _invoiceId)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return false;
            }

            if (_idsf == 0) return false;
            bool res = false;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.usp_ESFN_Set_Income(_idsf, _invoiceId) == 0;
                    //NdsInvoiceData data = l_dc.NdsInvoiceDatas.SingleOrDefault(d => d.idsf == _idsf);
                    //if (data == null)
                    //{
                    //    data = new NdsInvoiceData { idsf = _idsf, idsfprim = _idsf };
                    //    l_dc.NdsInvoiceDatas.InsertOnSubmit(data);
                    //}
                    //data.InVatInvoiceId = _invoiceId;                    
                    //l_dc.SubmitChanges();
                    res = true;
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                    res = false;
                }
            }
            return res;
        }

        public SfModel[] Get_Primary_Sfs(int _idsf)
        {
            SfModel[] res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.usp_ESFN_Get_Primaries(_idsf).Select(i => new SfModel(i.IdSf, i.Version.ToArray()) 
                    { 
                        NumSf = i.numsf,
                        DatPltr = i.datpltr,
                        SumPltr = i.sumpltr,
                        Memo = i.Documents
                    }).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        public bool Set_Primary_ESFN(int _idsf, int _primaryIdsf)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return false;
            }

            if (_idsf == 0 || _primaryIdsf == 0) return false;
            bool res = false;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    NdsInvoiceData data = l_dc.NdsInvoiceDatas.SingleOrDefault(d => d.idsf == _idsf);
                    if (data == null && _primaryIdsf > 0)
                    {
                        data = new NdsInvoiceData { idsf = _idsf};
                        l_dc.NdsInvoiceDatas.InsertOnSubmit(data);
                    }
                    data.idsfprim = _primaryIdsf;
                    l_dc.SubmitChanges();
                    res = true;
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                    res = false;
                }
            }
            return res;
        }

        public Tuple<int, string, DateTime?, string, string, decimal?, string>[] Get_ESFNs_ToLink(int _idsf)
        {
            Tuple<int, string, DateTime?, string, string, decimal?, string>[] res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.usp_ESFN_Get_ToLink(_idsf).Select(i => Tuple.Create(i.InvoiceId, i.NumberString, i.DateIssuance, i.ProviderName, i.ProviderUnp,  i.RosterTotalCostVat, i.Documents)).ToArray();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

        public bool Set_ESFN_Link(int _idsf, int? _idsfprim, int? _invoiceId)
        {
            if (isReadOnly)
            {
                OnCrash("READONLY", "Доступ к данным в режиме только для чтения!");
                return false;
            }

            if (_idsf == 0) return false;
            bool res = false;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    var lres = l_dc.usp_ESFN_Link(_idsf, _idsfprim, _invoiceId);
                    res = lres == 0;
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                    res = false;
                }
            }
            return res;
        }

        public ESFNCreateOptions GetESFNCreateOptions(int _idsf)
        {
            ESFNCreateOptions res = null;
            using (var l_dc = new RealizationDCDataContext())
            {
                try
                {
                    res = l_dc.usp_GetESFNCreateOptions(_idsf)
                        .Select(d => new ESFNCreateOptions
                        {
                            IsVozvrat = d.isvozv ?? false,
                            IsVozmUsl = d.isvozm ?? false,
                        }).FirstOrDefault();
                }
                catch (Exception e)
                {
                    OnCrash(e.GetType().ToString(), e.Message);
                }
            }
            return res;
        }

    }
}