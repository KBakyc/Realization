using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.ViewModels;
using DataObjects.Interfaces;

namespace Realization.ViewModels
{
    public class SysInfoViewModel:BasicViewModel
    {
        private IDbService repository;
        private Dictionary<string,string> parsedConnectionString;

        public SysInfoViewModel(IDbService _repository)
        {
            if (_repository == null)
                throw new ArgumentNullException("IRepository");
            repository = _repository;
            CollectSysInfo();
        }

        private void CollectSysInfo()
        {
            parsedConnectionString = ParseConnectionString(repository.ConnectionString);
        }

        //"Data Source=db2;Initial Catalog=real_test;Integrated Security=True"
        private Dictionary<string,string> ParseConnectionString(string _cstring)
        {
            if (String.IsNullOrEmpty(_cstring)) return null;
            var spair = _cstring.Split(';').Select(sp => sp.Split('=')).ToDictionary(p => p[0], p => p[1]);
            return spair;
        }

        public string Server
        {
            get
            {
                return parsedConnectionString["Data Source"];
            }
        }

        public string DataBase
        {
            get
            {
                return parsedConnectionString["Initial Catalog"];
            }
        }
    }
}
