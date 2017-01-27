using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace RwModule.Models.Mapping
{
    public class RwListMap : EntityTypeConfiguration<RwList>
    {
        public RwListMap()
        {
            // Primary Key
            this.HasKey(t => t.Id_rwlist);

            // Properties
            // Table & Column Mappings
            this.ToTable("RwList");
            this.Property(t => t.Id_rwlist).HasColumnName("id_rwlist");
            this.Property(t => t.Num_rwlist).HasColumnName("num_rwlist");
            this.Property(t => t.Bgn_date).HasColumnName("bgn_date").IsOptional();
            this.Property(t => t.End_date).HasColumnName("end_date").IsOptional();
            this.Property(t => t.Num_inv).HasColumnName("num_inv");
            this.Property(t => t.Dat_inv).HasColumnName("dat_inv");
            this.Property(t => t.Sum_inv).HasColumnName("sum_inv");
            this.Property(t => t.Sum_invnds).HasColumnName("sum_invnds");
            this.Property(t => t.Keykrt).HasColumnName("keykrt");
            this.Property(t => t.RwlType).HasColumnName("s_p");
            this.Property(t => t.Transition).HasColumnName("transition");
            this.Property(t => t.Iddog).HasColumnName("iddog");
            this.Property(t => t.User_accept).HasColumnName("user_accept");
            this.Property(t => t.Dat_accept).HasColumnName("dat_accept");
            this.Property(t => t.Dat_oplto).HasColumnName("dat_oplto");
            this.Property(t => t.Sum_excl).HasColumnName("sum_excl");
            this.Property(t => t.Dat_orc).HasColumnName("dat_orc");
            this.Property(t => t.Paystatus).HasColumnName("paystatus").HasColumnType("tinyint");
            this.Property(t => t.Paydate).HasColumnName("paydate").IsOptional();
            this.Property(t => t.Sum_opl).HasColumnName("sum_opl");
        }
    }
}
