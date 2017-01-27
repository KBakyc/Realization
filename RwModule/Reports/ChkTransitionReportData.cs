using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RwModule.Reports
{
    public class ChkTransitionReportData
    {
        public int RwListNum { get; set; }
        public DateTime RwListDate { get; set; }
        public string Num_doc { get; set; }
        public DateTime Dat_doc { get; set; }
        public string Note { get; set; }
        public decimal Sum_doc { get; set; }
        public decimal Nds_rate { get; set; }
        public decimal Sum_nds { get; set; }
        public decimal Sum_itog { get; set; }
        public DateTime? Rep_date { get; set; }
        public string PoupName { get; set; }
        public string UslType { get; set; }
    }
}
