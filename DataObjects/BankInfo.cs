using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataObjects
{
    public class BankInfo
    {
        private int id;
        private string rsh;
        private string bankName;
        private string mfo;

        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        public string Rsh
        {
            get { return rsh; }
            set { rsh = value; }
        }

        public string BankName
        {
            get { return bankName; }
            set { bankName = value; }
        }

        public string Mfo
        {
            get { return mfo; }
            set { mfo = value; }
        }
    }
}
