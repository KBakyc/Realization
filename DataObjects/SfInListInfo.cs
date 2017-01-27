using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataObjects
{
    public class SfInListInfo
    {
        public SfInListInfo(int _idSf)
        {
            idSf = _idSf;
        }

        private int idSf;
        public int IdSf
        {
            get { return idSf; }
        }
        public PoupModel Poup { get; set; }
        public short Pkod { get; set; }
        public int NumSf { get; set; }
        public int Kgr { get; set; }
        public int Kpok { get; set; }
        public DateTime DatUch { get; set; }
        public DateTime DatPltr { get; set; }
        public string KodVal { get; set; }
        public decimal SumPltr { get; set; }
        public decimal SumOpl { get; set; }
        public string OsnTxt { get; set; }
        public string DopOsnTxt { get; set; }
        public DateTime DatStart { get; set; }
        public DateTime LastDatOpl { get; set; }
        public string TrShortName { get; set; }
        public LifetimeStatuses Status { get; set; }
        public PayStatuses PayStatus { get; set; }
        public short SfType { get; set; }
        public bool IsESFN { get; set; }
    }
}
