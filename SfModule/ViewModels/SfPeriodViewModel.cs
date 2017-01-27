using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.ViewModels;
using DataObjects;

namespace SfModule.ViewModels
{
    public class SfPeriodViewModel : BasicViewModel, ITrackable
    {
        public SfPeriodViewModel(SfPayPeriodModel sfPeriodModel)
        {
            this.sfPeriodModel = sfPeriodModel;
        }

        private SfPayPeriodModel sfPeriodModel;

        public SfPayPeriodModel SfPeriodModel
        {
            get { return sfPeriodModel; }
        }

        //private DateTime? datStart;
        public DateTime? DatStart
        {
            get { return sfPeriodModel.DatStart; }
            set
            {
                if (value != sfPeriodModel.DatStart)
                {
                    sfPeriodModel.DatStart = value;
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("DatStart");
                }
            }
        }

        //private DateTime? lastDatOpl;
        public DateTime? LastDatOpl
        {
            get { return sfPeriodModel.LastDatOpl; }
            set
            {
                if (value != sfPeriodModel.LastDatOpl)
                {
                    sfPeriodModel.LastDatOpl = value;
                    if (TrackingState == TrackingInfo.Unchanged)
                        TrackingState = TrackingInfo.Updated;
                    NotifyPropertyChanged("LastDatOpl");
                }
            }
        }


        #region ITrackable Members

        public TrackingInfo TrackingState
        {
            get { return sfPeriodModel.TrackingState; }
            set { sfPeriodModel.TrackingState = value; }
        }

        #endregion
    }
}
