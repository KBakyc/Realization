using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataObjects
{
    public class TableSyncStatus
    {
        public string TaskName { get; set; }
        public string TableName { get; set; }
        public SyncStatuses Status { get; set; }
        public DateTime DtStart { get; set; }
        public DateTime DtEnd { get; set; }
        public string TableDescription { get; set; }
    }
}
