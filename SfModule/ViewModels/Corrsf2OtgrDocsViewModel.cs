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
    public class Corrsf2OtgrDocsViewModel : OtgrDocsDlgViewModel
    {
        public Corrsf2OtgrDocsViewModel(IDbService _rep, IEnumerable<OtgrDocModel> _docs)
            : base(_rep, _docs, o => o.IdCorrsf == 0)
        {
        }
    }
}
