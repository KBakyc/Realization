using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataObjects
{
    public class AgreementModel
    {
        public int IdAgreement { get; set; }
        public int IdPrimaryAgreement { get; set; }
        public int IdAgreeDBF { get; set; }
        public string NumberOfDocument { get; set; }
        public DateTime DateOfDocument { get; set; }
        public DateTime DateOfBegin { get; set; }
        public DateTime DateOfEnd { get; set; }
        public int IdCounteragent { get; set; }
        public string Contents { get; set; }
        public int IdStateType { get; set; }
    }
}
