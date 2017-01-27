using System.ComponentModel.Composition;
using System.Windows;

namespace CommonModule
{
    [Export("ModuleView", typeof(ResourceDictionary))]
    public partial class ExportedViews : ResourceDictionary
    {
        public ExportedViews()
        {
            InitializeComponent();
        }
    }
}
