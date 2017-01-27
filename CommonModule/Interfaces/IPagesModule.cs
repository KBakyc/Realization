using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.ComponentModel.Composition;
using System.Collections.ObjectModel;

namespace CommonModule.Interfaces
{
    public interface IPagesModule : IModule
    {
        ObservableCollection<IModuleContent> Pages { get; }
        IModuleContent SelectedPage { get; set; }
    }
}
