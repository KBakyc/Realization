using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Windows;

namespace RwModule
{
    [Export("ModuleView", typeof(ResourceDictionary))]
    public partial class ExportedModuleViews : ResourceDictionary
    {
        public ExportedModuleViews()
        {
            InitializeComponent();
        }
    }
}
