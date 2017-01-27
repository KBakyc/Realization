using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace RwModule.Models.Mapping
{
    public class RwBuhSchetMap : EntityTypeConfiguration<RwBuhSchet>
    {
        public RwBuhSchetMap()
        {
            // Primary Key
            this.HasKey(t => t.Id);

            // Properties
            // Table & Column Mappings
            this.ToTable("RwBuhSchets");
            this.Property(t => t.Poup).HasColumnName("Poup");
            this.Property(t => t.VidUsl).HasColumnName("VidUsl").HasColumnType("tinyint");
            this.Property(t => t.KodUsl).HasColumnName("KodUsl");
            this.Property(t => t.DebUsl).HasColumnName("DebUsl");
            this.Property(t => t.KreUsl).HasColumnName("KreUsl");
            this.Property(t => t.DebOpl).HasColumnName("DebOpl");
            this.Property(t => t.KreOpl).HasColumnName("KreOpl");
        }
    }
}
