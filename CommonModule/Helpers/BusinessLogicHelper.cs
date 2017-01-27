using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonModule.Helpers
{
    public static class BusinessLogicHelper
    {
        /// <summary>
        /// Для двух кодов продукта определяет, является ли один сортом другого.
        /// </summary>
        /// <param name="lkpr"></param>
        /// <param name="rkpr"></param>
        /// <returns></returns>
        public static bool IsSortEqual(int lkpr, int rkpr)
        {
            if (lkpr == 0 || rkpr == 0) return false;
            if (lkpr == rkpr) return true;
            
            bool res = false;

            char[] diga = "123456789".ToCharArray();

            string lkpr_s = lkpr.ToString("D");
            int lkpr_lastindex = lkpr_s.LastIndexOfAny(diga);
            lkpr_s = lkpr_s.Substring(0, lkpr_lastindex + 1);

            string rkpr_s = rkpr.ToString("D");
            int rkpr_lastindex = rkpr_s.LastIndexOfAny(diga);
            rkpr_s = rkpr_s.Substring(0, rkpr_lastindex + 1);
            
            if (lkpr_lastindex < rkpr_lastindex)
                res = rkpr_s.StartsWith(lkpr_s);
            else
                res = lkpr_s.StartsWith(rkpr_s);

            return res;
        }

        // слияние массива с выбросом промежуточных
        public static string ExtJoin(this int[] arr, string elSep, string rgSep)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            if (arr.Length > 0)
            {
                int fr = arr[0], lr = fr;
                sb.AppendFormat("{0}", fr);
                int i = 1;
                while (i < arr.Length)
                {
                    if (arr[i] - arr[i - 1] == 1)
                    {
                        lr = arr[i];
                        i++;
                        continue;
                    }
                    else
                    {
                        if (lr == fr)
                        {
                            sb.AppendFormat("{0}{1}", elSep, arr[i]);
                            fr = lr = arr[i];
                        }
                        else
                        {
                            sb.AppendFormat("{0}{1}", rgSep, lr);
                            fr = lr = arr[i];
                            sb.AppendFormat("{0}{1}", elSep, fr);
                        }
                        i++;
                    }
                }
                if (fr != lr)
                    sb.AppendFormat("{0}{1}", rgSep, lr);

            }
            return sb.ToString();
        }

        // возвращает ненулевое начало в коде продукта
        public static string MakeKProdPat(int _kpr)
        {
            string kprstr = _kpr.ToString();
            string res = kprstr;
            int kprlen = kprstr.Length;
            int len;
            for (len = kprlen; len > 1; len--)
                if (kprstr[len - 1] != '0') break;
            if (len < kprlen)
                res = kprstr.Substring(0, len);
            return res;
        }
    }
}
