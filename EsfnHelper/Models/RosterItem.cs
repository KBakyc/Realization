using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EsfnHelper.Models
{
    public class RosterItem
    {
        public int? Number { get; set; }
        public string Name { get; set; }
        public decimal? Price { get; set; }
        public decimal? Count { get; set; }
        public string UnitsShortName { get; set; }
        public decimal? Cost { get; set; }
        public decimal? SummaExcise { get; set; }
        public decimal? VatRate { get; set; }
        public decimal? SummaVat { get; set; }
        public decimal? CostVat { get; set; }
    }
}
