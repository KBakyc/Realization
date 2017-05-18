using System;
using System.Linq;
using CommonModule.Commands;
using CommonModule.Helpers;
using DataObjects;
using DataObjects.Interfaces;

namespace CommonModule.ViewModels
{
    /// <summary>
    /// Модель диалога выбора валюты
    /// </summary>
    public class ValSelectionViewModel : BaseDlgViewModel
    {
        private IDbService repository;

        public ValSelectionViewModel(IDbService _rep)
            :this(_rep, null)
        {
        }
        
        public ValSelectionViewModel(IDbService _rep, Func<Valuta,bool> _filt)
        {
            repository = _rep;
            var valdata = repository.GetValutes();
            valList = (_filt == null ? valdata : valdata.Where(_filt)).ToArray();
            string val = Remember.GetValue<string>("SelVal");
            if (!String.IsNullOrEmpty(val))
                selVal = valList.FirstOrDefault(v => v.Kodval == val);            
        }


        private Valuta[] valList;
        public Valuta[] ValList
        {
            get
            {
                return valList;
            }
        }

        public string ValSelectionTitle { get; set; }

        private Valuta selVal;
        public Valuta SelVal
        {
            get
            {
                if (selVal == null)
                    selVal = ValList.SingleOrDefault(v => v.Kodval == "RB");
                return selVal;
            }
            set
            {
                if (value != null && value != selVal)
                {
                    selVal = value;
                    Remember.SetValue("SelVal", selVal.Kodval);
                    NotifyPropertyChanged("SelVal");
                }
            }
        }

        public override bool IsValid()
        {
            return base.IsValid() 
                && SelVal != null;
        }
    }
}
