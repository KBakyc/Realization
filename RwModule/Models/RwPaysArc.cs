using System;
using System.Collections.Generic;

namespace RwModule.Models
{
    public sealed class RwPaysArc
    {
        public long Id { get; set; }
        public RwPayActionType Payaction { get; set; }
        public int? Idrwplat { get; set; }        
        public long? Iddoc { get; set; }        
        public decimal Summa { get; set; }
        public string Notes { get; set; }
        public DateTime Datopl { get; set; }
        public int Userid { get; set; }
        public DateTime Atime { get; set; }
    }
}
