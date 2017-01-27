using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataObjects;
using DataObjects.ESFN;

namespace RwModule.Models
{
    public sealed class RwDocIncomeEsfn
    {
        public long Id_rwdoc { get; set; }
        public int VatInvoiceId { get; set; }
        public string VatInvoiceNumber { get; set; }
        //public byte InvoiceTypeId { get; set; }
        public InvoiceTypes InvoiceType { get; set; }
        public string Account { get; set; }
        public string VatAccount { get; set; }
        public DateTime AccountingDate { get; set; }
        public string ApproveUser { get; set; }
        public DateTime? ApproveDate { get; set; }
        public DateTime? DeductionDate { get; set; }
        public decimal? SummaVat { get; set; }

        public RwDoc RwDoc { get; set; }
    }
}
