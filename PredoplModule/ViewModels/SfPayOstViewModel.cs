using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Interfaces;

namespace PredoplModule.ViewModels
{
    public class SfPayOstViewModel : BasicViewModel
    {
        private SfPayOst payOst;
        private IDbService repository;

        public SfPayOstViewModel(IDbService _rep, SfPayOst _ost)
        {
            payOst = _ost;
            repository = _rep;
        }

        public int IdPrilSf { get { return payOst.IdPrilSf; } }

        private SfProductPayModel sfPayModel;
        public SfProductPayModel SfPayModel
        {
            get
            {
                if (sfPayModel == null && payOst != null && payOst.IdPay > 0)
                    sfPayModel = repository.GetProductPayById(payOst.IdPay);
                return sfPayModel;
            }
        }


        //private SfPayTypeModel payTypeModel;

        private string payName;
        public string PayName
        {
            get 
            {
                if (payName == null)
                {
                    payName = GetPayName();
                }
                return payName;
            }
            set { SetAndNotifyProperty("PayName", ref payName, value); }
        }
        
        private string GetPayName()
        {
            string res = "Неизвестный тип платежа";
            if (SfPayModel != null || payOst != null && payOst.PayType > 0)
            {
                var ptype = (SfPayModel != null) ? SfPayModel.PayType : payOst.PayType;
                var payTypeModel = repository.GetPayTypeModel(ptype);
                if (payTypeModel != null)
                    res = payTypeModel.PayName;
            }
            else if (payOst != null && payOst.PayGroupId > 0)
            {
                res = repository.GetPayGroupName(payOst.PayGroupId);
            }
            return res;
        }

        public byte PayGroupId { get { return payOst.PayGroupId; } }
        public byte PayType { get { return payOst.PayType; } }
        
        public decimal Summa { get { return payOst.Summa; } }
    }
}
