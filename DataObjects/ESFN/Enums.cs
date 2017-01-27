using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace DataObjects.ESFN
{
    public enum InvoiceTypes : byte
    {
        [Description("Исходный")]
        ORIGINAL = 1,
        [Description("Дополнительный")]
        ADDITIONAL,
        [Description("Исправленный")]
        FIXED
    }

    public enum InvoiceStatuses : byte
    {
        [Description("В разработке")]
        STATUS1 = 1,
        [Description("В разработке. Ошибка")]
        STATUS2 = 2,
        [Description("Выставлен")]
        STATUS3 = 3,
        [Description("Выставлен. Подписан получателем")]
        STATUS4 = 4,
        [Description("Выставлен. Аннулирован поставщиком")]
        STATUS5 = 5,
        [Description("На согласовании")]
        STATUS6 = 6,
        [Description("Аннулирован")]
        STATUS7 = 7,
        [Description("Ошибка портала")]
        STATUS8 = 8,
        [Description("Обрабатывается порталом ")]
        STATUS9 = 9
    }
}
