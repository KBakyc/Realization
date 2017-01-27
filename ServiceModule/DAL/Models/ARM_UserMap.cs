using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity.ModelConfiguration;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceModule.DAL.Models
{
    public class ARM_UserMap : EntityTypeConfiguration<ARM_User>
    {
        public ARM_UserMap()
        {
            // Primary Key
            this.HasKey(t => t.Id);

            //[id] [int] NOT NULL,
            //[UserName] [varchar](50) NOT NULL,
            //[FullName] [varchar](250) NULL,
            //[IsEnabled] [bit] NOT NULL,
            //[TabNum] [varchar](25) NULL,
            //[Cex] [varchar](10) NULL,
            //[IsSystem] [bit] NOT NULL,
            //[SecurityContext] [int] NULL,
            //[EmailAddress] [varchar](100) NULL,
            //[ClientInfo] [xml] NULL,

            // Properties
            // Table & Column Mappings
            this.ToTable("ARM_Users");
            this.Property(t => t.Id).HasColumnName("id").HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            this.Property(t => t.UserName).HasColumnName("UserName").HasMaxLength(50).IsRequired();
            this.Property(t => t.FullName).HasColumnName("FullName").HasMaxLength(250).IsOptional();
            this.Property(t => t.IsEnabled).HasColumnName("IsEnabled").IsRequired();
            this.Property(t => t.IsSystem).HasColumnName("IsSystem").IsRequired();
        }
    }
}
