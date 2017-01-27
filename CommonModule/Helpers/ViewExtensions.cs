using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;

namespace CommonModule.Helpers
{
    public static class ViewExtensions
    {               
        /// <summary>
        /// Устанавливает свойство Loaded у DataContext
        /// </summary>
        /// <param name="_uc"></param>
        /// <param name="_l"></param>
        public static void SetLoaded(this UserControl _uc, bool _l)
        {
            _uc.SetLoaded(_uc.DataContext as CommonModule.Interfaces.ILoaded, _l);
        }

        public static void SetLoaded(this UserControl _uc, CommonModule.Interfaces.ILoaded _dc,  bool _l)
        {
            if (_dc != null)
                _dc.IsLoaded = _l;
        }
    }
}
