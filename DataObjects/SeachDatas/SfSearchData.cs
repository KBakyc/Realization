using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataObjects.SeachDatas
{
    public class SfSearchData
    {
        public int? Poup { get; set; }
        public short? Pkod { get; set; }
        public int? Kpok { get; set; }
        public int? Kgr { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int? NumsfFrom { get; set; }
        public int? NumsfTo { get; set; }        
        public LifetimeStatuses Status { get; set; }
        
        public string ESFN_BalSchet { get; set; }
        public string ESFN_Number { get; set; }
    }
}
