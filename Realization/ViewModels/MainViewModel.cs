using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CommonModule.Commands;
using CommonModule.Composition;
using CommonModule.Interfaces;
using CommonModule.ViewModels;
using DataObjects.Interfaces;
using DataObjects;
using Realization.Properties;
using System.Collections.Generic;
using System.IO;
using CommonModule.Helpers;
using System.Text.RegularExpressions;
using System.Globalization;



namespace Realization.ViewModels
{
    [Export(typeof(IShellModel))]
    public class MainViewModel : BasicViewModel, IPartImportsSatisfiedNotification, IShellModel
    {
        [Import]
        private CompositionContainer container;
        public CompositionContainer Container { get { return container; } }

        public MainViewModel()
        {
            Title = Properties.Settings.Default.Name;
            currentUserInfo = Repository.GetUserInfo(Repository.UserToken);
            Repository.OnError += new EventHandler<DataObjects.Events.ErrorEventArgs>(Repository_OnError);
        }

        void Repository_OnError(object sender, DataObjects.Events.ErrorEventArgs e)
        {
            WorkFlowHelper.WriteToLog(null, String.Format("{0} : {1}", e.Title, e.Message));
            if (!e.IsSilent)
                ShowError(e.Title, e.Message, e.OnSubmit);
        }

        private void ShowError(string _title, string _message, Action _onSubmit)
        {
            var oldDialog = Dialog;
            var msgDlg = new MsgDlgViewModel 
            { 
                Title = _title,
                Message = _message,
                BgColor = "Crimson",
                IsCancelable = _onSubmit != null,
                OnSubmit = (d) => 
                {
                    if (_onSubmit != null)
                        _onSubmit();
                    Dialog = oldDialog; 
                }

            };
            
            Dialog = new CommonModule.Helpers.DialogContainer(msgDlg);
        }

        public string Version
        {
            get
            {
                return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        public bool IsShowCommandLabels
        {
            get 
            {
                return CommonModule.CommonSettings.IsShowCommandLabels; ;
            }
            set 
            {
                if (value != CommonModule.CommonSettings.IsShowCommandLabels)
                {
                    CommonModule.CommonSettings.IsShowCommandLabels = value;
                    NotifyPropertyChanged("IsShowCommandLabels");
                }
            }
        }

        /// <summary>
        /// Комманда закрытия модуля
        /// </summary>
        public ICommand closeWorkSpaceCmd;
        public ICommand CloseWorkSpaceCmd
        {
            get
            {
                if (closeWorkSpaceCmd == null)
                    closeWorkSpaceCmd = new DelegateCommand(CloseWorkSpace,CanCloseWorkSpace);
                return closeWorkSpaceCmd;
            }
        }
        private void CloseWorkSpace()
        {
            if (CanCloseWorkSpace())
                WorkSpace.StopModule.Execute(null);
            UnLoadModule();
        }
        private bool CanCloseWorkSpace()
        {
            return WorkSpace!=null && WorkSpace.StopModule != null && WorkSpace.StopModule.CanExecute(null);
        }

        public ICommand showStartCommand;
        public ICommand ShowStartCommand
        {
            get
            {
                if (showStartCommand == null)
                    showStartCommand = new DelegateCommand(()=>WorkSpace=null);
                return showStartCommand;
            }
        }

        /// <summary>
        /// Загружаемые модули
        /// </summary>
        [ImportMany]
        private Lazy<IModule, IDisplayOrderMetaData>[] modules;

        #region IPartImportsSatisfiedNotification Members

        public void OnImportsSatisfied()
        {
            // сортировка модулей на основе метаданных
            Modules = modules.OrderBy(lm => lm.Metadata.DisplayOrder).Select(lm => lm.Value).ToArray();            
            //UpgradeUserSettingsIfRequired(); 
            DoAppServices();
        }

        private List<IAppService> appServices;
        
        private void DoAppServices()
        {
            var aServices = Container.GetExports<IAppService>().ToDictionary(se => se.Value, se => se);            
            if (aServices.Any())
            {
                System.Threading.Tasks.Task.Factory.StartNew(
                    () => 
                    {                        
                        foreach(var skv in aServices)
                        {
                            var serv = skv.Key;
                            if (serv != null)
                                serv.Start();
                            if (serv == null || !serv.IsStayInMemory)
                                Container.ReleaseExport(skv.Value);
                            else
                            {
                                if (appServices == null) appServices = new List<IAppService>();
                                appServices.Add(serv);
                            }
                        }
                    }
                );
            }
        }

        public void CloseServices()
        {
            if (appServices != null && appServices.Count > 0)
                appServices.ForEach(s => s.Stop());
        }

        /// <summary>
        /// переносит пользовательские настройки модулей из прошлой версии
        /// </summary>
        //private void UpgradeUserSettingsIfRequired()
        //{
        //    if (Settings.Default.UpgradeRequired)
        //    {
        //        Settings.Default.Upgrade();
        //        Settings.Default.UpgradeRequired = false;
        //        Settings.Default.Save();
        //        for (int i = 0; i < Modules.Length; i++)
        //        {
        //            var msettings = Modules[i].ModuleSettings;
        //            if (msettings != null)
        //            {
        //                msettings.Upgrade();
        //                msettings.Save();
        //            }
        //        }
        //    }
        //}

        #endregion

        /// <summary>
        /// Возврат к предыдущему модулю
        /// </summary>
        private ICommand goBackCommand;
        public ICommand GoBackCommand
        {
            get
            {
                if (goBackCommand == null)
                    goBackCommand = new DelegateCommand(() => 
                        LoadModule(PreviousModule));
                return goBackCommand;
            }
        }

        private UserInfo currentUserInfo;
        public UserInfo CurrentUserInfo
        {
            get { return currentUserInfo; }
        }

        public string UserName
        {
            get
            { 
                return CurrentUserInfo.Title;
            }
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
            System.Threading.Tasks.Task.Factory.StartNew(() => UiExt = Repository.GetUserInfoExt(Repository.UserToken));
        }

        /// <summary>
        /// Проверка соединения с БД
        /// </summary>
        public void CheckConnection()
        {
            var oldDialog = Dialog;
            Dialog = new WaitDlgViewModel()
            {
                Title = "Подождите",
                Message = "Проверка соединения с сервером базы данных..."
            };
            
            IsOnline = Repository.CheckOnlineStatus();
            if (IsOnline)
            {
                if (Repository.UserToken == 0)
                    Exit(null, "Доступ к АРМу не разрешён!", null, null);
                else
                        Dialog = oldDialog; // проверка соединения прошла успешно
            }
            else
                Exit(null, "Не удалось соединиться с базой данных!", null, null);
        }
          
        public void ReadMessages()
        {
            ReadCiMessages();
            ReadFileMessages();
        }

        private void ReadCiMessages()
        {
            if (currentUserInfo == null) return;
            var oldDialog = Dialog;
            
            var ci_xMessages = currentUserInfo.ClientInfo.Elements(WorkFlowHelper.CI_MESSAGE);
            var myMsgs = ci_xMessages.Select(x => x.Value).ToArray();

            MsgDlgViewModel xMsgDialog = null;
            if (myMsgs.Length > 0)
            {
                xMsgDialog = new MsgDlgViewModel()
                {
                    Title = "Новое сообщение",
                    Message = String.Join("\n\n---\n\n", myMsgs),
                    MessageType = myMsgs.Length == 1 ? MsgType.Message : MsgType.Text,
                    CloseCommand = new DelegateCommand(() => Dialog = oldDialog),
                    SubmitCommand = new LabelCommand(() =>
                    {
                        foreach (var xm in ci_xMessages)
                            xm.Remove();
                        Repository.UpdateUserInfo(currentUserInfo.Id, currentUserInfo);
                        Dialog = oldDialog;
                    }) { Label = "Прочтено" }
                };
                Dialog = new DialogContainer(xMsgDialog);
            }
        }

        private void ReadFileMessages()
        {
            var oldDialog = Dialog;            

            if (!Directory.Exists("Messages")) return;

            var fMsgDialog = new MsgDlgViewModel()
            {
                Title = "Новое сообщение",
                CloseCommand = new DelegateCommand(() => Dialog = oldDialog)
            };
            
            var supImages = new string[] { ".JPG", ".BMP", ".PNG", ".GIF" };

            var dtNow = DateTime.Now;
            var regExpr = new Regex(@"\[(ds|de)\=\d{12}\]");            
            var msgsInfos = new DirectoryInfo("Messages").EnumerateFiles().Where(fi => (fi.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden).OrderBy(fi => fi.LastWriteTime)
                .Select(fi => new
                {
                    Fileinfo = fi,
                    MessageType = supImages.Contains(fi.Extension.ToUpperInvariant()) ? MsgType.ImagePath : MsgType.Text,
                    FileNameAttr = regExpr.Matches(fi.Name).OfType<Match>().Select(m => m.Value)
                                      .ToDictionary(v => v.Substring(1, v.IndexOf('=') - 1), v => DateTime.ParseExact(v.Substring(v.IndexOf('=') + 1), "ddMMyyHHmmss]", CultureInfo.InvariantCulture))
                }).Where(t => (!t.FileNameAttr.ContainsKey("ds") || t.FileNameAttr["ds"] <= dtNow) && (!t.FileNameAttr.ContainsKey("de") || t.FileNameAttr["de"] > dtNow))
                .ToList();

            if (msgsInfos.Count == 0) return;

            Action work = () =>
            {
                if (msgsInfos.Count == 0)
                {
                    Dialog = oldDialog;
                    return;
                }
                var mFile = msgsInfos[0];                

                if (mFile.MessageType == MsgType.ImagePath)
                {
                    fMsgDialog.MessageType = MsgType.ImagePath;
                    fMsgDialog.Message = mFile.Fileinfo.FullName;
                }
                else
                {
                    
                    using (var sr = new StreamReader(mFile.Fileinfo.FullName))
                        fMsgDialog.Message = sr.ReadToEnd();
                    
                    fMsgDialog.MessageType = fMsgDialog.Message.Length > 500 ? MsgType.Text : MsgType.Message;
                } 
            };

            work();

            fMsgDialog.SubmitCommand = new LabelCommand(() =>
            {
                msgsInfos[0].Fileinfo.Attributes = msgsInfos[0].Fileinfo.Attributes | FileAttributes.Hidden;
                msgsInfos.RemoveAt(0);
                work();
            }) { Label = "Прочтено" };

            Dialog = new DialogContainer(fMsgDialog);
        }

        public void SendMessage(string _title, string _message, bool _iserr, Action<object> _onSubmit, Action<object> _onClose)
        {
            var wasonline = IsOnline;            
            Action<object> closedlg = o => { Dialog = null; IsOnline = wasonline; };
            Action<object> onSubmit = closedlg;
            if (_onSubmit != null) 
                onSubmit = (o) =>
                {
                    closedlg(null);
                    _onSubmit(null);
                };

            ICommand closeCmd = null;
            Action<object> onClose = null;
            if (_onClose != null)
            {
                onClose = (o) =>
                {
                    closedlg(null);
                    _onClose(null);
                };
                closeCmd = new DelegateCommand(() => onClose(null));
            }
            var dlg = new MsgDlgViewModel()
            {
                Title = _title,
                Message = _message,
                CloseCommand = closeCmd,
                OnCancel = onClose,
                OnSubmit = onSubmit
            };
            if (_iserr) dlg.BgColor = "Crimson";
            var container = new CommonModule.Helpers.DialogContainer(dlg);
            IsOnline = false;
            Dialog = container;
        }

        /// <summary>
        /// Выходит из приложения с выводом сообщения
        /// </summary>
        /// <param name="_mes"></param>
        public void Exit(string _tit, string _mes, Action<object> _onSubmit, Action<object> _onClose)
        {            
            Action<object> onSubmit = o => Application.Current.Shutdown();            
            SendMessage(_tit ?? "Внимание", _mes, true, _onSubmit ?? onSubmit, _onClose);
        }

        public IEnumerable<IModule> OpenedModules
        {
            get
            {
                return GetOpenedModules();
            }
        }

        private IEnumerable<IModule> GetOpenedModules()
        {
            IEnumerable<IModule> res = Enumerable.Empty<IModule>();

            if (Modules != null && Modules.Length > 0)
                res = Modules.Where(m => m.AccessLevel > 0 && m != workSpace 
                    && (m.IsContentLoaded || m.Dialog.Count > 0 || m == previousModule));
            return res;
        }


        #region IModuleLoader Members

        public IModule[] Modules { get; set; }

        private IModule previousModule;
        public IModule PreviousModule
        {
            get { return previousModule; }
        }

        public void LoadModule(IModule _content)
        {
            if (_content != null)
            {
                previousModule = WorkSpace as IModule;
                UpdateUi(() => WorkSpace = _content, false, false);
            }
        }
        
        public void UnLoadModule()
        {
            UpdateUi(() => WorkSpace = null, false, false);
        }

        #endregion

        private bool isOnline;
        public bool IsOnline
        {
            get 
            {
                return isOnline;
            }
            set 
            {
                if (value != isOnline)
                {
                    isOnline = value;
                    NotifyPropertyChanged("IsOnline");
                }
            }
        }

        /// <summary>
        /// Представление диалогового окна
        /// </summary>
        private Object dialog;
        public Object Dialog
        {
            get
            {
                return dialog;
            }
            set
            {
                if (value != dialog)
                {
                    dialog = value;
                    NotifyPropertyChanged("Dialog");
                    UpdateUi(ShowMainWindow, true, false);
                }
            }
        }

        private void ShowMainWindow()
        {
            var wnd = Application.Current.MainWindow;
            if (wnd.WindowState == WindowState.Minimized)
                wnd.WindowState = WindowState.Maximized;
            if (!wnd.Topmost)
                wnd.Activate();
        }

        [Import]
        private Dispatcher uiDispatcher;

        /// <summary>
        /// Рабочая область оболочки (загруженный модуль)
        /// </summary>
        private IModule workSpace;
        public IModule WorkSpace
        {
            get
            {
                return workSpace;
            }
            set
            {
                if (value != workSpace)
                {
                    workSpace = value;
                    NotifyPropertyChanged("WorkSpace");
                    NotifyPropertyChanged("OpenedModules");
                }
            }
        }

        /// <summary>
        /// Обновление UI через диспетчер
        /// </summary>
        /// <param name="_update"></param>
        public void UpdateUi(Action _update, bool _async, bool _forceDisp)
        {
            if (_update == null) return;

            if (uiDispatcher.Thread != System.Threading.Thread.CurrentThread || _forceDisp)
                if (_async)
                    uiDispatcher.BeginInvoke(_update, DispatcherPriority.Background);
                else
                    uiDispatcher.Invoke(_update);
            else
                _update();
        }

        public IDbService Repository
        {
            get { return CommonModule.CommonSettings.Repository; }
        }

        private SysInfoViewModel sysInfo;
        public SysInfoViewModel SysInfo
        {
            get
            {
                if (sysInfo == null)
                    sysInfo = new SysInfoViewModel(Repository);
                return sysInfo;
            }
        }

        // Скриншоты
        private ICommand makeScreenshotCmd;
        public ICommand MakeScreenshotCmd
        {
            get
            {
                if (makeScreenshotCmd == null)
                    makeScreenshotCmd = new DelegateCommand(ExecuteMakeScreenshotCmd);
                return makeScreenshotCmd;
            }
        }

        private void ExecuteMakeScreenshotCmd()
        {
            Action work = () =>
            {
                System.Threading.Thread.Sleep(500);
                CaptureAndSendScreenshot();
            };
            System.Threading.Tasks.Task.Factory.StartNew(work);
        }

        private void CaptureAndSendScreenshot()
        {
            var ssPath = CommonModule.CommonSettings.ScreenshotsPath;
            if (!Path.IsPathRooted(ssPath))
                ssPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ssPath);

            bool res = false;
            try
            {
                if (File.Exists(ssPath))
                    ssPath = FileSystemHelper.FindNextFileName(ssPath);
                else
                {
                    var dir = Path.GetDirectoryName(ssPath);
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                }
                res = ScreenshotHelper.ScreenShoter.CaptureToFile(ssPath);
            }
            catch (Exception _e)
            {
                WorkFlowHelper.OnCrash(_e);
            }

            string resMsg = (!res) ? "Ошибка получения снимка экрана!" : "Снимок экрана сохранён в файле: " + ssPath;
            WorkFlowHelper.WriteToLog(null, resMsg);
            
            var oldDialog = Dialog;            

            if (!res) 
                ShowError("Ошибка", resMsg, null);
            else
            {
                var resDlg = new BaseCompositeDlgViewModel()
                {
                    Title = "Результат получения снимка экрана",
                    OnSubmit = (d) => DoResult(d, oldDialog)
                };

                resDlg.Add(new MsgDlgViewModel { Message = resMsg });

                var aUsers = Repository.GetAllUserInfos();
                if (aUsers != null && aUsers.Length > 0)
                {
                    var eUsers = aUsers.Where(u => //u.Id != Parent.Repository.UserToken && 
                                                   u.IsEnabled && !String.IsNullOrEmpty(u.EmailAddress));
                    if (eUsers.Any())
                    {
                        var NotSendChoice = new Choice { Header = "Не отправлять", IsChecked = true, IsSingleInGroup = true, GroupName = "SendTo" };
                        var chList = eUsers.Select(u => new Choice { Header = String.Format("{0} ({1})", u.FullName, u.EmailAddress), IsChecked = false, IsSingleInGroup = true, GroupName = "SendTo", Item = new string[] { u.EmailAddress, ssPath } }).ToList();
                        chList.Insert(0, NotSendChoice);
                        var sendToDlgComp = new ChoicesDlgViewModel(chList.ToArray()) { Title = "Отправить снимок по почте" };
                        resDlg.Add(sendToDlgComp);
                    }
                }

                Dialog = new CommonModule.Helpers.DialogContainer(resDlg);
            }

        }

        private void DoResult(Object _d, Object _oldDlg)
        {
            var dlg = _d as BaseCompositeDlgViewModel;
            Dialog = _oldDlg;

            if (dlg == null) return;
            if (dlg.DialogViewModels.Length > 1)
            {
                var sendToDlgComp = dlg.DialogViewModels[1] as ChoicesDlgViewModel;
                if (sendToDlgComp == null) return;

                var selChoice = sendToDlgComp.Groups.First().Value.Where(ch => ch.IsChecked ?? false).FirstOrDefault();
                if (selChoice != null && selChoice.Item != null)
                {
                    var emailInfo = selChoice.Item as string[];
                    if (emailInfo == null || emailInfo.Length < 2 || String.IsNullOrEmpty(emailInfo[0]) || String.IsNullOrEmpty(emailInfo[1])) return;
                    DoSendScreenshotTo(emailInfo[0], emailInfo[1], _oldDlg);
                }
            }
        }

        private void DoSendScreenshotTo(string _to, string _ssFilePath, Object _oldDlg)
        {
            string resMsg;
            var msgDlg = new MsgDlgViewModel() { OnSubmit = (d) => Dialog = _oldDlg };

            if (String.IsNullOrEmpty(_ssFilePath) || !File.Exists(_ssFilePath))
            {
                resMsg = "Файл \"" + _ssFilePath + "\" не найден.";
                msgDlg.Title = "Ошибка";
                msgDlg.BgColor = "Crimson";
                msgDlg.Message = resMsg;
            }
            else
            {
                var curUser = Repository.GetUserInfo(Repository.UserToken);

                MAPIHelper.MAPI mapi = new MAPIHelper.MAPI();
                mapi.AddAttachment(_ssFilePath);

                var logPath = CommonModule.CommonSettings.LogPath;
                if (String.IsNullOrEmpty(logPath) || !File.Exists(logPath))
                    Array.ForEach(Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.log"), f => mapi.AddAttachment(f));
                else
                    mapi.AddAttachment(logPath);

                mapi.AddRecipientTo(_to);

                //int sendRes = mapi.SendMailPopup("Снимок экрана пользователя " + curUser.FullName, _ssFilePath);
                int sendRes = mapi.SendMailPopup("Снимок экрана пользователя " + curUser.FullName, _ssFilePath + "\n" + logPath);
                if (sendRes != 0)
                {
                    resMsg = "Ошибка MAPISendMail: " + mapi.GetError(sendRes);
                    msgDlg.BgColor = "Crimson";                
                }
                else
                    resMsg = "Снимок экрана пользователя " + curUser.FullName + " (" + _ssFilePath + ") отправлен по адресу \"" + _to + "\"";

                msgDlg.Title = "Результат операции";
                msgDlg.Message = resMsg;
            }

            Dialog = new CommonModule.Helpers.DialogContainer(msgDlg);
            WorkFlowHelper.WriteToLog(null, resMsg);
        }
        // ---
    }
}
