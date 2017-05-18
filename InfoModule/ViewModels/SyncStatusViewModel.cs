using System.Collections.Generic;
using System.Linq;
using CommonModule.Commands;
using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Interfaces;
using System.Windows.Input;
using System;
using System.Windows.Threading;
using CommonModule.Helpers;

namespace InfoModule.ViewModels
{
    /// <summary>
    /// Модель диалога отображения статуса системы синхнонизации.
    /// </summary>
    public class SyncStatusViewModel : BaseDlgViewModel
    {
        private System.Windows.Threading.DispatcherTimer timer;
        private string srvUrl;
        private IDbService repository;

        public SyncStatusViewModel(IDbService _repository)
        {
            repository = _repository;
            srvUrl = Properties.Settings.Default.ReplicationServiceUrl;
            InitTimer();
            LoadData();
            StartSyncAllCommand = new DelegateCommand(ExecuteSyncAll, CanExecuteSyncAll);
            StartSyncCommand = new DelegateCommand(ExecuteSync, CanExecuteSync);
            OnClosed = OnClose;
            timer.Start();
        }

        private void OnClose(Object _dlg)
        {
            var dlgvm = _dlg as SyncStatusViewModel;
            if (dlgvm == null || dlgvm.timer == null || !dlgvm.timer.IsEnabled) return;
            dlgvm.timer.Stop();
        }

        //public System.Windows.Threading.DispatcherTimer Timer { get { return timer; } }

        private void InitTimer()
        {
            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 5);
            timer.Tick += new EventHandler(TimerWork);
        }

        private void TimerWork(object sender, EventArgs e)
        {
            // Updating the Label which displays the current second
            LoadData();

            // Forcing the CommandManager to raise the RequerySuggested event
            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// Загрузка данных для редактирования
        /// </summary>
        private void LoadData()
        {
            var selection = SelectedTable;
            var rep = repository as DAL.LinqDbService;
            TablesStatuses = rep.GetTablesSyncStatuses().Select(m => new TableSyncStatusViewModel(m)).ToArray();
            NotifyPropertyChanged("TablesStatuses");
            if (selection != null)
                SelectedTable = TablesStatuses.SingleOrDefault(t => t.TableName == selection.TableName && t.TaskName == selection.TaskName);
        }

        public TableSyncStatusViewModel[] TablesStatuses { get; set; }

        private TableSyncStatusViewModel selectedTable;
        public TableSyncStatusViewModel SelectedTable
        {
            get { return selectedTable; }
            set { SetAndNotifyProperty("SelectedTable", ref selectedTable, value); }
        }

        /// <summary>
        /// Запуск синхронизации всех таблиц
        /// </summary>
        public ICommand StartSyncAllCommand { get; private set; }

        private void ExecuteSyncAll()
        {
            timer.Stop();
            try
            {
                var servStarter = new ReplicationService.ReplicationServiceStarter(srvUrl);
                foreach (var ts in TablesStatuses.Where(t => t.TaskName == SelectedTable.TaskName))
                {
                    servStarter.StartReplication(ts.TaskName, ts.TableName);
                    ts.Status = SyncStatuses.Busy;
                }
            }
            catch (Exception e)
            {
                WorkFlowHelper.OnCrash(e);
            }
            timer.Start();
        }

        private bool CanExecuteSyncAll()
        {
            bool res;
            res = TablesStatuses != null 
                  && SelectedTable != null
                  && !String.IsNullOrEmpty(SelectedTable.TableName)
                  && TablesStatuses.Length > 0 && !TablesStatuses.Any(ts => ts.Status == SyncStatuses.Busy);
            return res;
        }

        /// <summary>
        /// Синхронизация выбранной таблицы
        /// </summary>
        public ICommand StartSyncCommand { get; private set; }

        private void ExecuteSync()
        {
            timer.Stop();
            try
            {
                var servStarter = new ReplicationService.ReplicationServiceStarter(srvUrl);
                servStarter.StartReplication(SelectedTable.TaskName, SelectedTable.TableName);
                SelectedTable.Status = SyncStatuses.Busy;
            }
            catch (Exception e)
            {
                WorkFlowHelper.OnCrash(e);
            }
            timer.Start();
        }

        private bool CanExecuteSync()
        {
            bool res;
            res = SelectedTable != null 
                && !String.IsNullOrEmpty(SelectedTable.TableName)
                && !TablesStatuses.Any(ts => ts.Status == SyncStatuses.Busy);
            return res;
        }
    }
}