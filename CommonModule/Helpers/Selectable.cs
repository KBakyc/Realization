using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonModule.Helpers
{
    public class Selectable<T> : BasicNotifier, ISelectable
        where T:class
    {
        private T ivalue;
        private bool isSelected;
        
        public Selectable(T _obj, bool _sel)
        {
            ivalue = _obj;
            isSelected = _sel;
        }

        public Selectable(T _obj)
            : this(_obj, false)
        { }

        public T Value
        {
            get{ return ivalue; }
        }

        public bool IsSelected
        {
            get { return isSelected; }
            set { SetAndNotifyProperty("IsSelected", ref isSelected, value); }
        }
    }
}
