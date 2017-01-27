using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataObjects
{
    public class PoupModel : IComparable
    {
        public int Kod { get; set; }
        public string Name { get; set; }
        public string PlatName { get; set; }
        public string ShortName { get; set; }
        public PayDocTypes PayDoc { get; set; }
        public bool IsPkodsEnabled { get; set; }
        public bool IsAkciz { get; set; }
        public bool IsDav { get; set; }
        public bool IsDogExp { get; set; }
        public bool IsActive { get; set; }

        #region IComparable Members

        public int CompareTo(object obj)
        {
            if (obj is PoupModel)
            {
                PoupModel other = (PoupModel)obj;
                return this.Kod.CompareTo(other.Kod);
            }
            else
            {
                throw new ArgumentException("Object is not a PoupModel");
            }
        }

        #endregion
    }
}
