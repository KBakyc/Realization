using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace RwModule.Models.Mapping
{
    public class RwDocIncomeEsfnMap : EntityTypeConfiguration<RwDocIncomeEsfn>
    {
        public RwDocIncomeEsfnMap()
        {
            // Primary Key
            this.HasKey(t => t.Id_rwdoc);

            // Table & Column Mappings
            this.ToTable("RwDocIncomeEsfns");
            this.Property(t => t.Id_rwdoc).HasColumnName("id_rwdoc");
            this.Property(t => t.VatInvoiceId).IsRequired();
            this.Property(t => t.VatInvoiceNumber).IsRequired().HasMaxLength(25);
            this.Property(t => t.InvoiceType).HasColumnType("tinyint").HasColumnName("InvoiceTypeId").IsRequired();
            this.Property(t => t.Account).IsRequired().HasMaxLength(6);
            this.Property(t => t.VatAccount).IsRequired().HasMaxLength(6);
            this.Property(t => t.AccountingDate).HasColumnType("date").IsRequired();
            this.Property(t => t.ApproveUser).IsOptional().HasMaxLength(150);
            this.Property(t => t.ApproveDate).HasColumnType("date").IsOptional();
            this.Property(t => t.DeductionDate).HasColumnType("date").IsOptional();
            this.Property(t => t.SummaVat).IsOptional();

            // Relationships
            this.HasRequired(t => t.RwDoc).WithOptional(p => p.Esfn);
        }
    }
}
