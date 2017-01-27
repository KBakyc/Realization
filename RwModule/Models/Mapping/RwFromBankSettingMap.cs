using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace RwModule.Models.Mapping
{
    public class RwFromBankSettingMap : EntityTypeConfiguration<RwFromBankSetting>
    {
        public RwFromBankSettingMap()
        {
            // Primary Key
            this.HasKey(t => t.Id);

            // Properties
            // Table & Column Mappings
            this.ToTable("RwFromBankSettings");
            this.Property(t => t.IdUslType).HasColumnName("IdUslType");
            this.Property(t => t.Debet).HasColumnName("Debet").IsFixedLength().HasMaxLength(8);
            this.Property(t => t.Credit).HasColumnName("Credit").IsFixedLength().HasMaxLength(8);
            this.Property(t => t.FinNapr).HasColumnName("FinNapr").HasMaxLength(10);
            this.Property(t => t.IdBankGroup).HasColumnName("idbankgroup").HasColumnType("tinyint");
        }
    }
}
