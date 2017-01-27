using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Windows.Threading;
using CommonModule;
using CommonModule.Commands;
using CommonModule.Helpers;
using CommonModule.Interfaces;
using CommonModule.ViewModels;
using DAL;
using DataObjects;
using CommonModule.Composition;

namespace InfoModule.ViewModels
{
    [ExportModule(DisplayOrder = 6f)]
    public sealed class InfoModuleViewModel : PagesModuleViewModel, IPartImportsSatisfiedNotification
    {
        public InfoModuleViewModel()
        {
            Info = new ModuleDescription()
            {
                Name = Properties.Settings.Default.Name,
                Description = Properties.Settings.Default.Description,
                Version = Properties.Settings.Default.Version,
                Header = Properties.Settings.Default.Header,
                IconUri = @"/InfoModule;component/Resources/bar_chart.png"
            };
        }

        //[ImportMany("InfoModule.ModuleCommand")]
        //private Lazy<ModuleCommand, IDisplayOrderMetaData>[] moduleCommands;

        //public ModuleCommand[] ModuleCommands { get; set; }

        #region IPartImportsSatisfiedNotification Members

        public void OnImportsSatisfied()
        {
            LoadCommands("InfoModule.ModuleCommand");
        }

        #endregion
    }
}