using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Interfaces;

namespace CommonModule.DataViewModels
{
    public class SfProductPayViewModel : BasicViewModel, ITrackable
    {
        private SfProductPayModel modelRef;
        private string payName;
        private IDbService repository;

        public SfProductPayViewModel(IDbService _rep, SfProductPayModel _modelRef)
        {
            repository = _rep;
            modelRef = _modelRef;
        }

        public SfProductPayModel ModelRef
        {
            get { return modelRef; }
        }

        /// <summary>
        /// Наименование платежа
        /// </summary>
        public string PayName 
        {
            get
            {
                if (payName == null)
                    payName = repository.GetTunedPayName(modelRef.Idprilsf, modelRef.PayType);
                return payName;
            }
        }

        /// <summary>
        /// Сумма платежа
        /// </summary>
        public decimal PaySumma
        {
            get { return modelRef.Summa; }
            set 
            {
                if (value != modelRef.Summa)
                {
                    modelRef.Summa = value;
                    NotifyPropertyChanged("PaySumma");
                }
            }
        }

        /// <summary>
        /// Учитывать ли сумму платежа в общей сумме счёта
        /// </summary>
        public bool IsAddPayToSumma
        {
            get { return modelRef.Isaddtosum; }
            set 
            {
                if (value != modelRef.Isaddtosum)
                {
                    modelRef.Isaddtosum = value;
                    NotifyPropertyChanged("IsAddPayToSumma");
                }
            }
        }


        public TrackingInfo TrackingState 
        {
            get { return modelRef.TrackingState; }
            set { modelRef.TrackingState = value; }
        }
    }
}
