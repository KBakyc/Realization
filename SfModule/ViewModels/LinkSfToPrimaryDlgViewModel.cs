using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.ViewModels;
using DataObjects.Interfaces;
using CommonModule.DataViewModels;
using DataObjects;

namespace SfModule.ViewModels
{
    /// <summary>
    /// Модель диалога выбора исходного счёта-фактуры при возврате продукции.
    /// </summary>
    public class LinkSfToPrimaryDlgViewModel : BaseDlgViewModel
    {
        private IDbService repository;
        private SfViewModel sfv;

        public LinkSfToPrimaryDlgViewModel(IDbService _repository, SfViewModel _sfv)
        {
            repository = _repository;
            sfv = _sfv;
            LoadData();
        }

        public SfModel[] PrimarySfs { get; set; }

        private SfModel selectedPrimarySf;
        public SfModel SelectedPrimarySf
        {
            get { return selectedPrimarySf; }
            set { SetAndNotifyProperty("SelectedPrimarySf", ref selectedPrimarySf, value); }
        }

        private void LoadData()
        {
            PrimarySfs = repository.Get_Primary_Sfs(sfv.SfRef.IdSf);
        }

        public override bool IsValid()
        {
            return base.IsValid()
                && selectedPrimarySf != null;
        }
    }
}
