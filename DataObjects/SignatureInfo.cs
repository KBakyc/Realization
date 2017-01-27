using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataObjects
{
    public class SignatureInfo
    {
        public int Id { get; set; }
        public string Fio { get; set; }
        public string Position { get; set; }
        public string Short { get; set; }
        public byte SignTypeId { get; set; }
        //public short[] Poups { get; set; }
    }
}
