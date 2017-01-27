using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace CommonModule.Interfaces
{
    public interface ICancelViewModel
    {
        ICommand CancelCommand { get; set; }
    }
}
