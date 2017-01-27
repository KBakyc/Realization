using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataObjects.SeachDatas
{
    public class OtgruzSearchData
    {
        public OtgruzSearchData()
            :this(true)
        {
        }

        public OtgruzSearchData(bool _inRealiz)
        {
            InRealiz = _inRealiz;
        }

        public long? Id { get; set; }
        public long? IdRnn { get; set; }
        public bool InRealiz { get; set; }

        public int? InvoiceTypeId { get; set; }
        public string DocumentNumber { get; set; }
        public string RwBillNumber { get; set; }

        public int? Nv { get; set; }
        public DateTime? Dfrom { get; set; }
        public DateTime? Dto { get; set; }
        public short? Transportid { get; set; }
        public int? Poup { get; set; }
        public short? Pkod { get; set; }
        public int? Kdog { get; set; }
        public int? Kpok { get; set; }
        public int? Kgr { get; set; }
        public int? Kpr { get; set; }
    }
}
