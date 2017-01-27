using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Data.SqlClient;
using System;
using System.Collections.Generic;
using DotNetHelper;
using EsfnHelper.Models;

namespace EsfnHelper
{
    public partial class EsfnContext : DbContext
    {
        static EsfnContext()
        {
            Database.SetInitializer<EsfnContext>(null);
        }

        public EsfnContext()
            : base("Name=DAL.Properties.Settings.RealConnectionString")
        {
            Configuration.ProxyCreationEnabled = false;
            Configuration.AutoDetectChangesEnabled = false;
            Configuration.LazyLoadingEnabled = false;
        }


        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            Configuration.ProxyCreationEnabled = false;
            Configuration.AutoDetectChangesEnabled = false;
            Configuration.LazyLoadingEnabled = false;
        }

        //public int[] Search_ESFN_Ids(string _numend)
        //{
        //    int[] res = null;

        //    if (!String.IsNullOrWhiteSpace(_numend))
        //        res = this.Database.SqlQuery<int>(String.Format(@"select InvoiceId from uv_VatInvoice where NumberString like '%{0}'", _numend)).ToArray();

        //    return res;
        //}

        public VatInvoice Get_ESFN_Header(int _invoiceid)
        {
            VatInvoice res = null;

            res = this.Database.SqlQuery<VatInvoice>("exec usp_ESFN_Get_Header {0}".Format(_invoiceid)).FirstOrDefault();

            return res;
        }

        public Document[] Get_ESFN_Documents(int _invoiceid)
        {
            Document[] res = null;

            res = this.Database.SqlQuery<Document>("exec usp_ESFN_Get_Documents {0}".Format(_invoiceid)).ToArray();

            return res;
        }

        public RosterItem[] Get_ESFN_Roster(int _invoiceid)
        {
            RosterItem[] res = null;

            res = this.Database.SqlQuery<RosterItem>("exec usp_ESFN_Get_Roster {0}".Format(_invoiceid)).ToArray();

            return res;
        }
    }
}
