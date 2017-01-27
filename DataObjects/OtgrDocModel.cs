using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataObjects
{
    [Serializable]
    public class OtgrDocModel
    {
        private long id;

        public OtgrDocModel(long _id)
        {
            id = _id;
        }

        public OtgrDocModel()
        {

        }

        public int IdInvoiceType { get; set; }
        public string DocumentNumber { get; set; }
        public DateTime Datgr { get; set; }
        public int Kpr { get; set; }
        public int Kdog { get; set; }
        public decimal Kolf { get; set; }
        public decimal Cenprod { get; set; }
        public decimal Discount { get; set; }
        public decimal Sumprod { get; set; }
        public string KodCenprod { get; set; }
        public decimal KursCenprod { get; set; }
        public decimal NdsStake { get; set; }
        public string KodValNds { get; set; }
        public decimal KursValNds { get; set; }
        public decimal SumSper { get; set; }
        public string KodValSper { get; set; }
        public decimal KursValSper { get; set; }
        public decimal NdsStakeSper { get; set; }
        public string KodValNdsSper { get; set; }
        public decimal KursValNdsSper { get; set; }
        public decimal SumNdsSper { get; set; }
        public int IdPrilsf { get; set; }
        public int IdCorrsf { get; set; }


    }
}
