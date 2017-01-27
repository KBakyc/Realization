using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using ServiceModule.DAL.Models;
using System.Linq;
using System.Data.SqlClient;
using System;
using System.Collections.Generic;
using DotNetHelper;
using DataObjects;

namespace ServiceModule.DAL
{
    public partial class ServiceContext : DbContext
    {
        static ServiceContext()
        {
            Database.SetInitializer<ServiceContext>(null);
        }

        public ServiceContext()
            : base("Name=DAL.Properties.Settings.RealConnectionString")
        {
            //Configuration.ProxyCreationEnabled = false;
            //Configuration.AutoDetectChangesEnabled = false;
            //Configuration.LazyLoadingEnabled = false;            
        }

        public DbSet<ComponentUserRight> ComponentUserRights { get; set; }
        public DbSet<ReportInfo> ReportInfos { get; set; }
        public DbSet<ARM_User> ARM_Users { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            Configuration.ProxyCreationEnabled = false;
            Configuration.AutoDetectChangesEnabled = false;
            Configuration.LazyLoadingEnabled = false;

            modelBuilder.Configurations.Add(new ComponentUserRightMap());
            modelBuilder.Configurations.Add(new ReportInfoMap());
            modelBuilder.Configurations.Add(new ARM_UserMap());

            modelBuilder.Entity<ReportInfo>()
                .HasMany(t => t.FavoriteUsers)
                .WithMany(t => t.FavoriteReports)
                .Map(m =>
                {
                    m.ToTable("UserReports");
                    m.MapLeftKey("idreport");
                    m.MapRightKey("iduser");
                });
        }

        public class UserPermission
        {
            //public int UserId { get; set; }
            public string ComponentTypeName { get; set; }
            public int? AccessLevel { get; set; }
        }

        public ComponentUserRight[] GetUserEffectiveRights(int _userid)
        {
            ComponentUserRight[] res = null;
            if (_userid > 0)
            {
                res = this.Database.SqlQuery<UserPermission>("exec usp_GetUserComponents {0}".Format(_userid))
                    .Select(p => new ComponentUserRight
                    {
                        UserId = _userid,
                        AccessLevel = p.AccessLevel,
                        ComponentTypeName = p.ComponentTypeName
                    }).ToArray();
            }
            return res;
        }

        public ComponentUserRight[] GetUserComponentsRights(int _userid)
        {
            ComponentUserRight[] res = null;
            try
            {
                res = ComponentUserRights.Where(r => r.UserId == _userid || r.UserId == null && _userid == 0).DistinctBy(k => k.ComponentTypeName).ToArray();
            }
            catch (Exception e)
            {
                CommonModule.Helpers.WorkFlowHelper.OnCrash(e);
            }
            return res;
        }

        public void UpdateComponentUserRights(int _userid, IEnumerable<ComponentUserRight> _newr)
        {
            try
            {
                //var oldr = ComponentUserRights.Where(r => r.UserId == _userid || _userid == 0 && r.UserId == null).ToArray();
                //foreach (var oc in oldr)
                //{
                //    var nc = _newr.FirstOrDefault(n => n.ComponentTypeName == oc.ComponentTypeName);
                //    if (nc != null)
                //        oc.AccessLevel = nc.AccessLevel;
                //    else
                //        ComponentUserRights.Remove(oc);
                //}
                //foreach(var nc in _newr.Where(n => !oldr.Any(o => o.ComponentTypeName == n.ComponentTypeName)))                    
                //    ComponentUserRights.Add(nc);
                //this.SaveChanges();
                var oldr = ComponentUserRights.Where(r => r.UserId == _userid || _userid == 0 && r.UserId == null).ToArray();
                ComponentUserRights.RemoveRange(oldr);
                ComponentUserRights.AddRange(_newr);
                this.SaveChanges();
            }
            catch (Exception e)
            {
                CommonModule.Helpers.WorkFlowHelper.OnCrash(e);
            }        
        }
        
        public string[] GetNamedReports(string _module)
        {
            string[] namedReports = null;
            try
            {
                namedReports = this.Database.SqlQuery<string>(String.Format(@"SELECT Name FROM dbo.Reports where isnull(Name,'') <> '' AND ComponentTypeName = '{0}'", _module)).ToArray();
            }
            catch (Exception e)
            {
                CommonModule.Helpers.WorkFlowHelper.OnCrash(e);
            }
            return namedReports;
        }
        
        public UserReportStat[] GetReportStat(string _reportpath)
        {
            UserReportStat[] res = null;
            try
            {
                res = this.Database.SqlQuery<UserReportStat>(String.Format("exec usp_GetReportStat '{0}'", _reportpath)).ToArray();
            }
            catch (Exception e)
            {
                CommonModule.Helpers.WorkFlowHelper.OnCrash(e);
            }
            return res;
        }
    }
}
