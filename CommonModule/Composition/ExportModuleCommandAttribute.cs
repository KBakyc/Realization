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
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ExportModuleCommandAttribute : ExportAttribute
    {

        public ExportModuleCommandAttribute(string _contract)
            : base(_contract, typeof(ModuleCommand))
        {
//            Contract = _contract;
        }

//        public string Contract { get; set; }

        [DefaultValue(1f)]
        public float DisplayOrder { get; set; }

    }
}
