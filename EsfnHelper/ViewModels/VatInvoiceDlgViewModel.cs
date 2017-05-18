using System;
using System.Linq;
using CommonModule.Commands;
using CommonModule.Helpers;
using CommonModule.ViewModels;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using EsfnHelper.Models;
using DataObjects.ESFN;

namespace EsfnHelper.ViewModels
{
    /// <summary>
    /// Модель диалога отображения ЭСФН.
    /// </summary>
    public class VatInvoiceDlgViewModel : BaseDlgViewModel
    {
        private VatInvoiceViewModel vatinvoice;

        public VatInvoiceDlgViewModel(VatInvoiceViewModel _vatinvoice)
        {
            if (_vatinvoice == null) throw new ArgumentNullException();
            vatinvoice = _vatinvoice;
            Title = "Информация об электронном счёте-фактуре";
        }

        public string NumberString 
        {
            get { return vatinvoice.Header.NumberString; } 
        }
        
        public InvoiceTypes InvoiceType 
        {
            get { return vatinvoice.Header.InvoiceType; }
        }

        public DateTime? DateIssuance 
        { 
            get { return vatinvoice.Header.DateIssuance; }
        }

        public DateTime DateTransaction 
        {
            get { return vatinvoice.Header.DateTransaction; }
        }

        public string ProviderStatusName 
        {
            get { return vatinvoice.Header.ProviderStatusName; } 
        }

        public string ProviderUnp 
        {
            get { return vatinvoice.Header.ProviderUnp; } 
        }

        public string ProviderName 
        {
            get { return vatinvoice.Header.ProviderName; } 
        }

        public string ProviderAddress 
        {
            get { return vatinvoice.Header.ProviderAddress; } 
        }

        public string RecipientStatusName 
        {
            get { return vatinvoice.Header.RecipientStatusName; } 
        }

        public string RecipientUnp 
        {
            get { return vatinvoice.Header.RecipientUnp; } 
        }

        public string RecipientName 
        {
            get { return vatinvoice.Header.RecipientName; } 
        }

        public string RecipientAddress 
        {
            get { return vatinvoice.Header.RecipientAddress; } 
        }

        public string ContractNumber 
        {
            get { return vatinvoice.Header.ContractNumber; } 
        }

        public DateTime? ContractDate 
        {
            get { return vatinvoice.Header.ContractDate; } 
        }

        public string ContractDescription 
        {
            get { return vatinvoice.Header.ContractDescription; } 
        }

        public string Consignees 
        {
            get { return vatinvoice.Header.Consignees; } 
        }

        public string Consignors 
        {
            get { return vatinvoice.Header.Consignors; } 
        }

        public decimal RosterTotalCost 
        {
            get { return vatinvoice.Header.RosterTotalCost; }
        }
        
        public decimal RosterTotalVat 
        {
            get { return vatinvoice.Header.RosterTotalVat; }
        }

        public decimal RosterTotalCostVat 
        {
            get { return vatinvoice.Header.RosterTotalCostVat; }
        }

        public Document[] Documents
        {
            get { return vatinvoice.Documents; }
        }
        
        public RosterItem[] Roster
        {
            get { return vatinvoice.Roster; }
        }
    }
}
