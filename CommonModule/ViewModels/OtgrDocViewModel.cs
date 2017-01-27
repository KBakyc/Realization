using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataObjects;
using DAL;
using DataObjects.Interfaces;
using CommonModule.Helpers;

namespace CommonModule.ViewModels
{
    public class OtgrDocViewModel : BasicViewModel
    {
        private IDbService repository;

        public OtgrDocViewModel(OtgrDocModel _mod, IDbService _repository)
            :this( _mod, _repository, true)
        {}

        public OtgrDocViewModel(OtgrDocModel _mod, IDbService _repository, bool _lazy)
        {
            ModelRef = _mod;
            repository = _repository;
            if (!_lazy)
                LoadData();
        }

        private void LoadData()
        {
            if (ModelRef.Kpr > 0)
                product = repository.GetProductInfo(ModelRef.Kpr);
            else
                product = repository.GetProductsByOtgrDoc(ModelRef.DocumentNumber, ModelRef.IdInvoiceType, ModelRef.Datgr, ModelRef.Kdog)
                                    .FirstOrDefault(); 
            if (ModelRef.KodCenprod != null)
                valutaCen = repository.GetValutaByKod(ModelRef.KodCenprod);
        }

        /// <summary>
        /// Ссылка на модель
        /// </summary>
        public OtgrDocModel ModelRef
        {
            get;
            private set;
        }
        
        private InvoiceType docInvoiceType;
        public InvoiceType DocInvoiceType
        {
            get
            {
                if (docInvoiceType == null && ModelRef.IdInvoiceType != 0)
                    docInvoiceType = repository.GetInvoiceType(ModelRef.IdInvoiceType);
                return docInvoiceType;
            }
        }

        public string DocName { get { return DocInvoiceType == null ? "док." : docInvoiceType.Notation; } }

        /// <summary>
        /// Признак наличия корректировочного счёта
        /// </summary>
        public bool HasCorrSf
        {
            get { return ModelRef.IdCorrsf != 0; }
        }

        /// <summary>
        /// Признак наличия скидки
        /// </summary>
        public bool HasDiscount
        {
            get { return ModelRef.Discount != 0; }
        }

        public decimal Kolf 
        { 
            get 
            {
                return ModelRef == null ? 0M : ModelRef.Kolf;
            }
            set
            {
                if (ModelRef != null)  
                {
                    ModelRef.Kolf = value;
                    NotifyPropertyChanged("Kolf");
                }
            }
        }

        public decimal Cenprod 
        { 
            get 
            {
                return ModelRef == null ? 0M : ModelRef.Cenprod;
            }
            set
            {
                if (ModelRef != null)  
                {
                    ModelRef.Cenprod = value;
                    NotifyPropertyChanged("Cenprod");
                }
            }
        }

        public decimal Sumprod 
        { 
            get 
            {
                return ModelRef == null ? 0M : ModelRef.Sumprod;
            }
            set
            {
                if (ModelRef != null)  
                {
                    ModelRef.Sumprod = value;
                    NotifyPropertyChanged("Sumprod");
                }
            }
        }

        /// <summary>
        /// Информация о продукте
        /// </summary>
        private ProductInfo product;
        public ProductInfo Product
        {
            get
            {
                if (product == null)
                    product = repository.GetProductsByOtgrDoc(ModelRef.DocumentNumber, ModelRef.IdInvoiceType, ModelRef.Datgr, ModelRef.Kdog)
                        .FirstOrDefault();
                return product;
            }
        }

        /// <summary>
        /// Информация о валюте цены
        /// </summary>
        private Valuta valutaCen;
        public Valuta ValutaCen
        {
            get
            {
                if (valutaCen == null && ModelRef.KodCenprod != null)
                    valutaCen = repository.GetValutaByKod(ModelRef.KodCenprod);
                return valutaCen;
            }
        }

    }
}
