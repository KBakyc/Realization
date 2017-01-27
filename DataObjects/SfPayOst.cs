using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataObjects
{
    public class SfPayOst
    {
        public int IdPrilSf { get; set; }
        public long IdPay { get; set; }
        public byte PayType { get; set; }
        public byte PayGroupId { get; set; }
        public decimal Summa { get; set; }
    }
}
