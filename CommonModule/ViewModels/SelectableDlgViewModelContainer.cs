using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonModule.ViewModels
{
    /// <summary>
    /// Контейнер диалога, поддерживающий выделение.
    /// </summary>
    public class SelectableDlgViewModelContainer : DlgViewModelContainer
    {
        public SelectableDlgViewModelContainer(BaseDlgViewModel _dlgVM)
            : base(_dlgVM)
        {
        }

        private bool isSelected;
        public bool IsSelected
        {
            get { return isSelected; }
            set { SetAndNotifyProperty("IsSelected", ref isSelected, value); }
        }

        public override bool IsValid()
        {
            return isSelected && base.IsValid();
        }
    }
}
