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
    public class GetJournalBuyParamsDlgViewModel : BaseDlgViewModel
    {
        private IDbService repository;
        private int curYear;
        private string path_jza;

        public GetJournalBuyParamsDlgViewModel(IDbService _repository)
        {
            repository = _repository;
            //path_jza = DAL.DALSettings.JzaPath;
            LoadData();
        }

        public override bool IsValid()
        {
            return base.IsValid()
                && selectedJournalType != null
                && selectedMonth != null
                && selectedYear <= curYear
                ;
        }       

        private JournalTypeModel[] journals;
        public JournalTypeModel[] Journals
        {
            get { return journals; }
        }

        private string[] months = new string[12];//Enumerable.Range(1, 12).Select(n => System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(n)).ToArray();
        public string[] Months
        {
            get { return months; }
            set { SetAndNotifyProperty("Months", ref months, value); }
        } 

        private string selectedMonth;
        public string SelectedMonth
        {
            get { return selectedMonth; }
            set 
            {
                if (value != selectedMonth) ChangeMonth(value);
                //SetAndNotifyProperty("SelectedMonth", ref selectedMonth, value);                
            }
        }

        private void ChangeMonth(string _newMonth)
        {
            MonthIndex = Array.IndexOf(Months, _newMonth) + 1;            
            SetAndNotifyProperty("SelectedMonth", ref selectedMonth, _newMonth);
            ChangePath();
        }

        private void ChangePath()
        {
            if (!IsSaveToDbf || SelectedJournalType == null || String.IsNullOrWhiteSpace(SelectedJournalType.JournalType))
            {
                JzaPath = JzaName = JzaFullPath = null;
            }
            else
            {
                JzaPath = GetJzaPath(MonthIndex);
                JzaName = SelectedJournalType.JournalType.Trim() + MonthIndex.ToString("X");
                JzaFullPath = System.IO.Path.Combine(JzaPath, JzaName);
            }
            NotifyPropertyChanged("JzaFullPath");
        }

        private string GetJzaPath(int _month)
        {
            return _month < 1 || _month > 12 ? System.IO.Path.GetFullPath(path_jza)
                                             : System.IO.Path.GetFullPath(System.IO.Path.Combine(path_jza, _month.ToString("00")));
        }

        public string JzaPath { get; set; }
        public string JzaName { get; set; }
        public string JzaFullPath { get; set; }
        public int MonthIndex { get; set; }

        private int[] years;
        public int[] Years
        {
            get { return years; }
            set { SetAndNotifyProperty("Years", ref years, value); }
        } 
        
        private int selectedYear;
        public int SelectedYear
        {
            get { return selectedYear; }
            set { SetAndNotifyProperty("SelectedYear", ref selectedYear, value); }
        }

        private JournalTypeModel selectedJournalType;
        public JournalTypeModel SelectedJournalType
        {
            get { return selectedJournalType; }
            set 
            { 
                SetAndNotifyProperty("SelectedJournalType", ref selectedJournalType, value);
                ChangePath();
            }
        }

        private bool isSaveToDbf;
        public bool IsSaveToDbf
        {
            get { return isSaveToDbf; }
            set
            {
                SetAndNotifyProperty("IsSaveToDbf", ref isSaveToDbf, value);
                ChangePath();
            }
        }

        private void LoadData()
        {
            journals = repository.GetJournalTypes(JournalKind.Buy);

            var curDate = DateTime.Now.Date;
            var lastMonthDate = curDate.AddMonths(-1);
            curYear = curDate.Year;
            years = Enumerable.Range(0, 5).Select(n => curYear - n).ToArray();
            Array.Copy(System.Globalization.DateTimeFormatInfo.CurrentInfo.MonthNames, 0, months, 0, 12);
            selectedYear = lastMonthDate.Year;
            selectedMonth = months[lastMonthDate.Month-1];
            ChangeMonth(selectedMonth);
        }

    }
}
