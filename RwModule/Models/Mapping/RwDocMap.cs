using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace RwModule.Models.Mapping
{
    public class RwDocMap : EntityTypeConfiguration<RwDoc>
    {
        public RwDocMap()
        {
            // Primary Key
            this.HasKey(t => t.Id_rwdoc);

            // Table & Column Mappings
            this.ToTable("RwDoc");
            this.Property(t => t.Id_rwdoc).HasColumnName("id_rwdoc");
            this.Property(t => t.Id_rwlist).HasColumnName("id_rwlist");
            this.Property(t => t.Num_doc).HasColumnName("num_doc").IsRequired().HasMaxLength(50);
            this.Property(t => t.Dat_doc).HasColumnName("dat_doc").IsRequired();
            this.Property(t => t.Paycode).HasColumnName("paycode").IsRequired();
            this.Property(t => t.Sum_doc).HasColumnName("sum_doc");
            this.Property(t => t.Sum_nds).HasColumnName("sum_nds");
            this.Property(t => t.Ndsrate).HasColumnName("ndsrate");
            this.Property(t => t.Note).HasColumnName("note").IsRequired().HasMaxLength(100);
            this.Property(t => t.Kodst).HasColumnName("kodst").IsRequired().HasMaxLength(10);
            this.Property(t => t.Keysbor).HasColumnName("keysbor");
            this.Property(t => t.Nkrt).HasColumnName("nkrt").IsRequired().HasMaxLength(10);
            this.Property(t => t.Dzkrt).HasColumnName("dzkrt").IsOptional();
            this.Property(t => t.Rep_date).HasColumnName("rep_date");
            this.Property(t => t.Exclude).HasColumnName("exclude");
            this.Property(t => t.Sum_excl).HasColumnName("sum_excl");
            this.Property(t => t.Excl_info).HasColumnName("excl_info").HasMaxLength(150);
            this.Property(t => t.Comments).HasColumnName("comments").HasMaxLength(250);
            this.Property(t => t.Sum_opl).HasColumnName("sum_opl");

            // Relationships
            this.HasRequired(t => t.RwList)
                .WithMany(t => t.RwDocs)
                .HasForeignKey(d => d.Id_rwlist);

            this.HasOptional(t => t.Esfn).WithRequired(f => f.RwDoc);
        }
    }
}
