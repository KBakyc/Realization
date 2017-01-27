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
//using DAL;
using DataObjects;
using CommonModule.Composition;

namespace OtgrModule.ViewModels
{
    [ExportModule(DisplayOrder = 1f)]
    [Export(typeof(IOtgruzModule))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class OtgrModuleViewModel : PagesModuleViewModel, IOtgruzModule, IPartImportsSatisfiedNotification
    {
        //[Import]
        //private IShellModel shellModel;

        public OtgrModuleViewModel()
        {
            Info = new ModuleDescription()
                       {
                           Name = Properties.Settings.Default.Name,
                           Description = Properties.Settings.Default.Description,
                           Version = Properties.Settings.Default.Version,
                           Header = Properties.Settings.Default.Header,
                           IconUri = @"/OtgrModule;component/Resources/pogruzka.png"
                       };
        }

        /// <summary>
        /// Отображение отгрузки по счёту
        /// </summary>
        /// <param name="_idsf"></param>
        public void ShowOtgrArc(int _idsf)
        {
            SfModel mod = Repository.GetSfModel(_idsf);

            if (mod == null) return;

            Action work = () => (new OtgrArcViewModel(this, mod)).TryOpen();

            Services.DoWaitAction(work, "Ожидание выполнения", "Выборка из архива отгрузки...");
        }

        public void ShowOtgrArc(IEnumerable<OtgrLine> _otgrs)
        {
            Action work = () => (new OtgrArcViewModel(this, _otgrs) { Title = "Отобранная отгрузка"}).TryOpen();

            Services.DoWaitAction(work, "Ожидание выполнения", "Выборка из архива отгрузки...");
        }

        /// <summary>
        /// Комманда просмотра непринятого реестра отгрузки
        /// </summary>
        private ICommand showReestrCommand;
        public ICommand ShowReestrCommand
        {
            get
            {
                if (showReestrCommand == null)
                    showReestrCommand = new DelegateCommand(ExecShowReestr);
                return showReestrCommand;
            }
        }


        public void ExecShowReestr()
        {
            var remember = CommonSettings.Persister;
            int l_usertoken = Repository.UserToken;
            var poup = remember.GetValue<int>("P623.Poup");
            PoupModel poupm;
            Repository.Poups.TryGetValue(poup, out poupm);
            var pkod = remember.GetValue<short>("P623.Pkod");
            PkodModel pkodm = null;
            if (pkod != 0)
                pkodm = Repository.GetPkods(poup).SingleOrDefault(k => k.Pkod == pkod);
            var dateFrom = remember.GetValue<DateTime>("P623.DateFrom");
            var dateTo = remember.GetValue<DateTime>("P623.DateTo");
            var nContent = new p623ViewModel(this, poupm, pkodm, dateFrom, dateTo) 
            {
                Title = "Реестр принимаемой отгрузки"
            };
            if (nContent.OtgrRows.Count == 0)
                Services.ShowMsg("Результат", "Отсутствует сформированная непринятая отгрузка.", true);
            else
                nContent.TryOpen();
        }

        #region IPartImportsSatisfiedNotification Members

        public void OnImportsSatisfied()
        {
            LoadCommands("OtgrModule.ModuleCommand");
        }

        #endregion
    }
}