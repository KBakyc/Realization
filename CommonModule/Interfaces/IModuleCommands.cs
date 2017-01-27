using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.Commands;

namespace CommonModule.Interfaces
{
    public interface IModuleCommands
    {
        ModuleCommand[] ModuleCommands { get; set; }
    }
}
