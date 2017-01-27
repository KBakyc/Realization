using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataObjects
{
    public class Country
    {

        private int kstr;
        private string name;
        private string shortName;

        public int Kstr
        {
            get { return kstr; }
            set { kstr = value; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public string ShortName
        {
            get { return shortName; }
            set { shortName = value; }
        }
    }
}
