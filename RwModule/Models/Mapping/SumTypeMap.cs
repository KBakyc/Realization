using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace RwModule.Models.Mapping
{
    public class SumTypeMap : EntityTypeConfiguration<SumType>
    {
        public SumTypeMap()
        {
            // Primary Key
            this.HasKey(t => t.Id);

            // Properties
            // Table & Column Mappings
            this.ToTable("SumTypes");
            this.Property(t => t.Id).HasColumnName("id");
            this.Property(t => t.SumName).HasColumnName("SumName");
        }
    }
}
