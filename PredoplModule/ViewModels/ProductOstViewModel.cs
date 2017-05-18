using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.ViewModels;
using DataObjects.Interfaces;
using DataObjects;

namespace PredoplModule.ViewModels
{
    /// <summary>
    /// Модель отображения неоплаченного остатка по приложению счёта.
    /// </summary>
    public class ProductOstViewModel : BasicViewModel
    {
        private SfProductModel sfProductRef;
        private SfPayOst[] payOsts;
        private IDbService repository;
        private OtgrDocViewModel[] sfPrilDocs;
        
        public ProductOstViewModel(IDbService _repository, SfProductModel _sfProductRef)
        {
            repository = _repository;
            sfProductRef = _sfProductRef;
            LoadData();
        }

        private decimal? sumOst;

        public decimal SumOst
        {           
            get 
            { 
                if (sumOst == null)
                    sumOst = CalcPrilSumOst();
                return sumOst.Value; 
            }
            set { SetAndNotifyProperty("SumOst", ref sumOst, value); }
        }

        private decimal CalcPrilSumOst()
        {
            decimal res = 0;
            if (payOsts != null)
                res = payOsts.Sum(po => po.Summa);
            return res;
        }

        public bool IsCanBePayed
        {
            get { return SumOst != 0; }
        }

        private void LoadData()
        {
            if (sfProductRef == null) return;
            payOsts = repository.GetSfPrilPaysOsts(sfProductRef.IdprilSf);
            var sfPrilDocsData = repository.GetSfPrilDocs(sfProductRef.IdprilSf);
            if (sfPrilDocsData != null)
                sfPrilDocs = sfPrilDocsData.Select(d => new OtgrDocViewModel(d, repository)).ToArray();
        }

        public SfPayOst[] PayOsts
        {
            get { return payOsts; }
        }

        private ProductInfo productRef;
        public ProductInfo ProductRef
        {
            get
            {
                if (productRef == null)
                    productRef = GetProductRef();
                return productRef;
            }
        }

        public OtgrDocViewModel[] SfPrilDocs
        {
            get { return sfPrilDocs; }
        }

        private ProductInfo GetProductRef()
        {
            ProductInfo res = null;
            if (repository != null && sfProductRef != null)
                res = repository.GetProductInfo(sfProductRef.Kpr);
            return res;
        }

        public string ProductName
        {
            get { return ProductRef == null ? "" : ProductRef.Name; }// String.Format("{0,-10} {1}", ProductRef.Kpr, ProductRef.Name); }
        }

        public int IdPrilSf
        {
            get { return sfProductRef == null ? 0 : sfProductRef.IdprilSf; }
        }

        public SfProductModel SfProduct
        {
            get { return sfProductRef; }
        }

    }
}
