using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataObjects
{
    public class ProductInfo
    {
        private int kpr;
        private string name;
        private string edIzm;

        public int Kpr
        {
            get { return kpr; }
            set { kpr = value; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public string EdIzm
        {
            get { return edIzm; }
            set { edIzm = value; }
        }

        public int Pkod { get; set; }

        public short IdSpackage { get; set; }

        public bool IsCena { get; set; }
        public bool IsGood { get; set; }
        public bool IsService { get; set; }
        public bool IsActive { get; set; }
        public int MeasureUnitId { get; set; }
        public bool IsInReal { get; set; }

        public int IdAkcGroup { get; set; }

        public int? IdProdType { get; set; }
    }
}
