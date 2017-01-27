using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity.ModelConfiguration;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceModule.DAL.Models
{
    public class ReportInfoMap : EntityTypeConfiguration<ReportInfo>
    {
        public ReportInfoMap()
        {
            // Primary Key
            this.HasKey(t => t.Id);

    //[id] [int] NOT NULL,
    //[ComponentTypeName] [varchar](150) NOT NULL,
    //[CategoryName] [varchar](150) NULL,
    //[Name] [varchar](150) NULL,
    //[Title] [varchar](150) NOT NULL,
    //[Description] [varchar](250) NULL,
    //[Path] [varchar](150) NOT NULL,
    //[ParamsGetter] [varchar](250) NULL,
    //[ParamsGetterOptions] [varchar](2048) NULL,
    //[IsA3Enabled] [bit] NULL,

            // Properties
            // Table & Column Mappings
            this.ToTable("Reports");
            this.Property(t => t.Id).HasColumnName("id").HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            this.Property(t => t.ComponentTypeName).HasColumnName("ComponentTypeName").HasMaxLength(150).IsRequired();
            this.Property(t => t.CategoryName).HasColumnName("CategoryName").HasMaxLength(150).IsOptional();
            this.Property(t => t.Name).HasColumnName("Name").HasMaxLength(150).IsOptional();
            this.Property(t => t.Title).HasColumnName("Title").HasMaxLength(150).IsRequired();
            this.Property(t => t.Description).HasColumnName("Description").HasMaxLength(250).IsOptional();
            this.Property(t => t.Path).HasColumnName("Path").HasMaxLength(150).IsRequired();
            this.Property(t => t.ParamsGetter).HasColumnName("ParamsGetter").HasMaxLength(250).IsOptional();
            this.Property(t => t.ParamsGetterOptions).HasColumnName("ParamsGetterOptions").HasMaxLength(2048).IsOptional();
            this.Property(t => t.IsA3Enabled).HasColumnName("IsA3Enabled").IsOptional();                        
        }
    }
}
