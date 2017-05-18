using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using CommonModule.Helpers;
using DataObjects;
using ServiceModule.DAL.Models;
using DotNetHelper;
using DataObjects.Interfaces;
using System.Data.Entity;


namespace ServiceModule.ViewModels
{
    /// <summary>
    /// Модель отображения редактируемой информации об отчёте.
    /// </summary>
    public class EditedReportInfoViewModel : BasicNotifier
    {
        public EditedReportInfoViewModel(ReportInfo _r)
        {
            report = _r;
            if (_r != null)
                CollectData();
        }

        private void CollectData()
        {
            Id = report.Id;
            ComponentTypeName = report.ComponentTypeName;
            CategoryName = report.CategoryName;
            Name = report.Name;
            Title = report.Title;
            Description = report.Description;
            Path = report.Path;
            ParamsGetter = report.ParamsGetter;
            ParamsGetterOptions = report.ParamsGetterOptions;
            IsA3Enabled = report.IsA3Enabled;

            CollectFavoriteUsers(report.Id);
        }

        private void CollectFavoriteUsers(int _idrep)
        {
            using (var db = new ServiceModule.DAL.ServiceContext())
            {
                db.ReportInfos.Attach(report);
                db.Entry(report).Collection(r => r.FavoriteUsers).Load();
                FavoriteUsers = report.FavoriteUsers;
                FavoriteUsers.ForEach(u => u.FavoriteReports = null);
            }
        }

        private List<ARM_User> favoriteUsers;
        public List<ARM_User> FavoriteUsers
        {
            get { return favoriteUsers; }
            set { SetAndNotifyProperty(() => FavoriteUsers, ref favoriteUsers, value); }
        }

        public ReportInfo GetEditedReportInfo()
        {
            if (!IsValid()) return null;
            ReportInfo newReport = new ReportInfo()
            {
                Id = Id,
                ComponentTypeName = ComponentTypeName,
                CategoryName = CategoryName,
                Name = Name,
                Title = Title,
                Description = Description,
                Path = Path,
                ParamsGetter = ParamsGetter,
                ParamsGetterOptions = ParamsGetterOptions,
                IsA3Enabled = IsA3Enabled,
                FavoriteUsers = FavoriteUsers
            };
            return newReport;
        }

        public bool IsValid()
        {
            return Id > 0
                && !String.IsNullOrWhiteSpace(ComponentTypeName)
                && !String.IsNullOrWhiteSpace(Title)
                && !String.IsNullOrWhiteSpace(Path)
                && (String.IsNullOrWhiteSpace(ParamsGetterOptions) || paramsGetterOptionsXML != null);
        }

        public bool IsChanged()
        {
            return report == null
                || Id != report.Id
                || Name != report.Name
                || Title != report.Title
                || ComponentTypeName != report.ComponentTypeName
                || CategoryName != report.CategoryName
                || Description != report.Description
                || Path != report.Path
                || ParamsGetter != report.ParamsGetter
                || ParamsGetterOptions != report.ParamsGetterOptions
                || IsA3Enabled != report.IsA3Enabled
                ;
        }

        private ReportInfo report;
        public int PreviousId { get { return report == null ? 0 : report.Id; } }

        public int Id { get; set; }
        public string ComponentTypeName { get; set; }// [varchar](150) NOT NULL,
        public string CategoryName { get; set; }// [varchar](150) NULL,
        public string Name { get; set; }// [varchar](150) NULL,
        public string Title { get; set; }// [varchar](150) NOT NULL,
        public string Description { get; set; }// [varchar](250) NULL,
        public string Path { get; set; }// [varchar](150) NOT NULL,
        public string ParamsGetter { get; set; }// [varchar](250) NULL,

        private XElement paramsGetterOptionsXML;
        public XElement ParamsGetterOptionsXML
        {
            get { return paramsGetterOptionsXML; }
        }

        private string paramsGetterOptions;
        public string ParamsGetterOptions 
        {
            get { return paramsGetterOptions; }
            set 
            {
                paramsGetterOptionsXML = null;
                if (!String.IsNullOrWhiteSpace(value))
                {
                    try
                    {
                        paramsGetterOptionsXML = XElement.Parse(String.Format("<ParamsGetterOptions>{0}</ParamsGetterOptions>", value));
                    }
                    catch
                    { }
                }
                var newvalue = paramsGetterOptionsXML == null ? value : String.Join(Environment.NewLine, paramsGetterOptionsXML.Elements().Select(e => e.ToString()).ToArray());
                SetAndNotifyProperty(() => ParamsGetterOptions, ref paramsGetterOptions, newvalue);
            } 
        }// [varchar](2048) NULL,
        
        public bool? IsA3Enabled { get; set; }// [bit] NULL,
    }
}
