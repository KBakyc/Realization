using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonModule.ViewModels
{
    /// <summary>
    /// Контэйнер диалога с возможностью сокрытия.
    /// </summary>
    public class HidableDlgViewModelContainer : DlgViewModelContainer
    {
        public HidableDlgViewModelContainer(BaseDlgViewModel _dlgVM)
            :base(_dlgVM)
        {
        }

        private bool isHided;
        public bool IsHided
        {
            get { return isHided; }
            set { SetAndNotifyProperty("IsHided", ref isHided, value); }
        }

        public override bool IsValid()
        {
            return !isHided && base.IsValid();
        }
    }
}
