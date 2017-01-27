using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataObjects
{
    public class SfPayTypeModel
    {
        public short PayType { get; set; }
        public string PayName { get; set; }
        public short PayGroupId { get; set; }
        public short SfLine { get; set; }
        public short SfLinePos { get; set; }
        public byte KolProdPrec { get; set; }
    }
}
