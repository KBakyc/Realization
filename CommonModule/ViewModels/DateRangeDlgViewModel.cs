using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using CommonModule.Commands;
using CommonModule.Helpers;

namespace CommonModule.ViewModels
{
    /// <summary>
    /// Модель диалога выбора диапазона дат
    /// </summary>
    public class DateRangeDlgViewModel : BaseDlgViewModel
    {
        public DateRangeDlgViewModel()
            :this(true)
        {}

        public DateRangeDlgViewModel(bool _save)
        {
            IsSaveSettings = _save;

            if (_save)
            {
                dateFrom = Remember.GetValue<DateTime>("DateFrom");
                dateTo = Remember.GetValue<DateTime>("DateTo");
            }
                
            if (dateFrom == default(DateTime))
                dateFrom = DateTime.Now;            
            if (dateTo == default(DateTime))
                dateTo = DateTime.Now;
            datesLabel = "За период:";
        }

        public override bool IsValid()
        {
            return base.IsValid() //&& DateFrom <= DateTime.Now 
                && DateFrom <= DateTo
                && DateFrom > DateTime.MinValue;
        }

        public bool IsSaveSettings { get; set; }

        /// <summary>
        /// Подпись даты
        /// </summary>
        private string datesLabel;
        public string DatesLabel
        {
            get 
            {
                return datesLabel; 
            }
            set
            {
                SetAndNotifyProperty("DatesLabel", ref datesLabel, value);
            }
        }

        private DateTime dateFrom;
        public DateTime DateFrom
        {
            get { return dateFrom; }
            set
            {
                if (SetAndNotifyProperty("DateFrom", ref dateFrom, value) && IsSaveSettings)
                    Remember.SetValue("DateFrom", dateFrom);
            }
        }

        private DateTime dateTo;
        public DateTime DateTo
        {
            get { return dateTo; } 
            set
            {
                if (SetAndNotifyProperty("DateTo", ref dateTo, value) && IsSaveSettings)
                    Remember.SetValue("DateTo", dateTo);
            }
        }

        private ICommand copyDateCommand;
        public ICommand CopyDateCommand
        {
            get
            {
                if (copyDateCommand == null)
                    copyDateCommand = new DelegateCommand<String>(ExecCopyDate);
                return copyDateCommand;
            }
        }

        private void ExecCopyDate(string obj)
        {
            switch (obj)
            {
                case "ToDateTo":
                    DateTo = DateFrom;
                    break;
                case "ToDateFrom":
                    DateFrom = DateTo;
                    break;
            }
        }



    }
}
