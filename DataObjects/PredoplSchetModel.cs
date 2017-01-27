using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataObjects
{
    public class PredoplSchetModel : ITrackable
    {
        public int Id {get; set;}

        public int Poup {get; set;}

        public short Pkod {get; set;}

        public string Deb {get; set;}

        public string Kre {get; set;}

        public string KodvalFrom {get; set;}

        public string KodvalTo {get; set;}

        public string RealSch {get; set;}

        public int IdBankGroup {get; set;}
        
        public byte RecType {get; set;}

        public bool IsActive {get; set;}

        public string Kodnap {get; set;}

        public string Kodvozv {get; set;}

        #region ITrackable Members

        public TrackingInfo TrackingState { get; set; }

        #endregion
    }
}
