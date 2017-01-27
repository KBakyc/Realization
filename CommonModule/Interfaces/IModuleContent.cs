using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace CommonModule.Interfaces
{
    public interface IModuleContent : ICloseViewModel, ILoaded
    {
        IModule Parent { get; }
        bool IsReadOnly { get; }
        bool IsEnabled { get; }
        bool IsActive { get; set; }
    }
}
