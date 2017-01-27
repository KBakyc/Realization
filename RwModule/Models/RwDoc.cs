using System;
using System.Collections.Generic;

namespace RwModule.Models
{
    public sealed class RwDoc
    {
        public long Id_rwdoc { get; set; }
        public int Id_rwlist { get; set; }
        public string Num_doc { get; set; }
        public DateTime Dat_doc { get; set; }
        public short Paycode { get; set; }
        public decimal Sum_doc { get; set; }
        public decimal Sum_nds { get; set; }
        public decimal Ndsrate { get; set; }
        public string Note { get; set; }
        public string Kodst { get; set; }
        public long Keysbor { get; set; }
        public string Nkrt { get; set; }
        public DateTime? Dzkrt { get; set; }
        public DateTime? Rep_date { get; set; }
        public bool Exclude { get; set; }
        public decimal Sum_excl { get; set; }
        public string Excl_info { get; set; }
        public string Comments { get; set; }
        public decimal Sum_opl { get; set; }
        
        public RwList RwList { get; set; }
        public RwDocIncomeEsfn Esfn { get; set; }
    }
}
