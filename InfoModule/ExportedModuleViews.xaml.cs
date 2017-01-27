using System.ComponentModel.Composition;
using System.Windows;

namespace InfoModule
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