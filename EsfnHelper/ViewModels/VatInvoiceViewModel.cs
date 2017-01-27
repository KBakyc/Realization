using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.Helpers;
using EsfnHelper.Models;

namespace EsfnHelper.ViewModels
{
    public class VatInvoiceViewModel : BasicNotifier
    {
        public static VatInvoiceViewModel FromId(int _invoiceid)
        {
            VatInvoice header;
            Document[] docs;
            RosterItem[] roster;
            using (var db = new EsfnContext())
            {
                header = db.Get_ESFN_Header(_invoiceid);
                if (header == null) return null;
                docs = db.Get_ESFN_Documents(_invoiceid);
                roster = db.Get_ESFN_Roster(_invoiceid);
            }

            return new VatInvoiceViewModel(header, docs, roster);
        }

        public VatInvoiceViewModel(VatInvoice _header, Document[] _documents, RosterItem[] _roster)
        {
            if (_header == null) throw new ArgumentNullException();
            header = _header;
            documents = _documents;
            roster = _roster;
        }

        private VatInvoice header;
        public VatInvoice Header
        {
            get { return header; }
        }

        private Document[] documents;
        public Document[] Documents
        {
            get { return documents; }
        }

        private RosterItem[] roster;
        public RosterItem[] Roster
        {
            get { return roster; }
        }
    }
}
