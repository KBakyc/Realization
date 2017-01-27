using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataObjects;
using System.Globalization;


namespace CommonModule.DataViewModels
{
    /// <summary>
    /// Представляет информацию об одной строчке в таблице счёта-фактуры
    /// </summary>
    public class SfTableLineViewModel
    {
        private string[] formats = new string[10];

        public SfTableLineViewModel(SfTableLine _tl)
        {
            TableLine = _tl;
            ParseFormats();
        }

        private void ParseFormats()
        {
            if (TableLine.ColumnsFormatData == null) return;

            foreach (var c in TableLine.ColumnsFormatData.Elements("Column"))
            {
                var cnum = int.Parse(c.Attribute("cnum").Value);
                var fmtattr = c.Attribute("format");
                if (fmtattr != null)
                    formats[cnum] = fmtattr.Value;
            }
        }


        private string FormatValue(string _format, Object _value)
        {
            string res = "";
            if (_format != null)
            {
                CultureInfo ci = CultureInfo.InvariantCulture;
                string fmt = _format.Trim() == "" ? "" : "{0:" + _format + "}";
                res = String.Format(ci, fmt, _value); 
            }
            else
                res = _value.ToString();
            return res;
        }

        /// <summary>
        /// Ссылка на модель табличной строки
        /// </summary>
        public SfTableLine TableLine { get; set; }

        public string Name { get { return TableLine.Name; } }

        public string KolProd 
        {
            get 
            {
                return FormatValue(formats[3] ?? "#.#####;;#", TableLine.KolProd); 
            }
        }

        public string CenProd
        {
            get
            {
                return FormatValue(formats[4] ?? "#.#####;;#", TableLine.CenProd);
            }
        }

        public string ValName
        {
            get { return TableLine.ValStr; }
        }

        public string SumProd
        {
            get
            {
                return FormatValue(formats[5] ?? "#.#####;;#", TableLine.SumProd);
            }
        }

        public string NdsSt
        {
            get
            {
                string res = "";
                if (TableLine.LineType != LineTypes.SfItog)
                {
                    res = FormatValue(formats[7] ?? "#.#####;;#", TableLine.NdsSt);
                }
                return res;
            }
        }

        public string NdsSum
        {
            get
            {
                return FormatValue(formats[8] ?? "#.#####;;#", TableLine.NdsSum);
            }
        }

        public string SumItog
        {
            get
            {
                return FormatValue(formats[9] ?? "#.#####;;#", TableLine.SumItog);
            }
        }

    }
}