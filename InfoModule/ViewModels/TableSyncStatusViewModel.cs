using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.ViewModels;
using DataObjects;

namespace InfoModule.ViewModels
{
    /// <summary>
    /// Модель отображения статса синхнонизации таблицы
    /// </summary>
    public class TableSyncStatusViewModel : BasicViewModel
    {
        private TableSyncStatus model;

        public TableSyncStatusViewModel(TableSyncStatus _model)
        {
            model = _model;
        }

        public string TaskName { get { return model.TaskName; } }
        public string TableName { get { return model.TableName; } }
        public string TableDescription { get { return model.TableDescription; } }
        public SyncStatuses Status 
        { 
            get { return model.Status; }
            set 
            {
                if (value != model.Status)
                {
                    model.Status = value;
                    NotifyPropertyChanged("Status");
                }
            }
        }
        public DateTime DtStart { get { return model.DtStart; } }
        public DateTime DtEnd { get { return model.DtEnd; } }

        public string StatusLabel
        {
            get { return GetStatusLabel(); }
        }

        //private string GetFriendlyTableName(string _tname)
        //{
        //    String res = _tname;
        //    return res;
        //}


        private string GetStatusLabel()
        {
            string res = "";
            if (model != null)
            {
                switch (Status)
                {
                    case SyncStatuses.Ok:
                        res = "OK";
                        break;
                    case SyncStatuses.Busy:
                        res = "!";
                        break;
                    case SyncStatuses.Error:
                        res = "X";
                        break;
                    default:
                        break;
                }
            }
            return res;
        }
    }
}
