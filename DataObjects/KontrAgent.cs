using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataObjects
{
    public class KontrAgent : IComparable
    {

        private int kgr;
        private string name;
        private string fullName;
        private string inn;
        private string okpo;
        private string kpp;
        private string address;
        private string koddav;

        public int Kgr
        {
            get { return kgr; }
            set { kgr = value; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public string FullName
        {
            get { return fullName; }
            set { fullName = value; }
        }

        public string Inn
        {
            get { return inn; }
            set { inn = value; }
        }

        public string Okpo
        {
            get { return okpo; }
            set { okpo = value; }
        }

        public string Kpp
        {
            get { return kpp; }
            set { kpp = value; }
        }

        public string Address
        {
            get { return address; }
            set { address = value; }
        }

        public string Koddav
        {
            get { return koddav; }
            set { koddav = value; }
        }

        public short Kstr { get; set; }

        public string City { get; set; }
        public string Country { get; set; }

        #region IComparable Members

        public int CompareTo(object obj)
        {
            if (obj is KontrAgent)
            {
                KontrAgent other = (KontrAgent)obj;
                return this.Kgr.CompareTo(other.Kgr);
            }
            else
            {
                throw new ArgumentException("Object is not a Kontragent");
            }
        }

        #endregion
    }
}
