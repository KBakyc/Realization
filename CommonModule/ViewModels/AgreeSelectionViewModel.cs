using System;
using System.Linq;
using CommonModule.Commands;
using CommonModule.Helpers;
using DataObjects;
using DataObjects.Interfaces;
using System.Collections.Generic;

namespace CommonModule.ViewModels
{
    public class AgreeSelectionViewModel : BaseDlgViewModel
    {
        private IDbService repository;
        private int selectedAgreeId;
        private int kpok;

        private AgreeSelectionViewModel(IDbService _rep)
        {
            repository = _rep;
        }

        public AgreeSelectionViewModel(IDbService _rep, int _kpok)
            :this(_rep)
        {
            kpok = _kpok;
            LoadAvailableAgreements();
        }

        public AgreeSelectionViewModel(IDbService _rep, int _kpok, int _idagree)
            : this(_rep, _kpok)
        {
            selectedAgreeId = _idagree;
            SelectedAgreement = GetAgreeByIdFromList(selectedAgreeId);
        }

        public override bool IsValid()
        {
            return base.IsValid() && SelectedAgreement != null;
        }

        public int SelectedAgreeId
        {
            get { return IsValid() ? SelectedAgreement.AgreementRef.IdAgreement : 0; }
            set
            {
                if (value != SelectedAgreeId)
                    SelectedAgreement = GetAgreeByIdFromList(value);
            }
        }

        private AgreementViewModel selectedAgreement;
        public AgreementViewModel SelectedAgreement
        {
            get
            {
                return selectedAgreement;
            }
            set
            {
                SetAndNotifyProperty("SelectedAgreement", ref selectedAgreement, value);
            }
        }

        private AgreementViewModel GetAgreeByIdFromList(int _idAgree)
        {
            AgreementViewModel res = null;
            if (_idAgree != 0)
            {
                res = availableAgreements.FirstOrDefault(a => a.AgreementRef.IdAgreement == _idAgree);
                if (res == null)
                {
                    var curAggreeM = repository.GetAgreementById(_idAgree);
                    res = new AgreementViewModel(curAggreeM, repository);
                    availableAgreements.Insert(0, res);
                }
            }
            return res;
        }

        private List<AgreementViewModel> availableAgreements;
        public List<AgreementViewModel> AvailableAgreements
        {
            get
            {
                if (availableAgreements == null)
                    LoadAvailableAgreements();
                return availableAgreements;
            }
        }

        private void LoadAvailableAgreements()
        {
            if (kpok != 0)
            {
                var data = repository.GetKpokAgreements(kpok).Select(a => new AgreementViewModel(a, repository));
                availableAgreements = data == null ? new List<AgreementViewModel>()
                                                   : data.ToList();
                var withoutagre = new AgreementModel
                {
                     IdCounteragent = kpok,
                     NumberOfDocument = "Нет договора",
                     Contents = "Нет договора"
                };

                var withoutagreeVM = new AgreementViewModel(withoutagre, repository);
                availableAgreements.Insert(0, withoutagreeVM);
            }
        }


    }
}
