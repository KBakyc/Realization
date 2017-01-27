using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataObjects
{
    public class SfTypeInfo
    {
                    //case 1:
                    //    sfTypeLabel = "К";
                    //    sfTypeDescription = "Корректировочный счёт на продукцию";
                    //    break;
                    //case 2:
                    //    sfTypeLabel = "К";
                    //    sfTypeDescription = "Корректировочный счёт на провозную плату";
                    //    break;
                    //case 3:
                    //    sfTypeLabel = "Б";
                    //    sfTypeDescription = "Бонус / скидка";
                    //    break;
                    //default:
                    //    sfTypeLabel = "";
                    //    break;
        public short SfTypeId { get; set; }        
        public string SfTypeDescription { get; set; }
        public string SfTypeLabel { get { return SfTypeId == 0 || String.IsNullOrWhiteSpace(SfTypeDescription) ? "" : SfTypeDescription[0].ToString(); } }

    }
}
