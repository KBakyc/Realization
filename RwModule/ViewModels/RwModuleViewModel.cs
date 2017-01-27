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

namespace RwModule.ViewModels
{
    [ExportModule(DisplayOrder = 7f)]
    public sealed class RwModuleViewModel : PagesModuleViewModel, IPartImportsSatisfiedNotification
    {
        public RwModuleViewModel()
        {
            Info = new ModuleDescription()
            {
                Name = "RwModule",
                Description = "Услуги железной дороги",
                Version = 1,
                IconUri = @"/RwModule;component/Resources/wagon.png",
                Header = "ЖД услуги"
            };
        }

        #region IPartImportsSatisfiedNotification Members

        public void OnImportsSatisfied()
        {
            LoadCommands("RwModule.ModuleCommand");
        }

        #endregion
    }
}