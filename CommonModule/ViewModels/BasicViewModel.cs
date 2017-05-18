using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using CommonModule.Helpers;

namespace CommonModule.ViewModels
{
    /// <summary>
    /// Базовая модель отображения.
    /// </summary>
    public abstract class BasicViewModel : BasicNotifier, IDisposable
    {

        private string title;
        public String Title
        { 
            get { return title; }
            set { SetAndNotifyProperty("Title", ref title, value); }
        }

        #region IDisposable Members

        public virtual void Dispose(){}

        #endregion
    }
}