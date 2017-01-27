using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataObjects
{
    public class SfStatusInfo
    {
        public long Id { get; set; }
        public int IdSf { get; set; } 
        public LifetimeStatuses SfStatus { get; set; } 
        public DateTime SfStatusDateTime { get; set; } 
        public int UserId { get; set; }
    }
}
