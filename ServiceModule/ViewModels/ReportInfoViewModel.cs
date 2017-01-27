using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.Helpers;
using DataObjects;
using DataObjects.Interfaces;
using System.Xml.Linq;
using CommonModule.Interfaces;
using ServiceModule.DAL.Models;

namespace ServiceModule.ViewModels
{
    public class ReportInfoViewModel : BasicNotifier
    {
        private ReportInfo report;
        IDbService dbService;

        public ReportInfoViewModel(IDbService _dbService, ReportInfo _report)
        {
            if (_report == null) throw new ArgumentNullException("_user", "Не задан отчёт.");
            report = _report;
            dbService = _dbService;
        }

        public ReportInfo GetModel()
        {
            return report;
        }

        public int Id
        {
            get { return report.Id; }
        }

        public string Name
        {
            get { return report.Name; }
        }

        public string Title
        {
            get { return report.Title; }
        }

        public string Description
        {
            get { return report.Description; }
        }              

        public void Notify()
        {
        }
    }
}
