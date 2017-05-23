using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.ViewModels;
using CommonModule.Interfaces;
using DataObjects.Interfaces;
using System.Collections.ObjectModel;
using CommonModule.Commands;
using System.Windows.Data;
using System.Threading.Tasks;
using System.ServiceModel;
using System.Threading;
using System.Windows.Input;
using System.Xml.Linq;
using DataObjects;
using CommonModule.Helpers;
using DotNetHelper;
using System.ServiceModel.Discovery;
using ServiceModule.Helpers;
using ServiceModule.DAL.Models;
using CommonModule.Composition;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Diagnostics;
using ServiceModule.DAL;

namespace ServiceModule.ViewModels
{    
    /// <summary>
    /// Модель режима управления пользователями.
    /// </summary>
    public class UsersAdminViewModel : BasicModuleContent
    {
        IDbService dbService;

        public UsersAdminViewModel(IModule _parent, IDbService _dbService)
            : base(_parent)
        {
            Title = "Информация о пользователях";
            dbService = _dbService;
            LoadData();
            selectUserByIdCommand = new DelegateCommand<int>(ExecSelectUserById, id => id > 0);
            sendMessageCommand = new DelegateCommand<bool>(ExecSendMessage, CanSendMessage);
            saveUserInfoCommand = new DelegateCommand(ExecSaveUserInfo, CanSaveUserInfo);
            deleteUserInfoCommand = new DelegateCommand(ExecDeleteUserInfo, CanDeleteUserInfo);
            normaliseUsersCommand = new DelegateCommand(ExecNormaliseUsers, CanNormaliseUsers);
            addDopInfoCommand = new DelegateCommand(ExecAddDopInfo, CanAddDopInfo);
            delDopInfoCommand = new DelegateCommand(ExecDelDopInfo, CanDelDopInfo);
            createCiElementCommand = new DelegateCommand(ExecCreateCiElement, CanCreateCiElement);

            deleteRightCommand = new DelegateCommand<ComponentUserRight>(ExecDeleteRight, CanDeleteRight);
            undoRightsCommand = new DelegateCommand(ExecUndoRights, CanUndoRights);
            submitRightsCommand = new DelegateCommand(ExecSubmitRights, CanSubmitRights);

            addDefaultRightCommand = new DelegateCommand<int>(ExecAddDefaultRight, CanAddDefaultRight);
            addUserRightCommand = new DelegateCommand<int>(ExecAddUserRight, CanAddUserRight);
            pingServiceCommand = new DelegateCommand<UserInfoViewModel>(ExecPingService, CanPingService);
            getUserLogCommand = new DelegateCommand(ExecGetUserLog, CanGetUserLog);
            getUserScreenCommand = new DelegateCommand(ExecGetUserScreen, CanGetUserScreen);
            exploreUserCommand = new DelegateCommand(ExecExploreUser, CanExploreUser);
            exploredPath = AppDomain.CurrentDomain.BaseDirectory;

            collectFromADCommand = new DelegateCommand(ExecCollectFromADCommand, CanCollectFromAD);

            InitRepkaServiceAnnounceListener();

            //List<string> fhosts = new List<string>();
            //foreach (var user in users.Where(u => !String.IsNullOrWhiteSpace(u.HostName) && u.IpAddress == null))
            //    fhosts.Add(String.Format("user:{0} host:{1}", user.FullName, user.HostName));
            //Parent.Services.ShowMsg("Hosts not resolved", String.Join(Environment.NewLine, fhosts), true);
        }

        private AnnouncementService announcementService;
        private ServiceHost announcementServiceHost;

        private void InitRepkaServiceAnnounceListener()
        {
            announcementService = new AnnouncementService();
            announcementService.OnlineAnnouncementReceived += OnOnlineEvent;
            announcementService.OfflineAnnouncementReceived += OnOfflineEvent;

            announcementServiceHost = new ServiceHost(announcementService);
            announcementServiceHost.AddServiceEndpoint(new UdpAnnouncementEndpoint());
            announcementServiceHost.Open();
        }

        private void DisposeAnnounceListener()
        {
            if (announcementServiceHost != null)
            {
                announcementServiceHost.Close();
                announcementServiceHost = null;
                announcementService = null;
            }
        }

        private UserInfoViewModel GetUserByMetadata(XElement _met)
        {
            var userId = 0;
            UserInfoViewModel res = null;
            if (_met != null 
                && users != null && users.Count != 0
                && _met.Name.ToString().ToLower() == "userid" 
                && !String.IsNullOrWhiteSpace(_met.Value) 
                && int.TryParse(_met.Value.Trim(), out userId) 
                && userId > 0)
                res = users.FirstOrDefault(u => u.Id == userId);
            return res;
        }

        //private List<int> announcements = new List<int>();

        private void OnOnlineEvent(object sender, AnnouncementEventArgs e)
        {
            var userDataEl = e.EndpointDiscoveryMetadata.Extensions.FirstOrDefault(x => x.Name.ToString().ToLower() == "userid");
            var user = GetUserByMetadata(userDataEl);
            if (user != null)
                user.CommServiceStatus = 3;
        }

        private void OnOfflineEvent(object sender, AnnouncementEventArgs e)
        {
            var userDataEl = e.EndpointDiscoveryMetadata.Extensions.FirstOrDefault(x => x.Name.ToString().ToLower() == "userid");
            var user = GetUserByMetadata(userDataEl);
            if (user != null)
                user.CommServiceStatus = 1;
        }
        
        private ServicePinger pinger = new ServicePinger(50);

        private void LoadData()
        {
            if (dbService == null) return;
            var uData = dbService.GetAllUserInfos();
            users = new ObservableCollection<UserInfoViewModel>(uData.Select(u => new UserInfoViewModel(dbService, u)));
            
            Task.Factory.StartNew(() =>
            {
                foreach (var usr in users)
                    usr.IsOnline = usr.IsStarted && pinger.HostOnline(usr.HostName); 
                DiscoverAllServiceIsOnline();
                InitServTimer();
            });                        
        }

        private void RefreshData()
        {
            if (dbService == null) return;
            var uData = dbService.GetAllUserInfos();
            foreach (var nu in uData)
            {
                var user = users.FirstOrDefault(u => u.Id == nu.Id);
                if (user == null)
                {
                    var nuvm = new UserInfoViewModel(dbService, nu);
                    users.Add(nuvm);
                }
                else
                    user.ParseUserData(nu);
                user.Notify();
                user.IsOnline = user.IsStarted && pinger.HostOnline(user.HostName);
            }
        }        

        private void InitServTimer()
        { 
            servTimer = new System.Timers.Timer();
            servTimer.Interval = 10000;
            servTimer.Elapsed += new System.Timers.ElapsedEventHandler(servTimer_Tick);
            servTimer.Start();
        }

        private void DisposeServTimer()
        {
            if (servTimer != null)
            {
                servTimer.Stop();
                servTimer.Elapsed -= servTimer_Tick;
                servTimer.Dispose();
                servTimer = null;
            }
        }

        private DiscoveryClient dClient;
        
        private void DisposeDiscovery()
        {
            if (dClient != null)
            {
                dClient.FindProgressChanged -= dClient_FindProgressChanged;
                dClient.FindCompleted -= dClient_FindCompleted;
                dClient.Close();
                dClient = null;
            }
        }

        private void DiscoverAllServiceIsOnline()
        {
            dClient = new DiscoveryClient(new UdpDiscoveryEndpoint());
            dClient.FindProgressChanged += dClient_FindProgressChanged;
            dClient.FindCompleted += dClient_FindCompleted;
            foreach (var ou in users.Where(u => u.IsOnline && u.CommServiceStatus > 0))
                ou.CommServiceStatus = 2;
            dClient.FindAsync(new FindCriteria(typeof(IRepkaService)));
        }

        void dClient_FindCompleted(object sender, FindCompletedEventArgs e)
        {
            foreach (var ou in users.Where(u => u.CommServiceStatus == 2))
                ou.CommServiceStatus = 1;
            DisposeDiscovery();
        }

        void dClient_FindProgressChanged(object sender, FindProgressChangedEventArgs e)
        {
            var userDataEl = e.EndpointDiscoveryMetadata.Extensions.FirstOrDefault(x => x.Name.ToString().ToLower() == "userid");
            //int uid;
            var user = GetUserByMetadata(userDataEl);//, out uid);
            if (user != null)
                user.CommServiceStatus = 3;
        }

        private int refreshingStatus = 0;

        void servTimer_Tick(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (0 == Interlocked.CompareExchange(ref refreshingStatus, 1, 0))
                Task.Factory.StartNew(() =>
                {
                    RefreshData();
                    Interlocked.Exchange(ref refreshingStatus, 0);
                })
                .ContinueWith(t => UpdateUsersView());
        }

        private void UpdateUsersView()
        {
            Parent.ShellModel.UpdateUi(() =>
            {
                var view = CollectionViewSource.GetDefaultView(users);
                if (view.SortDescriptions.Count == 0)
                {
                    view.SortDescriptions.Add(new System.ComponentModel.SortDescription("IsOnline", System.ComponentModel.ListSortDirection.Descending));
                    view.SortDescriptions.Add(new System.ComponentModel.SortDescription("Id", System.ComponentModel.ListSortDirection.Ascending));
                }
                view.Refresh();
            }, false, true);
        }        

        private ObservableCollection<UserInfoViewModel> users;
        public ObservableCollection<UserInfoViewModel> Users
        {
            get { return users; }
        }
        
        private UserInfoViewModel selectedUser;
        public UserInfoViewModel SelectedUser
        {
            get { return selectedUser; }
            set 
            {
                if (selectedUser != value)
                {
                    selectedUser = value;
                    if (curAdminMode == AdminMode.Permissions)
                        Parent.Services.DoWaitAction(() => { LoadUserRights(selectedUser); });

                    NotifyPropertyChanged("SelectedUser");
                    if (selectedUser != null)
                        IsNewSelected = false;
                }
            }
        }

        private bool isNewSelected = false;
        public bool IsNewSelected
        {
            get { return isNewSelected; }
            set 
            {
                isNewSelected = value;
                NotifyPropertyChanged("IsNewSelected"); 
                MakeSelectedCopy();
            }
        }

        private EditedUserInfoViewModel selectedCopy;
        public EditedUserInfoViewModel SelectedCopy
        {
            get { return selectedCopy; }
            set { SetAndNotifyProperty("SelectedCopy", ref selectedCopy, value); }
        }

        private void MakeSelectedCopy()
        {
            EditedUserInfoViewModel selUser = null;
            if (isNewSelected || selectedUser != null)
                selUser = new EditedUserInfoViewModel(!isNewSelected ? selectedUser.GetModel() : null);
            SelectedCopy = selUser;
            if (isNewSelected)
                selectedCopy.Id = GetFreeUserId();
        }

        private int GetFreeUserId()
        {
            const int idfrom = 1000;
            int res = 0;
            var ids = users.Where(u => u.Id >= idfrom && u.Id < 90000).OrderBy(u => u.Id).Select(u => u.Id).ToArray();
            if (ids != null && ids.Length > 0)
                for (int i = 0; i < ids.Length; i++)
                    if (ids[i] != i + idfrom)
                    {
                        res = i + idfrom;
                        break;
                    }            
            return res;
        }

        private ICommand deleteUserInfoCommand;
        public ICommand DeleteUserInfoCommand
        {
            get { return deleteUserInfoCommand; }
        }

        private const string REAL_USERS_GROUP = "REAL_Users";

        private Tuple<string, string, bool> CollectADInfo(string _login)
        {
            if (String.IsNullOrWhiteSpace(_login)) return null;
            
            Tuple<string, string, bool> res = null;            
            var splLogin = _login.Split('\\');
            string domain = "LAN";
            string login = splLogin[splLogin.Length - 1];
            if (splLogin.Length > 1)
                domain = String.Join("\\", splLogin.Take(splLogin.Length - 1).ToArray());
            
            var context = new System.DirectoryServices.AccountManagement.PrincipalContext(System.DirectoryServices.AccountManagement.ContextType.Domain, domain);
            var user = System.DirectoryServices.AccountManagement.UserPrincipal.FindByIdentity(context, login);
            bool isInGroup =  user.IsMemberOf(context, System.DirectoryServices.AccountManagement.IdentityType.Name, REAL_USERS_GROUP);
            if (user != null)
                res = Tuple.Create(String.Join("\\", domain, login), user.DisplayName, isInGroup);

            return res;
        }

        private ICommand collectFromADCommand;
        public ICommand CollectFromADCommand
        {
            get { return collectFromADCommand; }
        }
        
        private bool CanCollectFromAD()
        {
            return isNewSelected && selectedCopy != null && !String.IsNullOrWhiteSpace(selectedCopy.Name) && !selectedCopy.IsSystem;
        }

        private void ExecCollectFromADCommand()
        {
            var login = selectedCopy.Name;
            Action work = () => 
            {
                var adinfo = CollectADInfo(login);
                if (adinfo != null)
                {
                    selectedCopy.Name = adinfo.Item1;
                    selectedCopy.FullName = adinfo.Item2;
                    if (!adinfo.Item3)
                        Parent.Services.ShowMsg("Внимание!", String.Format("Пользователь {0} ({1})\nне является членом группы {2}", selectedCopy.FullName, selectedCopy.Name, REAL_USERS_GROUP), true);
                }
            };
            Parent.Services.DoWaitAction(work);
        }
        
        private bool CanDeleteUserInfo()
        {
            return !isNewSelected && selectedCopy != null && selectedUser != null;
        }
        
        private void ExecDeleteUserInfo()
        {
            var title = selectedUser.FullName ?? selectedUser.Login;
            var askDlg = new MsgDlgViewModel
            {
                Title = "Внимание",
                Message = "Удалить пользователя " + title + " ?",
                IsCancelable = true,
                OnSubmit = d =>
                {
                    Parent.CloseDialog(d);
                    DeleteUserInfo();
                }
            };
            Parent.OpenDialog(askDlg);
        }

        private void DeleteUserInfo()
        {
            var id = selectedCopy.PreviousId;
            var title = selectedUser.FullName ?? selectedUser.Login;
            if (!dbService.UpdateUserInfo(selectedCopy.PreviousId, new UserInfo { Id = 0}))
            {
                Parent.Services.ShowMsg("Ошибка", "Не удалось удалить данные о пользователе", true);
                return;
            };
            users.Remove(selectedUser);
            SelectedCopy = null;
            SelectedUser = null;
            Parent.Services.ShowMsg("Информация", String.Format("Данные пользователя\nId={0} {1}\nудалены из системы!", id, title), true);
        }

        private ICommand saveUserInfoCommand;
        public ICommand SaveUserInfoCommand
        {
            get { return saveUserInfoCommand; }
        }
        
        private bool CanSaveUserInfo()
        {
            return selectedCopy != null
                && selectedCopy.IsValid()
                && selectedCopy.IsChanged();
        }

        private void ExecSaveUserInfo()
        {
            var user = selectedCopy.GetEditedUserInfo();
            if (user == null) 
            {
                Parent.Services.ShowMsg("Ошибка", "Не удалось получить изменённую информацию о пользователе", true);
                return;
            }
            if (!dbService.UpdateUserInfo(selectedCopy.PreviousId, user))
            {
                Parent.Services.ShowMsg("Ошибка", "Не удалось сохранить изменённый данные по пользователе", true);
                return;
            };            
            if (!isNewSelected)
                users.Remove(selectedUser);
            var updatedUser = dbService.GetUserInfo(user.Id);
            if (updatedUser != null) 
            {
                var nuVm = new UserInfoViewModel(dbService, updatedUser);
                var uAfter = users.OrderBy(u => u.Id).FirstOrDefault(u => u.Id > updatedUser.Id);
                if (uAfter == null)
                    users.Add(nuVm);
                else
                    users.Insert(users.IndexOf(uAfter), nuVm);
                SelectedUser = nuVm;
            }
        }

        private ICommand submitRightsCommand;
        public ICommand SubmitRightsCommand
        {
            get { return submitRightsCommand; }
        }

        private bool CanSubmitRights()
        {
            return (defaultRightsChanged || userRightsChanged) && selectedUser != null;
        }

        private void ExecSubmitRights()
        {
            
            Parent.Services.DoWaitAction(() => 
            {
                if (defaultRightsChanged)
                    DoSubmitRights(0, defaultRights);
                if (userRightsChanged && selectedUser != null)
                    DoSubmitRights(selectedUser.Id, userRights);
                LoadUserRights(selectedUser); 
            });
        }

        private void DoSubmitRights(int _userid, IEnumerable<ComponentUserRight> _newr)
        {
            using (var db = new ServiceContext())
                db.UpdateComponentUserRights(_userid, _newr);                
        }

        private ICommand undoRightsCommand;
        public ICommand UndoRightsCommand
        {
            get { return undoRightsCommand; }
        }

        private bool CanUndoRights()
        {
            return (defaultRightsChanged || userRightsChanged) && selectedUser != null;
        }

        private void ExecUndoRights()
        {
            Parent.Services.DoWaitAction(() => { LoadUserRights(selectedUser); });
        }

        private ICommand addDefaultRightCommand;
        public ICommand AddDefaultRightCommand
        {
            get { return addDefaultRightCommand; }
        }

        private bool CanAddDefaultRight(int _access)
        {
            return curAdminMode == AdminMode.Permissions
                && selectedComponent != null 
                && defaultRights != null 
                && !defaultRights.Any(r => r.ComponentTypeName == selectedComponent);
        }

        private void ExecAddDefaultRight(int _access)
        {
            defaultRights.Add(new ComponentUserRight { AccessLevel = _access, ComponentTypeName = selectedComponent, UserId = null });
            defaultRightsChanged = true;
        }

        private ICommand exploreUserCommand;
        public ICommand ExploreUserCommand
        {
            get { return exploreUserCommand; }
        }

        private bool CanExploreUser()
        {
            return selectedUser != null
                && selectedUser.IsOnline
                && selectedUser.CommServiceStatus == 3
                && !String.IsNullOrWhiteSpace(exploredPath);
        }

        private void ExecExploreUser()
        {            
            Action work = () =>
            {
                try
                {
                    if (exploredUser != null)
                    {
                        var eres = ExploreUser(exploredUser, false);
                        if (!String.IsNullOrWhiteSpace(eres))
                            ShowSentMessage(exploredUser, eres);
                    }
                    exploredUser = (selectedUser != exploredUser ? selectedUser : null);
                    if (exploredUser != null)
                    {
                        var eres = ExploreUser(exploredUser, true);
                        if (!String.IsNullOrWhiteSpace(eres))
                            ShowSentMessage(exploredUser, eres);
                        string cmd = String.Format(@"\\{0}\Repka", exploredUser.HostName);
                        RunCommand(cmd, "", true);
                    }
                }
                catch (Exception e)
                {
                    WorkFlowHelper.OnCrash(e);
                }
            };
         
            Parent.Services.DoWaitAction(work, "Подождите", "Запрос информации от сервиса");
        }

        private bool RunCommand(string _cmd, string _args, bool _shell)
        {
            bool res = false;
            var proc = new Process();
            proc.StartInfo = new ProcessStartInfo(_cmd, _args)
            {
                UseShellExecute = _shell,
            };
            try
            {
                proc.Start();
                proc.WaitForExit();
                res = true;
            }
            catch { }
            return res;
        }

        private string exploredPath;
        public string ExploredPath
        {
            get { return exploredPath; }
            set { SetAndNotifyProperty(() => ExploredPath, ref exploredPath, value); }
        }

        private UserInfoViewModel exploredUser;

        private string ExploreUser(UserInfoViewModel _user, bool _on)
        {
            string res;
            var address = new System.ServiceModel.EndpointAddress(new Uri(_user.CommServiceUrl), EndpointIdentity.CreateUpnIdentity(_user.Login));
            var bind = new NetTcpBinding() { MaxReceivedMessageSize = int.MaxValue };
            var factory = new ChannelFactory<IRepkaChannel>(bind, address);
            IRepkaChannel repkaService = factory.CreateChannel();
            try
            {                
                res = repkaService.SetShare(exploredPath, _on);
            }
            catch (Exception e)
            {
                res = e.ToString(); ;
            }
            finally
            {
                repkaService.Close();
                factory.Close();
            }
            return res;
        }

        private ICommand getUserScreenCommand;
        public ICommand GetUserScreenCommand
        {
            get { return getUserScreenCommand; }
        }

        private bool CanGetUserScreen()
        {
            return selectedUser != null
                && selectedUser.IsOnline
                && selectedUser.CommServiceStatus == 3;
        }

        private void ExecGetUserScreen()
        {            
            Action work = () =>
            {
                try
                {
                    var ustream = GetUserScreenAction(selectedUser.CommServiceUrl, selectedUser.Login);
                    Parent.ShellModel.UpdateUi(() => ShowUserScreen(ustream), true, true);
                }
                catch (Exception e)
                {
                    WorkFlowHelper.OnCrash(e);
                }
            };
         
            Parent.Services.DoWaitAction(work, "Подождите", "Запрос информации от сервиса");
        }

        private void ShowUserScreen(MemoryStream _stream)
        {
            if (_stream == null) return;

            BitmapImage screen = null;
            screen = new BitmapImage();
            screen.BeginInit();
            screen.CacheOption = BitmapCacheOption.OnLoad;
            screen.StreamSource = _stream;
            screen.EndInit();
            screen.Freeze();
            _stream.Dispose();

            if (screen != null)
            {
                var msgdlg = new MsgDlgViewModel
                {
                    Title = String.Format("Экран пользователя: {0}", selectedUser.FullName),
                    MessageType = MsgType.ImageSource,
                    ImageMsg = screen,
                    MaxWidth = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Width - 200,
                    BgColor = "CornflowerBlue"
                };
                Parent.OpenDialog(msgdlg);
            }            
        }


        private MemoryStream GetUserScreenAction(string _url, string _login)
        {
            MemoryStream res = null;
            var address = new System.ServiceModel.EndpointAddress(new Uri(_url), EndpointIdentity.CreateUpnIdentity(_login));
            var bind = new NetTcpBinding() { MaxReceivedMessageSize = int.MaxValue };
            var factory = new ChannelFactory<IRepkaChannel>(bind, address);
            IRepkaChannel repkaService = factory.CreateChannel();
            try
            {
                var sstream = repkaService.GetScreen();
                if (sstream != null)
                    using (sstream)
                    {
                        res = new MemoryStream();
                        sstream.CopyTo(res, 512);
                        sstream.Close();
                    }                    
                }
            catch
            {
                if (res != null)
                {
                    res.Dispose();
                    res = null;
                }
            }
            finally
            {
                repkaService.Close();
                factory.Close();
            }
            return res;
        }

        private ICommand getUserLogCommand;
        public ICommand GetUserLogCommand
        {
            get { return getUserLogCommand; }
        }

        private bool CanGetUserLog()
        {
            return selectedUser != null 
                && selectedUser.IsOnline
                && selectedUser.CommServiceStatus == 3;
        }

        private void ExecGetUserLog()
        {
            string log = null;
            Action work = () =>
            {
                try
                {
                    log = GetUserLogAction(selectedUser.CommServiceUrl, selectedUser.Login);
                }
                catch (Exception e)
                {
                    WorkFlowHelper.OnCrash(e);
                }
            };
            Action after = () =>
            {
                if (!String.IsNullOrWhiteSpace(log))
                {
                    var msgdlg = new MsgDlgViewModel
                    {
                        Title = String.Format("Протокол работы пользователя: {0}", selectedUser.FullName),
                        Message = log,
                        MessageType = MsgType.Text,
                        MaxWidth = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Width - 200,
                        BgColor = "CornflowerBlue"
                    };
                    Parent.OpenDialog(msgdlg);
                }
            };
            Parent.Services.DoWaitAction(work, "Подождите", "Запрос информации от сервиса", after);
        }

        private string GetUserLogAction(string _url, string _login)
        {
            string res = null;
            var address = new System.ServiceModel.EndpointAddress(new Uri(_url), EndpointIdentity.CreateUpnIdentity(_login));
            var bind = new NetTcpBinding() { MaxReceivedMessageSize = int.MaxValue };
            var factory = new ChannelFactory<IRepkaChannel>(bind, address);
            IRepkaChannel repkaService = factory.CreateChannel();
            var lstream = repkaService.GetLog();
            if (lstream != null)
            {
                using (var lreader = new System.IO.StreamReader(lstream, Encoding.GetEncoding(1251)))
                    res = lreader.ReadToEnd();
                lstream.Close();
            }
            repkaService.Close();
            factory.Close();
            return res;
        }

        private ICommand pingServiceCommand;
        public ICommand PingServiceCommand
        {
            get { return pingServiceCommand; }
        }

        private bool CanPingService(UserInfoViewModel _u)
        {
            return _u != null 
                && _u.IsOnline
                && _u.CommServiceStatus > 0;
        }

        private void ExecPingService(UserInfoViewModel _u)
        {
            Task.Factory.StartNew(() =>
            {
                _u.CommServiceStatus = 2;
                try
                {
                    _u.CommServiceStatus = PingServiceAction(_u.CommServiceUrl, _u.Login) ? 3 : 1;
                }
                catch (Exception e)
                {
                    WorkFlowHelper.OnCrash(e);
                    _u.CommServiceStatus = 1;
                }
            });
        }

        private bool PingServiceAction(string _url, string _login)
        {
            bool res = false;
            var address = new System.ServiceModel.EndpointAddress(new Uri(_url), EndpointIdentity.CreateUpnIdentity(_login));
            var bind = new NetTcpBinding();
            var factory = new ChannelFactory<IRepkaChannel>(bind, address);
            IRepkaChannel repkaService = factory.CreateChannel();
            res = repkaService.IsOnline();
            repkaService.Close();
            factory.Close();
            return res;
        }

        private ICommand addUserRightCommand;
        public ICommand AddUserRightCommand
        {
            get { return addUserRightCommand; }
        }

        private bool CanAddUserRight(int _access)
        {
            return curAdminMode == AdminMode.Permissions 
                && selectedUser != null 
                //&& !selectedUser.IsSystem 
                && selectedComponent != null 
                && userRights != null 
                && !userRights.Any(r => r.ComponentTypeName == selectedComponent);
        }

        private void ExecAddUserRight(int _access)
        {
            userRights.Add(new ComponentUserRight { AccessLevel = _access, ComponentTypeName = selectedComponent, UserId = selectedUser.Id });
            userRightsChanged = true;
        }

        private ICommand deleteRightCommand;
        public ICommand DeleteRightCommand
        {
            get { return deleteRightCommand; }
        }
        
        private bool CanDeleteRight(ComponentUserRight _r)
        {
            return _r != null;
        }

        private void ExecDeleteRight(ComponentUserRight _r)
        {
            if ((_r.UserId ?? 0) == 0)
            {
                defaultRights.Remove(_r);
                defaultRightsChanged = true;
            }
            else
            {
                userRights.Remove(_r);
                userRightsChanged = true;
            }
        }

        private ICommand delDopInfoCommand;
        public ICommand DelDopInfoCommand
        {
            get { return delDopInfoCommand; }
        }

        private bool CanDelDopInfo()
        {
            return createdClientInfoElement != null && curMaintMode > 0;
        }

        private void ExecDelDopInfo()
        {
            List<UserInfoViewModel> selUsers = GetUsersForMaintaince();
            if (selUsers != null && selUsers.Count > 0)
            {
                Action work = () =>
                {
                    selUsers.ForEach(DelDopInfo);
                    if (!isNewSelected && selectedUser != null && selUsers.Contains(selectedUser))
                        MakeSelectedCopy();
                };
                Parent.Services.DoWaitAction(work); 
            } 
        }

        private void DelDopInfo(UserInfoViewModel _u)
        {
            if (_u == null || createdClientInfoElement == null) return;
            var mod = _u.GetModel();
            if (mod.ClientInfo == null || mod.ClientInfo.Name != WorkFlowHelper.CI_CONTAINER) return;

            var el2Del = mod.ClientInfo.Elements(createdClientInfoElement.Name).Where(e => e.Value == createdClientInfoElement.Value).ToArray();
            Array.ForEach(el2Del, e => e.Remove());
            dbService.UpdateUserInfo(_u.Id, mod);
            var nu = dbService.GetUserInfo(_u.Id);
            if (nu != null)
                _u.ParseUserData(nu);
        }

        private ICommand addDopInfoCommand;
        public ICommand AddDopInfoCommand
        {
            get { return addDopInfoCommand; }
        }

        private bool CanAddDopInfo()
        {
            return createdClientInfoElement != null && curMaintMode > 0;
        }

        private void ExecAddDopInfo()
        { 
            List<UserInfoViewModel> selUsers = GetUsersForMaintaince();
            if (selUsers != null && selUsers.Count > 0)
            {
                Action work = () =>
                {
                    selUsers.ForEach(AddDopInfo);
                    if (!isNewSelected && selectedUser != null && selUsers.Contains(selectedUser))
                        MakeSelectedCopy();
                };
                Parent.Services.DoWaitAction(work); 
            }       
        }

        private void AddDopInfo(UserInfoViewModel _u)
        {
            if (_u == null || createdClientInfoElement == null) return;
            var mod = _u.GetModel();
            var nci = mod.ClientInfo == null ? new XElement(WorkFlowHelper.CI_CONTAINER)
                                             : (mod.ClientInfo.Name != WorkFlowHelper.CI_CONTAINER ? new XElement(WorkFlowHelper.CI_CONTAINER, mod.ClientInfo) 
                                                                                                   : mod.ClientInfo);
            nci.Add(createdClientInfoElement);            
            mod.ClientInfo = NormalizeClientInfo(nci);
            dbService.UpdateUserInfo(_u.Id, mod);
            var nu = dbService.GetUserInfo(_u.Id);
            if (nu != null)
                _u.ParseUserData(nu);
        }

        private ICommand normaliseUsersCommand;
        public ICommand NormaliseUsersCommand
        {
            get { return normaliseUsersCommand; }
        }
        
        private bool CanNormaliseUsers()
        {
            return curMaintMode > 0;
        }

        private void ExecNormaliseUsers()
        {
            List<UserInfoViewModel> selUsers = GetUsersForMaintaince();
            if (selUsers != null && selUsers.Count > 0)
            {
                Action work = () =>
                {
                    selUsers.ForEach(NormaliseUser);
                    if (!isNewSelected && selectedUser != null && selUsers.Contains(selectedUser))
                        MakeSelectedCopy();
                };
                Parent.Services.DoWaitAction(work);
            }
            //var nCi = NormalizeClientInfo(selectedCopy.GetEditedUserInfo().ClientInfo).ToString();
        }

        private void NormaliseUser(UserInfoViewModel _u)
        {
            if (_u == null) return;
            var mod = _u.GetModel();
            if (mod.ClientInfo == null) return;
            var nci = NormalizeClientInfo(mod.ClientInfo);
            mod.ClientInfo = nci;
            dbService.UpdateUserInfo(_u.Id, mod);
            var nu = dbService.GetUserInfo(_u.Id);
            if (nu != null)
                _u.ParseUserData(nu);
        }

        private List<UserInfoViewModel> GetUsersForMaintaince()
        {
            List<UserInfoViewModel> res = null;
            switch(curMaintMode)
            {
                case UserSelectMode.Current:
                    if (selectedUser != null)
                    {
                        res = new List<UserInfoViewModel>();
                        res.Add(selectedUser);
                    }
                    break;
                case UserSelectMode.All: res = users.ToList();
                    break;
                case UserSelectMode.Filtered:
                    var view = CollectionViewSource.GetDefaultView(users) as ListCollectionView;
                    if (view != null)
                        res = view.OfType<UserInfoViewModel>().ToList();
                    break;
                default: break;
            }
               
            return res;
        }                

        private ICommand selectUserByIdCommand;
        public ICommand SelectUserByIdCommand
        {
            get { return selectUserByIdCommand; }
        }

        private string messageToUser;
        public string MessageToUser
        {
            get { return messageToUser; }
            set { SetAndNotifyProperty(() => MessageToUser, ref messageToUser, value); }
        }
        
        private string sentMessages = "";
        public string SentMessages
        {
            get { return sentMessages; }
            set { SetAndNotifyProperty(() => SentMessages, ref sentMessages, value); }
        }        

        private ICommand sendMessageCommand;
        public ICommand SendMessageCommand
        {
            get { return sendMessageCommand; }
        }

        private bool CanSendMessage(bool _exit)
        {
            return (curMaintMode == UserSelectMode.Filtered || curMaintMode == UserSelectMode.All
                   || curMaintMode == UserSelectMode.Current && selectedUser != null && selectedUser.CommServiceStatus == 3) 
                && !String.IsNullOrWhiteSpace(messageToUser);
        }

        interface IRepkaChannel : IRepkaService, IClientChannel { }

        private void ExecSendMessage(bool _exit)
        {            
            try
            {
                SendMessageToUsers(_exit);
                MessageToUser = null;
            }
            catch (Exception e)
            {
                CommonModule.Helpers.WorkFlowHelper.OnCrash(e);
            }
        }

        private void SendMessageToUsers(bool _exit)
        {
            if (String.IsNullOrWhiteSpace(messageToUser)) return;
            
            var messToSend = messageToUser;

            if (curMaintMode == UserSelectMode.Current && selectedUser != null)
                SendMessageToUserAction(selectedUser, messToSend, _exit);
            else
                if (curMaintMode == UserSelectMode.Filtered || curMaintMode == UserSelectMode.All)
                {
                    var fusers = GetUsersForMaintaince();
                    if (fusers != null && fusers.Count > 0)
                        Task.Factory.StartNew(() =>
                        {
                            foreach (var ou in fusers.Where(u => u.IsOnline && u.CommServiceStatus == 3))
                                SendMessageToUserAction(ou, messToSend, ou.IsCurrentUser ? false : _exit);
                        });
                }
        }        

        private void SendMessageToUserAction(UserInfoViewModel _u, string _message, bool _exit)
        {
            if (_u == null || String.IsNullOrWhiteSpace(_message)) throw new ArgumentNullException();

            var address = new System.ServiceModel.EndpointAddress(new Uri(_u.CommServiceUrl), EndpointIdentity.CreateUpnIdentity(_u.Login));
            var bind = new NetTcpBinding();
            var factory = new ChannelFactory<IRepkaChannel>(bind, address);
            IRepkaChannel repkaService = factory.CreateChannel();
            repkaService.SendMessage(_message, _exit);
            repkaService.Close();
            factory.Close();
            ShowSentMessage(_u, _message);
        }

        private void ExecSelectUserById(int _id)
        {
            if (users == null || selectedUser != null && selectedUser.Id == _id) return;

            var user = users.FirstOrDefault(u => u.Id == _id);
            if (user != null)
                SelectedUser = user;
        }

        private XElement NormalizeClientInfo(XElement _ci)
        {            
            if (_ci == null) return null;
            XElement res = _ci.Name == WorkFlowHelper.CI_CONTAINER ? _ci : new XElement(WorkFlowHelper.CI_CONTAINER);

            var oldExecs = res.Elements(WorkFlowHelper.CI_EXECUTE).ToArray();
            var newExecs = new List<XElement>();
            foreach (var oe in oldExecs)
            {
                XElement one = null;
                if (!String.IsNullOrWhiteSpace(oe.Value))
                {
                    var value = oe.Value.Trim().ToUpperInvariant();
                    one = newExecs.FirstOrDefault(e => e.Value.Trim().ToUpperInvariant() == value);
                }
                else
                {
                    var urlAttr = oe.Attribute("Url");
                    if (urlAttr != null && !String.IsNullOrWhiteSpace(urlAttr.Value))
                    {
                        var url = urlAttr.Value.Trim().ToUpperInvariant();
                        one = newExecs.FirstOrDefault(e => e.Attribute("Url") != null && !String.IsNullOrWhiteSpace(e.Attribute("Url").Value) && e.Attribute("Url").Value.Trim().ToUpperInvariant() == url);
                    }
                }                
                
                if (one != null)
                {
                    one.Remove();
                    newExecs.Remove(one);
                }
                else
                    newExecs.Add(oe);
            }

            return res;
        }

        private string userFilterString;
        public string UserFilterString
        {
            get { return userFilterString; }
            set 
            {
                if (SetAndNotifyProperty(() => UserFilterString, ref userFilterString, value)
                    && (String.IsNullOrWhiteSpace(userFilterString) || userFilterString[userFilterString.Length - 1] != ','))
                    RefreshUserFilter();
            }
        }

        private void RefreshUserFilter()
        {
            var view = CollectionViewSource.GetDefaultView(users);
            view.Filter = null;
            HashSet<int> userids = null;
            HashSet<string> names = null;
            string stringToParse = null;
            if (!String.IsNullOrWhiteSpace(userFilterString) || filterOnlineMode != OnlineMode.All || filterEnabledMode != EnabledMode.All)                
            {
                if (!String.IsNullOrWhiteSpace(userFilterString))
                {
                    stringToParse = userFilterString.Trim().ToUpperInvariant();
                    if (stringToParse[stringToParse.Length - 1] == ',')
                        stringToParse = stringToParse.Remove(stringToParse.Length - 1);
                    if (!String.IsNullOrWhiteSpace(stringToParse) && stringToParse.Contains(','))
                    {
                        var strings = stringToParse.Split(',').Where(s => !String.IsNullOrWhiteSpace(s)).ToArray();
                        for (int i = 0; i < strings.Length; i++)
                        {
                            var curitem = strings[i];
                            if (curitem.All(c => Char.IsDigit(c)))
                            {
                                int newid = 0;
                                if (int.TryParse(curitem, out newid) && newid > 0)
                                {
                                    if (userids == null) userids = new HashSet<int>();
                                    userids.Add(newid);
                                }
                            }
                            else
                            {
                                if (names == null) names = new HashSet<string>();
                                names.Add(curitem.Trim().ToUpperInvariant());
                            }
                        }
                    }
                }
                
                view.Filter = u =>
                {
                    var usr = u as UserInfoViewModel;
                    int id = 0;
                    return
                        (filterOnlineMode == OnlineMode.All || filterOnlineMode == OnlineMode.Online && usr.IsOnline || filterOnlineMode == OnlineMode.Offline && !usr.IsOnline)
                        && (filterEnabledMode == EnabledMode.All || filterEnabledMode == EnabledMode.Enabled && usr.IsEnabled || filterEnabledMode == EnabledMode.Disabled && !usr.IsEnabled)
                        &&
                        (String.IsNullOrWhiteSpace(stringToParse) 
                        || userids != null && userids.Contains(usr.Id)
                        || names != null && names.Any(s => usr.FullName.Trim().ToUpperInvariant().Contains(s))
                        || int.TryParse(stringToParse, out id) && usr.Id == id
                        || usr.FullName.Trim().ToUpperInvariant().Contains(stringToParse)
                        );
                };
            }            
            view.Refresh();
        }

        private EnabledMode filterEnabledMode;
        public EnabledMode FilterEnabledMode
        {
            get { return filterEnabledMode; }
            set 
            { 
                if (SetAndNotifyProperty(() => FilterEnabledMode, ref filterEnabledMode, value))
                    RefreshUserFilter(); 
            }
        }

        private OnlineMode filterOnlineMode;
        public OnlineMode FilterOnlineMode
        {
            get { return filterOnlineMode; }
            set 
            { 
                if (SetAndNotifyProperty(() => FilterOnlineMode, ref filterOnlineMode, value))
                    RefreshUserFilter(); 
            }
        }

        private UserSelectMode curMaintMode;
        public UserSelectMode CurMaintMode
        {
            get { return curMaintMode; }
            set { SetAndNotifyProperty(()=>CurMaintMode, ref curMaintMode, value); }
        }        

        public IEnumerable<SecurityContextInfo> AllContexts { get { return Enumerable.Repeat(new SecurityContextInfo { Id = null, FullName = "Нет контекста" }, 1).Concat(users.Where(u => u.IsSystem && u.IsEnabled).Select(u => new SecurityContextInfo { Id = u.Id, FullName = u.FullName })); } }

        private CiElements ciCreateMode;
        public CiElements CiCreateMode
        {
            get { return ciCreateMode; }
            set 
            { 
                SetAndNotifyProperty(() => CiCreateMode, ref ciCreateMode, value);
                CiConstructor = new CiElementConstructor(value);
            }
        }

        private XElement createdClientInfoElement;
        public XElement CreatedClientInfoElement
        {
            get { return createdClientInfoElement; }
            set { SetAndNotifyProperty(() => CreatedClientInfoElement, ref createdClientInfoElement, value); }
        }

        private CiElementConstructor ciConstructor;
        public CiElementConstructor CiConstructor
        {
            get { return ciConstructor; }
            set { SetAndNotifyProperty(() => CiConstructor, ref ciConstructor, value); }
        }

        private ICommand createCiElementCommand;
        public ICommand CreateCiElementCommand
        {
            get { return createCiElementCommand; }
            set { createCiElementCommand = value; }
        }

        private bool CanCreateCiElement()
        {
            return ciConstructor != null;
        }

        private void ExecCreateCiElement()
        {
            CreatedClientInfoElement = ciConstructor.CreateCiElement();
        }

        private string[] availableComponents;
        public string[] AvailableComponents
        {
            get { return availableComponents; }
            set { SetAndNotifyProperty(() => AvailableComponents, ref availableComponents, value); }
        }

        private string selectedComponent;
        public string SelectedComponent
        {
            get { return selectedComponent; }
            set { SetAndNotifyProperty(() => SelectedComponent, ref selectedComponent, value); }
        }

        private void CollectAvailableComponents()
        {
            var ar = new List<string>() { "CommonModule" };
            var modules = Parent.ShellModel.Container.GetExportedValues<IModule>().Select(m => m.Info.Name.ToString());
            using (var db = new ServiceContext())
                foreach (var mn in modules)
                {
                    ar.Add(mn);
                    var modulecommands = Parent.ShellModel.Container.GetExportedValues<ModuleCommand>(mn + ".ModuleCommand").Select(m => m.GetType().ToString());
                    if (modulecommands != null)
                        ar.AddRange(modulecommands);
                    var modulereports = db.GetNamedReports(mn);
                    if (modulereports != null)
                        ar.AddRange(modulereports.Select(r => "Reports." + r));

                    var modulecompcmds = Parent.ShellModel.Container.GetExports<ICommand, IComponentNameMetaData>(mn + ".ComponentCommand").Select(e => e.Metadata.ComponentName);
                    if (modulecompcmds != null)
                        ar.AddRange(modulecompcmds);                    
                }            

            AvailableComponents = ar.ToArray();
        }

        private ComponentUserRight[] effectiveRights;
        public ComponentUserRight[] EffectiveRights
        {
            get { return effectiveRights; }
            set { SetAndNotifyProperty(()=>EffectiveRights, ref effectiveRights, value); }
        }

        private ComponentUserRight[] contextRights;
        public ComponentUserRight[] ContextRights
        {
            get { return contextRights; }
            set { SetAndNotifyProperty(() => ContextRights, ref contextRights, value); }
        }
        
        private ObservableCollection<ComponentUserRight> userRights;
        public ObservableCollection<ComponentUserRight> UserRights
        {
            get { return userRights; }
            set { SetAndNotifyProperty(() => UserRights, ref userRights, value); }
        }

        private ObservableCollection<ComponentUserRight> defaultRights;
        public ObservableCollection<ComponentUserRight> DefaultRights
        {
            get { return defaultRights; }
            set { SetAndNotifyProperty(() => DefaultRights, ref defaultRights, value); }
        }

        private void LoadUserRights(UserInfoViewModel _user)
        {
            if (_user == null)
            {
                EffectiveRights = ContextRights = null;
                UserRights = null;
                return;
            }

            if (availableComponents == null)
                CollectAvailableComponents();

            using (var sdb = new ServiceContext())
            {
                EffectiveRights = sdb.GetUserEffectiveRights(_user.Id);
                ContextRights = _user.Context > 0 ? sdb.GetUserComponentsRights(_user.Context.Value) : null;
                UserRights = new ObservableCollection<ComponentUserRight>(sdb.GetUserComponentsRights(_user.Id).OrderBy(c => c.ComponentTypeName));
                userRightsChanged = false;
                if (defaultRights == null || defaultRightsChanged)
                {
                    DefaultRights = new ObservableCollection<ComponentUserRight>(sdb.GetUserComponentsRights(0).OrderBy(c => c.ComponentTypeName));
                    defaultRightsChanged = false;
                }
            }
        }

        private bool defaultRightsChanged;
        private bool userRightsChanged;

        public enum AdminMode {UserDetails = 0, Maintaince, Service, Permissions}

        private AdminMode curAdminMode = AdminMode.UserDetails;
        public AdminMode CurAdminMode
        {
            get { return curAdminMode; }
            set 
            {
                if (SetAndNotifyProperty(() => CurAdminMode, ref curAdminMode, value))
                {
                    switch (curAdminMode)
                    { 
                        case AdminMode.Permissions:
                            Parent.Services.DoWaitAction(() => { LoadUserRights(selectedUser); });
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private void ShowSentMessage(UserInfoViewModel _u, string _mess)
        {
            SentMessages = String.Format("[{0:dd.MM.yy hh:mm:ss}] => {1} ({2})", DateTime.Now, _u.FullName, _u.Login)
                + Environment.NewLine + _mess
                + Environment.NewLine
                + Environment.NewLine + sentMessages ?? "";
        }

        private System.Timers.Timer servTimer;
        public override void Dispose()
        {
            base.Dispose();
            DisposeServTimer();
            DisposeAnnounceListener();
            DisposeDiscovery();
        }
    }
}
