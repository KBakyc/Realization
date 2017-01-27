using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Linq.Expressions;

namespace CommonModule.Helpers
{
    public abstract class BasicNotifier : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        #endregion

        protected bool SetAndNotifyProperty<T>(String propertyName, ref T currentValue, T newValue)
        {
            if (currentValue == null)
            {
                if (newValue == null)
                    return false;
            }
            else if (newValue != null && currentValue.Equals(newValue))
            {
                return false;
            }
            currentValue = newValue;
            NotifyPropertyChanged(propertyName);
            return true;
        }

        protected string GetPropName(Expression<Func<object>> _getPropName)
        {
            string res = null;
            if (_getPropName != null)
                if (_getPropName.Body is MemberExpression)
                    res = (_getPropName.Body as MemberExpression).Member.Name;
                else
                    if (_getPropName.Body is UnaryExpression)
                        res = ((_getPropName.Body as UnaryExpression).Operand as MemberExpression).Member.Name;

            return res;
        }

        protected virtual void NotifyPropertyChanged(Expression<Func<object>> _getPropName)
        {
            string prop = GetPropName(_getPropName);
            if (!String.IsNullOrWhiteSpace(prop))
                NotifyPropertyChanged(prop);
        }

        protected bool SetAndNotifyProperty<T>(Expression<Func<object>> _getPropName, ref T currentValue, T newValue)
        {
            bool res = false;
            string prop = GetPropName(_getPropName);
            if (!String.IsNullOrWhiteSpace(prop))
                res = SetAndNotifyProperty(prop, ref currentValue, newValue);
            return res;
        }
    }
}