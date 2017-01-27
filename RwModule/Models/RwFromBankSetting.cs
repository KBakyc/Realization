using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataObjects;

namespace RwModule.Models
{
    public class RwFromBankSetting
    {
        public int Id { get; set; }
        public RwUslType IdUslType { get; set; }
        public string Debet { get; set; }
        public string Credit { get; set; }
        public string FinNapr { get; set; }
        public byte IdBankGroup { get; set; }
    }
}
