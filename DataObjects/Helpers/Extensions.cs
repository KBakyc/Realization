using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace DataObjects.Helpers
{
    public static class Extensions
    {
        public static T DefaultValue<T>(this T? data)
            where T : struct 
        {
            return data == null ? default(T) : data.Value;
        }

        /// <summary>
        /// Проверяет на равенство предоплаты
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool DataEqualsTo(this PredoplModel left, PredoplModel right)
        {
            
            bool valuesEquals =
                left.Idpo == right.Idpo &&
                left.Kgr == right.Kgr &&
                left.DatPropl == right.DatPropl &&
                left.DatVvod == right.DatVvod &&
                left.KodVal == right.KodVal &&
                left.KodValB == right.KodValB &&
                left.KursVal == right.KursVal &&
                left.DatKurs == right.DatKurs &&
                left.Ndok == right.Ndok &&
                left.Pkod == right.Pkod &&
                left.Poup == right.Poup &&
                left.IdAgree == right.IdAgree &&
                left.SumBank == right.SumBank &&
                left.SumPropl == right.SumPropl &&
                left.Whatfor == right.Whatfor && 
                left.Prim == right.Prim &&
                left.IdTypeDoc == right.IdTypeDoc;
            
            bool versionEquals = left.Version.Equals(right.Version);
            if (!versionEquals)
                versionEquals = left.Version.SequenceEqual<byte>(right.Version);

            //Array.Equals()

            //if (!versionEquals &&
            //    (left.Version != null && right.Version != null && left.Version.Length == right.Version.Length
            //     || left.Version == null && right.Version == null)

            //for (int i = 0; i < left.Version.Length; i++)
            //    if (left.Version[i] != right.Version[i])
            //    {
            //        versionEquals = false;
            //        break;
            //    }
            return valuesEquals && versionEquals;
        }

        public static bool DataEqualsTo(this OtgrLine left, OtgrLine right)
        {

            bool valuesEquals =
                        left.Idrnn == right.Idrnn &&
                        left.Kpok == right.Kpok &&
                        left.Kgr == right.Kgr &&
                        left.Poup == right.Poup &&
                        left.Pkod == right.Pkod &&
                        left.Kodf == right.Kodf &&
                        left.Datgr == right.Datgr &&
                        left.Datnakl == right.Datnakl &&
                        left.Kpr == right.Kpr &&
                        left.Kolf == right.Kolf &&
                        left.Cena == right.Cena &&
                        left.Vidcen == right.Vidcen &&
                        left.Kodcen == right.Kodcen &&
                        left.Prodnds == right.Prodnds &&
                        left.DocumentNumber == right.DocumentNumber &&
                        left.RwBillNumber == right.RwBillNumber &&
                        left.Sper == right.Sper &&
                        left.Nds == right.Nds &&
                        left.Ndssper == right.Ndssper &&
                        left.Dopusl == right.Dopusl &&
                        left.Ndst_dop == right.Ndst_dop &&
                        left.Ndsdopusl == right.Ndsdopusl &&
                        left.Provoz == right.Provoz &&
                        left.Nomavt == right.Nomavt &&
                        left.Ndov == right.Ndov &&
                        left.Fdov == right.Fdov &&
                        left.Stgr == right.Stgr &&
                        left.Stotpr == right.Stotpr &&
                        left.Nv == right.Nv &&
                        left.TransportId == right.TransportId &&
                        (left.MeasureUnitId == right.MeasureUnitId || left.MeasureUnitId == null && right.MeasureUnitId == null) &&
                        left.Density == right.Density &&
                        left.SumNds == right.SumNds &&
                        left.VidAkc == right.VidAkc &&
                        left.AkcStake == right.AkcStake &&
                        left.AkcKodVal == right.AkcKodVal;

            return valuesEquals;
        }

        public static bool DataEqualsTo(this PenaltyModel left, PenaltyModel right)
        {
            bool valuesEquals =
                        left.Id == right.Id &&
                        left.DateAdd == right.DateAdd &&
                        left.DateKor == right.DateKor &&
                        left.Datgr == right.Datgr &&
                        left.Datkro == right.Datkro &&
                        left.Datopl == right.Datopl &&
                        left.Kdog == right.Kdog &&
                        left.Kodval == right.Kodval &&
                        left.Kpok == right.Kpok &&
                        left.Kursval == right.Kursval &&
                        left.Nomish == right.Nomish &&
                        left.Nomkro == right.Nomkro &&
                        left.Poup == right.Poup &&
                        left.Rnpl == right.Rnpl &&
                        left.Sumopl == right.Sumopl &&
                        left.Sumpenalty == right.Sumpenalty;                       
            return valuesEquals;
        }

        // Заменено на DeepCopy
        /// <summary>
        /// Создаёт копию модели предоплаты
        /// </summary>
        /// <param name="_old"></param>
        /// <returns></returns>
        //public static PredoplModel DataClone(this PredoplModel _old)
        //{
        //    if (_old == null) return null;
        //    else
        //        return new PredoplModel(_old.Idpo, _old.Version.Clone() as byte[])
        //        {
        //            Poup = _old.Poup,
        //            Pkod = _old.Pkod,
        //            Kgr = _old.Kgr,
        //            Ndok = _old.Ndok,
        //            DatPropl = _old.DatPropl,
        //            DatVvod = _old.DatVvod,
        //            KodVal = _old.KodVal,
        //            KodValB = _old.KodValB,
        //            SumPropl = _old.SumPropl,
        //            SumBank = _old.SumBank,
        //            Whatfor = _old.Whatfor,
        //            TrackingState = _old.TrackingState
        //        };
        //}

        /// <summary>
        /// Обновляет элемент коллекции
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="_col"></param>
        /// <param name="_oldItem"></param>
        /// <param name="_newItem"></param>
        /// <returns></returns>
        public static T UpdateItem<T>(this IList<T> _col, T _oldItem, T _newItem)
        {
            int ind = _col.IndexOf(_oldItem);
            _col.RemoveAt(ind);
            if (_newItem != null)
                _col.Insert(ind, _newItem);
            return _newItem;
        }

    }
}