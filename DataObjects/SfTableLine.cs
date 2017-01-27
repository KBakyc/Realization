using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;


namespace DataObjects
{
    /// <summary>
    /// Хранит информацию об одной строчке в таблице счёта-фактуры
    /// </summary>
    public class SfTableLine
    {
        public SfTableLine(LineTypes _lt)
        {
            LineType = _lt;
        }

        /// <summary>
        /// Тип строчки
        /// </summary>
        public LineTypes LineType { get; set; }

        public string  Name { get; set; }
        public int     KodProd { get; set; }

        /// <summary>
        /// Единицы измерения
        /// </summary>
        public string  edIzm;
        public string  EdIzm
        {
            get { return edIzm; } 
            set
            {
                if (edIzm != value)
                {
                    edIzm = value;
                    if (edIzm != null)
                        edIzm = edIzm.Trim();
                }
            } 
        }
        public decimal KolProd { get; set; }
        public byte KolProdPrec { get; set; }

        public decimal CenProd { get; set; }
        public byte CenProdPrec { get; set; }

        public string  ValStr { get; set; }
        public decimal SumProd { get; set; }

        /// <summary>
        /// Строка за суммой ("Без акциза", валюта ...)
        /// </summary>
        public string SumInfo { get; set; }

        public decimal SumAkc { get; set; }
        public decimal NdsSt { get; set; }
        public decimal NdsSum { get; set; }
        public string NdsNullFormat { get; set; }

        public decimal SumItog { get; set; }

        public XElement ColumnsFormatData { get; set; }

    }
}