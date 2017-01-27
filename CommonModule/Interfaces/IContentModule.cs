using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.ComponentModel.Composition;
using System.Collections.ObjectModel;

namespace CommonModule.Interfaces
{
    public interface IContentModule : IModule//, IContentViewModel
    {
        IModuleContent Content { get; set; }
    }
}
