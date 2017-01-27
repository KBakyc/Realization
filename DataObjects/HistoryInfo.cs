using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataObjects
{
    public class HistoryInfo
    {
        public long logId { get; set; }

        //public long IdP623 { get; set; }

        public System.DateTime StatusDateTime { get; set; }

        //public System.Xml.Linq.XElement Changes { get; set; }

        public int UserId { get; set; }

        public string FullName { get; set; }

        public string UserName { get; set; }

        //public bool IsUserEnabled { get; set; }

        //public int StatusId { get; set; }

        public string StatusDescription { get; set; }
    }
}
