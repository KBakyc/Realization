using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.Helpers;
using DataObjects;

namespace CommonModule.ViewModels
{
    /// <summary>
    /// Модель отображения опции выбора вы диалоге.
    /// </summary>
    public class ChoiceViewModel : BasicNotifier
    {
        private Choice choice;

        public ChoiceViewModel(Choice _ch)
        {
            choice = _ch;
        }

        public string Header 
        {
            get { return choice.Header; }
            set
            {
                if (value != choice.Header)
                {
                    choice.Header = value;
                    NotifyPropertyChanged("Header");
                }
            }
        }
        
        public string Info 
        {
            get { return choice.Info; }
            set
            {
                if (value != choice.Info)
                {
                    choice.Info = value;
                    NotifyPropertyChanged("Info");
                }
            }
        }
        
        public bool? IsChecked
        {
            get { return choice.IsChecked; }
            set
            {
                if (value != choice.IsChecked)
                {
                    choice.IsChecked = value;
                    NotifyPropertyChanged("IsChecked");
                }
            }
        }

        public bool IsSingleInGroup 
        {
            get { return choice.IsSingleInGroup; }
            set
            {
                if (value != choice.IsSingleInGroup)
                {
                    choice.IsSingleInGroup = value;
                    NotifyPropertyChanged("IsSingleInGroup");
                }
            }
        }

        public string GroupName 
        {
            get { return choice.GroupName; }
            set
            {
                if (value != choice.GroupName)
                {
                    choice.GroupName = value;
                    NotifyPropertyChanged("GroupName");
                }
            }
        }
        
        public string Name 
        {
            get { return choice.Name; }
            set
            {
                if (value != choice.Name)
                {
                    choice.Name = value;
                    NotifyPropertyChanged("Name");
                }
            }
        }
        
        public Object Item
        {
            get { return choice.Item; }
            set
            {
                if (value != choice.Item)
                {
                    choice.Item = value;
                    NotifyPropertyChanged("Item");
                }
            }
        }

        public T GetItem<T>()
        {
            return Item != null && Item is T ? (T)Item : default(T);
        }
    }
}
