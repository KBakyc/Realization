using System;
using System.Linq;
using System.Windows.Input;
using CommonModule.Commands;
using CommonModule.Helpers;
using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Interfaces;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OtgrModule.ViewModels
{
    public class ShowAktSlivViewModel : BaseDlgViewModel
    {
        private Dictionary<OtgrLine, decimal> data;

        public ShowAktSlivViewModel(Dictionary<OtgrLine, decimal> _data)
        {
            if (_data == null) throw(new ArgumentNullException("_data","Нет данных по акту для отображения"));

            data = _data;
        }

        public DelegateCommand SubmitChangesCommand { get; set; }

        public Dictionary<OtgrLine, decimal> DataInAkt { get { return data; } }

        public decimal TotalInAkt { get { return data.Values.Sum(); } }

    }
}
