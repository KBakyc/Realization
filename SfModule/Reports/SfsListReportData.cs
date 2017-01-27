using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SfModule.Reports
{
    public class SfsListReportData
    {
        public int NumSf { get; set; }
        public int Kgr { get; set; }
        public string KgrName { get; set; }
        public int Kpok { get; set; }
        public string KpokName { get; set; }
        public DateTime DateGr { get; set; }
        public DateTime DatePltr { get; set; }
        public string ValName { get; set; }
        public decimal SumPltr { get; set; }
        public bool IsDeleted { get; set; }
    }
}
