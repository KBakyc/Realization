using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.NetworkInformation;
using System.Net;

namespace ServiceModule.Helpers
{
    public class ServicePinger
    {
        /// <summary>
        /// Таймаут в милисекундах
        /// </summary>
        private int timeout;
        private Ping pinger;
        private Dictionary<string, IPAddress> hostsIp = new Dictionary<string, IPAddress>();
      
        public ServicePinger(int _timeout)
        {
            timeout = _timeout;
            pinger = new Ping();
        }

        public bool HostOnline(string _host)
        {
            return IsAddressOnline(ResolveHost(_host));
        }

        public bool IsAddressOnline(System.Net.IPAddress _ip)
        {
            return _ip != null && pinger.Send(_ip, timeout).Status == IPStatus.Success;
        }        

        public IPAddress ResolveHost(string _host)
        {
            if (String.IsNullOrWhiteSpace(_host)) return null;
            if (hostsIp.ContainsKey(_host)) return hostsIp[_host];

            IPAddress addr = null;
            try
            {
                var entry = Dns.GetHostEntry(_host);
                if (entry != null && entry.AddressList != null && entry.AddressList.Length > 0)
                    addr = entry.AddressList[0];
            }
            catch { }
            hostsIp[_host] = addr;

            return addr;
        }



    }
}
