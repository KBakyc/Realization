using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DataObjects;
using DataObjects.Interfaces;
using DotNetHelper;

namespace CommonModule.DataViewModels
{
    public class SfLineViewModel
    {
        private IDbService repository;

        public SfLineViewModel(IDbService _rep)
        {
            repository = _rep;
        }

        public SfLineViewModel(IDbService _rep, SfProductModel _sfProductRef, bool _isVal)
            :this(_rep)
        {
            sfProductRef = _sfProductRef;
            IdprilSf = _sfProductRef.IdprilSf;
            CollectInfo(_isVal);
        }
        
        
        /// <summary>
        /// Загружает информацию о приложении счёта (продуктовую строку и дополнительные)
        /// </summary>
        private void CollectInfo(bool _isVal)
        {
            if (sfProductRef == null) return;
            var lines = repository.GetSfLine(IdprilSf);
            if (lines != null && lines.Length > 0)
            {
                productLineInfo = new SfTableLineViewModel(lines[0]);
                lineDopPays = new List<SfTableLineViewModel>(lines.Skip(1)
                                                                 .Select(l=>new SfTableLineViewModel(l)));
                if (_isVal)
                    lineDopPays.Add(new SfTableLineViewModel(new SfTableLine(LineTypes.DopPay){ Name = "Курс на {0:dd.MM.yy} -> {1:f6}".Format(sfProductRef.DatKurs ?? sfProductRef.DatGr, sfProductRef.KursVal) }));
            }
        }

        public int IdprilSf { get; private set; }

        // данные по продуктовой строчке (цена, суммы...)
        private SfTableLineViewModel productLineInfo;
        public SfTableLineViewModel ProductLineInfo
        {
            get
            {
                return productLineInfo;
            }
        }


        // Дополнительные платежи по продукту
        private List<SfTableLineViewModel> lineDopPays;
        public List<SfTableLineViewModel> LineDopPays
        {
            get
            {
                return lineDopPays;
            }
        }

        private SfProductModel sfProductRef;
        public SfProductModel SfProductRef
        {
            get
            {
                if (sfProductRef == null)
                    sfProductRef = new SfProductModel();
                return sfProductRef;
            }
        }

        private ProductInfo prodRef;
        public ProductInfo ProdRef
        {
            get
            {
                if (prodRef==null)
                    prodRef = repository.GetProductInfo(SfProductRef.Kpr);
                return prodRef;
            }
        }

        public String ProductName // наименование продукции / услуги
        {
            get
            {
                string res = ProdRef.Name;
                int l_idsp = SfProductRef.Idspackage;
                if (l_idsp!=0)
                    res += ' ' + repository.GetPackageVolume(l_idsp);

                return res;
            }
        }

        public int KodProd
        {
            get { return SfProductRef.Kpr; }
        }

        public String ProductNei //единицы измерения из product.nei
        {
            get
            {
                return ProdRef.EdIzm.Trim();
            }
        } 

        public Decimal KolProd  //количество продукции
        {
            get { return ProductLineInfo.TableLine.KolProd; }
        }
        
        public Decimal CenProd  //цена единицы продукции
        { 
            get
            {
                return ProductLineInfo.TableLine.CenProd;
            }
        }
        
        public Decimal SumProd //сумма по продукту 
        {
            get
            {
                return ProductLineInfo.TableLine.SumProd;
            }
        }

        public Decimal SumAkcProd //в т.ч. сумма акциза (треб форматирование)
        {
            get { return ProductLineInfo.TableLine.SumAkc; }
        }

        public Decimal NdsStake //ставка НДС
        {
            get 
            {
                return ProductLineInfo.TableLine.NdsSt;
            }
        }

        public Decimal NdsSum  //сумма НДС по продукту
        {
            get
            {
                return ProductLineInfo.TableLine.NdsSum;
            }
        }

        public Decimal ItogProd //итого по продукту
        {
            get
            {
                return ProductLineInfo.TableLine.SumItog;
            }
        }

        //возвращает количество строчек, занимаемое моделью при выводе
        public int NumLines
        {
            get
            {
                return 1 + LineDopPays.Count;// +DopInfo.Count;
            }
        }
    }
}