using System.Linq;
using CommonModule.Commands;
using CommonModule.Helpers;
using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Interfaces;
using System.Collections.Generic;

namespace RwModule.ViewModels
{
    /// <summary>
    /// Модель диалога подтверждения погашений банковских платежей на ЖД услуги.
    /// </summary>
    public class SubmitRwSinksDlgViewModel : BaseDlgViewModel
    {
        public SubmitRwSinksDlgViewModel()
        {
            Title = "Подтвердите следующие погашения";
        }

        public List<RwPayActionViewModel> PayActions { get; set; }
    }
}