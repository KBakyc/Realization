using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.ViewModels;
using System.Collections.ObjectModel;
using DataObjects;
using DataObjects.Interfaces;
using DotNetHelper;
using RwModule.Models;
using DAL;
using DataObjects.ESFN;

namespace RwModule.ViewModels
{
    public class LinkRwDocToEsfnViewModel : BasicViewModel
    {
        private RwDocViewModel doc;        

        public LinkRwDocToEsfnViewModel(RwDocViewModel _doc)
        {
            doc = _doc;
            LoadData();
        }

        private void LoadAllESFNs()
        {
            using (var db = new RealContext())
            {
                allRwDocEsfns = db.GetAllRwDocEsfns(doc.Id_rwdoc);
            }

            isAllRwDocEsfnsLoaded = true;
        }

        private void LoadData()
        {
            if (doc == null) return;
            using (var db = new RealContext())
            {
                linkedEsfn = db.RwDocIncomeEsfns.FirstOrDefault(e => e.Id_rwdoc == doc.Id_rwdoc);                
            }
            if (linkedEsfn == null)
            {
                LoadAllESFNs();

                if (allRwDocEsfns != null)
                {
                    var sumalldoc = doc.RwPay.IdUslType == RwUslType.Provoz
                        ? doc.ModelRef.RwList.RwDocs.Where(d => d.Num_doc == doc.Num_doc).Sum(d => d.Sum_doc + d.Sum_nds)
                        : doc.ModelRef.RwList.RwDocs.Where(d => d.Nkrt == doc.Nkrt).Sum(d => d.Sum_doc + d.Sum_nds);
                    var newLinkedEsfn = allRwDocEsfns.Where(e => e.RosterTotalCost == doc.Sum_itog || e.RosterTotalCost == sumalldoc).ToArray();
                    if (newLinkedEsfn != null && newLinkedEsfn.Length == 1)
                    {
                        selRwDocEsfn = newLinkedEsfn[0];
                        accountingDate = newLinkedEsfn[0].AccountingDate;//doc.ModelRef.Rep_date;//doc.ModelRef.RwList.Dat_orc;
                    }
                }
            }
            else
            {
                accountingDate = linkedEsfn.AccountingDate;
                approveUser = linkedEsfn.ApproveUser;
                approveDate = linkedEsfn.ApproveDate;
                selRwDocEsfn = new EsfnData
                {
                    VatInvoiceId = linkedEsfn.VatInvoiceId,
                    VatInvoiceNumber = linkedEsfn.VatInvoiceNumber,
                    BalSchet = linkedEsfn.Account,
                    BalSchetNds = linkedEsfn.VatAccount,
                    ApprovedByUserFIO = linkedEsfn.ApproveUser,
                    InvoiceType = linkedEsfn.InvoiceType
                };
            }
        }

        public void SetNewLinkedESFN(EsfnHelper.Models.VatInvoice _invoice)
        {
            EsfnData newsel = null;
            if (_invoice != null)
            {
                if (allRwDocEsfns != null && allRwDocEsfns.Length > 0)
                    newsel = allRwDocEsfns.FirstOrDefault(e => e.VatInvoiceId == _invoice.InvoiceId);
                if (newsel == null)
                    newsel = new EsfnData 
                    {
                        VatInvoiceId = _invoice.InvoiceId,
                        VatInvoiceNumber = _invoice.NumberString,
                        BalSchet = _invoice.Account,
                        BalSchetNds = _invoice.VatAccount,
                        ApprovedByUserFIO = _invoice.ApproveUser,
                        InvoiceType = _invoice.InvoiceType
                    };
            }
            SelRwDocEsfn = newsel;
        }

        private RwDocIncomeEsfn linkedEsfn;
        public RwDocIncomeEsfn LinkedEsfn
        {
            get { return linkedEsfn; }
        }

        private bool isAllRwDocEsfnsLoaded = false;
        private EsfnData[] allRwDocEsfns;
        public EsfnData[] AllRwDocEsfns
        {
            get 
            {
                if (!isAllRwDocEsfnsLoaded)
                {
                    LoadAllESFNs();
                    if (allRwDocEsfns != null && allRwDocEsfns.Length > 0 && linkedEsfn != null)
                        SelRwDocEsfn = allRwDocEsfns.FirstOrDefault(e => e.VatInvoiceId == linkedEsfn.VatInvoiceId);
                }
                return allRwDocEsfns; 
            }
        }

        private EsfnData selRwDocEsfn;
        public EsfnData SelRwDocEsfn
        {
            get { return selRwDocEsfn; }
            set 
            { 
                SetAndNotifyProperty("SelRwDocEsfn", ref selRwDocEsfn, value);
                NotifyPropertyChanged("IsChanged");
                NotifyPropertyChanged("EsfnNumber");
                NotifyPropertyChanged("Account");
                NotifyPropertyChanged("VatAccount");
                if (selRwDocEsfn == null)
                {
                    AccountingDate = null;
                }
                else
                    if (linkedEsfn != null && selRwDocEsfn.VatInvoiceId == linkedEsfn.VatInvoiceId)
                    {
                        AccountingDate = linkedEsfn.AccountingDate;
                    }
                    else
                        AccountingDate = doc.Rep_date;//doc.ModelRef.RwList.Dat_orc;
            }
        }

        public RwDocViewModel DocVm { get { return doc; } }

        public string NumDoc
        {
            get { return doc.RwPay.IdUslType == RwUslType.DopSbor ? doc.Nkrt : doc.Num_doc; }
        }

        public DateTime? DatDoc
        {
            get { return doc.RwPay.IdUslType == RwUslType.DopSbor ? doc.Dzkrt : doc.Dat_doc; }
        }

        public int PayCode
        {
            get { return doc.RwPay.Paycode; }
        }

        public string PayName
        {
            get { return doc.RwPay.Payname; }
        }

        public decimal SumPay
        {
            get { return doc.Sum_doc; }
        }

        public decimal SumNds
        {
            get { return doc.Sum_nds; }
        }

        public decimal SumItog
        {
            get { return doc.Sum_itog; }
        }

        public string EsfnNumber
        {
            get { return selRwDocEsfn == null ? null : selRwDocEsfn.VatInvoiceNumber; }
        }        

        public string Account 
        {
            get { return selRwDocEsfn == null ? null : selRwDocEsfn.BalSchet; }
        }

        public string VatAccount
        {
            get { return selRwDocEsfn == null ? null : selRwDocEsfn.BalSchetNds; }
        }

        private DateTime? accountingDate;
        public DateTime? AccountingDate
        {
            get { return accountingDate; }
            set 
            { 
                SetAndNotifyProperty("", ref accountingDate, value); 
                NotifyPropertyChanged("IsChanged");
            }
        }

        private string approveUser;
        public string ApproveUser
        {
            get { return approveUser; }
            set 
            { 
                SetAndNotifyProperty("ApproveUser", ref approveUser, value);
                NotifyPropertyChanged("IsChanged");
            }
        }

        private DateTime? approveDate;
        public DateTime? ApproveDate
        {
            get { return approveDate; }
            set 
            { 
                SetAndNotifyProperty("", ref approveDate, value); 
                NotifyPropertyChanged("IsChanged");
            }
        }

        public bool IsLinked
        {
            get 
            {
                return linkedEsfn != null;
            }
        }

        public bool IsLinkChanged
        {
            get 
            {
                return linkedEsfn == null && selRwDocEsfn != null 
                    || linkedEsfn != null 
                                  && (selRwDocEsfn == null 
                                                   || accountingDate.GetValueOrDefault() != linkedEsfn.AccountingDate
                                                   || linkedEsfn.VatInvoiceId != selRwDocEsfn.VatInvoiceId);
            }
        }

        public bool IsCanLink
        {
            get 
            {
                return selRwDocEsfn != null && selRwDocEsfn.VatInvoiceId > 0 && allRwDocEsfns.Any(e => e.VatInvoiceId == selRwDocEsfn.VatInvoiceId)
                    && accountingDate.HasValue;
            }
        }

        //public bool IsApproveChanged
        //{
        //    get 
        //    {
        //        return linkedEsfn != null 
        //            && (approveUser != linkedEsfn.ApproveUser || approveDate.GetValueOrDefault() != linkedEsfn.ApproveDate.GetValueOrDefault());
        //    }
        //}
    }
}
