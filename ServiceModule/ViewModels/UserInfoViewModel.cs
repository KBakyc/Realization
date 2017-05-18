using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.Helpers;
using DataObjects;
using DataObjects.Interfaces;
using System.Xml.Linq;
using CommonModule.Interfaces;

namespace ServiceModule.ViewModels
{
    /// <summary>
    /// Модель отображения информации о пользователе.
    /// </summary>
    public class UserInfoViewModel : BasicNotifier
    {
        private UserInfo user;
        IDbService dbService;

        public UserInfoViewModel(IDbService _dbService, UserInfo _user)
        {
            if (_user == null) throw new ArgumentNullException("_user", "Не задан пользователь.");
            dbService = _dbService;
            ParseUserData(_user);
        }

        public UserInfo GetModel()
        {
            return user;
        }

        private bool isCurrentUser = false;
        public bool IsCurrentUser 
        {
            get { return isCurrentUser; }
        }

        private bool isOnline;
        public bool IsOnline
        {
            get { return isOnline; }
            set { SetAndNotifyProperty(()=>IsOnline, ref isOnline, value); }
        }

        public bool IsStarted
        {
            get { return (startTime != null && stopTime == null); }
        }
     
        public bool IsEnabled
        {
            get { return user.IsEnabled; }
        }
        
        public bool IsSystem
        {
            get { return user.IsSystem; }
        }

        public string StatusDescription
        {
            get { return !IsEnabled ? "Пользователь отключён" : (IsOnline ? "Пользователь работает" : "Пользователь не работает"); }
        }

        public int Id
        {
            get { return user.Id; }
        }

        public string FullName
        {
            get { return user.FullName; }
        }
        
        public string Login
        {
            get { return user.Name; }
        }
        
        public int? Context
        {
            get { return user.Context; }
        }
        
        private DateTime? startTime;
        public DateTime? StartTime
        {
            get { return startTime; }
            set { SetAndNotifyProperty("StartTime", ref startTime, value); }
        }

        private DateTime? stopTime;
        public DateTime? StopTime
        {
            get { return stopTime; }
            set 
            {
                SetAndNotifyProperty("StopTime", ref stopTime, value);
                NotifyPropertyChanged("IsStarted");
            }
        }        

        private string commServiceUrl;
        public string CommServiceUrl
        {
            get { return commServiceUrl; }
            set { SetAndNotifyProperty("CommServiceUrl", ref commServiceUrl, value); }
        }        

        private string hostName;
        public string HostName
        {
            get { return hostName; }
            set 
            { SetAndNotifyProperty("HostName", ref hostName, value); }
        }

        private int commServiceStatus = 0;
        public int CommServiceStatus
        {
            get { return commServiceStatus; }
            set { SetAndNotifyProperty("CommServiceStatus", ref commServiceStatus, value); }
        }


        public void ParseUserData(UserInfo _ui)
        {
            if (_ui == user || _ui == null) return;
            user = _ui;
            commServiceUrl = null;
            startTime = null;
            stopTime = null;            

            if (!user.IsEnabled || user.IsSystem) return;

            if (dbService != null)
                isCurrentUser = dbService.UserToken > 0 && user.Id == dbService.UserToken;                    
          
            var clientInfo = user.ClientInfo == null || user.ClientInfo.Name != WorkFlowHelper.CI_CONTAINER ? new XElement(WorkFlowHelper.CI_CONTAINER) : user.ClientInfo;           
            var envEl = clientInfo.Element(WorkFlowHelper.CI_ENVIRONMENT) ?? new XElement(WorkFlowHelper.CI_ENVIRONMENT);        
            var servEl = clientInfo.Element(WorkFlowHelper.CI_COMMSERVLOC) ?? new XElement(WorkFlowHelper.CI_COMMSERVLOC);

            var hostAttr = envEl.Attribute("MachineName"); 
            HostName = hostAttr == null || String.IsNullOrWhiteSpace(hostAttr.Value) ? null : hostAttr.Value.Trim();

            var servUrlAtrr = servEl.Attribute("Url");
            CommServiceUrl = servUrlAtrr == null || String.IsNullOrWhiteSpace(servUrlAtrr.Value) ? null : servUrlAtrr.Value.Trim();
            if (CommServiceStatus == 0 && commServiceUrl != null)
                CommServiceStatus = 1;
            
            DateTime dtParsed;
            var startAtrr = envEl.Attribute("StartTime");            
            StartTime = startAtrr == null || !DateTime.TryParse(startAtrr.Value.Trim(), out dtParsed) ? (DateTime?)null : dtParsed;
            var stopAtrr = envEl.Attribute("StopTime");
            StopTime = stopAtrr == null || !DateTime.TryParse(stopAtrr.Value.Trim(), out dtParsed) ? (DateTime?)null : dtParsed;
            
            if (!IsStarted && CommServiceStatus > 1)
                CommServiceStatus = 1;
        }

        private UserInfoExt uiExt;
        public UserInfoExt UiExt
        {
            get 
            {
                if (uiExt == null) LoadExtUserInfoAsync();
                return uiExt; 
            }
            set { SetAndNotifyProperty("UiExt", ref uiExt, value); }
        }

        private void LoadExtUserInfoAsync()
        {
            System.Threading.Tasks.Task.Factory.StartNew( () => UiExt = dbService.GetUserInfoExt(user.Id) );
        }

        public void Notify()
        {
            NotifyPropertyChanged("FullName");
            NotifyPropertyChanged("Context");
            NotifyPropertyChanged("Login");
            NotifyPropertyChanged("IsEnabled");
            NotifyPropertyChanged("IsCurrentUser");
            NotifyPropertyChanged("IsStarted");
        }
    }
}
