using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace RwModule.Models
{
    public class RwModuleLog
    {
        public long Id { get; set; }
        public int Userid { get; set; }
        public DateTime Adatetime { get; set; }
        public string Action { get; set; }
        public string Resource { get; set; }
        public long Idres { get; set; }
        public string Data { get; set; }
        public string Description { get; set; }
    }
}
