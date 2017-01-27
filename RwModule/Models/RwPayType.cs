using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RwModule.Models
{
    public class RwPayType
    {
        public short Paycode { get; set; }
        public string Payname { get; set; }
        public RwUslType IdUslType { get; set; }
    }
}
