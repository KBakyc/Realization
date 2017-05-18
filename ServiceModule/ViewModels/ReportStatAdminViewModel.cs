using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.Helpers;
using Microsoft.Reporting.WinForms;
using DataObjects;
using System.Xml.Linq;
using System.Windows.Input;
using CommonModule.Commands;
using ServiceModule.DAL.Models;
using ServiceModule.DAL;

namespace ServiceModule.ViewModels
{
    /// <summary>
    /// Модель отображения режима просмотра статистики по отчёту.
    /// </summary>
    public class ReportStatAdminViewModel : BasicNotifier
    {
        private EditedReportInfoViewModel eReportInfo;

        public ReportStatAdminViewModel(EditedReportInfoViewModel _ereport)
        {
            eReportInfo = _ereport;
            Init();
        }

        private void Init()
        {
            if (eReportInfo != null && !String.IsNullOrWhiteSpace(eReportInfo.Path))
                GetReportUsers();
            
        }

        private void GetReportUsers()
        {
            using (var db = new ServiceContext())
            {
                var sData = db.GetReportStat(eReportInfo.Path);
                if (sData != null && sData.Length > 0)
                    reportUsers = sData.GroupBy(d => new { d.UserId, d.UserName, d.UserFIO, d.IsActiveUser }).OrderByDescending(g => g.Max(i => i.TimeStart))
                        .ToDictionary(g => new UserInfo 
                        {
                            Id = g.Key.UserId ?? 0, 
                            Name = g.Key.UserName, 
                            FullName = g.Key.UserFIO
                        }, 
                        g => g.OrderByDescending(i => i.TimeStart)
                            .Select(i => new UserReportStatViewModel(i)).ToArray());
            }
        }

        private Dictionary<UserInfo, UserReportStatViewModel[]> reportUsers;
        public Dictionary<UserInfo, UserReportStatViewModel[]> ReportUsers
        {
            get { return reportUsers; }
        }

        private KeyValuePair<UserInfo, UserReportStatViewModel[]> selReportUser;
        public KeyValuePair<UserInfo, UserReportStatViewModel[]> SelReportUser
        {
            get { return selReportUser; }
            set { SetAndNotifyProperty(() => SelReportUser, ref selReportUser, value); }
        }

        private UserReportStatViewModel selReportUserReportStat;

        public UserReportStatViewModel SelReportUserReportStat
        {
            get { return selReportUserReportStat; }
            set { SetAndNotifyProperty("SelReportUserReportStat", ref selReportUserReportStat, value); }
        }
    }
}
