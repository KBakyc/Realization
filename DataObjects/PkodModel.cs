using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataObjects
{
    public class PkodModel
    {
        public PkodModel(short _pkod)
        {
            pkod = _pkod;
        }

        private short poup;

        private short pkod;

        public short Pkod
        {
            get { return pkod; }
        }

        private string name;

        private decimal kpr;

        private string shortName;

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public decimal Kpr
        {
            get { return kpr; }
            set { kpr = value; }
        }

        public string ShortName
        {
            get { return shortName; }
            set { shortName = value; }
        }

        public short Poup
        {
            get { return poup; }
            set { poup = value; }
        }
    }
}
