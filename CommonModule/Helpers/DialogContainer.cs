using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.ViewModels;
using System.Windows.Input;
using CommonModule.Interfaces;

namespace CommonModule.Helpers
{
    public class DialogContainer : BasicNotifier
    {
        private ICloseViewModel dialogContent;

        public DialogContainer(ICloseViewModel _dlg)
        {
            dialogContent = _dlg;
        }

        public ICloseViewModel DialogContent
        {
            get { return dialogContent; }
        }

        //public ICommand SubmitCommand
        //{
        //    get 
        //    {
        //        var trueDlg = dialogContent as BaseDlgViewModel;
        //        return trueDlg == null ? null : trueDlg.SubmitCommand; 
        //    }
        ////    set { DialogContent.SubmitCommand = value; }
        //}

        public ICommand CloseCommand
        {
            get { return DialogContent.CloseCommand; }
        //    set { DialogContent.CloseCommand = value; }
        }

    }
}
