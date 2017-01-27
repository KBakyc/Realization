using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataObjects
{
    public class Choice
    {
        public string Header { get; set; }
        public string Info { get; set; }
        public bool? IsChecked { get; set; }
        public bool IsSingleInGroup { get; set; }
        public string GroupName { get; set; }
        public string Name { get; set; }
        public Object Item { get; set; }
    }
}
