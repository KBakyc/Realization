using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace DataObjects
{
    public class UserInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string FullName { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsSystem { get; set; }
        public string EmailAddress { get; set; }
        public string TabNum { get; set; }
        public string Ceh { get; set; }
        public XElement ClientInfo { get; set; }
        public int? Context { get; set; }
        public string Title
        {
            get
            {
                return String.IsNullOrEmpty(FullName) ? Name : FullName;
            }
        }
    }
}
