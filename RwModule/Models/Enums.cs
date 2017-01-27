using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace RwModule.Models
{
    public enum RwUslType : byte
    {
        [Description("Доп.сборы")]
        DopSbor = 0,
        [Description("Провоз")]
        Provoz = 1
    }
    
    public enum RwPlatDirection : byte
    {
        [Description("Платёж")]
        Out = 1,
        [Description("Возврат")]
        In = 0
    }

    public enum RwPayActionType : byte
    {
        [Description("Оплата услуги")]
        PayUsl = 0,

        [Description("Списание услуги")]
        CloseUsl = 1,

        [Description("Списание платежа")]
        ClosePlat = 2,

        [Description("Возврат платежа")]
        DoVozvrat = 3,

        [Description("Списание возврата")]
        CloseVozvrat = 4
    }
}
