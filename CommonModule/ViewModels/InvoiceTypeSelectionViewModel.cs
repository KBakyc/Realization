using System;
using System.Linq;
using CommonModule.Commands;
using CommonModule.Helpers;
using DataObjects;
using DataObjects.Interfaces;
using System.Collections.Generic;

namespace CommonModule.ViewModels
{
    /// <summary>
    /// Модель диалога выбора типа документа.
    /// </summary>
    public class InvoiceTypeSelectionViewModel : BaseDlgViewModel
    {
        private IDbService repository;

        public InvoiceTypeSelectionViewModel(IDbService _rep)
            : this(_rep, null)
        {
        }

        public InvoiceTypeSelectionViewModel(IDbService _rep, Func<InvoiceType, bool> _filt)
        {
            repository = _rep;
            var idata = repository.GetInvoiceTypes();
            invoiceTypesList = (_filt == null ? idata : idata.Where(_filt)).ToList();            
        }

        private InvoiceType allSelectOption;
        public InvoiceType AllSelectOption
        {
            get 
            {
                if (allSelectOption == null)
                    allSelectOption = new InvoiceType { NameOfInvoiceType = "Все типы документов", Notation = "ВСЕ" };
                return allSelectOption; 
            }
            set 
            { 
                allSelectOption = value;
                SetAllSelectOption();
            }
        }

        private bool isAllSelectOption;
        public bool IsAllSelectOption
        {
            get { return isAllSelectOption; }
            set 
            { 
                if (SetAndNotifyProperty("", ref isAllSelectOption, value))
                    SetAllSelectOption(); 
            }
        }

        private void SetAllSelectOption()
        {
            if (invoiceTypesList != null)
            {
                if (invoiceTypesList.Any(t => t.IdInvoiceType == 0))
                    invoiceTypesList.RemoveAll(t => t.IdInvoiceType == 0);
                invoiceTypesList.Add(AllSelectOption);
            }
        }

        private List<InvoiceType> invoiceTypesList;
        public List<InvoiceType> InvoiceTypesList
        {
            get
            {
                return invoiceTypesList;
            }
        }

        public string SelectionTitle { get; set; }

        private InvoiceType selInvoiceType;
        public InvoiceType SelInvoiceType
        {
            get { return selInvoiceType; }
            set { SetAndNotifyProperty("SelInvoiceType", ref selInvoiceType, value); }
        }

        public override bool IsValid()
        {
            return base.IsValid()
                && SelInvoiceType != null;
        }
    }
}
