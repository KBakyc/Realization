using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceModule.DAL.Models
{
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

    public class ARM_User
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsSystem { get; set; }

        public List<ReportInfo> FavoriteReports { get; set; }
    }
}
