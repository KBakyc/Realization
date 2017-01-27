using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RwModule.Models
{
    public class RwPlat
    {
        public int Idrwplat { get; set; }
        public int Numplat { get; set; }
        public DateTime Datplat { get; set; }
        public DateTime Datbank { get; set; }
        public int? Idpostes { get; set; }
        public int? Idagree { get; set; }
        public decimal Sumplat { get; set; }
        public decimal Ostatok { get; set; }
        public DateTime? Datzakr { get; set; }
        public string Whatfor { get; set; }
        public RwPlatDirection Direction { get; set; }
        public string Notes { get; set; }
        public RwUslType Idusltype { get; set; }
        public string Debet { get; set; }
        public string Credit { get; set; }
    }
}
