using System.ComponentModel.Composition;
using System.Windows;

namespace OtgrModule
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