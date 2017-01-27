using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceModule.DAL.Models
{
    public class UserReportStat
    {
        public int? UserId { get; set; }
        public string UserName { get; set; }
        public string UserFIO { get; set; }
        public bool? IsActiveUser { get; set; }
        public string ReportPath { get; set; }
        public string Parameters { get; set; }
        public DateTime TimeStart { get; set; }
        public DateTime TimeEnd { get; set; }
        public string Format { get; set; }
        public string ReportAction { get; set; }
        //public DateTime ReportLastTimeStart { get; set; }
        //public int ReportRunCount { get; set; }
    }
}
