using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataObjects
{
    public class ReportModel
    {
        public int ReportId { get; set; }
        public string CategoryName { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Path { get; set; }       
        public bool IsA3Enabled { get; set; }
        public ReportModes Mode { get; set; }
        public string ParamsGetterName { get; set; }
        public string ParamsGetterOptions { get; set; }
        public Dictionary<string, string> Parameters { get; set; }
        public Dictionary<string, IEnumerable<object>> DataSources { get; set; }
        public bool IsFavorite { get; set; }
    }
}
