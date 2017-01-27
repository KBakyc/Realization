using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReplicationService
{
    public class ReplicationServiceStarter
    {
        private string serviceUrl;

        public ReplicationServiceStarter(string _srvUrl)
        {
            serviceUrl = _srvUrl;
        }

        public string StartReplication(string _task, string _table)
        {
            string res = "";
            var serv = new DBFReplication.ReplicationStarter();
            serv.Url = serviceUrl;
            var args = String.Format(@"/t:{0} /n:{1}", _task, _table);
            if (_table == "otgruz")
                args = @"/c " + args; // пересоздание DTS пакетов для OTGRUZ
            res = serv.Execute(args);
            return res;
        }
    }
}
