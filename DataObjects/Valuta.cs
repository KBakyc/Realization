using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataObjects
{
    public class Valuta
    {
        private string kodval;
		private string nameVal;
        private string shortName;

        public Valuta(string _kodval, string _nameVal, string _shortName)
        {
            kodval = _kodval;
            nameVal = _nameVal;
            shortName = _shortName;
        }

        public string Kodval { get { return kodval; } }
        public string NameVal { get { return nameVal; } }
        public string ShortName { get { return shortName; } }

    }
}
