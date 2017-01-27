using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Input;
using CommonModule;
using CommonModule.Commands;
using CommonModule.ViewModels;
//using DAL;
using DataObjects;
using CommonModule.Interfaces;
using CommonModule.Composition;
using System.Collections.Generic;
using CommonModule.ModuleServices;
using PredoplModule.Helpers;

namespace PredoplModule.ViewModels
{
    [ExportModule(DisplayOrder = 5f)]
    [Export(typeof(IPredoplModule))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class PredoplModuleViewModel : PagesModuleViewModel, IPredoplModule, IPartImportsSatisfiedNotification
    {
        //[ImportMany("PredoplModule.ModuleCommand")]
        //public ModuleCommand[] ModuleCommands { get; set; }

        public PredoplModuleViewModel()
        {
            Info = new ModuleDescription()
            {
                Name = Properties.Settings.Default.Name,
                Description = Properties.Settings.Default.Description,
                Version = Properties.Settings.Default.Version,
                Header = Properties.Settings.Default.Header,
                IconUri = @"/PredoplModule;component/Resources/credit_card.png"
            };
        }

        #region IPartImportsSatisfiedNotification Members

        public void OnImportsSatisfied()
        {
            LoadCommands("PredoplModule.ModuleCommand");
        }

        #endregion

        /// <summary>
        /// Отображает список предоплат
        /// </summary>
        /// <param name="ms"></param>
        /// <param name="t"></param>
        public void ListPredopls(IEnumerable<PredoplModel> _models, string _title)
        {
            if (_models != null)
            {
                var nContent = new PredoplsArcViewModel(this, _models) 
                { 
                    Title = _title
                };
                nContent.TryOpen();
            }
        }

        protected override IModuleService GetModuleService()
        {
            return new PredoplService(this); ;
        }
    }
}