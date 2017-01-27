using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace DataObjects.ESFN
{    
    public class EsfnData
    {
        public int? VatInvoiceId { get; set; }
        public string VatInvoiceNumber { get; set; }        
        
        public string BalSchet { get; set; }
        public string BalSchetNds { get; set; }
        public DateTime? AccountingDate { get; set; }

        public InvoiceStatuses StatusId { get; set; }
        public string StatusName { get; set; }
        public string StatusMessage { get; set; }
        
        public int? InVatInvoiceId { get; set; }
        public string InVatInvoiceNumber { get; set; }

        public int? PrimaryIdsf { get; set; }
        public string ApprovedByUserFIO { get; set; }
        public decimal RosterTotalCost { get; set; }
        public InvoiceTypes InvoiceType { get; set; }

        //public string VatInvoiceNumDoc { get; set; }
        //public DateTime? VatInvoiceDatDoc { get; set; }
    }
}
