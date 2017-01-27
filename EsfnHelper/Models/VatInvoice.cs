using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataObjects.ESFN;

namespace EsfnHelper.Models
{
    public class VatInvoice
    {
        public int InvoiceId { get; set; }
        public string NumberString { get; set; }
        public InvoiceTypes InvoiceType { get; set; }
        public InvoiceStatuses Status { get; set; }
        public DateTime? DateIssuance { get; set; }
        public DateTime DateTransaction { get; set; }

        public string Account { get; set; }
        public string VatAccount { get; set; }
        public string ApproveUser { get; set; }

        public string ProviderStatusName { get; set; }
        public string ProviderUnp { get; set; }
        public string ProviderName { get; set; }
        public string ProviderAddress { get; set; }

        public string RecipientStatusName { get; set; }
        public string RecipientUnp { get; set; }
        public string RecipientName { get; set; }
        public string RecipientAddress { get; set; }

        public string ContractNumber { get; set; }
        public DateTime? ContractDate { get; set; }
        public string ContractDescription { get; set; }

        public decimal RosterTotalCost { get; set; }
        public decimal RosterTotalExcise { get; set; }
        public decimal RosterTotalVat { get; set; }
        public decimal RosterTotalCostVat { get; set; }

        public string Consignees { get; set; }
        public string Consignors { get; set; }
    }
}
