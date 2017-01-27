using System.Linq;
using CommonModule.Commands;
using CommonModule.Helpers;
using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Interfaces;
using System.Collections.Generic;

namespace RwModule.ViewModels
{
    public class SubmitRwSinksDlgViewModel : BaseDlgViewModel
    {
        public SubmitRwSinksDlgViewModel()
        {
            Title = "Подтвердите следующие погашения";
        }

        public List<RwPayActionViewModel> PayActions { get; set; }
    }
}