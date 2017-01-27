using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataObjects
{
    public class MeasureUnit
    {
        public int Id { get; set; }
        public string ShortName { get; set; }
        public string FullName { get; set; }
        public string NeiStat { get; set; }
        public bool IsNeedDensity { get; set; }
    }
}
