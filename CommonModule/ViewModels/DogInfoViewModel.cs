using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataObjects;
using DAL;
using DataObjects.Interfaces;

namespace CommonModule.ViewModels
{
    public class DogInfoViewModel : BasicViewModel
    {
        private IDbService repository;

        public DogInfoViewModel(DogInfo _mod, IDbService _repository)
        {
            ModelRef = _mod;
            repository = _repository;
        }

        /// <summary>
        /// Ссылка на модель
        /// </summary>
        public DogInfo ModelRef
        {
            get;
            private set;
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
                    isDopDog = !String.IsNullOrEmpty(ModelRef.DopOsn)
                               && ModelRef.DatDop.HasValue;
                return isDopDog ?? false;
            }
        }

        //private bool? isAlterDog;
        //public bool IsAlterDog
        //{
        //    get
        //    {
        //        if (isAlterDog == null)
        //            isAlterDog = !String.IsNullOrEmpty(ModelRef.AlterDog)
        //                       && ModelRef.DatAlterDog.HasValue;
        //        return isAlterDog ?? false;
        //    }
        //}

        //private bool? isSpecDog;
        //public bool IsSpecDog
        //{
        //    get
        //    {
        //        if (isSpecDog == null)
        //            isSpecDog = !String.IsNullOrEmpty(ModelRef.SpecDog)
        //                       && ModelRef.DatSpecDog.HasValue;
        //        return isSpecDog ?? false;
        //    }
        //}

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
                    textOsn = IsDopDog ? String.Format("{0}, доп.{1}", ModelRef.NaiOsn, ModelRef.DopOsn) : ModelRef.NaiOsn;
                    //if (IsAlterDog)
                    //    textOsn += ", изм." + ModelRef.AlterDog;
                    //if (IsSpecDog)
                    //    textOsn += ", спец." + ModelRef.SpecDog;
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
        public DateTime DocDate
        {
            get
            {
                return 
                    //IsSpecDog ? ModelRef.DatSpecDog.GetValueOrDefault()
                    //             : IsAlterDog ? ModelRef.DatAlterDog.GetValueOrDefault() :
                                               IsDopDog ? ModelRef.DatDop.GetValueOrDefault()
                                                         : ModelRef.DatOsn;
            }
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

        ///// <summary>
        ///// Информация о грузополучателе
        ///// </summary>
        //private KontrAgent poluchatel;
        //public KontrAgent Poluchatel
        //{
        //    get
        //    {
        //        if (poluchatel == null)
        //            poluchatel = repository.GetKontrAgent(ModelRef.Kgr);
        //        return poluchatel;
        //    }
        //}

        /// <summary>
        /// Информация о валюте оплаты
        /// </summary>
        private Valuta valutaOpl;
        public Valuta ValutaOpl
        {
            get
            {
                if (valutaOpl == null)
                    valutaOpl = repository.GetValutaByKod(ModelRef.KodVal);
                return valutaOpl;
            }
        }

        public bool ProvozSpis
        {
            get { return ModelRef.Provoz == 1; }
        }


    }
}
