using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.ComponentModel.Composition;
using CommonModule.Interfaces;

namespace CommonModule.Composition
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ExportModuleAttribute : ExportAttribute
    {

        public ExportModuleAttribute()

            : base(typeof(IModule))
        {

        }

        [DefaultValue(1f)]
        public float DisplayOrder { get; set; }

    }
}
