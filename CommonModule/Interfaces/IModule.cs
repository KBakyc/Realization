using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.ComponentModel.Composition;
using System.Collections.ObjectModel;
using DataObjects.Interfaces;
using System.Configuration;
using CommonModule.ModuleServices;

namespace CommonModule.Interfaces
{
    public interface IModule : IDialogViewModel
    {
        ModuleDescription Info { get; }
        IShellModel ShellModel { get; }
        IDbService Repository { get; }
        ICommand StartModule { get; }
        ICommand StopModule { get; }
        void LoadContent(IModuleContent _content);
        void UnLoadContent(IModuleContent _content);
        bool IsContentLoaded { get; }
        IModuleContent GetLoadedContent<T>(Func<T, Boolean> _filter) where T : class, IModuleContent;
        bool SelectContent<T>(Func<T, Boolean> _filter) where T : class, IModuleContent;
        int AccessLevel { get; }        
        IModuleService Services { get; }
    }
}
