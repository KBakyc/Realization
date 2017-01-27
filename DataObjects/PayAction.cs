using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataObjects;

namespace DataObjects
{
    public class PayAction
    {
        public PayActionTypes PayActionType { get; set; }
        public int IdPo { get; set; }
        public int Ndoc { get; set; }
        public DateTime DatDoc { get; set; }
        public int Idsf { get; set; }
        public int Numsf { get; set; }
        public DateTime DatPltr { get; set; }
        public int IdPrilsf { get; set; }
        public byte PayGroupId { get; set; }
        public byte PayType { get; set; }
        public string Whatfor { get; set; }
        public decimal SumOpl { get; set; }
        public string KodVal { get; set; } 
        public DateTime DatOpl { get; set; }
        public DateTime PayTime { get; set; }
    }
}
