using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataObjects
{
    public class RailStation
    {

        private int kodst;
        private string stationName;
        private string railwaysName;
        private int kstr;

        public int Kodst
        {
            get { return kodst; }
            set { kodst = value; }
        }

        public string StationName
        {
            get { return stationName; }
            set { stationName = value; }
        }

        public string RailwaysName
        {
            get { return railwaysName; }
            set { railwaysName = value; }
        }
        
        public int Kstr
        {
            get { return kstr; }
            set { kstr = value; }
        }
    }
}
