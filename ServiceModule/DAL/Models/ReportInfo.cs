using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceModule.DAL.Models
{
    public class ReportInfo
    {
        public int Id { get; set; }
        public string ComponentTypeName { get; set; }// [varchar](150) NOT NULL,
        public string CategoryName { get; set; }// [varchar](150) NULL,
        public string Name { get; set; }// [varchar](150) NULL,
        public string Title { get; set; }// [varchar](150) NOT NULL,
        public string Description { get; set; }// [varchar](250) NULL,
        public string Path { get; set; }// [varchar](150) NOT NULL,
        public string ParamsGetter { get; set; }// [varchar](250) NULL,
        public string ParamsGetterOptions { get; set; }// [varchar](2048) NULL,
        public bool? IsA3Enabled { get; set; }// [bit] NULL,

        public List<ARM_User> FavoriteUsers { get; set; }
    }
}
