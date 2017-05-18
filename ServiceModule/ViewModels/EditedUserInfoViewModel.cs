using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using CommonModule.Helpers;
using DataObjects;

namespace ServiceModule.ViewModels
{
    /// <summary>
    /// Модель отображения редактируемой информации о пользователе.
    /// </summary>
    public class EditedUserInfoViewModel : BasicNotifier
    {
        public EditedUserInfoViewModel(UserInfo _u)
        {
            user = _u;
            if (_u != null)
                CollectData();
        }

        private void CollectData()
        {
            Id = user.Id;
            Name = user.Name;
            FullName = user.FullName;
            EmailAddress = user.EmailAddress;
            Ceh = user.Ceh;
            TabNum = user.TabNum;
            IsSystem = user.IsSystem;
            IsEnabled = user.IsEnabled;
            Context = user.Context;
            if (user.ClientInfo != null)
                clientInfo = new XElement(user.ClientInfo);
        }

        public UserInfo GetEditedUserInfo()
        {
            if (!IsValid()) return null;
            UserInfo newUser = new UserInfo() 
            {
                Id = Id,
                Name = Name,
                FullName = FullName,
                IsEnabled = IsEnabled,
                IsSystem = IsSystem,
                EmailAddress = EmailAddress,
                TabNum = TabNum,
                Ceh = Ceh,
                Context = Context,
                ClientInfo = clientInfo
            };
            return newUser;
        }

        public bool IsValid()
        {
            return Id > 0
                && !String.IsNullOrWhiteSpace(Name);
        }

        public bool IsChanged()
        {
            return user == null
                || Id != user.Id
                || Name != user.Name
                || FullName != user.FullName
                || IsEnabled != user.IsEnabled
                || IsSystem != user.IsSystem
                || EmailAddress != user.EmailAddress
                || TabNum != user.TabNum
                || Ceh != user.Ceh
                || Context != user.Context
                || ClientInfo != (user.ClientInfo != null ? user.ClientInfo.ToString() : null);
        }

        private UserInfo user;
        public int PreviousId { get { return user == null ? 0 : user.Id; } }

        private int id;
        public int Id
        {
            get { return id; }
            set { SetAndNotifyProperty(() => Id, ref id, value); }
        }

        private string name;
        public string Name
        {
            get { return name; }
            set { SetAndNotifyProperty(() => Name, ref name, value); }
        }

        private string fullName;
        public string FullName
        {
            get { return fullName; }
            set { SetAndNotifyProperty(() => FullName, ref fullName, value); }
        }
        
        public bool IsEnabled { get; set; }
        public bool IsSystem { get; set; }
        public string EmailAddress { get; set; }
        public string TabNum { get; set; }
        public string Ceh { get; set; }

        private XElement clientInfo;
        public string ClientInfo
        {
            get { return clientInfo == null ? null : clientInfo.ToString(); }
            set 
            {
                XElement newInfo = null;
                if (!String.IsNullOrWhiteSpace(value))
                {
                    try
                    {
                        newInfo = XElement.Parse(value, LoadOptions.None);
                    }
                    catch { }
                    if (newInfo != null && newInfo.Name != WorkFlowHelper.CI_CONTAINER)
                    {
                        var newcont = new XElement(WorkFlowHelper.CI_CONTAINER);
                        newcont.Add(newInfo);
                        newInfo = newcont;
                    }
                }
                clientInfo = newInfo;
                NotifyPropertyChanged(() => ClientInfo);
            }
        }        

        public int? Context { get; set; }
    }
}
