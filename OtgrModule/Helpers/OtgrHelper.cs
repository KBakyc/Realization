using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OtgrModule.ViewModels;
using DataObjects.Interfaces;
using DataObjects;
using DotNetHelper;

namespace OtgrModule.Helpers
{
    public static class OtgrHelper
    {
        public static void SetNaklTotals(OtgrLineViewModel _line, IEnumerable<OtgrLineViewModel> _lines)
        {
            var doc = _line.DocumentNumber;
            var rwdoc = _line.RwBillNumber;
            DateTime datgr = _line.Datgr;
            int kpr = _line.Otgr.Kpr;
            var totals = GetOtgrTotals(_line, _lines);
            foreach (var l in _lines.Where(r => r.DocumentNumber == doc && r.RwBillNumber == rwdoc && r.Datgr == datgr && r.Otgr.Kpr == kpr))
                l.Totals = totals;
        }

        public static Dictionary<string, KeyValuePair<string, decimal>> GetOtgrTotals(OtgrLineViewModel _line, IEnumerable<OtgrLineViewModel> _lines)
        {
            IDbService repository = CommonModule.CommonSettings.Repository;
            Dictionary<string, KeyValuePair<string, decimal>> res = new Dictionary<string, KeyValuePair<string, decimal>>();
            bool isRW = _line.TransportId == (short)TransportTypes.Railway;
            int kpr = _line.Otgr.Kpr;
            bool isUsl = false; ;
            string edizm = null;
            if (_line.Product != null)
            {
                isUsl = _line.Product.IsService;
                edizm = _line.Product.EdIzm;
            }
            string doc = _line.DocumentNumber;
            string rwdoc = _line.RwBillNumber;
            int kdog = _line.Otgr.Kdog;
            int poup = _line.Otgr.Poup; 
            DateTime datgr = _line.Datgr;
            KeyValuePair<string, decimal> totval;
            string kodval = _line.Otgr != null ? _line.Otgr.Kodcen : null;
            string valRW = "руб.РБ";
            decimal doctotal = 0;
            decimal rwtotkolf = 0;
            OtgrLineViewModel[] ourlines = new OtgrLineViewModel[] {_line};

            if (_lines.Count(r => r.DocumentNumber == doc && r.Datgr == datgr && r.Otgr.Poup == poup && r.Otgr.Kpr == kpr) > 1)
            {
                ourlines = _lines.Where(r => r.DocumentNumber == doc && r.Datgr == datgr && r.Otgr.Poup == poup && r.Otgr.Kpr == kpr).ToArray();
                doctotal = ourlines.Sum(r => r.Kolf);
                if (doctotal != 0)
                {
                    totval = new KeyValuePair<string, decimal>(edizm, doctotal);
                    res.Add("Кол-во " + (isUsl ? "услуги" : "продукта") + " по документу:", totval);
                }
            }
            if (isRW && !String.IsNullOrWhiteSpace(rwdoc))
            {
                ourlines = _lines.Where(r => r.RwBillNumber == rwdoc && r.Datgr == datgr && r.Otgr.Poup == poup && r.Otgr.Kpr == kpr).ToArray();
                decimal rwtotsper = 0;
                decimal rwtotspernds = 0;
                decimal rwtotsperdop = 0;
                decimal rwtotsperdopnds = 0;                
                foreach (var l in ourlines)
                {
                    var o = l.Otgr;
                    rwtotkolf += l.Kolf;
                    rwtotsper += o.Sper;
                    rwtotspernds += o.Ndssper;
                    rwtotsperdop += o.Dopusl;
                    rwtotsperdopnds += o.Ndsdopusl;
                }
                if (rwtotkolf != 0)
                {
                    totval = new KeyValuePair<string, decimal>(edizm, rwtotkolf);
                    res.Add("Кол-во " + (isUsl ? "услуги" : "продукта") + " по ЖД накладной:", totval);
                }
                if (rwtotsper != 0)
                {
                    totval = new KeyValuePair<string, decimal>(valRW, rwtotsper);
                    res.Add("За провоз по ЖД накладной:", totval);
                }
                if (rwtotspernds != 0)
                {
                    totval = new KeyValuePair<string, decimal>(valRW, rwtotspernds);
                    res.Add("НДС за провоз по ЖД накладной:", totval);
                }
                if (rwtotsperdop != 0)
                {
                    totval = new KeyValuePair<string, decimal>(valRW, rwtotsperdop);
                    res.Add("За маршрут:", totval);
                }
                if (rwtotsperdopnds != 0)
                {
                    totval = new KeyValuePair<string, decimal>(valRW, rwtotsperdopnds);
                    res.Add("НДС за маршрут:", totval);
                }
            }            

            if (!_line.Poup.Poup.IsDav)
            {
                var ourlinesbycena = ourlines.GroupBy(l => new { Cena = (l.Product == null || !l.Product.IsCena ? 0M : l.Cena), 
                                                                 Val = l.Otgr.Kodcen, 
                                                                 IsCena = l.Product == null ? false : l.Product.IsCena,
                                                                 Datgr = l.Datgr,
                                                                 ProdNds = l.Otgr.Prodnds,
                                                                 IsSumNds = l.Otgr.SumNds != null})
                                             .Select(g => new { Cena = g.Key.Cena, 
                                                                Val = g.Key.Val,
                                                                Kolf = g.Sum(l => l.Kolf),
                                                                IsCena = g.Key.IsCena,
                                                                Datgr = g.Key.Datgr,
                                                                ProdNds = g.Key.ProdNds,
                                                                IsSumNds = g.Key.IsSumNds,
                                                                SumNds = g.Key.IsSumNds ? g.Sum(l => l.Otgr.SumNds ?? 0) : 0M});
                decimal docsumprod = 0M;
                decimal docsumnds = 0M;

                foreach (var l in ourlinesbycena)
                {
                    var sumprod = CalcProduct(repository, l.Cena, l.Val, l.Kolf, l.IsCena, l.Datgr);
                    docsumprod += sumprod;
                    if (_line.Otgr.Prodnds > 0)
                        docsumnds += l.IsSumNds ? l.SumNds : CalcNds(repository, sumprod, l.ProdNds, l.Val, l.Datgr);                    
                }

                var prodVal = _line.Otgr.Kodcen;
                totval = new KeyValuePair<string, decimal>(prodVal, docsumprod);
                res.Add(String.Format("За " + (isUsl ? "услугу" : "продукт") + " по {0}:", _line.DocName), totval);
                if (docsumnds != 0)
                {
                    totval = new KeyValuePair<string, decimal>(prodVal, docsumnds);
                    res.Add(String.Format("За НДС по {0}:", _line.DocName), totval);
                    totval = new KeyValuePair<string, decimal>(prodVal, docsumprod + docsumnds);
                    res.Add(String.Format("Сумма по {0}:", _line.DocName), totval);
                }                
            }

            return res;
        }

        public static decimal CalcNds(IDbService _repository, decimal _sum, decimal _stake, string _kodval, DateTime _date)
        {
            decimal res = 0;

            var newsumnds = _sum * _stake / 100;
            var kodval = _kodval ?? "RB";
            res = _repository.ConvertSumToVal(newsumnds, kodval, kodval, _date, null, null);

            return res;
        }

        public static decimal CalcProduct(IDbService _repository, decimal _cena, string _kodval, decimal _kolf, bool _iscena, DateTime _date)
        {
            var sProd = _cena;
            var kodval = _kodval ?? "RB";
            if (_kolf != 0 && _iscena)
                sProd *= _kolf;            
            sProd = _repository.ConvertSumToVal(sProd, kodval, kodval, _date, null, null);
            return sProd;
        }
    }
}
