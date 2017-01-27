using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace RwModule.Models.Mapping
{
    public class RwModuleLogMap : EntityTypeConfiguration<RwModuleLog>
    {
        public RwModuleLogMap()
        {
            // Primary Key
            this.HasKey(t => t.Id);

            // Table & Column Mappings
            this.ToTable("RwModuleLog");
            this.Property(t => t.Id).HasColumnName("id");
            this.Property(t => t.Action).HasColumnName("action").HasColumnType("char").IsFixedLength().HasMaxLength(1);
            this.Property(t => t.Adatetime).HasColumnName("adatetime");
            this.Property(t => t.Data).HasColumnName("data").HasColumnType("xml");
            this.Property(t => t.Idres).HasColumnName("idres");
            this.Property(t => t.Resource).HasColumnName("resource").IsRequired().IsVariableLength().HasMaxLength(150);
            this.Property(t => t.Userid).HasColumnName("userid");
            this.Property(t => t.Description).HasColumnName("description").IsOptional().IsVariableLength().HasMaxLength(512);

            // Relationships

        }
    }
}
