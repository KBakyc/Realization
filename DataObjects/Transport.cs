using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataObjects
{
    public class Transport
    {
        public short Id { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
        public Directions Direction { get; set; }
    }
}
