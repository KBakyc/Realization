using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonModule.Helpers
{
    public class Sumprop
    {
        //рубли
        public static string RurPhrase(decimal money)
        {
            //return  CurPhrase(money,"рубль","рубля","рублей","копейка","копейки","копеек");
            return CurPhrase(money, "руб.РФ", "руб.РФ", "руб.РФ", "коп.", "коп.", "коп.");
            //return CurPhrase(money,"руб.","руб.","руб.");

        }

        public static string RBPhrase(decimal money)
        {
            //return  CurPhrase(money,"рубль","рубля","рублей","копейка","копейки","копеек");
            return CurPhrase(money, "руб.", "руб.", "руб.", "", "", "");
            //return CurPhrase(money,"руб.","руб.","руб.");

        }

        //доллары
        public static string UsdPhrase(decimal money)
        {
            return CurPhrase(money, "долл. ", "долл. ", "долл. ", "цент. США", "цент. США", "цент. США");
            //return CurPhrase(money,"долл. США","долл. США","долл. США");
        }
        //немецкие
        public static string DenPhrase(decimal money)
        {
            return CurPhrase(money, "нем. мар.", "нем.мар.", "нем. мар.", "", "", "");
            //return CurPhrase(money,"нем. мар.","нем.мар.","нем. мар.");

        }

        //украинские гривны
        public static string UkrPhrase(decimal money)
        {
            return CurPhrase(money, "укр. грв.", "укр. грв.", "укр. грв.", "", "", "");
            //return CurPhrase(money,"укр. грв.","укр. грв.","укр. грв.");
        }

        //литовские литы
        public static string LitPhrase(decimal money)
        {
            return CurPhrase(money, "лит.", "лит.", "лит.", "", "", "");
            //return CurPhrase(money,"лит.","лит.","лит.");
        }

        //латвийские латы
        public static string LatPhrase(decimal money)
        {
            return CurPhrase(money, "лат.", "лат.", "лат.", "", "", "");
            //return CurPhrase(money,"лат.","лат.","лат.");
        }

        //эстонские кроны
        public static string EEPhrase(decimal money)
        {
            return CurPhrase(money, "эст.крон.", "эст.крон.", "эст.крон.", "", "", "");
            //return CurPhrase(money,"эст.крон.","эст.крон.","эст.крон.");
        }

        //Евро
        public static string EuroPhrase(decimal money)
        {
            return CurPhrase(money, "евро", "евро", "евро", "евроцент.", "евроцент.", "евроцент.");
            //return CurPhrase(money,"евро","евро","евро");
        }

        //Экю
        public static string EkuPhrase(decimal money)
        {
            return CurPhrase(money, "экю", "экю", "экю", "", "", "");
            //return CurPhrase(money,"экю","экю","экю");
        }

        public static string NumPhrase(ulong Value, bool IsMale, bool _isCapitalize)
        {
            if (Value == 0UL) return "Ноль";
            string[] Dek1 = { "", " од", " дв", " три", " четыре", " пять", " шесть", " семь", " восемь", " девять", " десять", " одиннадцать", " двенадцать", " тринадцать", " четырнадцать", " пятнадцать", " шестнадцать", " семнадцать", " восемнадцать", " девятнадцать" };
            string[] Dek2 = { "", "", " двадцать", " тридцать", " сорок", " пятьдесят", " шестьдесят", " семьдесят", " восемьдесят", " девяносто" };
            string[] Dek3 = { "", " сто", " двести", " триста", " четыреста", " пятьсот", " шестьсот", " семьсот", " восемьсот", " девятьсот" };
            string[] Th = { "", "", " тысяч", " миллион", " миллиард", " триллион", " квадрилион", " квинтилион" };
            string str = "";
            for (byte th = 1; Value > 0; th++)
            {
                ushort gr = (ushort)(Value % 1000);
                Value = (Value - gr) / 1000;
                if (gr > 0)
                {
                    byte d3 = (byte)((gr - gr % 100) / 100);
                    byte d1 = (byte)(gr % 10);
                    byte d2 = (byte)((gr - d3 * 100 - d1) / 10);
                    if (d2 == 1) d1 += (byte)10;
                    bool ismale = (th > 2) || ((th == 1) && IsMale);
                    str = Dek3[d3] + Dek2[d2] + Dek1[d1] + EndDek1(d1, ismale) + Th[th] + EndTh(th, d1) + str;
                };
            };
            if (_isCapitalize)
                str = str.Substring(1, 1).ToUpper() + str.Substring(2);
            return str;
        }


        private static string CurPhrase(decimal money, string word1, string word234, string wordmore, string sword1, string sword234, string swordmore)
        {
            money = decimal.Round(money, 2);
            decimal decintpart = decimal.Truncate(money);
            ulong intpart = decimal.ToUInt64(decintpart);
            string str = NumPhrase(intpart, true, true) + " ";
            byte endpart = (byte)(intpart % 100UL);
            if (endpart > 19) endpart = (byte)(endpart % 10);
            switch (endpart)
            {
                case 1: str += word1; break;
                case 2:
                case 3:
                case 4: str += word234; break;
                default: str += wordmore; break;
            }
            byte fracpart = decimal.ToByte((money - decintpart) * 100M);
            bool isFrPartMale = true;
            bool isFrPartExist = true;
            if (word1 == "руб.")
                isFrPartMale = false;
            //if (word1 == "руб." && fracpart == 0) 
            if (fracpart == 0) 
            {
                str += "";
                isFrPartExist = false;
            }
            else 
            { 
                //str += " " + ((fracpart < 10) ? "0" : "") + NumPhrase(fracpart, isFrPartMale, false) + " "; 
                str += " " + NumPhrase(fracpart, isFrPartMale, false) + " "; 
            }
            //str+="";//+((fracpart<10)?"0":"")+fracpart.ToString()+" ";
            if (fracpart > 19) fracpart = (byte)(fracpart % 10);

            //if (word1 == "руб.")
            //{
            //    switch (fracpart)
            //    {
            //        case 1: str += ""; break;
            //        case 2:
            //        case 3:
            //        case 4: str += ""; break;
            //        default: str += ""; break;
            //    };
            //}
            //else
            //{
                switch (fracpart)
                {
                    case 0:
                        str += (isFrPartExist ? swordmore : "");
                        break;
                    case 1: str += sword1; break;
                    case 2:
                    case 3:
                    case 4: str += sword234; break;
                    default: str += swordmore; break;
                };
            //}


            return str;
        }

        private static string EndTh(byte ThNum, byte Dek)
        {
            bool In234 = ((Dek >= 2) && (Dek <= 4));
            bool More4 = ((Dek > 4) || (Dek == 0));
            if (((ThNum > 2) && In234) || ((ThNum == 2) && (Dek == 1))) return "а";
            else if ((ThNum > 2) && More4) return "ов";
            else if ((ThNum == 2) && In234) return "и";
            else return "";
        }

        private static string EndDek1(byte Dek, bool IsMale)
        {
            if ((Dek > 2) || (Dek == 0)) return "";
            else if (Dek == 1)
            {
                if (IsMale) return "ин";
                else return "на";
            }
            else
            {
                if (IsMale) return "а";
                else return "е";
            }
        }

        public static string SumInVal(decimal sum, string kodval)
        {
            string rval = "";
            switch (kodval)
            {
                case "RR":
                    rval = RurPhrase(sum);
                    break;
                case "EU":
                    rval = EuroPhrase(sum);
                    break;
                case "US":
                    rval = UsdPhrase(sum);
                    break;
                default:
                    rval = RBPhrase(sum);
                    break;
            }
            return rval;
        }

    }
}