using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.Interfaces;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.PeerResolvers;
using System.ServiceModel.Channels;
using System.ComponentModel.Composition;
using DataObjects;
using System.Diagnostics;
using System.Xml.Linq;
using System.ServiceModel.Discovery;
using DataObjects.Interfaces;
using System.IO;

namespace RepkaService
{
    [Export(typeof(IAppService))]
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, IncludeExceptionDetailInFaults = true)]
    public class CommunicationService : IRepkaService, IAppService
    {
        //private string localIP;
        private IDbService dbServ;
        private int userId;

        public CommunicationService()
        {
            //localIP = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName())
            //         .AddressList.First(f => f.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            //         .ToString();
            dbServ = CommonModule.CommonSettings.Repository;
            userId = dbServ.UserToken;
        }

        [Import]
        private IShellModel shellModel;

        public bool IsStayInMemory
        {
            get { return true; ; }
        }

        private ServiceHost host;
        private Uri url;
        //private ServiceHost hostCPRS;
        //private IMessengerChannel channel;


        private static int FindFreeTcpPort()
        {
            var l = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Parse ("127.0.0.1"), 0);
            l.Start ();
            int port = ((System.Net.IPEndPoint) l.LocalEndpoint).Port;
            l.Stop ();
            return port;
        }

        private string RunCommand(string _cmd, string _args, bool _shell, Encoding _enc, string[] _inputs)
        {
            string res = null;
            var proc = new Process();
            proc.StartInfo = new ProcessStartInfo(_cmd, _args)
            {
                CreateNoWindow = true,
                ErrorDialog = false,
                UseShellExecute = _shell,
                RedirectStandardOutput = _shell ? false : true,
                RedirectStandardError = _shell ? false : true,
                RedirectStandardInput = _shell ? false : true,                
                StandardOutputEncoding = _enc,
                StandardErrorEncoding = _enc
            };
            try
            {
                proc.Start();
                if (proc.StartInfo.RedirectStandardInput && _inputs != null && _inputs.Length > 0)
                {
                    Array.ForEach(_inputs, i => proc.StandardInput.WriteLine(i));
                    proc.StandardInput.Flush();
                }
                proc.WaitForExit();
                
                res = _cmd + " " + _args;
                if (!_shell)
                {
                    string log = proc.StandardOutput.ReadToEnd();
                    string logerr = proc.StandardError.ReadToEnd();
                    if (!String.IsNullOrWhiteSpace(log))
                        res += Environment.NewLine + log;
                    if (!String.IsNullOrWhiteSpace(logerr))
                        res += Environment.NewLine + logerr;
                }                
            }
            catch {}
            return res;
        }

        private void AddToFirewall()
        {                       
            string command = "netsh";
            string repkaname = System.IO.Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, AppDomain.CurrentDomain.SetupInformation.ApplicationName);
            string args = Environment.OSVersion.Version.Major > 5 
                        ? "advfirewall firewall add rule name=\"Repka\" dir=in action=allow program=\"" + repkaname + "\" enable=yes"
                        : "firewall add allowedprogram \"" + repkaname +  "\" \"Repka\" ENABLE";
            CommonModule.Helpers.WorkFlowHelper.WriteToLog("", "AddToFirewall starting...");
            var l = RunCommand(command, args, false, Encoding.GetEncoding(866), null);           
            if (!String.IsNullOrWhiteSpace(l))
                CommonModule.Helpers.WorkFlowHelper.WriteToLog("", l);
        }

        private void PrepareService()
        {
            AddToFirewall();

            url = new Uri(String.Format(@"net.tcp://{0}:{1}/Repka", Environment.MachineName, FindFreeTcpPort()));
            UpdateServiceLocation();

            host = new ServiceHost(this, url);
            var bind = new NetTcpBinding(SecurityMode.Transport);
            //bind.Security.Message.ClientCredentialType = MessageCredentialType.Windows;
            bind.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;
            bind.Security.Transport.ProtectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign;

            var servEndpoint = host.AddServiceEndpoint(typeof(IRepkaService), bind, "");

            var udpDiscEndp = new UdpDiscoveryEndpoint();
            host.AddServiceEndpoint(udpDiscEndp);

            var edpDiscBehaviour = new EndpointDiscoveryBehavior();
            edpDiscBehaviour.Extensions.Add(new XElement("UserId", userId));

            //var servEndpoint = host.Description.Endpoints.Find(typeof(IRepkaService));
            var annEndpoint = new UdpAnnouncementEndpoint();
            annEndpoint.Behaviors.Add(edpDiscBehaviour);
            servEndpoint.Behaviors.Add(edpDiscBehaviour);

            var serviceDiscoveryBehavior = new ServiceDiscoveryBehavior();
            serviceDiscoveryBehavior.AnnouncementEndpoints.Add(annEndpoint);
            host.Description.Behaviors.Add(serviceDiscoveryBehavior);
        }

        public void Start()
        {
            if (host == null)
                PrepareService();
           
            try
            {
                host.Open();                
            }
            catch (Exception e)
            {
                CommonModule.Helpers.WorkFlowHelper.WriteToLog(null, 
                "CommunicationService failed to start"
                + Environment.NewLine
                + e.Message
                + e.InnerException != null ? Environment.NewLine + e.InnerException.Message : "");
            }
        }

        public void Stop()
        {
            if (host != null && host.State == CommunicationState.Opened)
                host.Close();
        }

        private void UpdateServiceLocation()
        {            
            if (userId == 0) return;
            UserInfo curUser = dbServ.GetUserInfo(userId);
            if (curUser.ClientInfo == null) return;
            XElement clientInfo = null;
            if (curUser.ClientInfo.Name != CommonModule.Helpers.WorkFlowHelper.CI_CONTAINER)
            {
                clientInfo = new XElement(CommonModule.Helpers.WorkFlowHelper.CI_CONTAINER);
                clientInfo.Add(curUser.ClientInfo);
            }
            else
                clientInfo = curUser.ClientInfo;

            var servElements = clientInfo.Elements(CommonModule.Helpers.WorkFlowHelper.CI_COMMSERVLOC);
            foreach (var servEl in servElements) servEl.Remove(); // удаляем прежние элементы
            
            if (url == null) return;

            System.Xml.Linq.XElement sEl = new XElement(CommonModule.Helpers.WorkFlowHelper.CI_COMMSERVLOC);            
            sEl.SetAttributeValue("Url", url.ToString());
            clientInfo.Add(sEl);
            curUser.ClientInfo = clientInfo;
            dbServ.UpdateUserInfo(curUser.Id, curUser);

        }

        public void SendMessage(string _mess, bool _exit)
        {
            if (String.IsNullOrWhiteSpace(_mess) || shellModel == null) return;
            var sec = ServiceSecurityContext.Current;
            var username = sec.WindowsIdentity.Name;
            var user = dbServ.GetUserInfo(username);
            var title = String.Format("Сообщение от {0}", user != null ? user.FullName : username);

            Action<object> onsubmit = null;
            if (_exit)
                shellModel.Exit(title, _mess, _exit ? onsubmit : o => {}, null);
            else
                shellModel.SendMessage(title, _mess, false, null, null);

            var mwind = shellModel.Container.GetExportedValueOrDefault<System.Windows.Window>("MainWindow");
            shellModel.UpdateUi(() =>
            {
                if (mwind != null)
                {
                    if (mwind.WindowState == System.Windows.WindowState.Minimized)
                        mwind.WindowState = System.Windows.WindowState.Normal;
                    mwind.Activate();
                }
            }, true, false);
        }

        public bool IsOnline()
        {
            return true;
        }


        public Stream GetLog()
        {
            Stream res = null;
            var lpath = CommonModule.CommonSettings.LogPath;
            if (File.Exists(lpath))
            {
                res = File.OpenRead(lpath);
            }
            return res;
        }

        public Stream GetScreen()
        {
            return ScreenshotHelper.ScreenShoter.CaptureToStream();
        }

        public string SetShare(string _path, bool _on)
        {
            string res = null;
            var path = _path;
            if (_on)
            {
                if (String.IsNullOrWhiteSpace(path)) return "Путь не может быть пустым!";
                path = String.Join(@"\", path.Split('\\').Where(s => !String.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToArray());
                if (!Directory.Exists(path)) return String.Format("Путь \"{0}\" не найден!", path);
            }
            string command = "net";
            string args = _on ? "share Repka=\"" + path + "\""
                              : @"share Repka /DELETE /Y";
            res = RunCommand(command, args, false, Encoding.GetEncoding(866), !_on ? new string[] {"Y"} : null);    
            
            return res;
        }
    }
}
