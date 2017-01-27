using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceModule.DAL.Models
{
    public class ComponentUserRight
    {
        public int Id { get; set; }
        public string ComponentTypeName { get; set; }
        public int? AccessLevel { get; set; }
        public int? UserId { get; set; }
    }
}
