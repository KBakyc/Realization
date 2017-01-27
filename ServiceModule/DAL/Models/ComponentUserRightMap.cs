using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity.ModelConfiguration;

namespace ServiceModule.DAL.Models
{
    public class ComponentUserRightMap : EntityTypeConfiguration<ComponentUserRight>
    {
        public ComponentUserRightMap()
        {
            // Primary Key
            this.HasKey(t => t.Id);

            // Properties
            // Table & Column Mappings
            this.ToTable("NsiComponentUserRights");
            this.Property(t => t.Id).HasColumnName("id");
            this.Property(t => t.ComponentTypeName).HasColumnName("ComponentTypeName").HasMaxLength(150).IsRequired();
            this.Property(t => t.AccessLevel).HasColumnName("AccessLevel").IsOptional();
            this.Property(t => t.UserId).HasColumnName("UserId").IsOptional();
        }
    }
}
