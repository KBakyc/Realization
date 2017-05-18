using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Interfaces;
using DataObjects.ESFN;

namespace CommonModule.ViewModels
{
    /// <summary>
    /// Модель отображения данных по ЭСФН.
    /// </summary>
    public class EsfnDataViewModel : BasicViewModel
    {
        private EsfnData esfn;
        private IDbService repository;

        public EsfnDataViewModel(IDbService _repository, EsfnData _esfn)
        {
            repository = _repository;
            esfn = _esfn;
            PrimaryIdsf = _esfn.PrimaryIdsf;
            VatInvoiceId = _esfn.VatInvoiceId;
            VatInvoiceNumber = _esfn.VatInvoiceNumber;
            InVatInvoiceId = _esfn.InVatInvoiceId;
            InVatInvoiceNumber = _esfn.InVatInvoiceNumber;
            VatInvoiceId = _esfn.VatInvoiceId;
            BalSchet = _esfn.BalSchet;
            RosterTotalCost = _esfn.RosterTotalCost;
            ApprovedByUserFIO = _esfn.ApprovedByUserFIO;
            InvoiceType = _esfn.InvoiceType;
            GetStatusAsync();
        }

        private void GetStatusAsync()
        {
            System.Threading.Tasks.Task.Factory.StartNew(GetStatus);
        }

        public void GetStatus()
        {
            IsStatusLoaded = false;
            if (VatInvoiceId != null)
            {
                var status = repository.Get_ESFN_Status(VatInvoiceId.Value);
                if (status != null)
                {
                    StatusId = status.Item1;
                    StatusName = status.Item2;
                    StatusMessage = status.Item3;
                    IsStatusLoaded = true;
                }
            }
        }

        private bool isStatusLoaded;
        public bool IsStatusLoaded
        {
            get { return isStatusLoaded; }
            set { SetAndNotifyProperty("IsStatusLoaded", ref isStatusLoaded, value); }
        }

        public int? PrimaryIdsf { get; set; }
        public int? VatInvoiceId { get; set; }
        public string VatInvoiceNumber { get; set; }
        
        public int? InVatInvoiceId { get; set; }
        public string InVatInvoiceNumber { get; set; }
        
        public string BalSchet { get; set; }
        public decimal RosterTotalCost { get; set; }

        public string ApprovedByUserFIO { get; set; }

        public InvoiceTypes InvoiceType { get; set; }

        public InvoiceStatuses StatusId
        {
            get { return esfn.StatusId; }
            set 
            { 
                esfn.StatusId = value;
                NotifyPropertyChanged("StatusId");
            }
        }
        
        public string StatusName 
        {
            get { return esfn.StatusName; }
            set
            {
                esfn.StatusName = value;
                NotifyPropertyChanged("StatusName");
            }
        }

        public string StatusMessage 
        {
            get { return esfn.StatusMessage; }
            set
            {
                esfn.StatusMessage = value;
                NotifyPropertyChanged("StatusMessage");
            }
        }
    }
}
