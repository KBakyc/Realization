using System;
using System.Collections.Generic;
using DataObjects;

namespace RwModule.Models
{
    public sealed class RwList
    {
        public RwList()
        {
            this.RwDocs = new List<RwDoc>();
        }

        public int Id_rwlist { get; set; }
        public int Num_rwlist { get; set; }
        public DateTime? Bgn_date { get; set; }
        public DateTime? End_date { get; set; }
        public int Num_inv { get; set; }
        public DateTime Dat_inv { get; set; }
        public decimal Sum_inv { get; set; }
        public decimal Sum_invnds { get; set; }
        public long Keykrt { get; set; }
        public RwUslType RwlType { get; set; }
        public bool Transition { get; set; }
        public DateTime? Dat_accept { get; set; }
        public DateTime? Dat_oplto { get; set; }
        public int Iddog { get; set; }
        public int User_accept { get; set; }
        public decimal Sum_excl { get; set; }
        public DateTime Dat_orc { get; set; }
        public PayStatuses Paystatus { get; set; }
        public DateTime? Paydate { get; set; }
        public decimal Sum_opl { get; set; }

        public List<RwDoc> RwDocs { get; set; }
    }
}
