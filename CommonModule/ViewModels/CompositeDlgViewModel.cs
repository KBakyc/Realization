using System.Collections.Generic;
using System.Linq;
using CommonModule.Commands;
using CommonModule.Interfaces;
using DataObjects;

namespace CommonModule.ViewModels
{
    public abstract class CompositeDlgViewModel : BaseDlgViewModel
    {

        public override bool IsValid()
        {
            return base.IsValid()
                && ItemsCorrect();
        }

        protected abstract bool ItemsCorrect();

    }
}