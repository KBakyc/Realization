using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace RwModule.Models.Mapping
{
    public class RwPayTypeMap : EntityTypeConfiguration<RwPayType>
    {
        public RwPayTypeMap()
        {
            // Primary Key
            this.HasKey(t => t.Paycode);

            // Properties
            // Table & Column Mappings
            this.ToTable("RwPayTypes");
            this.Property(t => t.Paycode).HasColumnName("Paycode");
            this.Property(t => t.Payname).HasColumnName("Payname").IsOptional();
            this.Property(t => t.IdUslType).HasColumnName("IdUslType");
        }
    }
}
