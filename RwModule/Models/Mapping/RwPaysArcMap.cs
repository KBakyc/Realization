using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace RwModule.Models.Mapping
{
    public class RwPaysArcMap : EntityTypeConfiguration<RwPaysArc>
    {
        public RwPaysArcMap()
        {
            // Primary Key
            this.HasKey(t => t.Id);

            // Table & Column Mappings
            this.ToTable("RwPaysArc");
            
            // Properties
            this.Property(t => t.Id).HasColumnName("id");
            this.Property(t => t.Payaction).HasColumnName("payaction").HasColumnType("tinyint");
            this.Property(t => t.Idrwplat).HasColumnName("idrwplat");
            this.Property(t => t.Iddoc).HasColumnName("iddoc");            
            this.Property(t => t.Summa).IsRequired().HasColumnName("summa");
            this.Property(t => t.Notes).HasMaxLength(512).HasColumnName("notes");
            this.Property(t => t.Datopl).IsRequired().HasColumnName("datopl");      
            this.Property(t => t.Userid).IsRequired().HasColumnName("userid");
            this.Property(t => t.Atime).IsRequired().HasColumnName("atime");                     
        }
    }
}
