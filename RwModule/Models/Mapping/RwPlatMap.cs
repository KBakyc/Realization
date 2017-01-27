using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace RwModule.Models.Mapping
{
    public class RwPlatMap : EntityTypeConfiguration<RwPlat>
    {
        public RwPlatMap()
        {
            // Primary Key
            this.HasKey(t => t.Idrwplat);

            // Properties
            // Table & Column Mappings
            this.ToTable("RwPlats");
            this.Property(t => t.Numplat).HasColumnName("numplat");
            this.Property(t => t.Datplat).HasColumnName("datplat");
            this.Property(t => t.Datbank).HasColumnName("datbank");
            this.Property(t => t.Idpostes).HasColumnName("idpostes");
            this.Property(t => t.Idagree).HasColumnName("idagree");
            this.Property(t => t.Sumplat).HasColumnName("sumplat");
            this.Property(t => t.Ostatok).HasColumnName("ostatok");
            this.Property(t => t.Datzakr).HasColumnName("datzakr");
            this.Property(t => t.Whatfor).HasColumnName("whatfor");
            this.Property(t => t.Direction).HasColumnName("direction").HasColumnType("tinyint");
            this.Property(t => t.Notes).HasColumnName("notes");
            this.Property(t => t.Idusltype).HasColumnName("idusltype").HasColumnType("tinyint");
            this.Property(t => t.Debet).HasColumnName("debet").IsFixedLength().HasMaxLength(8);
            this.Property(t => t.Credit).HasColumnName("credit").IsFixedLength().HasMaxLength(8);
        }
    }
}
