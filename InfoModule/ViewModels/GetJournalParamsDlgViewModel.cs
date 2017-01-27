using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.ViewModels;
using CommonModule.Commands;
using DataObjects;
using DataObjects.Interfaces;

namespace InfoModule.ViewModels
{
    public class GetJournalParamsDlgViewModel : BaseDlgViewModel
    {
        private IDbService repository;

        public GetJournalParamsDlgViewModel(IDbService _repository)
        {
            repository = _repository;
            LoadData();
        }

        public override bool IsValid()
        {
            return base.IsValid()
                && DateRangeSelection.IsValid() 
                && DateRangeSelection.DateFrom.Year == DateRangeSelection.DateTo.Year 
                && DateRangeSelection.DateFrom.Month == DateRangeSelection.DateTo.Month
                && SelectedJournalType != null
                && (!IsPerev || PerevDateRangeSelection.IsValid())
                && SelectedPodvid != -1
                ;
        }

        private string[] podvids = new string[] { "Вся отгрузка", "НДС - 18%", "НДС - 0%", "Без НДС", "Безвозмездно", "НДС - 10%", "НДС - 20%", "НДС - 25%" };
        public string[] Podvids
        {
            get { return podvids; }
        }

        private int selectedPodvid = 0;
        public int SelectedPodvid
        {
            get { return selectedPodvid; }
            set { SetAndNotifyProperty("SelectedPodvid", ref selectedPodvid, value); }
        }

        private JournalTypeModel[] journals;
        public JournalTypeModel[] Journals
        {
            get { return journals; }
        }

        private JournalTypeModel selectedJournalType;
        public JournalTypeModel SelectedJournalType
        {
            get { return selectedJournalType; }
            set { SetAndNotifyProperty("SelectedJournalType", ref selectedJournalType, value); }
        }

        private DateRangeDlgViewModel dateRangeSelection;
        public DateRangeDlgViewModel DateRangeSelection
        {
            get { return dateRangeSelection; }
        }

        public bool IsInterval
        {
            get
            {
                bool res = false;
                if (DateRangeSelection != null && DateRangeSelection.IsValid())
                {
                    res = DateRangeSelection.DateFrom.Day != 1 || DateRangeSelection.DateTo.AddDays(1).Month == DateRangeSelection.DateTo.Month;
                }
                return res;
            }
        }

        private bool isPerev;
        public bool IsPerev
        {
            get { return isPerev; }
            set { SetAndNotifyProperty("IsPerev", ref isPerev, value); }
        }

        private bool isWithCorrSfs;
        public bool IsWithCorrSfs
        {
            get { return isWithCorrSfs; }
            set { SetAndNotifyProperty("IsWithCorrSfs", ref isWithCorrSfs, value); }
        }

        private DateRangeDlgViewModel perevDateRangeSelection;
        public DateRangeDlgViewModel PerevDateRangeSelection
        {
            get { return perevDateRangeSelection; }
        }


        private void LoadData()
        {
            journals = repository.GetJournalTypes(JournalKind.Sell).Where(j => !String.IsNullOrWhiteSpace(j.JournalType)).ToArray();

            var curDate = DateTime.Now.Date;
            DateTime dfrom = curDate.AddMonths(-1).AddDays(-(curDate.Day-1));
            DateTime dto = curDate.AddDays(-curDate.Day);

            dateRangeSelection = new DateRangeDlgViewModel(false)
            {
                DatesLabel = "Журнал за период",
                DateFrom = dfrom,
                DateTo = dto
            };

            perevDateRangeSelection = new DateRangeDlgViewModel(false)
            {
                DateFrom = dfrom,
                DateTo = dto
            };
        }

    }
}
