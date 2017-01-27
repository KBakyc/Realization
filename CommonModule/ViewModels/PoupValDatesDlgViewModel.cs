using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Input;
using CommonModule.Commands;
using DAL;
using DataObjects;
using DataObjects.Interfaces;

namespace CommonModule.ViewModels
{
    public class PoupValDatesDlgViewModel : PoupAndDatesDlgViewModel
    {
        private ValSelectionViewModel valSelVm;
        //private IRepository repository;

        public PoupValDatesDlgViewModel(IDbService _rep, bool _issave)
            :base(_rep,_issave)
        {
            valSelVm = new ValSelectionViewModel(_rep);           
        }

        public PoupValDatesDlgViewModel(IDbService _rep)
            :this(_rep, true)
        {
        }

        public ValSelectionViewModel ValSelection { get { return valSelVm; } }

        public Valuta SelVal
        {
            get
            {
                return valSelVm.SelVal;
            }
        }

        public override bool IsValid()
        {
            return base.IsValid() 
                && ValSelection.IsValid();
        }
    }
}
