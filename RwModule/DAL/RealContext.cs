using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using RwModule.Models.Mapping;
using RwModule.Models;
using System.Linq;
using System.Data.SqlClient;
using System;
using System.Collections.Generic;
using DotNetHelper;
using DataObjects;
using DataObjects.ESFN;

namespace DAL
{
    public partial class RealContext : DbContext
    {
        static RealContext()
        {
            Database.SetInitializer<RealContext>(null);
        }

        public RealContext()
            : base("Name=DAL.Properties.Settings.RealConnectionString")
        {
            Configuration.ProxyCreationEnabled = false;
            Configuration.AutoDetectChangesEnabled = false;
            Configuration.LazyLoadingEnabled = false;     
        }

        public DbSet<RwDoc> RwDocs { get; set; }
        public DbSet<RwList> RwLists { get; set; }
        public DbSet<RwPayType> RwPayTypes { get; set; }
        public DbSet<RwPaysArc> RwPaysArcs { get; set; }
        public DbSet<RwBuhSchet> RwBuhSchets { get; set; }
        public DbSet<RwFromBankSetting> RwFromBankSettings { get; set; }
        public DbSet<RwPlat> RwPlats { get; set; }
        public DbSet<SumType> SumTypes { get; set; }
        public DbSet<RwModuleLog> RwModuleLogs { get; set; }
        public DbSet<RwDocIncomeEsfn> RwDocIncomeEsfns { get; set; }

        public override int SaveChanges()
        {
            return CommonModule.CommonSettings.IsDALReadOnly ? 0 : base.SaveChanges();
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            Configuration.ProxyCreationEnabled = false;
            Configuration.AutoDetectChangesEnabled = false;
            Configuration.LazyLoadingEnabled = false;

            modelBuilder.Configurations.Add(new RwDocMap());
            modelBuilder.Configurations.Add(new RwListMap());
            modelBuilder.Configurations.Add(new RwPayTypeMap());
            modelBuilder.Configurations.Add(new RwPaysArcMap());
            modelBuilder.Configurations.Add(new RwBuhSchetMap());
            modelBuilder.Configurations.Add(new SumTypeMap());
            modelBuilder.Configurations.Add(new RwFromBankSettingMap());
            modelBuilder.Configurations.Add(new RwPlatMap());
            modelBuilder.Configurations.Add(new RwModuleLogMap());
            modelBuilder.Configurations.Add(new RwDocIncomeEsfnMap());
            
            modelBuilder.Entity<ReportModel>().HasKey(t => t.ReportId).Ignore(t => t.Parameters).Ignore(t => t.DataSources);//.Ignore(t => t.IsFavorite);
            modelBuilder.Entity<EsfnData>().HasKey(t => t.VatInvoiceId).Ignore(t => t.InVatInvoiceId).Ignore(t => t.InVatInvoiceNumber).Ignore(t => t.PrimaryIdsf);
        }

        private static Dictionary<short, RwPayType> rwPayTypeCache;

        public RwPayType GetRwPayType(short _paycode)
        {
            RwPayType res = null;
            if (rwPayTypeCache == null) PopulateRwPayTypeCache();
            if (rwPayTypeCache != null)
                rwPayTypeCache.TryGetValue(_paycode, out res);
            return res;
        }

        private object syncres = new object();

        private void PopulateRwPayTypeCache()
        {
            lock (syncres)
            {
                if (rwPayTypeCache == null)
                {
                    rwPayTypeCache = RwPayTypes.ToDictionary(r => r.Paycode, r => new RwPayType { Paycode = r.Paycode, Payname = r.Payname, IdUslType = r.IdUslType });
                    if (!rwPayTypeCache.ContainsKey(0))
                        rwPayTypeCache.Add(0, new RwPayType { Paycode = 0, Payname = "" });
                }
            }
        }

        private static Dictionary<byte, SumType> sumTypeCache;
        
        public SumType GetSumType(byte _sumtype)
        {
            SumType res = null;
            if (sumTypeCache == null) PopulateSumTypeCache();
            if (sumTypeCache != null)
                sumTypeCache.TryGetValue(_sumtype, out res);
            return res;
        }

        public SumType[] GetSumTypes()
        {
            SumType[] res = null;
            if (sumTypeCache == null) PopulateSumTypeCache();
            if (sumTypeCache != null)
                res = sumTypeCache.Values.ToArray();
            return res;
        }

        private void PopulateSumTypeCache()
        {
            lock (syncres)
            {
                if (sumTypeCache == null)
                {
                    sumTypeCache = SumTypes.ToDictionary(r => r.Id, r => r);
                    if (!sumTypeCache.ContainsKey((byte)0))
                        sumTypeCache.Add(0, new SumType { Id = 0, SumName = "Итог" });
                }
            }
        }

        public RwList[] GetNewRwLists()
        {
            RwList[] res = null;
            
            try
            {
                res = this.Database.SqlQuery<RwList>("exec usp_CollectNewRwLists").ToArray();
            }
            catch
            {}

            return res;
        }
        
        public bool AcceptNewRwList(long _keykrt)
        {
            if (CommonModule.CommonSettings.IsDALReadOnly) return false;

            bool res = false;
            
            try
            {
                var keykrtParam = new SqlParameter("keykrt", System.Data.SqlDbType.BigInt) {Value = _keykrt };
                var sres = new SqlParameter("RC", System.Data.SqlDbType.Int) { Direction = System.Data.ParameterDirection.Output };
                this.Database.ExecuteSqlCommand("exec @RC = usp_AcceptRwList @keykrt", sres, keykrtParam);
                res = (int)sres.Value == 0;
            }
            catch
            {}

            return res;
        }

        public ReportModel[] GetRwListReports(int _id_rwlist)
        {
            ReportModel[] res = null;
            try
            {
                res = this.Database.SqlQuery<ReportModel>("exec usp_GetRWListReports {0}".Format(_id_rwlist)).ToArray();
                if (res != null && res.Length > 0)
                    foreach (var r in res)
                    {
                        r.Parameters = new Dictionary<string, string> { { "ConnString", this.Database.Connection.ConnectionString } };
                    }
            }
            catch
            { }
            return res;
        }

        public RwPlat[] GetRwPlatsFromBank(DateTime _dfrom, DateTime _dto, RwUslType _rwusl, byte _idbankgroup)
        {
            RwPlat[] res = null;
            
            try
            {                
                var dfromParam = new SqlParameter("dfrom", System.Data.SqlDbType.DateTime) {Value = _dfrom };
                var dtoParam = new SqlParameter("dto", System.Data.SqlDbType.DateTime) {Value = _dto };
                var rwuslParam = new SqlParameter("idusltype", System.Data.SqlDbType.TinyInt) {Value = _rwusl };
                var bankParam = new SqlParameter("idbankgroup", System.Data.SqlDbType.TinyInt) {Value = _idbankgroup };
                res = this.Database.SqlQuery<RwPlat>("exec usp_GetRwPlatsFromBank @dfrom, @dto, @idusltype, @idbankgroup", dfromParam, dtoParam, rwuslParam, bankParam).ToArray();

                //var conn = this.Database.Connection;
                //if (conn.State != System.Data.ConnectionState.Open)
                //    conn.Open();
                //var cmdres = this.Database.Connection.CreateCommand();
                //cmdres.CommandText = "usp_GetRwPlatsFromBank";
                //cmdres.CommandType = System.Data.CommandType.StoredProcedure;
                //cmdres.Parameters.Add(dfromParam);cmdres.Parameters.Add(dtoParam);cmdres.Parameters.Add(rwuslParam);cmdres.Parameters.Add(bankParam);
                //List<RwPlat> readRes = new List<RwPlat>();
                //using (var reader = cmdres.ExecuteReader())                
                //    while (reader.Read())
                //    {
                //        readRes.Add(new RwPlat()
                //        { 
                //            Numplat = reader.GetInt32(reader.GetOrdinal("numplat")),
                //            Debet = reader.GetString(reader.GetOrdinal("debet")),
                //            Credit = reader.GetString(reader.GetOrdinal("credit")),
                //            Datbank = reader.GetDateTime(reader.GetOrdinal("datbank")),
                //            Datplat = reader.GetDateTime(reader.GetOrdinal("datplat")),
                //            Direction = (RwPlatDirection)reader.GetByte(reader.GetOrdinal("direction")),
                //            Idusltype = (RwUslType)reader.GetByte(reader.GetOrdinal("idusltype")),
                //            Idpostes = reader.GetInt32(reader.GetOrdinal("idpostes"))
                //        });
                //    }
                //res = readRes.ToArray();
                //conn.Close();
            }
            catch (Exception e)
            {
                CommonModule.Helpers.WorkFlowHelper.OnCrash(e);
            }

            return res;
        }

        public bool MarkRwListReceived(long _keykrt)                
        {
            if (CommonModule.CommonSettings.IsDALReadOnly) return false;

            bool res = false;
            
            try
            {
                var keykrtParam = new SqlParameter("keykrt", System.Data.SqlDbType.BigInt) { Value = _keykrt };
                var sres = new SqlParameter("RC", System.Data.SqlDbType.Int) { Direction = System.Data.ParameterDirection.Output };
                this.Database.ExecuteSqlCommand("exec @RC = usp_MarkRwListReceived @keykrt", sres, keykrtParam);
                res = (int)sres.Value == 0;
            }
            catch (Exception e)
            {
                CommonModule.Helpers.WorkFlowHelper.OnCrash(e);
            }

            return res;
        }

        public RwDocInfo GetRwDocInfo(long _id_rwdoc)
        {
            RwDocInfo res = null;

            try
            {
                //res = ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<RwDocInfo>("dbo.uf_GetRwDocInfo", new System.Data.Entity.Core.Objects.ObjectParameter("id_rwdoc", _id_rwdoc));
                res = this.Database.SqlQuery<RwDocInfo>("select * from uf_GetRwDocInfo({0})".Format(_id_rwdoc)).FirstOrDefault();
            }
            catch
            { }

            return res;
        }

        public decimal CalcRwDocSumOpl(long _id_rwdoc)
        {
            decimal res = 0M;
            try
            {
                res = RwPaysArcs.Where(a => a.Iddoc == _id_rwdoc && (a.Payaction == RwPayActionType.PayUsl || a.Payaction == RwPayActionType.CloseUsl)).Sum(a => a.Summa);
            }
            catch (Exception e)
            {
                CommonModule.Helpers.WorkFlowHelper.OnCrash(e);
            }
            return res;
        }

        public EsfnData[] GetAllRwDocEsfns(long _id_rwdoc)
        {
            EsfnData[] res = null;
            try
            {
                res = this.Database.SqlQuery<EsfnData>("exec usp_GetRwDocESFNsToLink {0}".Format(_id_rwdoc)).ToArray();                
            }
            catch (Exception e)
            {
                CommonModule.Helpers.WorkFlowHelper.OnCrash(e);
            }
            return res;
        }

        public bool LinkRwDocToESFN(long _id_rwdoc, int _vatinvoiceid, string _account, string _vatAccount, DateTime _accountingDate)
        {
            if (CommonModule.CommonSettings.IsDALReadOnly) return false;
            bool res = false;

            var id_rwdocParam = new SqlParameter("id_rwdoc", System.Data.SqlDbType.BigInt) { Value = _id_rwdoc };
            var vatInvoiceIdParam = new SqlParameter("VatInvoiceId", System.Data.SqlDbType.Int) { Value = _vatinvoiceid };
            var accountParam = new SqlParameter("Account", System.Data.SqlDbType.VarChar, 8) { Value = _account };
            var vatAccountParam = new SqlParameter("VatAccount", System.Data.SqlDbType.VarChar, 8) { Value = _vatAccount };
            var accountingDateParam = new SqlParameter("AccountingDate", System.Data.SqlDbType.Date) { Value = _accountingDate };
            var sres = new SqlParameter("RC", System.Data.SqlDbType.Int) { Direction = System.Data.ParameterDirection.Output };
            this.Database.ExecuteSqlCommand("exec @RC = usp_LinkRwDocToESFN @id_rwdoc, @VatInvoiceId, @Account, @VatAccount, @AccountingDate",
                                                                            sres, id_rwdocParam, vatInvoiceIdParam, accountParam, vatAccountParam, accountingDateParam);
            res = (int)sres.Value == 0;

            return res;
        }
        
        public bool UnLinkRwDocFromESFN(long _id_rwdoc, int _vatinvoiceid)
        {
            if (CommonModule.CommonSettings.IsDALReadOnly) return false;
            bool res = false;

            var id_rwdocParam = new SqlParameter("id_rwdoc", System.Data.SqlDbType.BigInt) { Value = _id_rwdoc };
            var vatInvoiceIdParam = new SqlParameter("VatInvoiceId", System.Data.SqlDbType.Int) { Value = _vatinvoiceid };
            var sres = new SqlParameter("RC", System.Data.SqlDbType.Int) { Direction = System.Data.ParameterDirection.Output };
            this.Database.ExecuteSqlCommand("exec @RC = usp_UnLinkRwDocFromESFN @id_rwdoc, @VatInvoiceId", sres, id_rwdocParam, vatInvoiceIdParam);
            res = (int)sres.Value == 0;

            return res;
        }        

        public bool ApproveRwESFN(int _vatinvoiceid)
        {
            if (CommonModule.CommonSettings.IsDALReadOnly) return false;
            bool res = false;

            var vatInvoiceIdParam = new SqlParameter("VatInvoiceId", System.Data.SqlDbType.Int) { Value = _vatinvoiceid };
            var sres = new SqlParameter("RC", System.Data.SqlDbType.Int) { Direction = System.Data.ParameterDirection.Output };
            this.Database.ExecuteSqlCommand("exec @RC = usp_ApproveRwIncomeESFN	@VatInvoiceId", sres, vatInvoiceIdParam);
            res = (int)sres.Value == 0;

            return res;
        }

        public bool CancelApproveRwESFN(int _vatinvoiceid)
        {
            if (CommonModule.CommonSettings.IsDALReadOnly) return false;
            bool res = false;

            var vatInvoiceIdParam = new SqlParameter("VatInvoiceId", System.Data.SqlDbType.Int) { Value = _vatinvoiceid };
            var sres = new SqlParameter("RC", System.Data.SqlDbType.Int) { Direction = System.Data.ParameterDirection.Output };
            this.Database.ExecuteSqlCommand("exec @RC = usp_CancelApproveRwIncomeESFN	@VatInvoiceId", sres, vatInvoiceIdParam);
            res = (int)sres.Value == 0;

            return res;
        }
    }
}
