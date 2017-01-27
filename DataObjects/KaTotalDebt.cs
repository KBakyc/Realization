using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataObjects
{
    public class KaTotalDebt
    {
        public int Kpok { get; set; }
        public string Kodval { get; set; }
        public int Poup { get; set; }
        public DateTime DatZakr { get; set; }
        //public decimal SumPltr { get; set; }
        //public decimal SumOpl { get; set; }
        public decimal SumNeopl { get; set; } // Сумма неоплаченных счетов
        public decimal SumPredopl { get; set; } // Сумма доступной предоплаты
        public decimal SumVozvrat { get; set; } // Сумма непогашенных возвратов
    }
}
