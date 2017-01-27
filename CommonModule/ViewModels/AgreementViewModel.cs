using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.Helpers;
using DataObjects;
using DataObjects.Interfaces;

namespace CommonModule.ViewModels
{
    public class AgreementViewModel : BasicNotifier
    {
        AgreementModel agreement;
        AgreementModel primaryAgreement;
        IDbService repository;

        public AgreementViewModel(AgreementModel _agree, IDbService _repository)
        {
            agreement = _agree;
            repository = _repository;
        }

        /// <summary>
        /// Договор или дополнение к договору
        /// </summary>
        public bool IsPrimary
        {
            get
            {
                return agreement.IdAgreement == agreement.IdPrimaryAgreement;
            }
        }

        public AgreementModel AgreementRef
        {
            get
            {
                return agreement;
            }
        }

        public string PrimaryDocName
        {
            get
            {
                return IsPrimary ? AgreementRef.NumberOfDocument
                                 : PrimaryAgreementRef.NumberOfDocument;
            }
        }

        public string DopDocName
        {
            get
            {
                return !IsPrimary ? String.Format("доп. {0}", AgreementRef.NumberOfDocument)
                                  : "";
            }
        }


        public DateTime DateOfDocument
        {
            get
            {
                return AgreementRef.DateOfDocument;
            }
        }
        
        public string Contents
        {
            get
            {
                return AgreementRef.Contents;
            }
        }


        /// <summary>
        /// Для дополнения возвращается основной договор
        /// </summary>
        public AgreementModel PrimaryAgreementRef
        {
            get
            {
                if (!IsPrimary && primaryAgreement == null && agreement.IdPrimaryAgreement != 0)
                    primaryAgreement = repository.GetAgreementById(agreement.IdPrimaryAgreement);
                return primaryAgreement;
            }
        }

        private string fullAgreeName;
        public string FullAgreeName
        {
            get
            {
                if (String.IsNullOrEmpty(fullAgreeName))
                    fullAgreeName = MakeFullAgreeName();
                return fullAgreeName;
            }
        }

        private string MakeFullAgreeName()
        {
            string res = AgreementRef.NumberOfDocument;
            if (!IsPrimary)
                res = String.Format("{0} доп. {1}", PrimaryAgreementRef.NumberOfDocument, res);
            return res;
        }



    }
}
