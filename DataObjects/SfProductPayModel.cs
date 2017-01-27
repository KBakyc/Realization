using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataObjects
{
    public class SfProductPayModel : ITrackable
    {
        private long id;
        private short payType;
        private int idprilsf;
        private decimal stake;
        private string kodval;
        private decimal kursval;
        private decimal kolf;
        private decimal summa;
        private bool isaddtosum;


        public SfProductPayModel(long _id, short _payType, int _idprilsf)
        {
            id = _id;
            payType = _payType;
            idprilsf = _idprilsf;
        }

        public long Id
        {
            get { return id; }
        }

        public short PayType
        {
            get { return payType; }
        }

        public int Idprilsf
        {
            get { return idprilsf; }
        }

        public decimal Stake
        {
            get { return stake; }
            set { stake = value; }
        }

        public string Kodval
        {
            get { return kodval; }
            set { kodval = value; }
        }    

        public decimal Kursval
        {
            get { return kursval; }
            set { kursval = value; }
        }

        public decimal Kolf
        {
            get { return kolf; }
            set { kolf = value; }
        }

        public decimal Summa
        {
            get { return summa; }
            set { summa = value; }
        }

        public bool Isaddtosum
        {
            get { return isaddtosum; }
            set { isaddtosum = value; }
        }

        public TrackingInfo TrackingState { get; set; }
    }
}
