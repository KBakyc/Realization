using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataObjects
{
    public class SfPayPeriodModel : ITrackable, ICloneable
    {
        private int id;
        private int idSf;
        private DateTime? datStart;
        private DateTime? lastDatOpl;
        private byte[] version;

        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        public int IdSf
        {
            get { return idSf; }
            set { idSf = value; }
        }

        public DateTime? DatStart
        {
            get { return datStart; }
            set { datStart = value; }
        }

        public DateTime? LastDatOpl
        {
            get { return lastDatOpl; }
            set { lastDatOpl = value; }
        }

        public byte[] Version
        {
            get { return version; }
            set { version = value; }
        }

        #region ITrackable Members

        public TrackingInfo TrackingState { get; set; }

        #endregion

        #region ICloneable Members

        public object Clone()
        {
            return new SfPayPeriodModel()
                       {
                           Id = Id,
                           IdSf = IdSf,
                           DatStart = DatStart,
                           LastDatOpl = LastDatOpl,
                           Version = Version,
                           TrackingState = TrackingState
                       };
        }

        #endregion
    }
}
