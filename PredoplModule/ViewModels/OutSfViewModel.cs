using System;
using System.Linq;
using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Interfaces;
using System.Collections.Generic;


namespace PredoplModule.ViewModels
{
    /// <summary>
    /// Модель отображения данных о задолженности по счёту.
    /// </summary>
    public class OutSfViewModel : BasicViewModel
    {
        private IDbService repository;

        public OutSfViewModel(IDbService _rep, SfInListInfo _outSfRef)
        {
            repository = _rep;
            outSfDiRef = _outSfRef;
            outSfRef = repository.GetSfModel(outSfDiRef.IdSf);
        }

        private SfInListInfo outSfDiRef;
        private SfModel outSfRef;

        public int IdSf
        {
            get
            {
                return outSfRef.IdSf;
            }
        }

        public int NumSf
        {
            get
            {
                return outSfRef.NumSf;
            }
        }

        public SfTypeInfo SfType
        {
            get { return repository.GetSfTypeInfo(outSfRef.SfTypeId); }//SfTypesCache .Get(outSfRef.SfTypeId); }
        }

        public DateTime? DatPltr
        {
            get
            {
                return outSfRef.DatPltr;
            }
        }

        //private DateRange? datGr;
        public DateTime DatGr
        {
            get
            {
                //if (datGr == null)
                  //  datGr = outSfRef.DatBuch;//repository.GetSfDateGrRange(outSfRef.IdSf);
                return outSfRef.DatBuch.GetValueOrDefault();// datGr.Value.DateTo;
            }
        }

        private Valuta sfVal;
        public Valuta SfVal
        {
            get
            {
                if (sfVal == null)
                    sfVal = repository.GetValutaByKod(outSfRef.KodVal);
                return sfVal;
            }
        }

        //public decimal? KursVal
        //{
        //    get
        //    {
        //        return outSfRef.KursVal;
        //    }
        //}

        public decimal SumPltr
        {
            get
            {
                return outSfDiRef.SumPltr;
            }
        }

        public decimal SumOpl
        {
            get
            {
                return outSfDiRef.SumOpl;
            }
            set
            {
                outSfDiRef.SumOpl = value;
                NotifyPropertyChanged("SumOpl");
                NotifyPropertyChanged("SumOst");
            }
        }

        public decimal SumOst
        {
            get
            {
                return SumPltr - SumOpl;
            }
        }

        public string Osntxt
        {
            get
            {
                return outSfDiRef.OsnTxt;
            }
        }
        
        public string DopOsntxt
        {
            get
            {
                return outSfDiRef.DopOsnTxt;
            }
        }

        public DateTime? LastDatOpl
        {
            get
            {
                return outSfDiRef.LastDatOpl;
            }
        }

        private Dictionary<int,ProductInfo> sfProducts;
        public Dictionary<int, ProductInfo> SfProducts
        {
            get
            {
                if (sfProducts == null)
                    sfProducts = repository.GetSfProducts(IdSf).GroupBy(p => p.Kpr)
                                           .ToDictionary(g => g.Key,
                                                         g => repository.GetProductInfo(g.Key));
                return sfProducts;
            }
        }
    }
}