using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CommonModule.Commands;
using CommonModule.Helpers;
using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Interfaces;

namespace SfModule.ViewModels
{
    public class BonusSfOtgrDocsViewModel : OtgrDocsDlgViewModel
    {
        public BonusSfOtgrDocsViewModel(IDbService _rep, IEnumerable<OtgrDocModel> _docs)
            : base(_rep, _docs, o => o.Discount > 0)
        {
            otgrDocsVM.SubscribeToSelection();
        }

        /// <summary>
        /// Информация о первоначальном договоре
        /// </summary>
        public DogInfo  InDogInfo { get; set; }

        /// <summary>
        /// Инвормация о новом договоре
        /// </summary>
        private DogInfo outDogInfo;
        public DogInfo OutDogInfo
        {
            get { return outDogInfo; }
            set
            {
                if (value != outDogInfo)
                {
                    outDogInfo = value;
                    NotifyPropertyChanged("OutDogInfo");
                }
            }
        }

        public override bool IsValid()
        {
            return base.IsValid()
                && SelectedOtgrDocs.Any(d => d.Discount != 0);
        }

    }
}
