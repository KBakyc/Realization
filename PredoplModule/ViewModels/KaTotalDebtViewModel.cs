using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Interfaces;


namespace PredoplModule.ViewModels
{
    public class KaTotalDebtViewModel : BasicViewModel
    {
        private IDbService repository;

        public KaTotalDebtViewModel(IDbService _rep, KaTotalDebt _outst)
        {
            repository = _rep;
            outstRef = _outst;
        }

        private KaTotalDebt outstRef;
        public KaTotalDebt OutstRef 
        { 
            get
            {
                if (outstRef == null)
                {
                    outstRef = new KaTotalDebt();
                }
                return outstRef;
            }
        }

        // плательщик
        private KontrAgent platelschik;
        public KontrAgent Platelschik
        {
            get
            {
                if (platelschik == null)
                    platelschik = repository.GetKontrAgent(OutstRef.Kpok);
                return platelschik;
            }
        }

        // Сумма неоплаченных счетов
        public decimal SumNeopl
        {
            get
            {
                return OutstRef.SumNeopl;
            }
        }

        // Сумма доступной предоплаты
        public decimal SumPredopl
        {
            get
            {
                return OutstRef.SumPredopl;
            }
        }

        // Сумма непогашенных возвратов
        public decimal SumVozvrat
        {
            get
            {
                return OutstRef.SumVozvrat;
            }
        }
    }
}