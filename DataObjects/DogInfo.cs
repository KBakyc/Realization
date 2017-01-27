using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataObjects
{
    public class DogInfo
    {
        private string naiOsn;
        private DateTime datOsn;
        private string dopOsn;
        private int idporsh;

        public string NaiOsn
        {
            get { return naiOsn; }
            set { naiOsn = value; }
        }

        public DateTime DatOsn
        {
            get { return datOsn; }
            set { datOsn = value; }
        }

        public string DopOsn
        {
            get { return dopOsn; }
            set { dopOsn = value; }
        }

        public DateTime? DatDop { get; set; }

        public string AltOsn { get; set; }
        public DateTime DatAlt { get; set; }

        public int Idporsh
        {
            get { return idporsh; }
            set { idporsh = value; }
        }
        
        public int Kpok { get; set; }
        public int Kfond { get; set; }
        public string KodVal { get; set; }
        public int Provoz { get; set; }
        public int Srok { get; set; }
        public short TypeRespite { get; set; }

        public int IdDog { get; set; }
        public int IdAgree { get; set; }
    }
}
