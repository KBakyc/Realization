using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataObjects
{
    [Serializable]
    public class PredoplModel : ITrackable
    {
        public PredoplModel(int idpo, byte[] version)
        {
            this.idpo = idpo;
            this.version = version;
        }

        private int idpo;
        private int poup;
        private short pkod;
        private int kgr;
        private int kpokreal;
        private int ndok;
        private DateTime datpropl;
        private DateTime datvvod;
        private DateTime? datzakr;

        private short nop;
        private short prpropl;

        private string kodval;
        private decimal kursval;
        private DateTime? datkurs;
        private string kodval_b;

        private int idRegDoc;
//		private System.Nullable<int> _bh;
//		private System.Nullable<int> _idaddagree;
    
        private decimal sumpropl;
        private decimal sum_bank;
        private decimal sumotgr;

//		private System.DateTime _datsys;
        
//		private System.Nullable<int> _userid;
        
        private string whatfor;
        private short direction;
        private string prim;

        private byte[] version;

        public int Idpo
        {
            get { return idpo; }
            set { idpo = value; }
        }

        public int IdAgree
        {
            get;
            set;
        }

        public byte[] Version
        {
            get { return version; }
        }

        public int Poup
        {
            get { return poup; }
            set { poup = value; }
        }

        public short Pkod
        {
            get { return pkod; }
            set { pkod = value; }
        }

        public int Kgr
        {
            get { return kgr; }
            set { kgr = value; }
        }

        public int Kpokreal
        {
            get { return kpokreal; }
            set { kpokreal = value; }
        }

        public short Nop
        {
            get { return nop; }
            set { nop = value; }
        }

        public short Prpropl
        {
            get { return prpropl; }
            set { prpropl = value; }
        }

        public int Ndok
        {
            get { return ndok; }
            set { ndok = value; }
        }

        public DateTime DatPropl
        {
            get { return datpropl; }
            set { datpropl = value; }
        }

        public DateTime DatVvod
        {
            get { return datvvod; }
            set { datvvod = value; }
        }

        public DateTime? DatZakr
        {
            get { return datzakr; }
            set { datzakr = value; }
        }

        public string KodVal
        {
            get { return kodval; }
            set { kodval = value; }
        }

        public decimal KursVal
        {
            get { return kursval; }
            set { kursval = value; }
        }

        public DateTime? DatKurs
        {
            get { return datkurs; }
            set { datkurs = value; }
        }

        public string KodValB
        {
            get { return kodval_b; }
            set { kodval_b = value; }
        }

        public int IdRegDoc
        {
            get { return idRegDoc; }
            set { idRegDoc = value; }
        }

        public decimal SumPropl
        {
            get { return sumpropl; }
            set { sumpropl = value; }
        }

        public decimal SumBank
        {
            get { return sum_bank; }
            set { sum_bank = value; }
        }

        public decimal SumOtgr
        {
            get { return sumotgr; }
            set { sumotgr = value; }
        }

        public string Whatfor
        {
            get { return whatfor; }
            set { whatfor = value; }
        }

        public short Direction
        {
            get { return direction; }
            set { direction = value; }
        }

        public string Prim
        {
            get { return prim; }
            set { prim = value; }
        }

        public int IdSourcePO { get; set; }
        public byte IdTypeDoc { get; set; }

        #region ITrackable Members

        public TrackingInfo TrackingState { get; set;}

        #endregion
    }
}
