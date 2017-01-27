using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonModule.ViewModels
{
    public class DlgViewModelContainer : BasicViewModel
    {
        BaseDlgViewModel innerViewModel;

        public DlgViewModelContainer(BaseDlgViewModel _dlgVM)
        {
            innerViewModel = _dlgVM;
        }

        public BaseDlgViewModel InnerViewModel
        {
            get { return innerViewModel; }
        }

        public virtual bool IsValid()
        {
            return innerViewModel.IsValid();
        }
    }
}
