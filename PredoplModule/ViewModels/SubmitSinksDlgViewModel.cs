using System.Linq;
using CommonModule.Commands;
using CommonModule.Helpers;
using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Interfaces;
using System.Collections.Generic;

namespace PredoplModule.ViewModels
{
    /// <summary>
    /// Модель диалога отображения и подтверждения погашений предоплат.
    /// </summary>
    public class SubmitSinksDlgViewModel : BaseDlgViewModel
    {
        public SubmitSinksDlgViewModel()
        {
            Title = "Подтвердите следующие погашения";
        }

        public List<PayAction> PayActions { get; set; }
    }
}