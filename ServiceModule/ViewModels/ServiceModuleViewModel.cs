using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CommonModule;
using CommonModule.Commands;
using CommonModule.Helpers;
using CommonModule.Interfaces;
using CommonModule.ViewModels;
using CommonModule.Composition;

namespace ServiceModule.ViewModels
{
    [ExportModule(DisplayOrder = 99f)]
    public sealed class ServiceModuleViewModel : PagesModuleViewModel, IPartImportsSatisfiedNotification
    {
        public ServiceModuleViewModel()
        {
            Info = new ModuleDescription()
            {
                Name = "ServiceModule",
                Description = "Системный сервис",
                Version = 1,
                Header = "Сервис",
                IconUri = @"/ServiceModule;component/Resources/service.png"
            };
        }

        private bool commandsLoaded;

        private ModuleMenuItemViewModel[] menuItems;
        public ModuleMenuItemViewModel[] MenuItems 
        {
            get
            {
                if (menuItems == null && commandsLoaded)
                    menuItems = ModuleCommands.GroupBy(c => c.GroupName ?? c.Label)
                                              .Select(g => new ModuleMenuItemViewModel(g.Key, g))
                                              .ToArray();
                return menuItems;
            }
        }

        #region IPartImportsSatisfiedNotification Members

        public void OnImportsSatisfied()
        {
            LoadCommands("ServiceModule.ModuleCommand");
            commandsLoaded = true;            
        }

        #endregion
    }
}