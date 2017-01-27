using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RwModule.Reports
{
    public class ExclDocsReportData
    {
        public int RwListNum { get; set; }
        public string Num_doc { get; set; }
        public DateTime Dat_doc { get; set; }
        public string Nkart { get; set; }
        public int PayType { get; set; }
        public string PayName { get; set; }
        public decimal Sum_doc { get; set; }
        public decimal Nds_rate { get; set; }
        public decimal Sum_nds { get; set; }
        public decimal Sum_itog { get; set; }
        public string Excl_info { get; set; }
        public string PoupName { get; set; }
    }
}
