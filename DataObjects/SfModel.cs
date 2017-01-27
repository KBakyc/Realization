using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataObjects
{
    public class SfModel : ITrackable
    {
        public SfModel(int idSf, byte[] ver)
        {
            this.idSf = idSf;
            this.version = ver;
        }

        private int idSf;
        private int numSf;
        private int poup;
        private int idDog;
        private DateTime datPltr;
        private DateTime? datBuch;
        private SfPayPeriodModel sfPeriod;
        private int kotpr;
        private int kgr;
        private int kpok;
        private int stOtpr;
        private int stPol;
        private short transportId;
        private string kodVal;
        private decimal sumPltr;
        private short sfTypeId;
        //private decimal sumOpl;
        private byte[] version;

        //private EntitySet<SfProduct> _SfProducts;

        public int IdSf
        {
            get { return idSf; }
        }

        public int Poup
        {
            get { return poup; }
            set { poup = value; }
        }

        public int IdDog
        {
            get { return idDog; }
            set { idDog = value; }
        }

        public DateTime DatPltr
        {
            get { return datPltr; }
            set { datPltr = value; }
        }

        //public DateTime DatGr { get; set; }

        public DateTime? DatBuch
        {
            get { return datBuch; }
            set { datBuch = value; }
        }

        //public DateTime? DatKurs
        //{
        //    get { return datKurs; }
        //    set { datKurs = value; }
        //}

        public SfPayPeriodModel SfPeriod
        {
            get { return sfPeriod; }
            set { sfPeriod = value; }
        }

        public int Kotpr
        {
            get { return kotpr; }
            set { kotpr = value; }
        }

        public int Kgr
        {
            get { return kgr; }
            set { kgr = value; }
        }

        public int Kpok
        {
            get { return kpok; }
            set { kpok = value; }
        }

        public int StOtpr
        {
            get { return stOtpr; }
            set { stOtpr = value; }
        }

        public int StPol
        {
            get { return stPol; }
            set { stPol = value; }
        }

        public short TransportId
        {
            get { return transportId; }
            set { transportId = value; }
        }

        public string KodVal
        {
            get { return kodVal; }
            set { kodVal = value; }
        }

        public decimal SumPltr
        {
            get { return sumPltr; }
            set { sumPltr = value; }
        }

        public int NumSf
        {
            get { return numSf; }
            set { numSf = value; }
        }

        /// <summary>
        /// 0 - обычный, 1 - корректировочный
        /// </summary>
        public short SfTypeId 
        {
            get { return sfTypeId; }
            set { sfTypeId = value; }
        }

        public byte[] Version
        {
            get { return version; }
        }

        public string Memo { get; set; }
        public byte SfStatus { get; set; }
        public byte PayStatus { get; set; }
        public DateTime? PayDate { get; set; }

        #region ITrackable Members

        public TrackingInfo TrackingState { get; set; }

        #endregion
    }
}
