using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.ComponentModel.Composition;
using CommonModule.Interfaces;
using CommonModule.Commands;

namespace CommonModule.Composition
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class ExportNamedComponentAttribute : ExportAttribute
    {

        public ExportNamedComponentAttribute(string _contract, string _componentName)
            : base(_contract)
        {
            ComponentName = _componentName;
        }

        public string ComponentName { get; set; }
    }
}
