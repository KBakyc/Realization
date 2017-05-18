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
    /// <summary>
    /// Модель диалога по запросу параметров формирования журнала продаж.
    /// </summary>
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
                && dateRangeSelection.IsValid() 
                && dateRangeSelection.DateFrom.Year == dateRangeSelection.DateTo.Year 
                && dateRangeSelection.DateFrom.Month == dateRangeSelection.DateTo.Month
                && selectedJournalType != null
                && (!IsSfInterval || sfDateRangeSelection.IsValid())
                && selectedPodvid != -1
                && selectedSfType != -1
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

        private string[] sftypes = new string[] { "Все с/ф", "Первичные", "Корректировочные" };
        public string[] SfTypes
        {
            get { return sftypes; }
        }

        private int selectedSfType = 0;
        public int SelectedSfType
        {
            get { return selectedSfType; }
            set { SetAndNotifyProperty("SelectedSfType", ref selectedSfType, value); }
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

        private DateRangeDlgViewModel dateRangeSelection; // интервал бухучёта
        public DateRangeDlgViewModel DateRangeSelection
        {
            get { return dateRangeSelection; }
        }

        private bool isSfInterval;
        public bool IsSfInterval
        {
            get { return isSfInterval; }
            set
            {
                SetAndNotifyProperty("IsSfInterval", ref isSfInterval, value);
                if (value && sfDateRangeSelection == null)
                    SfDateRangeSelection = new DateRangeDlgViewModel(false)
                    {
                        DateFrom = dateRangeSelection.DateFrom,
                        DateTo = dateRangeSelection.DateTo
                    };
            }
        }

        private DateRangeDlgViewModel sfDateRangeSelection; // интервал выставления с/ф
        public DateRangeDlgViewModel SfDateRangeSelection
        {
            get { return sfDateRangeSelection; }
            set { SetAndNotifyProperty("SfDateRangeSelection", ref sfDateRangeSelection, value); }
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
        }

    }
}
