using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using CommonModule.Commands;
using CommonModule.Helpers;
using CommonModule.Interfaces;

namespace CommonModule.ViewModels
{
    /// <summary>
    /// Модель диалога выбора даты.
    /// </summary>
    public class DateDlgViewModel : BaseDlgViewModel
    {
        public DateDlgViewModel()
            :this(true)
        { }

        private bool saveDate;        

        public DateDlgViewModel(bool _saveDate)
        {
            saveDate = _saveDate;
            if (saveDate)
                selDate = Remember.GetValue<DateTime?>("SelDate");
            //if (selDate == default(DateTime))
            //    selDate = DateTime.Now;
        }

        public DateTime? MinDate { get; set; }
        public DateTime? MaxDate { get; set; }

        public bool CanBeNull { get; set; }

        public override bool IsValid()
        {
            return base.IsValid() && (MaxDate == null || SelDate <= MaxDate.Value)
                                  && (MinDate == null || SelDate >= MinDate.Value)
                                  && SelDate > DateTime.MinValue
                                  || (CanBeNull && SelDate == null);
        }

        /// <summary>
        /// Подпись даты
        /// </summary>
        private string dateLabel;
        public string DateLabel
        {
            get { return dateLabel; }
            set
            {
                if (value != dateLabel)
                {
                    dateLabel = value;
                    NotifyPropertyChanged("DateLabel");
                }
            }
        }

        private DateTime? selDate;
        public DateTime? SelDate
        {
            get { return selDate; }
            set
            {
                if (value != selDate)
                {
                    selDate = value;
                    if (saveDate && value != null)
                        Remember.SetValue("SelDate", SelDate.Value);
                    NotifyPropertyChanged("SelDate");
                }
            }
        }

    }
}
