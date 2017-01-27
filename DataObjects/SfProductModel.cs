using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataObjects
{
    public class SfProductModel
    {
        public int IdprilSf { get; set; }
        public int Kdog { get; set; }
        public DateTime DatGr { get; set; }
        public DateTime? DatKurs { get; set; }
        public decimal KursVal { get; set; }
        public int Kpr { get; set; }
        public decimal Kolf { get; set; }
        public int Vidcen { get; set; }
        public bool Bought { get; set; }
        public int Maker { get; set; }
        //public int Varsch { get; set; }
        public int Period { get; set; }
        public int IdSf { get; set; }
        public int Vozvrat { get; set; }
        public int Idspackage { get; set; }
    }
}
