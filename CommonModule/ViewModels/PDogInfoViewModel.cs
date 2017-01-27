using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataObjects;
using DAL;
using DataObjects.Interfaces;

namespace CommonModule.ViewModels
{
    public class PDogInfoViewModel : BasicViewModel
    {
        private IDbService repository;

        public PDogInfoViewModel(PDogInfoModel _mod, IDbService _repository)
        {
            ModelRef = _mod;
            repository = _repository;
        }

        /// <summary>
        /// Ссылка на модель
        /// </summary>
        public PDogInfoModel ModelRef
        {
            get;
            private set;
        }

        private PoupModel poup;
        public PoupModel Poup
        {
            get
            {
                if (poup == null)
                    poup = repository.Poups[ModelRef.Poup];
                return poup;
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
                    product = repository.GetProductInfo(ModelRef.Kprod);
                return product;
            }
        }

        /// <summary>
        /// Полное название продукта (с упаковкой)
        /// </summary>
        public string fullProductName;
        public string FullProductName
        {
            get 
            {
                if (fullProductName == null)
                    fullProductName = ModelRef.Idspackage == 0 ? Product.Name 
                                                               : String.Format("{0} {1}",Product.Name,repository.GetPackageVolume(ModelRef.Idspackage));
                return fullProductName;
            }
        }

        /// <summary>
        /// Признак дополнения
        /// </summary>
        private bool? isDopDog;
        public bool IsDopDog
        {
            get
            {
                if (isDopDog == null)
                    isDopDog = !String.IsNullOrEmpty(ModelRef.Dopdog) 
                               && ModelRef.Datdopdog.HasValue;
                return isDopDog ?? false;
            }
        }

        private bool? isAlterDog;
        public bool IsAlterDog
        {
            get
            {
                if (isAlterDog == null)
                    isAlterDog = !String.IsNullOrEmpty(ModelRef.AlterDog)
                               && ModelRef.DatAlterDog.HasValue;
                return isAlterDog ?? false;
            }
        }
        
        private bool? isSpecDog;
        public bool IsSpecDog
        {
            get
            {
                if (isSpecDog == null)
                    isSpecDog = !String.IsNullOrEmpty(ModelRef.SpecDog)
                               && ModelRef.DatSpecDog.HasValue;
                return isSpecDog ?? false;
            }
        }

        /// <summary>
        /// Полное название договора (с дополнением)
        /// </summary>
        private string textOsn;
        public string TextOsn
        {
            get
            {
                if (textOsn == null)
                {
                    textOsn = IsDopDog ? String.Format("{0}, доп.{1}", ModelRef.Osn, ModelRef.Dopdog) : ModelRef.Osn;
                    if (IsAlterDog)
                        textOsn += ", изм." + ModelRef.AlterDog;
                    if (IsSpecDog)
                        textOsn += ", спец." + ModelRef.SpecDog;
                }
                return textOsn;
            }
        }

        private string kfondStr;
        public string KfondStr
        {
            get
            {
                if (kfondStr == null)
                    kfondStr = (ModelRef.Kfond > 0 && ModelRef.Kfond != ModelRef.Kpok) ? String.Format("Влад.: {0}", ModelRef.Kfond) : "";
                return kfondStr;
            }
        }

        /// <summary>
        /// Дата документа (или договора, или дополнения)
        /// </summary>
        public DateTime PDogDate
        { 
            get { return IsSpecDog ? ModelRef.DatSpecDog.GetValueOrDefault() 
                                   : IsAlterDog ? ModelRef.DatAlterDog.GetValueOrDefault() 
                                                : IsDopDog ? ModelRef.Datdopdog.GetValueOrDefault() 
                                                           : ModelRef.Datd; }
        }

        /// <summary>
        /// Информация о плательщике
        /// </summary>
        private KontrAgent platelschik;
        public KontrAgent Platelschik
        {
            get
            {
                if (platelschik == null)
                    platelschik = repository.GetKontrAgent(ModelRef.Kpok);
                return platelschik;
            }
        }

        /// <summary>
        /// Информация о грузополучателе
        /// </summary>
        private KontrAgent poluchatel;
        public KontrAgent Poluchatel
        {
            get
            {
                if (poluchatel == null)
                    poluchatel = repository.GetKontrAgent(ModelRef.Kgr);
                return poluchatel;
            }
        }

        /// <summary>
        /// Информация о валюте оплаты
        /// </summary>
        private Valuta valutaOpl;
        public Valuta ValutaOpl
        {
            get
            {
                if (valutaOpl == null)
                    valutaOpl = repository.GetValutaByKod(ModelRef.Kodval);
                return valutaOpl;
            }
        }

        public bool ProvozSpis
        {
            get { return ModelRef.Provoz == 1; }
        }


    }
}
