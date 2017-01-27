using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OtgrModule.Reports
{
    public class VagListReportData
    {
        public int Nv { get; set; }
        public DateTime Datgr { get; set; }
        public string RwBillNumber { get; set; }
        public string DocumentNumber { get; set; }
        public int Kpr { get; set; }
        public string KprName { get; set; }
        public int Poup { get; set; }
        public int Kodf { get; set; }
        public int Kpok { get; set; }
        public string KpokName { get; set; }        
        public string Numsf { get; set; }
        public decimal SumSper { get; set; }
        public decimal NdsSt { get; set; }
        public decimal SumNds { get; set; }
        public decimal SumItog { get; set; }
        public bool IsProvozSpis { get; set; }
    }
}
