using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataObjects;

namespace RwModule.Models
{
    public class RwBuhSchet
    {
        public int Id { get; set; }
        public int Poup { get; set; }
        public RefundTypes	VidUsl { get; set; }
        public int KodUsl { get; set; }
        public byte SumType { get; set; }
        public string DebUsl { get; set; }
        public string KreUsl { get; set; }
        public string DebOpl { get; set; }
        public string KreOpl { get; set; }
    }
}
