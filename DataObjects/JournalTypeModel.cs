using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataObjects
{
    public class JournalTypeModel : ITrackable
    {
        public int JournalId { get; set; }
        public string JournalType { get; set; }
        public string JournalName { get; set; }
        public string BalSchet { get; set; }
        public int Poup { get; set; }
        public short Pkod { get; set; }
        public string Kodval { get; set; }
        public string Ceh { get; set; }
        public string TabIsp { get; set; }
        public string TabNach { get; set; }
        public byte Prsng { get; set; }
        public int Kstr { get; set; }
        public bool IsVozm { get; set; }
        public decimal? Nds { get; set; }

        #region ITrackable Members

        public TrackingInfo TrackingState { get; set; }

        #endregion
    }
}
