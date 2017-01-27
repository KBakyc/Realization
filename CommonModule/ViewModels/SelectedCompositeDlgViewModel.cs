using System.Collections.Generic;
using System.Linq;
using CommonModule.Commands;
using CommonModule.Interfaces;
using DataObjects;

namespace CommonModule.ViewModels
{
    public class SelectedCompositeDlgViewModel : BaseCompositeDlgViewModel
    {
        protected override bool ItemsCorrect()
        {
            bool res = true;
            if (SelectedDialog != null)
                res = SelectedDialog.IsValid();
            return res;
        }

        public BaseDlgViewModel SelectedDialog
        {
            get { return selectedPart == null ? null : selectedPart.InnerViewModel; }
            set { TrySetSelectedDialog(value); }
        }

        private void TrySetSelectedDialog(BaseDlgViewModel _value)
        {
            DlgViewModelContainer newSel = null;
            if (_value != null && InnerParts.Count > 0)
                newSel = InnerParts.FirstOrDefault(p => p.InnerViewModel == _value);
            SelectedPart = newSel;
        }

        private DlgViewModelContainer selectedPart;
        public DlgViewModelContainer SelectedPart
        {
            get { return selectedPart; }
            set { SetAndNotifyProperty("SelectedPart", ref selectedPart, value); }
        }
    }
}