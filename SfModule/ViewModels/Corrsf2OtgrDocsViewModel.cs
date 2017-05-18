using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CommonModule.Commands;
using CommonModule.Helpers;
using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Interfaces;

namespace SfModule.ViewModels
{
    /// <summary>
    /// Модель диалога выбора отгрузки для формирования корректировочного счёта-фактуры по ЖД услугам.
    /// </summary>
    public class Corrsf2OtgrDocsViewModel : OtgrDocsDlgViewModel
    {
        public Corrsf2OtgrDocsViewModel(IDbService _rep, IEnumerable<OtgrDocModel> _docs)
            : base(_rep, _docs, o => o.IdCorrsf == 0)
        {
        }
    }
}
