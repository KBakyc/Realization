using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataObjects
{
    [Serializable]
    public class PenaltyModel : ITrackable
    {
        public int Id {get; set;}
        public int Poup {get; set;}
        public int Kpok {get; set;}
        public int Kdog {get; set;}
        public string Nomish {get; set;}
        public int Nomkro {get; set;}
        public int Rnpl {get; set;}
        public DateTime Datgr {get; set;}
        public DateTime Datkro {get; set;}
        public decimal Sumpenalty {get; set;}
        public string Kodval {get; set;}
        public decimal Sumopl {get; set;}
        public DateTime? Datopl {get; set;}
        public decimal Kursval {get; set;}
        public int UserAdd {get; set;}
        public DateTime DateAdd {get; set;}
        public int UserKor {get; set;}
        public DateTime? DateKor {get; set;}

        #region ITrackable Members

        public TrackingInfo TrackingState { get; set; }

        #endregion
    }
}
